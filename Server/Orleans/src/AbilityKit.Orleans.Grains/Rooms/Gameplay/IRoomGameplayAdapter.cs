using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;

namespace AbilityKit.Orleans.Grains.Rooms.Gameplay;

internal interface IRoomGameplayAdapter
{
    string RoomType { get; }

    object CreateState(RoomSummary summary);

    void Join(object state, RoomSummary summary, HashSet<string> members, string accountId);

    void Leave(object state, string accountId);

    void SetReady(object state, RoomReadyRequest request);

    void PickHero(object state, RoomPickHeroRequest request);

    bool CanStart(object state);

    List<RoomPlayerSnapshot> BuildPlayerSnapshots(object state);

    BattleInitParams BuildBattleInitParams(object state, RoomSummary summary, StartRoomBattleRequest request);
}
