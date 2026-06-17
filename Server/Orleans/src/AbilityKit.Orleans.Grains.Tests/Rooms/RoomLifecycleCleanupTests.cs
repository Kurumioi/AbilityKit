using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Rooms;

public sealed class RoomLifecycleCleanupTests
{
    [Fact]
    public async Task ClearAccountRoomAsync_WhenRoomMatches_RemovesAccountMapping()
    {
        var mapping = new RoomIdMappingGrain();
        await mapping.BindAccountRoomAsync("account-a", "room-a");

        await mapping.ClearAccountRoomAsync("account-a", "room-a");

        Assert.Null(await mapping.TryGetAccountRoomAsync("account-a"));
    }

    [Fact]
    public async Task ClearAccountRoomAsync_WhenRoomDoesNotMatch_KeepsCurrentMapping()
    {
        var mapping = new RoomIdMappingGrain();
        await mapping.BindAccountRoomAsync("account-a", "room-new");

        await mapping.ClearAccountRoomAsync("account-a", "room-old");

        Assert.Equal("room-new", await mapping.TryGetAccountRoomAsync("account-a"));
    }

    [Fact]
    public void CollectExpiredOfflineMembersForTests_WhenTimeoutElapsed_ReturnsOnlyExpiredOfflineMembers()
    {
        var room = new RoomGrain();
        var summary = new RoomSummary(
            Region: "local",
            ServerId: "server-a",
            RoomId: "room-a",
            RoomType: "shooter",
            Title: "Shooter Room",
            IsPublic: true,
            MaxPlayers: 2,
            PlayerCount: 2,
            OwnerAccountId: "account-a",
            CreatedAtUnixMs: 0,
            Tags: new Dictionary<string, string> { ["offlineTimeoutSeconds"] = "5" });
        SetMemberStates(room, new Dictionary<string, RoomMemberState>
        {
            ["expired"] = new RoomMemberState(false, TimeSpan.FromSeconds(1).Ticks, TimeSpan.FromSeconds(1).Ticks),
            ["recent"] = new RoomMemberState(false, TimeSpan.FromSeconds(8).Ticks, TimeSpan.FromSeconds(8).Ticks),
            ["online"] = new RoomMemberState(true, TimeSpan.FromSeconds(1).Ticks, 0)
        });

        var expired = room.CollectExpiredOfflineMembersForTests(summary, TimeSpan.FromSeconds(10).Ticks);

        Assert.Equal(new[] { "expired" }, expired);
    }

    private static void SetMemberStates(RoomGrain room, Dictionary<string, RoomMemberState> states)
    {
        var field = typeof(RoomGrain).GetField("_memberStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var target = Assert.IsType<Dictionary<string, RoomMemberState>>(field!.GetValue(room));
        foreach (var state in states)
        {
            target[state.Key] = state.Value;
        }
    }
}
