using System;
using System.Collections.Generic;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Rooms;

public sealed class RoomMemberTrackerTests
{
    [Fact]
    public void MembersSnapshot_WhenMembersAdded_ReturnsCurrentMembers()
    {
        var tracker = new RoomMemberTracker();

        tracker.Add("account-a");
        tracker.Add("account-b");

        var members = tracker.MembersSnapshot();

        Assert.Equal(2, members.Count);
        Assert.Contains("account-a", members);
        Assert.Contains("account-b", members);
    }

    [Fact]
    public void CollectExpiredOfflineMembers_WhenOfflineTimeoutElapsed_ReturnsExpiredAccounts()
    {
        var tracker = new RoomMemberTracker();
        tracker.Add("expired");
        tracker.Add("recent");
        tracker.Add("online");

        var summary = new RoomSummary(
            Region: "local",
            ServerId: "server-a",
            RoomId: "room-a",
            RoomType: "any",
            Title: "Room",
            IsPublic: true,
            MaxPlayers: 4,
            PlayerCount: 3,
            OwnerAccountId: "owner",
            CreatedAtUnixMs: 0,
            Tags: new Dictionary<string, string> { ["offlineTimeoutSeconds"] = "5" });

        tracker.SetMemberStateForTests("expired", new RoomMemberState(false, TimeSpan.FromSeconds(1).Ticks, TimeSpan.FromSeconds(1).Ticks));
        tracker.SetMemberStateForTests("recent", new RoomMemberState(false, TimeSpan.FromSeconds(8).Ticks, TimeSpan.FromSeconds(8).Ticks));
        tracker.SetMemberStateForTests("online", new RoomMemberState(true, TimeSpan.FromSeconds(1).Ticks, 0));

        var expired = tracker.CollectExpiredOfflineMembers(summary, TimeSpan.FromSeconds(10).Ticks);

        Assert.Equal(new[] { "expired" }, expired);
    }

    [Fact]
    public void RemoveMembersAndStates_WhenRemovingAccounts_ClearsBothCollections()
    {
        var tracker = new RoomMemberTracker();
        tracker.Add("account-a");
        tracker.Add("account-b");
        tracker.SetMemberStateForTests("account-a", new RoomMemberState(true, 1, 0));
        tracker.SetMemberStateForTests("account-b", new RoomMemberState(false, 2, 2));

        tracker.RemoveMembersAndStates(new[] { "account-a" });

        Assert.False(tracker.Contains("account-a"));
        Assert.True(tracker.Contains("account-b"));
        var states = tracker.CloneMemberStates();
        Assert.NotNull(states);
        Assert.False(states!.ContainsKey("account-a"));
        Assert.True(states.ContainsKey("account-b"));
    }
}
