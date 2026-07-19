using System.Collections.Generic;
using AbilityKit.Game.Battle.Agent;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class ClientRoomStoreTests
    {
        private static ClientRoomSnapshot NewSnapshot(long revision, long eventSequence, string roomId = "room-1")
        {
            return new ClientRoomSnapshot
            {
                RoomId = roomId,
                Phase = ClientRoomPhase.Lobby,
                RoomRevision = revision,
                LastEventSequence = eventSequence
            };
        }

        [Test]
        public void FirstApply_SucceedsAndPublishes()
        {
            var store = new ClientRoomStore();
            var published = new List<ClientRoomSnapshot>();
            store.OnSnapshotChanged += s => published.Add(s);

            var snapshot = NewSnapshot(1, 1);

            var result = store.ApplySnapshot(snapshot);

            Assert.AreEqual(ClientRoomSnapshotApplyResult.Applied, result);
            Assert.AreSame(snapshot, store.Current);
            Assert.AreEqual(1, published.Count);
            Assert.IsFalse(store.IsStale);
        }

        [Test]
        public void ApplyOldRevision_IsIgnored()
        {
            var store = new ClientRoomStore();
            store.ApplySnapshot(NewSnapshot(5, 5));

            var result = store.ApplySnapshot(NewSnapshot(3, 3));

            Assert.AreEqual(ClientRoomSnapshotApplyResult.StaleIgnored, result);
            Assert.AreEqual(5, store.Current.RoomRevision);
        }

        [Test]
        public void ApplyDuplicateRevision_IsIdempotent()
        {
            var store = new ClientRoomStore();
            var published = new List<ClientRoomSnapshot>();
            store.OnSnapshotChanged += s => published.Add(s);

            store.ApplySnapshot(NewSnapshot(5, 5));
            var result = store.ApplySnapshot(NewSnapshot(5, 5));

            Assert.AreEqual(ClientRoomSnapshotApplyResult.DuplicateIgnored, result);
            Assert.AreEqual(1, published.Count);
        }

        [Test]
        public void ApplyDuplicateRevision_WithNumericRoomId_PublishesMetadataCompletion()
        {
            var store = new ClientRoomStore();
            var published = new List<ClientRoomSnapshot>();
            store.OnSnapshotChanged += snapshot => published.Add(snapshot);
            store.ApplySnapshot(NewSnapshot(5, 5));
            var completed = NewSnapshot(5, 5);
            completed.NumericRoomId = 9001UL;

            var result = store.ApplySnapshot(completed);

            Assert.AreEqual(ClientRoomSnapshotApplyResult.Applied, result);
            Assert.AreEqual(9001UL, store.Current.NumericRoomId);
            Assert.AreEqual(2, published.Count);
        }

        [Test]
        public void ApplyNewRevision_InheritsNumericRoomIdFromSameRoom()
        {
            var store = new ClientRoomStore();
            var initial = NewSnapshot(5, 5);
            initial.NumericRoomId = 9001UL;
            store.ApplySnapshot(initial);

            var result = store.ApplySnapshot(NewSnapshot(6, 6));

            Assert.AreEqual(ClientRoomSnapshotApplyResult.Applied, result);
            Assert.AreEqual(6, store.Current.RoomRevision);
            Assert.AreEqual(9001UL, store.Current.NumericRoomId);
        }

        [Test]
        public void ApplyNewRevision_IsAccepted()
        {
            var store = new ClientRoomStore();
            store.ApplySnapshot(NewSnapshot(5, 5));

            var result = store.ApplySnapshot(NewSnapshot(6, 6));

            Assert.AreEqual(ClientRoomSnapshotApplyResult.Applied, result);
            Assert.AreEqual(6, store.Current.RoomRevision);
        }

        [Test]
        public void EventSequenceGap_MarksStale()
        {
            var store = new ClientRoomStore();
            store.ApplySnapshot(NewSnapshot(5, 5));

            // 期望 next = 6，实际 8 -> 缺口
            store.ApplySnapshot(NewSnapshot(6, 8));

            Assert.IsTrue(store.IsStale);
        }

        [Test]
        public void MarkRefreshed_ClearsStale()
        {
            var store = new ClientRoomStore();
            store.ApplySnapshot(NewSnapshot(5, 5));
            store.ApplySnapshot(NewSnapshot(6, 8));
            Assert.IsTrue(store.IsStale);

            store.MarkRefreshed();

            Assert.IsFalse(store.IsStale);
        }

        [Test]
        public void OnSnapshotChanged_FiresOnlyOnApplied()
        {
            var store = new ClientRoomStore();
            var published = new List<ClientRoomSnapshot>();
            store.OnSnapshotChanged += s => published.Add(s);

            store.ApplySnapshot(NewSnapshot(1, 1));
            store.ApplySnapshot(NewSnapshot(1, 1)); // duplicate
            store.ApplySnapshot(NewSnapshot(0, 0)); // stale
            store.ApplySnapshot(NewSnapshot(2, 2)); // applied

            Assert.AreEqual(2, published.Count);
            Assert.AreEqual(2, published[1].RoomRevision);
        }
    }
}
