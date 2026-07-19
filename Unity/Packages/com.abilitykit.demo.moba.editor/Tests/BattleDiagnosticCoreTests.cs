using System;
using NUnit.Framework;

namespace AbilityKit.Demo.Moba.Diagnostics.Tests
{
    public sealed class BattleDiagnosticCoreTests
    {
        [Test]
        public void Selection_BelongsTo_RequiresMatchingWorldEpoch()
        {
            var firstEpoch = new BattleDiagnosticSessionScope("session", "world", 1);
            var nextEpoch = new BattleDiagnosticSessionScope("session", "world", 2);
            var selection = Actor(firstEpoch, 1001, 25);

            Assert.That(selection.BelongsTo(firstEpoch), Is.True);
            Assert.That(selection.BelongsTo(nextEpoch), Is.False);
        }

        [Test]
        public void FrameCursor_UserSelection_DisablesFollowLive()
        {
            var cursor = BattleDiagnosticFrameCursor.CreateFollowingLive(100);

            cursor = cursor.SelectFrame(80);
            cursor = cursor.AdvanceLive(101);

            Assert.That(cursor.Frame, Is.EqualTo(80));
            Assert.That(cursor.FollowsLive, Is.False);
            Assert.That(cursor.ChangeReason, Is.EqualTo(BattleDiagnosticFrameCursorChangeReason.UserSelectedFrame));
        }

        [Test]
        public void FrameCursor_ConstrainToRetainedRange_MovesEvictedFrameToFirstAvailable()
        {
            var cursor = BattleDiagnosticFrameCursor.CreateFollowingLive(100).SelectFrame(20);

            cursor = cursor.ConstrainTo(new BattleDiagnosticFrameRange(40, 100));

            Assert.That(cursor.Frame, Is.EqualTo(40));
            Assert.That(cursor.FollowsLive, Is.False);
            Assert.That(cursor.ChangeReason, Is.EqualTo(BattleDiagnosticFrameCursorChangeReason.RetainedRangeClamped));
        }

        [Test]
        public void NavigationHistory_NewSelectionAfterBack_RemovesForwardBranch()
        {
            var scope = new BattleDiagnosticSessionScope("session", "world", 1);
            var history = new BattleDiagnosticNavigationHistory();
            var first = Actor(scope, 1, 10);
            var second = Actor(scope, 2, 20);
            var branch = Actor(scope, 3, 30);

            history.NavigateTo(first);
            history.NavigateTo(second);
            Assert.That(history.TryGoBack(out var back), Is.True);
            Assert.That(back, Is.EqualTo(first));

            history.NavigateTo(branch);

            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.Current, Is.EqualTo(branch));
            Assert.That(history.CanGoForward, Is.False);
        }

        [Test]
        public void NavigationHistory_DifferentScope_ClearsPreviousEntries()
        {
            var firstScope = new BattleDiagnosticSessionScope("session", "world", 1);
            var nextScope = new BattleDiagnosticSessionScope("session", "world", 2);
            var history = new BattleDiagnosticNavigationHistory();

            history.NavigateTo(Actor(firstScope, 1, 10));
            history.NavigateTo(Actor(nextScope, 1, 11));

            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history.Scope, Is.EqualTo(nextScope));
            Assert.That(history.Current.Scope, Is.EqualTo(nextScope));
            Assert.That(history.CanGoBack, Is.False);
        }

        [Test]
        public void NavigationHistory_ExceedingCapacity_DropsOldestEntry()
        {
            var scope = new BattleDiagnosticSessionScope("session", "world", 1);
            var history = new BattleDiagnosticNavigationHistory(2);

            history.NavigateTo(Actor(scope, 1, 10));
            history.NavigateTo(Actor(scope, 2, 20));
            history.NavigateTo(Actor(scope, 3, 30));

            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.TryGetEntry(0, out var firstRetained), Is.True);
            Assert.That(firstRetained.Id, Is.EqualTo(2));
            Assert.That(history.Current.Id, Is.EqualTo(3));
        }

        [Test]
        public void Workspace_RejectsSelectionFromDifferentScope()
        {
            var currentScope = new BattleDiagnosticSessionScope("session", "world", 1);
            var otherScope = new BattleDiagnosticSessionScope("session", "world", 2);
            var workspace = new BattleDiagnosticWorkspaceState();
            workspace.AttachSession(currentScope, 50);

            var changed = workspace.Select(Actor(otherScope, 7, 30));

            Assert.That(changed, Is.False);
            Assert.That(workspace.Selection.IsValid, Is.False);
            Assert.That(workspace.FrameCursor.Frame, Is.EqualTo(50));
        }

        [Test]
        public void Workspace_SelectingEvent_MovesCursorAndCreatesNavigationEntry()
        {
            var scope = new BattleDiagnosticSessionScope("session", "world", 1);
            var workspace = new BattleDiagnosticWorkspaceState();
            workspace.AttachSession(scope, 50);
            var selection = new BattleDiagnosticSelection(
                scope,
                BattleDiagnosticSelectionKind.Event,
                9001,
                35);

            var changed = workspace.Select(selection);

            Assert.That(changed, Is.True);
            Assert.That(workspace.Selection, Is.EqualTo(selection));
            Assert.That(workspace.FrameCursor.Frame, Is.EqualTo(35));
            Assert.That(workspace.FrameCursor.FollowsLive, Is.False);
            Assert.That(workspace.Navigation.Count, Is.EqualTo(1));
        }

        [Test]
        public void Filter_Default_IsUnboundedAndHasNoActiveDimensions()
        {
            var filter = BattleDiagnosticFilter.Default;

            Assert.That(filter.Frames.IsBounded, Is.False);
            Assert.That(filter.ActiveFilterCount, Is.EqualTo(0));
        }

        [Test]
        public void Filter_NormalizesSearchText_AndCountsActiveDimensions()
        {
            var filter = BattleDiagnosticFilter.Default
                .WithActor(42, BattleDiagnosticActorRelation.Either)
                .WithSearchText("  Fire Ball  ");

            Assert.That(filter.SearchText, Is.EqualTo("Fire Ball"));
            Assert.That(filter.ActiveFilterCount, Is.EqualTo(2));
        }

        [Test]
        public void QueryStatus_Partial_RejectsAmbiguousAvailability()
        {
            Assert.Throws<ArgumentException>(() =>
                BattleDiagnosticQueryStatus.Partial(
                    1,
                    2,
                    10,
                    BattleDiagnosticDataAvailability.Available));
        }

        [Test]
        public void PageRequest_NextPage_KeepsStoreRevision()
        {
            var first = new BattleDiagnosticPageRequest(123, 0, 100);

            var next = first.NextPage();

            Assert.That(next.StoreRevision, Is.EqualTo(123));
            Assert.That(next.Offset, Is.EqualTo(100));
            Assert.That(next.Limit, Is.EqualTo(100));
        }

        private static BattleDiagnosticSelection Actor(
            BattleDiagnosticSessionScope scope,
            long actorId,
            int frame)
        {
            return new BattleDiagnosticSelection(
                scope,
                BattleDiagnosticSelectionKind.Actor,
                actorId,
                frame);
        }
    }
}
