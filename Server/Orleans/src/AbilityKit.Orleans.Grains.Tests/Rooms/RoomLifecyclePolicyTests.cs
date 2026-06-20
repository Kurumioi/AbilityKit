using AbilityKit.Orleans.Grains.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Rooms;

public sealed class RoomLifecyclePolicyTests
{
    [Fact]
    public void Evaluate_WhenRoomInLobby_AllowsLobbyActionsAndJoin()
    {
        var snapshot = RoomLifecyclePolicy.Evaluate(closed: false, battleId: null, memberCount: 1);

        Assert.Equal(RoomLifecycleState.Lobby, snapshot.State);
        Assert.True(snapshot.IsOpenForLobbyActions);
        Assert.True(snapshot.IsJoinable);
        Assert.False(snapshot.HasBattle);
        Assert.False(snapshot.ShouldRemoveFromDirectory);
    }

    [Fact]
    public void Evaluate_WhenRoomInBattle_AllowsJoinButBlocksLobbyActions()
    {
        var snapshot = RoomLifecyclePolicy.Evaluate(closed: true, battleId: "battle-a", memberCount: 2);

        Assert.Equal(RoomLifecycleState.InBattle, snapshot.State);
        Assert.False(snapshot.IsOpenForLobbyActions);
        Assert.True(snapshot.IsJoinable);
        Assert.True(snapshot.HasBattle);
        Assert.False(snapshot.ShouldRemoveFromDirectory);
    }

    [Fact]
    public void Evaluate_WhenClosedWithoutBattle_RemovesFromDirectory()
    {
        var snapshot = RoomLifecyclePolicy.Evaluate(closed: true, battleId: null, memberCount: 0);

        Assert.Equal(RoomLifecycleState.Closed, snapshot.State);
        Assert.False(snapshot.IsOpenForLobbyActions);
        Assert.False(snapshot.IsJoinable);
        Assert.True(snapshot.ShouldRemoveFromDirectory);
    }
}
