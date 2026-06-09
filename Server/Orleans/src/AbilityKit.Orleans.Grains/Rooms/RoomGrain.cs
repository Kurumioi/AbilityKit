using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;
using Orleans;

namespace AbilityKit.Orleans.Grains.Rooms;

public sealed class RoomGrain : Grain, IRoomGrain
{
    private static readonly RoomGameplayRegistry GameplayRegistry = new();

    private RoomSummary? _summary;
    private string? _directoryKey;
    private IRoomGameplayAdapter? _gameplay;
    private object? _gameplayState;
    private readonly HashSet<string> _members = new(StringComparer.Ordinal);
    private bool _closed;
    private string? _battleId;
    private ulong _worldId;
    private WorldStartAnchor? _worldStartAnchor;

    public Task InitializeAsync(RoomSummary summary, string directoryKey)
    {
        if (_summary is not null)
        {
            return Task.CompletedTask;
        }

        _summary = summary;
        _directoryKey = directoryKey;
        _gameplay = GameplayRegistry.Resolve(summary.RoomType);
        _gameplayState = _gameplay.CreateState(summary);
        return Task.CompletedTask;
    }

    public Task<RoomSnapshot> GetSnapshotAsync()
    {
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        return Task.FromResult(new RoomSnapshot(
            summary with { PlayerCount = _members.Count },
            _members.ToList(),
            gameplay.BuildPlayerSnapshots(gameplayState),
            gameplay.CanStart(gameplayState),
            _battleId,
            _worldStartAnchor));
    }

    public async Task JoinAsync(string accountId)
    {
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureOpen();
        EnsureAccountId(accountId);

        if (_members.Contains(accountId))
        {
            return;
        }

        if (summary.MaxPlayers > 0 && _members.Count >= summary.MaxPlayers)
        {
            throw new InvalidOperationException("Room is full.");
        }

        _members.Add(accountId);
        gameplay.Join(gameplayState, summary, _members, accountId);
        await NotifyRoomChangedAsync();
    }

    public async Task LeaveAsync(string accountId)
    {
        RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureAccountId(accountId);

        if (!_members.Remove(accountId))
        {
            return;
        }

        gameplay.Leave(gameplayState, accountId);
        await NotifyRoomChangedAsync();

        if (_members.Count == 0)
        {
            await RemoveFromDirectoryAsync();
            DeactivateOnIdle();
        }
    }

    public Task SetReadyAsync(RoomReadyRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureOpen();
        EnsureMember(request.AccountId);

        gameplay.SetReady(gameplayState, request);
        return Task.CompletedTask;
    }

    public Task PickHeroAsync(RoomPickHeroRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureOpen();
        EnsureMember(request.AccountId);

        gameplay.PickHero(gameplayState, request);
        return Task.CompletedTask;
    }

    public async Task<StartRoomBattleResponse> StartBattleAsync(StartRoomBattleRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureOwner(request.AccountId, summary);

        if (!string.IsNullOrEmpty(_battleId))
        {
            return new StartRoomBattleResponse(_battleId, _worldId, true, _worldStartAnchor, DateTime.UtcNow.Ticks);
        }

        EnsureOpen();

        _battleId = summary.RoomId;
        var initParams = gameplay.BuildBattleInitParams(gameplayState, summary, request);
        _worldId = initParams.WorldId;

        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(_battleId);
        await battleGrain.InitializeBattleAsync(initParams);
        _worldStartAnchor = await battleGrain.GetWorldStartAnchorAsync();
        _closed = true;
        await NotifyRoomChangedAsync();

        return new StartRoomBattleResponse(_battleId, _worldId, true, _worldStartAnchor, DateTime.UtcNow.Ticks);
    }

    public async Task CloseAsync(string accountId)
    {
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        EnsureOwner(accountId, summary);

        if (_closed && string.IsNullOrEmpty(_battleId))
        {
            return;
        }

        _closed = true;
        _members.Clear();
        _gameplayState = gameplay.CreateState(summary);

        await NotifyRoomChangedAsync();
        await RemoveFromDirectoryAsync();
        DeactivateOnIdle();
    }

    private RoomSummary RequireSummary()
    {
        if (_summary is null)
        {
            throw new InvalidOperationException("Room not initialized.");
        }

        return _summary;
    }

    private IRoomGameplayAdapter RequireGameplay()
    {
        if (_gameplay is null)
        {
            throw new InvalidOperationException("Room gameplay adapter not initialized.");
        }

        return _gameplay;
    }

    private object RequireGameplayState()
    {
        if (_gameplayState is null)
        {
            throw new InvalidOperationException("Room gameplay state not initialized.");
        }

        return _gameplayState;
    }

    private void EnsureOpen()
    {
        if (_closed)
        {
            throw new InvalidOperationException("Room is closed.");
        }
    }

    private void EnsureMember(string accountId)
    {
        EnsureAccountId(accountId);
        if (!_members.Contains(accountId))
        {
            throw new InvalidOperationException("Account is not in room.");
        }
    }

    private static void EnsureOwner(string accountId, RoomSummary summary)
    {
        EnsureAccountId(accountId);
        if (!string.Equals(accountId, summary.OwnerAccountId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only owner can operate the room.");
        }
    }

    private static void EnsureAccountId(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("accountId is required", nameof(accountId));
        }
    }

    private async Task NotifyRoomChangedAsync()
    {
        var summary = RequireSummary();
        if (_directoryKey is null)
        {
            throw new InvalidOperationException("Room directory not initialized.");
        }

        var directory = GrainFactory.GetGrain<IRoomDirectoryGrain>(_directoryKey);
        await directory.NotifyRoomChangedAsync(summary.RoomId, _members.Count);
    }

    private async Task RemoveFromDirectoryAsync()
    {
        var summary = RequireSummary();
        if (_directoryKey is null)
        {
            throw new InvalidOperationException("Room directory not initialized.");
        }

        var directory = GrainFactory.GetGrain<IRoomDirectoryGrain>(_directoryKey);
        await directory.RemoveRoomAsync(summary.RoomId);
    }
}
