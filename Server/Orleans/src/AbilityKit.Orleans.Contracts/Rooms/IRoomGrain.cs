using Orleans;

namespace AbilityKit.Orleans.Contracts.Rooms;

public interface IRoomGrain : IGrainWithStringKey
{
    Task InitializeAsync(RoomSummary summary, string directoryKey);

    Task<RoomSnapshot> GetSnapshotAsync();

    Task JoinAsync(string accountId);

    Task LeaveAsync(string accountId);

    Task SetReadyAsync(RoomReadyRequest request);

    Task PickHeroAsync(RoomPickHeroRequest request);

    Task<StartRoomBattleResponse> StartBattleAsync(StartRoomBattleRequest request);

    Task CloseAsync(string accountId);
}
