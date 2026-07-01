using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Automation;

public interface IRoomRobotManagerGrain : IGrainWithStringKey
{
    Task<AddRoomRobotsResponse> AddRobotsAsync(AddRoomRobotsRequest request);

    Task<MountRoomRobotBattleAiResponse> MountBattleAiAsync(MountRoomRobotBattleAiRequest request);

    Task<RoomRobotManagerState> GetStateAsync();
}

[GenerateSerializer]
public sealed record AddRoomRobotsRequest(
    [property: Id(0)] string RoomId,
    [property: Id(1)] string RequesterAccountId,
    [property: Id(2)] int Count,
    [property: Id(3)] string? AccountPrefix = null,
    [property: Id(4)] bool AutoReady = true,
    [property: Id(5)] bool MountBattleAi = true,
    [property: Id(6)] string? BattleAiProfileId = "simple-battle");

[GenerateSerializer]
public sealed record AddRoomRobotsResponse(
    [property: Id(0)] string RoomId,
    [property: Id(1)] int RequestedCount,
    [property: Id(2)] int AddedCount,
    [property: Id(3)] string[] RobotAccounts,
    [property: Id(4)] RoomRobotBattleAiMount[] BattleAiMounts,
    [property: Id(5)] RoomSnapshot Snapshot,
    [property: Id(6)] long ServerNowTicks);

[GenerateSerializer]
public sealed record RoomRobotBattleAiMount(
    [property: Id(0)] string AccountId,
    [property: Id(1)] uint PlayerId,
    [property: Id(2)] bool Accepted,
    [property: Id(3)] string Status,
    [property: Id(4)] string Message);

[GenerateSerializer]
public sealed record MountRoomRobotBattleAiRequest(
    [property: Id(0)] string RoomId,
    [property: Id(1)] string? BattleAiProfileId = "simple-battle");

[GenerateSerializer]
public sealed record MountRoomRobotBattleAiResponse(
    [property: Id(0)] string RoomId,
    [property: Id(1)] string BattleId,
    [property: Id(2)] ulong WorldId,
    [property: Id(3)] RoomRobotBattleAiMount[] BattleAiMounts,
    [property: Id(4)] long ServerNowTicks);

[GenerateSerializer]
public sealed record RoomRobotManagerState(
    [property: Id(0)] string RoomId,
    [property: Id(1)] string[] RobotAccounts,
    [property: Id(2)] long ServerNowTicks);
