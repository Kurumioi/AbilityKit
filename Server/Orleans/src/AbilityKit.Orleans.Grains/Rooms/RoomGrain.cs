using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

namespace AbilityKit.Orleans.Grains.Rooms;

public sealed class RoomGrain : Grain, IRoomGrain
{
    private static readonly DefaultMobaRoomGameStartSpecBuilder StartSpecBuilder = new();

    private RoomSummary? _summary;
    private string? _directoryKey;
    private MobaRoomState? _roomState;
    private readonly HashSet<string> _members = new(StringComparer.Ordinal);
    private bool _closed;
    private string? _battleId;
    private ulong _worldId;

    public Task InitializeAsync(RoomSummary summary, string directoryKey)
    {
        if (_summary is not null)
        {
            return Task.CompletedTask;
        }

        _summary = summary;
        _directoryKey = directoryKey;
        _roomState = CreateRoomState(summary);
        return Task.CompletedTask;
    }

    public Task<RoomSnapshot> GetSnapshotAsync()
    {
        var summary = RequireSummary();
        return Task.FromResult(new RoomSnapshot(
            summary with { PlayerCount = _members.Count },
            _members.ToList(),
            BuildPlayerSnapshots(),
            _roomState?.CanStart() == true,
            _battleId));
    }

    public async Task JoinAsync(string accountId)
    {
        var summary = RequireSummary();
        var roomState = RequireRoomState();
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
        roomState.TryJoin(new PlayerId(accountId), GuessTeamId(_members.Count));
        await NotifyRoomChangedAsync();
    }

    public async Task LeaveAsync(string accountId)
    {
        RequireSummary();
        var roomState = RequireRoomState();
        EnsureAccountId(accountId);

        if (!_members.Remove(accountId))
        {
            return;
        }

        roomState.TryLeave(new PlayerId(accountId));
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
        var roomState = RequireRoomState();
        EnsureOpen();
        EnsureMember(request.AccountId);

        roomState.TrySetReady(new PlayerId(request.AccountId), request.Ready);
        return Task.CompletedTask;
    }

    public Task PickHeroAsync(RoomPickHeroRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        RequireSummary();
        var roomState = RequireRoomState();
        EnsureOpen();
        EnsureMember(request.AccountId);

        var playerId = new PlayerId(request.AccountId);
        roomState.TrySetTeam(playerId, request.TeamId);
        roomState.TrySetSpawnPoint(playerId, request.SpawnPointId);
        roomState.TryPickHero(
            playerId,
            request.HeroId,
            request.AttributeTemplateId,
            request.Level > 0 ? request.Level : 1,
            request.BasicAttackSkillId,
            request.SkillIds?.ToArray());

        return Task.CompletedTask;
    }

    public async Task<StartRoomBattleResponse> StartBattleAsync(StartRoomBattleRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var summary = RequireSummary();
        var roomState = RequireRoomState();
        EnsureOwner(request.AccountId, summary);

        if (!string.IsNullOrEmpty(_battleId))
        {
            return new StartRoomBattleResponse(_battleId, _worldId, true);
        }

        EnsureOpen();

        if (!StartSpecBuilder.TryBuild(roomState, out var roomSpec))
        {
            throw new InvalidOperationException("Room is not ready to start battle.");
        }

        roomSpec = new MobaRoomGameStartSpec(
            roomSpec.MatchId,
            roomSpec.MapId,
            roomSpec.RandomSeed,
            roomSpec.TickRate,
            roomSpec.InputDelayFrames,
            roomSpec.Players,
            request.GameplayId);

        _battleId = summary.RoomId;
        var initParams = OrleansRoomBattleStartMapper.ToBattleInitParams(
            summary.RoomId,
            in roomSpec,
            request.RuleSetId,
            request.ConfigVersion,
            request.ProtocolVersion,
            request.WorldType,
            request.ClientId);
        _worldId = initParams.WorldId;

        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(_battleId);
        await battleGrain.InitializeBattleAsync(initParams);
        _closed = true;
        await NotifyRoomChangedAsync();

        return new StartRoomBattleResponse(_battleId, _worldId, true);
    }

    public async Task CloseAsync(string accountId)
    {
        var summary = RequireSummary();
        EnsureOwner(accountId, summary);

        if (_closed && string.IsNullOrEmpty(_battleId))
        {
            return;
        }

        _closed = true;
        _members.Clear();
        _roomState = CreateRoomState(summary);

        await NotifyRoomChangedAsync();
        await RemoveFromDirectoryAsync();
        DeactivateOnIdle();
    }

    private MobaRoomState CreateRoomState(RoomSummary summary)
    {
        var roomState = new MobaRoomState(
            summary.RoomId,
            ReadIntTag(summary, "mapId", 1),
            ReadIntTag(summary, "randomSeed", Environment.TickCount),
            ReadIntTag(summary, "tickRate", 30),
            ReadIntTag(summary, "inputDelayFrames", 0));
        roomState.Configure(ReadIntTag(summary, "minPlayers", 1), summary.MaxPlayers);
        return roomState;
    }

    private List<RoomPlayerSnapshot> BuildPlayerSnapshots()
    {
        var roomState = _roomState;
        if (roomState == null || roomState.Players.Count == 0)
        {
            return new List<RoomPlayerSnapshot>();
        }

        var players = new List<RoomPlayerSnapshot>(roomState.Players.Count);
        foreach (var kv in roomState.Players)
        {
            var slot = kv.Value;
            players.Add(new RoomPlayerSnapshot(
                kv.Key,
                slot.TeamId,
                slot.Ready,
                slot.HeroId,
                slot.SpawnPointId,
                slot.Level,
                slot.AttributeTemplateId,
                slot.BasicAttackSkillId,
                slot.SkillIds == null ? null : slot.SkillIds.ToList()));
        }

        return players;
    }

    private RoomSummary RequireSummary()
    {
        if (_summary is null)
        {
            throw new InvalidOperationException("Room not initialized.");
        }

        return _summary;
    }

    private MobaRoomState RequireRoomState()
    {
        if (_roomState is null)
        {
            throw new InvalidOperationException("Room state not initialized.");
        }

        return _roomState;
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

    private static int GuessTeamId(int memberCount)
    {
        return memberCount % 2 == 0 ? 2 : 1;
    }

    private static int ReadIntTag(RoomSummary summary, string key, int fallback)
    {
        if (summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
