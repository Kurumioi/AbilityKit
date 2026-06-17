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
    private const string OfflineTimeoutTagKey = "offlineTimeoutSeconds";

    private RoomSummary? _summary;
    private string? _directoryKey;
    private IRoomGameplayAdapter? _gameplay;
    private object? _gameplayState;
    private readonly HashSet<string> _members = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RoomMemberState> _memberStates = new(StringComparer.Ordinal);
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
            _worldStartAnchor,
            _worldId,
            CloneMemberStates()));
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
            _members.ToList(),
            CloneMemberStates(),
            DateTime.UtcNow.Ticks,
            summary.Tags == null ? null : new Dictionary<string, string>(summary.Tags)));
    }

    public async Task<JoinRoomResponse> JoinAsync(string accountId)
    {
        var summary = RequireSummary();
        var gameplay = RequireGameplay();
        var gameplayState = RequireGameplayState();
        EnsureAccountId(accountId);

        var alreadyMember = _members.Contains(accountId);
        if (_closed && !string.IsNullOrEmpty(_battleId))
        {
            if (!alreadyMember)
            {
                if (summary.MaxPlayers > 0 && _members.Count >= summary.MaxPlayers)
                {
                    throw new InvalidOperationException("Room is full.");
                }

                _members.Add(accountId);
                TouchMember(accountId, isOnline: true);
                await NotifyRoomChangedAsync();
            }

            var runningKind = alreadyMember ? RoomJoinKind.Reconnect : RoomJoinKind.LateJoin;
            return new JoinRoomResponse(await GetSnapshotAsync(), runningKind, DateTime.UtcNow.Ticks);
        }

        EnsureOpen();
        if (alreadyMember)
        {
            TouchMember(accountId, isOnline: true);
            return new JoinRoomResponse(await GetSnapshotAsync(), RoomJoinKind.Reconnect, DateTime.UtcNow.Ticks);
        }

        if (summary.MaxPlayers > 0 && _members.Count >= summary.MaxPlayers)
        {
            throw new InvalidOperationException("Room is full.");
        }

        _members.Add(accountId);
        TouchMember(accountId, isOnline: true);
        gameplay.Join(gameplayState, summary, _members, accountId);
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

        TouchMember(accountId, isOnline: true);
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

        MarkOffline(accountId);
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

        MarkOffline(accountId);
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

        TouchMember(request.AccountId, isOnline: true);
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

        TouchMember(request.AccountId, isOnline: true);
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
        initParams.SyncOptions = RoomBattleSyncOptionsMapper.Resolve(summary, request);
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

        var removedMembers = _members.ToList();
        _closed = true;
        _members.Clear();
        _memberStates.Clear();
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

    private void TouchMember(string accountId, bool isOnline)
    {
        var now = DateTime.UtcNow.Ticks;
        _memberStates[accountId] = new RoomMemberState(
            isOnline,
            now,
            isOnline ? 0L : now);
    }

    private void MarkOffline(string accountId)
    {
        var now = DateTime.UtcNow.Ticks;
        _memberStates[accountId] = new RoomMemberState(false, now, now);
    }

    private Dictionary<string, RoomMemberState>? CloneMemberStates()
    {
        if (_memberStates.Count == 0)
        {
            return null;
        }

        return new Dictionary<string, RoomMemberState>(_memberStates, StringComparer.Ordinal);
    }

    private async Task CleanupExpiredOfflineMembersAsync(RoomSummary summary)
    {
        var offlineTimeoutSeconds = ReadIntTag(summary, OfflineTimeoutTagKey, 0);
        if (offlineTimeoutSeconds <= 0 || _memberStates.Count == 0)
        {
            return;
        }

        var expired = CollectExpiredOfflineMembers(summary, DateTime.UtcNow.Ticks);
        if (expired.Count == 0)
        {
            return;
        }

        foreach (var accountId in expired)
        {
            _memberStates.Remove(accountId);
            _members.Remove(accountId);
        }

        await ClearAccountRoomMappingsAsync(expired, summary.RoomId);
    }

    internal IReadOnlyList<string> CollectExpiredOfflineMembersForTests(RoomSummary summary, long nowTicks)
    {
        return CollectExpiredOfflineMembers(summary, nowTicks);
    }

    private List<string> CollectExpiredOfflineMembers(RoomSummary summary, long nowTicks)
    {
        var offlineTimeoutSeconds = ReadIntTag(summary, OfflineTimeoutTagKey, 0);
        if (offlineTimeoutSeconds <= 0 || _memberStates.Count == 0)
        {
            return new List<string>();
        }

        var timeoutTicks = TimeSpan.FromSeconds(offlineTimeoutSeconds).Ticks;
        var expired = new List<string>();

        foreach (var kv in _memberStates)
        {
            if (kv.Value.IsOnline || kv.Value.OfflineSinceTicks <= 0)
            {
                continue;
            }

            if (nowTicks - kv.Value.OfflineSinceTicks >= timeoutTicks)
            {
                expired.Add(kv.Key);
            }
        }

        return expired;
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

    private static int ReadIntTag(RoomSummary summary, string key, int fallback)
    {
        if (summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
