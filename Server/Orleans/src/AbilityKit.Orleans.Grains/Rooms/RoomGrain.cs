using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.FrameSync;
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
    private readonly RoomMemberTracker _members = new();
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
            _members.MembersSnapshot(),
            gameplay.BuildPlayerSnapshots(gameplayState),
            gameplay.CanStart(gameplayState),
            _battleId,
            _worldStartAnchor,
            _worldId,
            _members.CloneMemberStates()));
    }

    public Task<RoomRuntimeState> GetRuntimeStateAsync()
    {
        var summary = RequireSummary();
        return Task.FromResult(new RoomRuntimeState(
            summary.RoomId,
            summary.RoomType,
            _battleId,
            _worldId,
            _closed,
            !string.IsNullOrEmpty(_battleId),
            _members.MembersSnapshot(),
            _members.CloneMemberStates(),
            DateTime.UtcNow.Ticks,
            summary.Tags == null ? null : new Dictionary<string, string>(summary.Tags)));
    }

    public Task<JoinRoomResponse> JoinAsync(string accountId)
    {
        return JoinMemberAsync(new JoinRoomMemberRequest(accountId));
    }

    public async Task<JoinRoomResponse> JoinMemberAsync(JoinRoomMemberRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureAccountId(request.AccountId);

        var alreadyMember = _members.Contains(request.AccountId);
        var lifecycle = GetLifecycleSnapshot();
        if (lifecycle.State == RoomLifecycleState.InBattle)
        {
            if (!alreadyMember)
            {
                if (summary.MaxPlayers > 0 && _members.Count >= summary.MaxPlayers)
                {
                    throw new InvalidOperationException("Room is full.");
                }

                _members.Add(request.AccountId);
                _members.Touch(request.AccountId, isOnline: true, isBot: request.IsBot);
                gameplay.Join(gameplayState, summary, _members.MembersSnapshot(), request.AccountId);
                await JoinRunningBattleAsync(gameplay, gameplayState, summary, request);
                await NotifyRoomChangedAsync();
            }

            var runningKind = alreadyMember ? RoomJoinKind.Reconnect : RoomJoinKind.LateJoin;
            return new JoinRoomResponse(await GetSnapshotAsync(), runningKind, DateTime.UtcNow.Ticks);
        }

        EnsureOpen();
        if (alreadyMember)
        {
            _members.Touch(request.AccountId, isOnline: true, isBot: request.IsBot);
            return new JoinRoomResponse(await GetSnapshotAsync(), RoomJoinKind.Reconnect, DateTime.UtcNow.Ticks);
        }

        if (summary.MaxPlayers > 0 && _members.Count >= summary.MaxPlayers)
        {
            throw new InvalidOperationException("Room is full.");
        }

        _members.Add(request.AccountId);
        _members.Touch(request.AccountId, isOnline: true, isBot: request.IsBot);
        gameplay.Join(gameplayState, summary, _members.MembersSnapshot(), request.AccountId);
        await NotifyRoomChangedAsync();
        return new JoinRoomResponse(await GetSnapshotAsync(), RoomJoinKind.TeamLobby, DateTime.UtcNow.Ticks);
    }

    public async Task<RestoreRoomResponse> RestoreAsync(string accountId)
    {
        EnsureAccountId(accountId);
        if (!_members.Contains(accountId))
        {
            return RestoreRoomResponse.Failed(RoomRestoreStatus.NotMember, RoomRestoreErrorCode.AccountNotInRoom, "Account is not in room.");
        }

        _members.Touch(accountId, isOnline: true);
        await NotifyRoomChangedAsync();

        var snapshot = await GetSnapshotAsync();
        var joinKind = string.IsNullOrEmpty(_battleId) ? RoomJoinKind.TeamLobby : RoomJoinKind.Reconnect;
        return RestoreRoomResponse.Active(
            snapshot,
            joinKind,
            IsInBattle: !string.IsNullOrEmpty(_battleId),
            DateTime.UtcNow.Ticks);
    }

    public async Task MarkOfflineAsync(string accountId)
    {
        RequireSummary();
        EnsureMember(accountId);

        _members.MarkOffline(accountId);
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

        _members.MarkOffline(accountId);
        gameplay.Leave(gameplayState, accountId);
        await ClearAccountRoomMappingAsync(accountId, RequireSummary().RoomId);
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

        _members.Touch(request.AccountId, isOnline: true);
        gameplay.SetReady(gameplayState, request);
        return Task.CompletedTask;
    }

    public Task SubmitGameplayCommandAsync(RoomGameplayCommandRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureOpen();
        EnsureMember(request.AccountId);

        _members.Touch(request.AccountId, isOnline: true);
        gameplay.SubmitCommand(gameplayState, request);
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
        if (!gameplay.CanStart(gameplayState))
        {
            throw new InvalidOperationException("Room is not ready to start.");
        }

        _battleId = summary.RoomId;
        var initParams = gameplay.BuildBattleInitParams(gameplayState, summary, request);
        initParams.SyncOptions = RoomBattleSyncOptionsMapper.Resolve(summary, request);
        _worldId = initParams.WorldId;

        var startRoute = RoomFrameSyncRoute.ResolveStartRoute(summary, _battleId, initParams);
        if (startRoute.FrameSyncOptions is { } frameSyncOptions)
        {
            var frameSyncGrain = GrainFactory.GetGrain<IBattleFrameSyncGrain>(frameSyncOptions.RoomId.ToString());
            await frameSyncGrain.InitializeAsync(frameSyncOptions);
        }

        if (startRoute.RequiresBattleRuntime)
        {
            var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(_battleId);
            await battleGrain.InitializeBattleAsync(initParams);
            _worldStartAnchor = await battleGrain.GetWorldStartAnchorAsync();
        }
        else
        {
            _worldStartAnchor = CreateWorldStartAnchor(initParams.TickRate);
            initParams.WorldStartAnchor = _worldStartAnchor;
        }

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

        var removedMembers = _members.MembersSnapshot();
        _closed = true;
        _members.Clear();
        _gameplayState = gameplay.CreateState(summary);

        await ClearAccountRoomMappingsAsync(removedMembers, summary.RoomId);

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
        if (!GetLifecycleSnapshot().IsOpenForLobbyActions)
        {
            throw new InvalidOperationException("Room is closed.");
        }
    }

    private RoomLifecycleSnapshot GetLifecycleSnapshot()
    {
        return RoomLifecyclePolicy.Evaluate(_closed, _battleId, _members.Count);
    }

    private void EnsureMember(string accountId)
    {
        EnsureAccountId(accountId);
        if (!_members.Contains(accountId))
        {
            throw new InvalidOperationException("Account is not in room.");
        }
    }

    private static WorldStartAnchor CreateWorldStartAnchor(int tickRate)
    {
        var resolvedTickRate = tickRate > 0 ? tickRate : 30;
        return new WorldStartAnchor(
            DateTime.UtcNow.Ticks,
            TimeSpan.TicksPerSecond,
            0,
            1.0 / resolvedTickRate);
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

    private async Task JoinRunningBattleAsync(
        IRoomGameplayAdapter gameplay,
        object gameplayState,
        RoomSummary summary,
        JoinRoomMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(_battleId))
        {
            return;
        }

        var player = gameplay.BuildLateJoinPlayer(gameplayState, summary, request.AccountId);
        if (player is null)
        {
            return;
        }

        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(_battleId);
        var joinResult = await battleGrain.JoinPlayerAsync(new BattlePlayerJoinRequest(_worldId, player, request.IsBot));
        if (!joinResult.Accepted)
        {
            _members.RemoveMemberAndState(request.AccountId);
            gameplay.Leave(gameplayState, request.AccountId);
            throw new InvalidOperationException($"Battle late join rejected. Status={joinResult.Status}, Message={joinResult.Message}");
        }
    }

    private async Task NotifyRoomChangedAsync()
    {
        var summary = RequireSummary();
        if (_directoryKey is null)
        {
            throw new InvalidOperationException("Room directory not initialized.");
        }

        await CleanupExpiredOfflineMembersAsync(summary);

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

    private async Task CleanupExpiredOfflineMembersAsync(RoomSummary summary)
    {
        if (!_members.HasMemberStates)
        {
            return;
        }

        var expired = _members.CollectExpiredOfflineMembers(summary, DateTime.UtcNow.Ticks);
        if (expired.Count == 0)
        {
            return;
        }

        _members.RemoveMembersAndStates(expired);

        await ClearAccountRoomMappingsAsync(expired, summary.RoomId);
    }

    internal IReadOnlyList<string> CollectExpiredOfflineMembersForTests(RoomSummary summary, long nowTicks)
    {
        return _members.CollectExpiredOfflineMembers(summary, nowTicks);
    }

    private Task ClearAccountRoomMappingsAsync(IReadOnlyCollection<string> accountIds, string roomId)
    {
        if (accountIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        var mapping = GrainFactory.GetGrain<IRoomIdMappingGrain>("global");
        return Task.WhenAll(accountIds.Select(accountId => mapping.ClearAccountRoomAsync(accountId, roomId)));
    }

    private Task ClearAccountRoomMappingAsync(string accountId, string roomId)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        var mapping = GrainFactory.GetGrain<IRoomIdMappingGrain>("global");
        return mapping.ClearAccountRoomAsync(accountId, roomId);
    }

}
