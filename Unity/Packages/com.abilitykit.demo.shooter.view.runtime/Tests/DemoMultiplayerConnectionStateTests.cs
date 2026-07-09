#nullable enable

using AbilityKit.Demo.Common.Rooms;
using NUnit.Framework;

namespace AbilityKit.Demo.Shooter.View.Tests
{
    public sealed class DemoMultiplayerConnectionStateTests
    {
        [Test]
        public void AccountStateCreatesStableUniqueDefaultIdentities()
        {
            var state = new DemoMultiplayerAccountState("unity-account", "unity-guest", "reserved-token");
            var accountId = "unity-account";
            var guestId = "unity-guest";

            state.EnsureUniqueDefaultIdentity(ref accountId, ref guestId);

            Assert.That(accountId, Does.StartWith("unity-account-"));
            Assert.That(guestId, Does.StartWith("unity-guest-"));
            Assert.AreEqual(accountId.Substring("unity-account-".Length), guestId.Substring("unity-guest-".Length));
        }

        [Test]
        public void AccountStateTracksSessionOwnerAndRejectsReservedToken()
        {
            var state = new DemoMultiplayerAccountState("account", "guest", "reserved-token");

            state.RecordLogin("account-a");

            Assert.IsTrue(state.HasSessionToken("session-token", "account-a"));
            Assert.IsFalse(state.HasSessionToken("reserved-token", "account-a"));
            Assert.IsFalse(state.HasSessionToken("session-token", "account-b"));

            state.ClearSession();

            Assert.IsFalse(state.HasSessionToken("session-token", "account-a"));
        }

        [Test]
        public void RoomListStateReplacesRoomsAndMaintainsSelectionBounds()
        {
            var state = new DemoRoomListState<string>();

            state.ReplaceRooms(new[] { "room-a", "room-b", "room-c" }, 12);
            Assert.AreEqual(3, state.Count);
            Assert.AreEqual(12, state.NextOffset);
            Assert.IsTrue(state.TrySelect(2, out var selected));
            Assert.AreEqual("room-c", selected);
            Assert.AreEqual(2, state.SelectedIndex);

            state.ReplaceRooms(new[] { "room-a" }, -1);

            Assert.AreEqual(1, state.Count);
            Assert.AreEqual(0, state.NextOffset);
            Assert.AreEqual(0, state.SelectedIndex);
        }

        [Test]
        public void RoomListStateClearsSelectionForEmptyOrInvalidSelection()
        {
            var state = new DemoRoomListState<string>();

            state.ReplaceRooms(new[] { "room-a" }, 1);
            Assert.IsFalse(state.TrySelect(5, out _));
            Assert.AreEqual(-1, state.SelectedIndex);

            state.ReplaceRooms(System.Array.Empty<string>(), 0);

            Assert.AreEqual(0, state.Count);
            Assert.AreEqual(-1, state.SelectedIndex);
        }
    }
}
