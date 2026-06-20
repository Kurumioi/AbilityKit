using System.Collections.Generic;
using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Persistence;
using AbilityKit.Orleans.Grains.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Rooms;

public sealed class RoomLifecycleCleanupTests
{
    [Fact]
    public async Task ClearAccountRoomAsync_WhenRoomMatches_RemovesAccountMapping()
    {
        var mapping = new RoomIdMappingGrain(new InMemoryRoomStateStore());
        await mapping.BindAccountRoomAsync("account-a", "room-a");

        await mapping.ClearAccountRoomAsync("account-a", "room-a");

        Assert.Null(await mapping.TryGetAccountRoomAsync("account-a"));
    }

    [Fact]
    public async Task ClearAccountRoomAsync_WhenRoomDoesNotMatch_KeepsCurrentMapping()
    {
        var mapping = new RoomIdMappingGrain(new InMemoryRoomStateStore());
        await mapping.BindAccountRoomAsync("account-a", "room-new");

        await mapping.ClearAccountRoomAsync("account-a", "room-old");

        Assert.Equal("room-new", await mapping.TryGetAccountRoomAsync("account-a"));
    }

    [Fact]
    public void CollectExpiredOfflineMembersForTests_WhenTimeoutElapsed_ReturnsOnlyExpiredOfflineMembers()
    {
        var tracker = new RoomMemberTracker();
        var summary = new RoomSummary(
            Region: "local",
            ServerId: "server-a",
            RoomId: "room-a",
            RoomType: ShooterGameplay.RoomType,
            Title: "Shooter Room",
            IsPublic: true,
            MaxPlayers: 2,
            PlayerCount: 2,
            OwnerAccountId: "account-a",
            CreatedAtUnixMs: 0,
            Tags: new Dictionary<string, string> { ["offlineTimeoutSeconds"] = "5" });

        tracker.SetMemberStateForTests("expired", new RoomMemberState(false, TimeSpan.FromSeconds(1).Ticks, TimeSpan.FromSeconds(1).Ticks));
        tracker.SetMemberStateForTests("recent", new RoomMemberState(false, TimeSpan.FromSeconds(8).Ticks, TimeSpan.FromSeconds(8).Ticks));
        tracker.SetMemberStateForTests("online", new RoomMemberState(true, TimeSpan.FromSeconds(1).Ticks, 0));

        var expired = tracker.CollectExpiredOfflineMembers(summary, TimeSpan.FromSeconds(10).Ticks);

        Assert.Equal(new[] { "expired" }, expired);
    }
}
