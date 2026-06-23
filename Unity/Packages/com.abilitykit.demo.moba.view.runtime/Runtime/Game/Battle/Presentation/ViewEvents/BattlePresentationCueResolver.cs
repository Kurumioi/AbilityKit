using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal enum BattlePresentationCueDecisionKind
    {
        None = 0,
        Play = 1,
        Stop = 2,
    }

    internal readonly struct BattlePresentationCueSpawnRequest
    {
        public BattlePresentationCueSpawnRequest(
            BattlePresentationCueRequestKey requestKey,
            int vfxId,
            int sourceActorId,
            int targetActorId,
            int firstTargetActorId,
            bool hasExplicitPosition,
            SnapshotVec3 explicitPosition,
            SnapshotVec3 offset)
        {
            RequestKey = requestKey;
            VfxId = vfxId;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            FirstTargetActorId = firstTargetActorId;
            HasExplicitPosition = hasExplicitPosition;
            ExplicitPosition = explicitPosition;
            Offset = offset;
        }

        public BattlePresentationCueRequestKey RequestKey { get; }
        public int VfxId { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public int FirstTargetActorId { get; }
        public bool HasExplicitPosition { get; }
        public SnapshotVec3 ExplicitPosition { get; }
        public SnapshotVec3 Offset { get; }

        public bool IsEmpty => RequestKey.IsEmpty || VfxId <= 0;
    }

    internal readonly struct BattlePresentationCueDecision
    {
        public BattlePresentationCueDecision(
            BattlePresentationCueDecisionKind kind,
            BattlePresentationCueRequestKey requestKey,
            BattlePresentationCueSpawnRequest spawnRequest)
        {
            Kind = kind;
            RequestKey = requestKey;
            SpawnRequest = spawnRequest;
        }

        public BattlePresentationCueDecisionKind Kind { get; }
        public BattlePresentationCueRequestKey RequestKey { get; }
        public BattlePresentationCueSpawnRequest SpawnRequest { get; }

        public bool IsNone => Kind == BattlePresentationCueDecisionKind.None || RequestKey.IsEmpty;

        public static BattlePresentationCueDecision None => default;
    }

    internal sealed class BattlePresentationCueResolver
    {
        public BattlePresentationCueDecision Resolve(in PresentationCueData data)
        {
            var requestKey = BattlePresentationCueRequestKey.From(in data);
            if (requestKey.IsEmpty) return BattlePresentationCueDecision.None;

            if (ShouldStart(data.Stage) || ShouldKeepActive(data.Stage))
            {
                var spawnRequest = CreateSpawnRequest(requestKey, in data);
                if (spawnRequest.IsEmpty) return BattlePresentationCueDecision.None;

                return new BattlePresentationCueDecision(BattlePresentationCueDecisionKind.Play, requestKey, spawnRequest);
            }

            if (ShouldStop(data.Stage))
            {
                return new BattlePresentationCueDecision(BattlePresentationCueDecisionKind.Stop, requestKey, default);
            }

            return BattlePresentationCueDecision.None;
        }

        public static bool ShouldStart(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionPassed
                || stage == PresentationCueStage.BeforeAction
                || stage == PresentationCueStage.Executed
                || stage == PresentationCueStage.Started;
        }

        public static bool ShouldKeepActive(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.Ticked
                || stage == PresentationCueStage.Refreshed
                || stage == PresentationCueStage.StackChanged;
        }

        public static bool ShouldStop(PresentationCueStage stage)
        {
            return stage == PresentationCueStage.ConditionFailed
                || stage == PresentationCueStage.Interrupted
                || stage == PresentationCueStage.Skipped
                || stage == PresentationCueStage.Expired
                || stage == PresentationCueStage.Removed
                || stage == PresentationCueStage.Completed;
        }

        public static int ResolveVfxId(in PresentationCueData data)
        {
            if (data.VfxId > 0) return data.VfxId;
            if (data.TemplateId > 0) return data.TemplateId;
            return 0;
        }

        public static BattlePresentationCueSpawnRequest CreateSpawnRequest(
            BattlePresentationCueRequestKey requestKey,
            in PresentationCueData data)
        {
            var vfxId = ResolveVfxId(in data);
            if (requestKey.IsEmpty || vfxId <= 0) return default;

            return new BattlePresentationCueSpawnRequest(
                requestKey,
                vfxId,
                data.SourceActorId,
                data.TargetActorId,
                ResolveFirstTargetActorId(in data),
                data.Positions != null && data.Positions.Count > 0,
                data.Positions != null && data.Positions.Count > 0 ? data.Positions[0] : default,
                new SnapshotVec3(data.OffsetX, data.OffsetY, data.OffsetZ));
        }

        public static int ResolveFirstTargetActorId(in PresentationCueData data)
        {
            if (data.Targets != null && data.Targets.Count > 0) return data.Targets[0];
            return 0;
        }
    }

    internal readonly struct BattlePresentationCueRequestKey : IEquatable<BattlePresentationCueRequestKey>
    {
        private readonly string _externalKey;
        private readonly int _triggerKey;
        private readonly int _triggerEventId;
        private readonly int _actionIndex;
        private readonly int _order;
        private readonly int _sourceActorId;
        private readonly int _targetActorId;
        private readonly bool _hasExternalKey;
        private readonly bool _hasValue;

        public bool IsEmpty => !_hasValue;

        private BattlePresentationCueRequestKey(string externalKey)
        {
            _externalKey = externalKey;
            _triggerKey = 0;
            _triggerEventId = 0;
            _actionIndex = 0;
            _order = 0;
            _sourceActorId = 0;
            _targetActorId = 0;
            _hasExternalKey = true;
            _hasValue = true;
        }

        private BattlePresentationCueRequestKey(
            int triggerKey,
            int triggerEventId,
            int actionIndex,
            int order,
            int sourceActorId,
            int targetActorId)
        {
            _externalKey = null;
            _triggerKey = triggerKey;
            _triggerEventId = triggerEventId;
            _actionIndex = actionIndex;
            _order = order;
            _sourceActorId = sourceActorId;
            _targetActorId = targetActorId;
            _hasExternalKey = false;
            _hasValue = true;
        }

        public static BattlePresentationCueRequestKey From(in PresentationCueData data)
        {
            if (!string.IsNullOrWhiteSpace(data.InstanceKey)) return FromExternal(data.InstanceKey);
            if (!string.IsNullOrWhiteSpace(data.RequestKey)) return FromExternal(data.RequestKey);

            return FromGenerated(
                data.TriggerId,
                data.TriggerEventId,
                data.ActionIndex,
                data.Order,
                data.SourceActorId,
                data.TargetActorId);
        }

        public static BattlePresentationCueRequestKey FromExternal(string externalKey)
        {
            return string.IsNullOrWhiteSpace(externalKey) ? default : new BattlePresentationCueRequestKey(externalKey);
        }

        public static BattlePresentationCueRequestKey FromGenerated(
            int triggerKey,
            int triggerEventId,
            int actionIndex,
            int order,
            int sourceActorId,
            int targetActorId)
        {
            return new BattlePresentationCueRequestKey(triggerKey, triggerEventId, actionIndex, order, sourceActorId, targetActorId);
        }

        public bool Equals(BattlePresentationCueRequestKey other)
        {
            if (_hasValue != other._hasValue) return false;
            if (!_hasValue) return true;
            if (_hasExternalKey != other._hasExternalKey) return false;
            if (_hasExternalKey) return string.Equals(_externalKey, other._externalKey, StringComparison.Ordinal);

            if (_triggerKey > 0 || other._triggerKey > 0)
            {
                return _triggerKey == other._triggerKey && _order == other._order;
            }

            if (_triggerEventId > 0 || other._triggerEventId > 0)
            {
                return _triggerEventId == other._triggerEventId && _order == other._order;
            }

            return _actionIndex == other._actionIndex
                && _order == other._order
                && _sourceActorId == other._sourceActorId
                && _targetActorId == other._targetActorId;
        }

        public override bool Equals(object obj)
        {
            return obj is BattlePresentationCueRequestKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (!_hasValue) return 0;
            if (_hasExternalKey) return _externalKey != null ? StringComparer.Ordinal.GetHashCode(_externalKey) : 0;

            unchecked
            {
                if (_triggerKey > 0)
                {
                    return (_triggerKey * 397) ^ _order;
                }

                if (_triggerEventId > 0)
                {
                    return (_triggerEventId * 397) ^ _order;
                }

                var hash = _actionIndex;
                hash = (hash * 397) ^ _order;
                hash = (hash * 397) ^ _sourceActorId;
                hash = (hash * 397) ^ _targetActorId;
                return hash;
            }
        }
    }
}
