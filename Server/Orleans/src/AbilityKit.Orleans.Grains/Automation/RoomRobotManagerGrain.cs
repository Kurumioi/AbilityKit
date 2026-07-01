using AbilityKit.Orleans.Contracts.Automation;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

namespace AbilityKit.Orleans.Grains.Automation;

public sealed class RoomRobotManagerGrain : Grain, IRoomRobotManagerGrain
{
    private const int MaxRobotsPerRequest = 64;
    private readonly List<string> _robotAccounts = new();

    public async Task<AddRoomRobotsResponse> AddRobotsAsync(AddRoomRobotsRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RoomId))
        {
            throw new ArgumentException("RoomId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RequesterAccountId))
        {
            throw new ArgumentException("RequesterAccountId is required.", nameof(request));
        }

        var count = Math.Clamp(request.Count <= 0 ? 1 : request.Count, 1, MaxRobotsPerRequest);
        var room = GrainFactory.GetGrain<IRoomGrain>(request.RoomId);
        var added = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var accountId = CreateRobotAccount(request, _robotAccounts.Count + 1);
            await room.JoinMemberAsync(new JoinRoomMemberRequest(accountId, IsBot: true));
            if (request.AutoReady)
            {
                await TrySetReadyAsync(room, accountId);
            }

            _robotAccounts.Add(accountId);
            added.Add(accountId);
        }

        RoomRobotBattleAiMount[] mounts = [];
        if (request.MountBattleAi)
        {
            var mount = await MountBattleAiAsync(new MountRoomRobotBattleAiRequest(request.RoomId, request.BattleAiProfileId));
            mounts = mount.BattleAiMounts;
        }

        var snapshot = await room.GetSnapshotAsync();
        return new AddRoomRobotsResponse(
            request.RoomId,
            count,
            added.Count,
            added.ToArray(),
            mounts,
            snapshot,
            DateTime.UtcNow.Ticks);
    }

    public async Task<MountRoomRobotBattleAiResponse> MountBattleAiAsync(MountRoomRobotBattleAiRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RoomId))
        {
            throw new ArgumentException("RoomId is required.", nameof(request));
        }

        var room = GrainFactory.GetGrain<IRoomGrain>(request.RoomId);
        var runtime = await room.GetRuntimeStateAsync();
        if (string.IsNullOrWhiteSpace(runtime.BattleId) || runtime.WorldId == 0UL || _robotAccounts.Count == 0)
        {
            return new MountRoomRobotBattleAiResponse(
                request.RoomId,
                runtime.BattleId ?? string.Empty,
                runtime.WorldId,
                [],
                DateTime.UtcNow.Ticks);
        }

        var snapshot = await room.GetSnapshotAsync();
        var battle = GrainFactory.GetGrain<IBattleLogicHostGrain>(runtime.BattleId);
        var mounts = new List<RoomRobotBattleAiMount>(_robotAccounts.Count);
        var profileId = string.IsNullOrWhiteSpace(request.BattleAiProfileId) ? "simple-battle" : request.BattleAiProfileId;

        foreach (var accountId in _robotAccounts)
        {
            var playerId = ResolvePlayerId(snapshot, accountId);
            if (playerId == 0)
            {
                mounts.Add(new RoomRobotBattleAiMount(accountId, 0, false, "PlayerMissing", "Robot account is not present in room player snapshots."));
                continue;
            }

            var result = await battle.MountBotAiAsync(new BattleBotAiMountRequest(runtime.WorldId, playerId, profileId));
            mounts.Add(new RoomRobotBattleAiMount(accountId, result.PlayerId, result.Accepted, result.Status, result.Message));
        }

        return new MountRoomRobotBattleAiResponse(
            request.RoomId,
            runtime.BattleId,
            runtime.WorldId,
            mounts.ToArray(),
            DateTime.UtcNow.Ticks);
    }

    public Task<RoomRobotManagerState> GetStateAsync()
    {
        return Task.FromResult(new RoomRobotManagerState(
            this.GetPrimaryKeyString(),
            _robotAccounts.ToArray(),
            DateTime.UtcNow.Ticks));
    }

    private static async Task TrySetReadyAsync(IRoomGrain room, string accountId)
    {
        try
        {
            await room.SetReadyAsync(new RoomReadyRequest(accountId, Ready: true));
        }
        catch (InvalidOperationException)
        {
            // Late-joining a running battle has no lobby ready phase.
        }
    }

    private string CreateRobotAccount(AddRoomRobotsRequest request, int index)
    {
        var prefix = string.IsNullOrWhiteSpace(request.AccountPrefix)
            ? "room-robot"
            : request.AccountPrefix.Trim();
        return $"{prefix}:{this.GetPrimaryKeyString()}:{index}";
    }

    private static uint ResolvePlayerId(RoomSnapshot snapshot, string accountId)
    {
        for (var i = 0; i < snapshot.Players.Count; i++)
        {
            if (string.Equals(snapshot.Players[i].AccountId, accountId, StringComparison.Ordinal))
            {
                return (uint)(i + 1);
            }
        }

        return 0;
    }
}
