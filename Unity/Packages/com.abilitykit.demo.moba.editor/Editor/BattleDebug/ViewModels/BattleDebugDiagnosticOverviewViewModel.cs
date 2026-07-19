using System;
using System.Collections.Generic;
using System.Text;
using AbilityKit.Demo.Moba.Diagnostics;

namespace AbilityKit.Game.Editor
{
    internal sealed class BattleDebugDiagnosticOverviewViewModel
    {
        private long _lastRequestId;
        private BattleDiagnosticSessionScope _lastScope;
        private long _lastStateRevision = -1;
        private long _lastTagRevision = -1;
        private long _lastEffectRevision = -1;
        private long _lastActorId;
        private int _lastFrame;
        private bool _hasCachedResult;
        private BattleDiagnosticActorSummary? _actor;
        private IReadOnlyList<BattleDiagnosticActorTag> _tags = Array.Empty<BattleDiagnosticActorTag>();
        private IReadOnlyList<BattleDiagnosticActorEffect> _effects = Array.Empty<BattleDiagnosticActorEffect>();

        public BattleDiagnosticActorSummary? Actor => _actor;
        public int TagCount => _tags.Count;
        public int EffectCount => _effects.Count;
        public string StatusMessage { get; private set; } = string.Empty;
        public long StateStoreRevision => _lastStateRevision;
        public long TagStoreRevision => _lastTagRevision;
        public long EffectStoreRevision => _lastEffectRevision;

        public void InvalidateCache()
        {
            _actor = null;
            _tags = Array.Empty<BattleDiagnosticActorTag>();
            _effects = Array.Empty<BattleDiagnosticActorEffect>();
            _lastStateRevision = -1;
            _lastTagRevision = -1;
            _lastEffectRevision = -1;
            _hasCachedResult = false;
        }

        public void RefreshIfNeeded(
            IBattleDiagnosticReadOnlySession session,
            long actorId,
            int frame = 0)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (actorId == 0) throw new ArgumentOutOfRangeException(nameof(actorId));

            var scope = session.SessionInfo.Scope;
            var stateRevision = session.StateStoreRevision;
            var tagRevision = session.ActorTagStoreRevision;
            var effectRevision = session.ActorEffectStoreRevision;
            var queryFrame = frame < 0 ? 0 : frame;
            if (_hasCachedResult &&
                _lastScope == scope &&
                _lastStateRevision == stateRevision &&
                _lastTagRevision == tagRevision &&
                _lastEffectRevision == effectRevision &&
                _lastActorId == actorId &&
                _lastFrame == queryFrame)
            {
                return;
            }

            _lastRequestId++;
            if (_lastRequestId <= 0) _lastRequestId = 1;

            var actorsResult = session.QueryActors(_lastRequestId, queryFrame);
            var tagsResult = session.QueryActorTags(_lastRequestId, queryFrame, actorId);
            var effectsResult = session.QueryActorEffects(_lastRequestId, queryFrame, actorId);

            _lastScope = scope;
            _lastStateRevision = stateRevision;
            _lastTagRevision = tagRevision;
            _lastEffectRevision = effectRevision;
            _lastActorId = actorId;
            _lastFrame = queryFrame;
            _hasCachedResult = true;
            _actor = FindActor(actorsResult.Items, actorId);
            _tags = tagsResult.Items ?? Array.Empty<BattleDiagnosticActorTag>();
            _effects = effectsResult.Items ?? Array.Empty<BattleDiagnosticActorEffect>();
            StatusMessage = BuildStatusMessage(
                actorsResult.Status,
                tagsResult.Status,
                effectsResult.Status);
        }

        public string BuildTagList()
        {
            if (_tags.Count == 0) return string.Empty;

            var builder = new StringBuilder(256);
            for (var i = 0; i < _tags.Count; i++)
            {
                if (builder.Length > 0) builder.Append('\n');
                var tag = _tags[i];
                builder.Append(string.IsNullOrEmpty(tag.Name) ? tag.TagId.ToString() : tag.Name);
            }

            return builder.ToString();
        }

        private static BattleDiagnosticActorSummary? FindActor(
            IReadOnlyList<BattleDiagnosticActorSummary> actors,
            long actorId)
        {
            if (actors == null) return null;

            for (var i = 0; i < actors.Count; i++)
            {
                if (actors[i].ActorId == actorId) return actors[i];
            }

            return null;
        }

        private static string BuildStatusMessage(
            BattleDiagnosticQueryStatus actorStatus,
            BattleDiagnosticQueryStatus tagStatus,
            BattleDiagnosticQueryStatus effectStatus)
        {
            if (!actorStatus.CanDisplayResults && actorStatus.Phase != BattleDiagnosticQueryPhase.Empty)
            {
                return $"Actor 状态不可用：{actorStatus.Availability} {actorStatus.Message}";
            }

            if (!tagStatus.CanDisplayResults && tagStatus.Phase != BattleDiagnosticQueryPhase.Empty)
            {
                return $"标签数据不可用：{tagStatus.Availability} {tagStatus.Message}";
            }

            if (!effectStatus.CanDisplayResults && effectStatus.Phase != BattleDiagnosticQueryPhase.Empty)
            {
                return $"Effect 数据不可用：{effectStatus.Availability} {effectStatus.Message}";
            }

            return string.Empty;
        }
    }
}
