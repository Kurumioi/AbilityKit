using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Game.Editor;
using NUnit.Framework;

namespace AbilityKit.Demo.Moba.Diagnostics.Tests
{
    public sealed class BattleDebugDiagnosticViewModelTests
    {
        [Test]
        public void EventsCacheKey_IncludesRevisionAndEveryFilterOrSelectionInput()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticEventsViewModel();

            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(1));

            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(1));

            session.EventStoreRevision++;
            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(2));

            viewModel.FilterBySelectedActor = false;
            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(3));

            viewModel.FailuresOnly = true;
            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(4));

            viewModel.SearchText = "damage";
            viewModel.RefreshIfNeeded(session, 10, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(5));

            viewModel.RefreshIfNeeded(session, 11, true);
            Assert.That(session.EventQueryCount, Is.EqualTo(6));

            viewModel.RefreshIfNeeded(session, 11, false);
            Assert.That(session.EventQueryCount, Is.EqualTo(7));
        }

        [Test]
        public void EventsCache_IgnoresStateRevision()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticEventsViewModel();
            viewModel.RefreshIfNeeded(session, 0, false);

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 0, false);

            Assert.That(session.EventQueryCount, Is.EqualTo(1));
        }

        [Test]
        public void StateCacheKey_IncludesStateRevisionAndFrameInput()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticStateViewModel();

            viewModel.RefreshIfNeeded(session);
            Assert.That(session.WorldQueryCount, Is.EqualTo(1));
            Assert.That(session.ActorQueryCount, Is.EqualTo(1));

            viewModel.RefreshIfNeeded(session);
            Assert.That(session.WorldQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session);
            Assert.That(session.WorldQueryCount, Is.EqualTo(2));

            viewModel.FrameInput = 5;
            viewModel.RefreshIfNeeded(session);
            Assert.That(session.WorldQueryCount, Is.EqualTo(3));
            Assert.That(session.LastWorldFrame, Is.EqualTo(5));
            Assert.That(session.LastActorFrame, Is.EqualTo(5));
        }

        [Test]
        public void StateCache_IgnoresEventRevisionAndCachesUnavailableResult()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticStateViewModel();
            viewModel.RefreshIfNeeded(session);

            session.EventStoreRevision++;
            viewModel.RefreshIfNeeded(session);

            Assert.That(session.WorldQueryCount, Is.EqualTo(1));
            Assert.That(session.ActorQueryCount, Is.EqualTo(1));
        }

        [Test]
        public void AttributeCacheKey_IncludesAttributeRevisionActorAndFrame()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticAttributesViewModel();

            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.AttributeQueryCount, Is.EqualTo(1));
            Assert.That(session.ModifierQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.AttributeQueryCount, Is.EqualTo(1));

            session.ActorAttributeStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 12);
            viewModel.RefreshIfNeeded(session, 12, 5);

            Assert.That(session.AttributeQueryCount, Is.EqualTo(4));
            Assert.That(session.ModifierQueryCount, Is.EqualTo(4));
            Assert.That(session.LastAttributeActorId, Is.EqualTo(12));
            Assert.That(session.LastAttributeFrame, Is.EqualTo(5));
        }

        [Test]
        public void BuffCacheKey_IncludesBuffRevisionActorAndFrame()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticBuffsViewModel();

            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.BuffQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.BuffQueryCount, Is.EqualTo(1));

            session.ActorBuffStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 12);
            viewModel.RefreshIfNeeded(session, 12, 5);

            Assert.That(session.BuffQueryCount, Is.EqualTo(4));
            Assert.That(session.LastBuffActorId, Is.EqualTo(12));
            Assert.That(session.LastBuffFrame, Is.EqualTo(5));
        }

        [Test]
        public void TagCacheKey_IncludesTagRevisionActorAndFrame()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticTagsViewModel();

            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.TagQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.TagQueryCount, Is.EqualTo(1));

            session.ActorTagStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 12);
            viewModel.RefreshIfNeeded(session, 12, 5);

            Assert.That(session.TagQueryCount, Is.EqualTo(4));
            Assert.That(session.LastTagActorId, Is.EqualTo(12));
            Assert.That(session.LastTagFrame, Is.EqualTo(5));
        }

        [Test]
        public void EffectCacheKey_IncludesEffectRevisionActorAndFrame()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticEffectsViewModel();

            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.EffectQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.EffectQueryCount, Is.EqualTo(1));

            session.ActorEffectStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 12);
            viewModel.RefreshIfNeeded(session, 12, 5);

            Assert.That(session.EffectQueryCount, Is.EqualTo(4));
            Assert.That(session.LastEffectActorId, Is.EqualTo(12));
            Assert.That(session.LastEffectFrame, Is.EqualTo(5));
        }

        [Test]
        public void OverviewCacheKey_IncludesAllRevisionsActorAndFrame()
        {
            var session = new RecordingSession();
            var viewModel = new BattleDebugDiagnosticOverviewViewModel();

            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 11);
            Assert.That(session.ActorQueryCount, Is.EqualTo(1));
            Assert.That(session.TagQueryCount, Is.EqualTo(1));
            Assert.That(session.EffectQueryCount, Is.EqualTo(1));

            session.StateStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            session.ActorTagStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            session.ActorEffectStoreRevision++;
            viewModel.RefreshIfNeeded(session, 11);
            viewModel.RefreshIfNeeded(session, 12);
            viewModel.RefreshIfNeeded(session, 12, 5);

            Assert.That(session.ActorQueryCount, Is.EqualTo(6));
            Assert.That(session.TagQueryCount, Is.EqualTo(6));
            Assert.That(session.EffectQueryCount, Is.EqualTo(6));
            Assert.That(session.LastActorFrame, Is.EqualTo(5));
            Assert.That(session.LastTagActorId, Is.EqualTo(12));
            Assert.That(session.LastEffectActorId, Is.EqualTo(12));
        }

        [Test]
        public void Overview_ProjectsSelectedActorCountsAndTagClipboardText()
        {
            var session = new RecordingSession
            {
                Actors = new[]
                {
                    new BattleDiagnosticActorSummary(
                        RecordingSession.Scope, 7, 10, BattleDiagnosticActorKind.Minion,
                        100, 2, 0, 0, 0, 20, 20, true, "Other"),
                    new BattleDiagnosticActorSummary(
                        RecordingSession.Scope, 7, 11, BattleDiagnosticActorKind.Hero,
                        200, 1, 1, 2, 3, 80, 100, true, "Selected")
                },
                Tags = new[]
                {
                    new BattleDiagnosticActorTag(RecordingSession.Scope, 7, 11, 1001, "State.Stunned"),
                    new BattleDiagnosticActorTag(RecordingSession.Scope, 7, 11, 1002)
                },
                Effects = new[]
                {
                    new BattleDiagnosticActorEffect(
                        RecordingSession.Scope, 7, 11, 1,
                        BattleDiagnosticEffectDurationPolicy.Infinite, 1,
                        0, 0, false, 0, false, 0, 0, 0, false)
                }
            };
            var viewModel = new BattleDebugDiagnosticOverviewViewModel();

            viewModel.RefreshIfNeeded(session, 11, 7);

            Assert.That(viewModel.Actor.HasValue, Is.True);
            Assert.That(viewModel.Actor.Value.DisplayName, Is.EqualTo("Selected"));
            Assert.That(viewModel.TagCount, Is.EqualTo(2));
            Assert.That(viewModel.EffectCount, Is.EqualTo(1));
            Assert.That(viewModel.BuildTagList(), Is.EqualTo("State.Stunned\n1002"));
            Assert.That(viewModel.StatusMessage, Is.Empty);
        }

        private sealed class RecordingSession : IBattleDiagnosticReadOnlySession
        {
            internal static readonly BattleDiagnosticSessionScope Scope =
                new BattleDiagnosticSessionScope("test", "world", 1);

            public BattleDiagnosticSessionInfo SessionInfo { get; } =
                new BattleDiagnosticSessionInfo(
                    Scope,
                    "test",
                    string.Empty,
                    1,
                    1,
                    BattleDiagnosticCapabilities.WorldState |
                    BattleDiagnosticCapabilities.ActorState |
                    BattleDiagnosticCapabilities.Events,
                    BattleDiagnosticConnectionState.Connected,
                    BattleDiagnosticCaptureState.Capturing);

            public long EventStoreRevision { get; set; }
            public long StateStoreRevision { get; set; }
            public long TraceStoreRevision { get; set; }
            public long ActorAttributeStoreRevision { get; set; }
            public long ActorBuffStoreRevision { get; set; }
            public long ActorTagStoreRevision { get; set; }
            public long ActorEffectStoreRevision { get; set; }
            public IReadOnlyList<BattleDiagnosticActorSummary> Actors { get; set; }
            public IReadOnlyList<BattleDiagnosticActorTag> Tags { get; set; }
            public IReadOnlyList<BattleDiagnosticActorEffect> Effects { get; set; }
            public long StoreRevision => EventStoreRevision;
            public int EventQueryCount { get; private set; }
            public int WorldQueryCount { get; private set; }
            public int ActorQueryCount { get; private set; }
            public int AttributeQueryCount { get; private set; }
            public int ModifierQueryCount { get; private set; }
            public int BuffQueryCount { get; private set; }
            public int TagQueryCount { get; private set; }
            public int EffectQueryCount { get; private set; }
            public int LastWorldFrame { get; private set; }
            public int LastActorFrame { get; private set; }
            public int LastAttributeFrame { get; private set; }
            public long LastAttributeActorId { get; private set; }
            public int LastBuffFrame { get; private set; }
            public long LastBuffActorId { get; private set; }
            public int LastTagFrame { get; private set; }
            public long LastTagActorId { get; private set; }
            public int LastEffectFrame { get; private set; }
            public long LastEffectActorId { get; private set; }

            public BattleDiagnosticQueryResult<BattleDiagnosticWorldSummary> QueryWorld(
                long requestId,
                int frame)
            {
                WorldQueryCount++;
                LastWorldFrame = frame;
                return BattleDiagnosticQueryResult<BattleDiagnosticWorldSummary>.Unavailable(
                    requestId,
                    StateStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorSummary> QueryActors(
                long requestId,
                int frame)
            {
                ActorQueryCount++;
                LastActorFrame = frame;
                if (Actors != null)
                {
                    return BattleDiagnosticQueryResult<BattleDiagnosticActorSummary>.FromItems(
                        requestId, StateStoreRevision, new List<BattleDiagnosticActorSummary>(Actors), false);
                }

                return BattleDiagnosticQueryResult<BattleDiagnosticActorSummary>.Unavailable(
                    requestId,
                    StateStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticEvent> QueryEvents(
                BattleDiagnosticEventQuery query)
            {
                EventQueryCount++;
                return BattleDiagnosticQueryResult<BattleDiagnosticEvent>.FromItems(
                    query.RequestId,
                    EventStoreRevision,
                    new List<BattleDiagnosticEvent>(),
                    false);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticTraceNodeSummary> QueryTrace(
                long requestId,
                long rootContextId)
            {
                return BattleDiagnosticQueryResult<BattleDiagnosticTraceNodeSummary>.Unavailable(
                    requestId,
                    TraceStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorAttribute> QueryActorAttributes(
                long requestId,
                int frame,
                long actorId)
            {
                AttributeQueryCount++;
                LastAttributeFrame = frame;
                LastAttributeActorId = actorId;
                return BattleDiagnosticQueryResult<BattleDiagnosticActorAttribute>.Unavailable(
                    requestId,
                    ActorAttributeStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorAttributeModifier> QueryActorAttributeModifiers(
                long requestId,
                int frame,
                long actorId)
            {
                ModifierQueryCount++;
                return BattleDiagnosticQueryResult<BattleDiagnosticActorAttributeModifier>.Unavailable(
                    requestId,
                    ActorAttributeStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorBuff> QueryActorBuffs(
                long requestId,
                int frame,
                long actorId)
            {
                BuffQueryCount++;
                LastBuffFrame = frame;
                LastBuffActorId = actorId;
                return BattleDiagnosticQueryResult<BattleDiagnosticActorBuff>.Unavailable(
                    requestId,
                    ActorBuffStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorTag> QueryActorTags(
                long requestId,
                int frame,
                long actorId)
            {
                TagQueryCount++;
                LastTagFrame = frame;
                LastTagActorId = actorId;
                if (Tags != null)
                {
                    return BattleDiagnosticQueryResult<BattleDiagnosticActorTag>.FromItems(
                        requestId, ActorTagStoreRevision, new List<BattleDiagnosticActorTag>(Tags), false);
                }

                return BattleDiagnosticQueryResult<BattleDiagnosticActorTag>.Unavailable(
                    requestId,
                    ActorTagStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }

            public BattleDiagnosticQueryResult<BattleDiagnosticActorEffect> QueryActorEffects(
                long requestId,
                int frame,
                long actorId)
            {
                EffectQueryCount++;
                LastEffectFrame = frame;
                LastEffectActorId = actorId;
                if (Effects != null)
                {
                    return BattleDiagnosticQueryResult<BattleDiagnosticActorEffect>.FromItems(
                        requestId, ActorEffectStoreRevision, new List<BattleDiagnosticActorEffect>(Effects), false);
                }

                return BattleDiagnosticQueryResult<BattleDiagnosticActorEffect>.Unavailable(
                    requestId,
                    ActorEffectStoreRevision,
                    BattleDiagnosticDataAvailability.NotProduced);
            }
        }
    }
}
