namespace AbilityKit.Orleans.Grains.Rooms;

internal enum RoomLifecycleState
{
    Lobby = 0,
    Starting = 1,
    InBattle = 2,
    Closing = 3,
    Closed = 4,
    Expired = 5
}

internal readonly record struct RoomLifecycleSnapshot(
    RoomLifecycleState State,
    bool IsOpenForLobbyActions,
    bool IsJoinable,
    bool HasBattle,
    bool ShouldRemoveFromDirectory);

internal static class RoomLifecyclePolicy
{
    public static RoomLifecycleSnapshot Evaluate(bool closed, string? battleId, int memberCount)
    {
        var hasBattle = !string.IsNullOrWhiteSpace(battleId);
        var state = (closed, hasBattle, memberCount) switch
        {
            (false, false, _) => RoomLifecycleState.Lobby,
            (false, true, _) => RoomLifecycleState.Starting,
            (true, true, _) => RoomLifecycleState.InBattle,
            (true, false, > 0) => RoomLifecycleState.Closing,
            (true, false, 0) => RoomLifecycleState.Closed
        };

        return new RoomLifecycleSnapshot(
            state,
            IsOpenForLobbyActions: state == RoomLifecycleState.Lobby,
            IsJoinable: state is RoomLifecycleState.Lobby or RoomLifecycleState.InBattle,
            HasBattle: hasBattle,
            ShouldRemoveFromDirectory: state is RoomLifecycleState.Closing or RoomLifecycleState.Closed or RoomLifecycleState.Expired);
    }
}
