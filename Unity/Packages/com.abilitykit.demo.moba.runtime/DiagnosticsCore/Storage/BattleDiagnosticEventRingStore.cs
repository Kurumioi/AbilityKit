using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Diagnostics
{
    public readonly struct BattleDiagnosticStoreMetrics : IEquatable<BattleDiagnosticStoreMetrics>
    {
        public BattleDiagnosticStoreMetrics(
            int capacity,
            int count,
            long revision,
            long acceptedCount,
            long evictedCount,
            long rejectedCount,
            bool isFrozen)
        {
            Capacity = capacity;
            Count = count;
            Revision = revision;
            AcceptedCount = acceptedCount;
            EvictedCount = evictedCount;
            RejectedCount = rejectedCount;
            IsFrozen = isFrozen;
        }

        public int Capacity { get; }
        public int Count { get; }
        public long Revision { get; }
        public long AcceptedCount { get; }
        public long EvictedCount { get; }
        public long RejectedCount { get; }
        public bool IsFrozen { get; }

        public bool Equals(BattleDiagnosticStoreMetrics other)
        {
            return Capacity == other.Capacity && Count == other.Count && Revision == other.Revision &&
                   AcceptedCount == other.AcceptedCount && EvictedCount == other.EvictedCount &&
                   RejectedCount == other.RejectedCount && IsFrozen == other.IsFrozen;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleDiagnosticStoreMetrics other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Capacity;
                hashCode = (hashCode * 397) ^ Count;
                hashCode = (hashCode * 397) ^ Revision.GetHashCode();
                hashCode = (hashCode * 397) ^ AcceptedCount.GetHashCode();
                hashCode = (hashCode * 397) ^ EvictedCount.GetHashCode();
                hashCode = (hashCode * 397) ^ RejectedCount.GetHashCode();
                hashCode = (hashCode * 397) ^ IsFrozen.GetHashCode();
                return hashCode;
            }
        }
    }

    public interface IBattleDiagnosticEventReadStore
    {
        BattleDiagnosticSessionScope Scope { get; }
        long Revision { get; }
        BattleDiagnosticQueryResult<BattleDiagnosticEvent> Query(BattleDiagnosticEventQuery query);
    }

    public sealed class BattleDiagnosticEventRingStore : IBattleDiagnosticEventReadStore
    {
        public const int DefaultCapacity = 20000;
        public const int DefaultRetainedReadViewCount = 4;

        private readonly BattleDiagnosticEvent[] _buffer;
        private readonly Dictionary<long, BattleDiagnosticEvent[]> _readViews;
        private readonly Queue<long> _readViewOrder;
        private readonly int _retainedReadViewCount;
        private int _head;
        private int _count;
        private long _revision;
        private long _lastSequence;
        private long _acceptedCount;
        private long _evictedCount;
        private long _rejectedCount;
        private bool _isFrozen;

        public BattleDiagnosticEventRingStore(
            BattleDiagnosticSessionScope scope,
            int capacity = DefaultCapacity,
            int retainedReadViewCount = DefaultRetainedReadViewCount)
        {
            if (!scope.IsValid) throw new ArgumentException("A valid session scope is required.", nameof(scope));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (retainedReadViewCount <= 0) throw new ArgumentOutOfRangeException(nameof(retainedReadViewCount));

            Scope = scope;
            _buffer = new BattleDiagnosticEvent[capacity];
            _retainedReadViewCount = retainedReadViewCount;
            _readViews = new Dictionary<long, BattleDiagnosticEvent[]>(retainedReadViewCount);
            _readViewOrder = new Queue<long>(retainedReadViewCount);
        }

        public BattleDiagnosticSessionScope Scope { get; }
        public int Capacity => _buffer.Length;
        public int Count => _count;
        public long Revision => _revision;
        public bool IsFrozen => _isFrozen;

        public BattleDiagnosticStoreMetrics Metrics => new BattleDiagnosticStoreMetrics(
            Capacity,
            _count,
            _revision,
            _acceptedCount,
            _evictedCount,
            _rejectedCount,
            _isFrozen);

        public bool TryAppend(BattleDiagnosticEvent diagnosticEvent)
        {
            if (_isFrozen || diagnosticEvent.Scope != Scope || diagnosticEvent.Sequence <= _lastSequence)
            {
                _rejectedCount++;
                return false;
            }

            if (_count == Capacity)
            {
                _buffer[_head] = diagnosticEvent;
                _head = (_head + 1) % Capacity;
                _evictedCount++;
            }
            else
            {
                var tail = (_head + _count) % Capacity;
                _buffer[tail] = diagnosticEvent;
                _count++;
            }

            _lastSequence = diagnosticEvent.Sequence;
            _acceptedCount++;
            _revision++;
            return true;
        }

        public void SetFrozen(bool frozen)
        {
            _isFrozen = frozen;
        }

        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }

            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
            _revision++;
            ClearReadViews();
        }

        public BattleDiagnosticQueryResult<BattleDiagnosticEvent> Query(BattleDiagnosticEventQuery query)
        {
            var requestedRevision = query.Page.StoreRevision;
            BattleDiagnosticEvent[] readView;

            if (requestedRevision <= 0 || requestedRevision == _revision)
            {
                requestedRevision = _revision;
                readView = GetOrCreateCurrentReadView();
            }
            else if (!_readViews.TryGetValue(requestedRevision, out readView))
            {
                return BattleDiagnosticQueryResult<BattleDiagnosticEvent>.Unavailable(
                    query.RequestId,
                    requestedRevision,
                    BattleDiagnosticDataAvailability.Evicted,
                    "The requested store revision is no longer retained.");
            }

            var matches = new List<BattleDiagnosticEvent>(Math.Min(query.Page.Limit, readView.Length));
            var skipped = 0;
            var hasMore = false;

            for (var index = 0; index < readView.Length; index++)
            {
                var diagnosticEvent = readView[index];
                if (!Matches(diagnosticEvent, query.Filter))
                {
                    continue;
                }

                if (skipped < query.Page.Offset)
                {
                    skipped++;
                    continue;
                }

                if (matches.Count == query.Page.Limit)
                {
                    hasMore = true;
                    break;
                }

                matches.Add(diagnosticEvent);
            }

            return BattleDiagnosticQueryResult<BattleDiagnosticEvent>.FromItems(
                query.RequestId,
                requestedRevision,
                matches,
                hasMore);
        }

        private BattleDiagnosticEvent[] GetOrCreateCurrentReadView()
        {
            if (_readViews.TryGetValue(_revision, out var existing))
            {
                return existing;
            }

            var readView = new BattleDiagnosticEvent[_count];
            for (var index = 0; index < _count; index++)
            {
                readView[index] = _buffer[(_head + index) % Capacity];
            }

            _readViews.Add(_revision, readView);
            _readViewOrder.Enqueue(_revision);
            while (_readViewOrder.Count > _retainedReadViewCount)
            {
                var oldestRevision = _readViewOrder.Dequeue();
                _readViews.Remove(oldestRevision);
            }

            return readView;
        }

        private void ClearReadViews()
        {
            _readViews.Clear();
            _readViewOrder.Clear();
        }

        private static bool Matches(BattleDiagnosticEvent diagnosticEvent, BattleDiagnosticFilter filter)
        {
            if (!filter.Frames.Contains(diagnosticEvent.Frame)) return false;
            if ((filter.Channels & diagnosticEvent.Channel) == 0) return false;
            if (filter.ConfigId != 0 && filter.ConfigId != diagnosticEvent.ConfigId) return false;
            if (filter.RootContextId != 0 && filter.RootContextId != diagnosticEvent.RootContextId) return false;
            if (filter.ContextId != 0 && filter.ContextId != diagnosticEvent.ContextId) return false;
            if (filter.SkillRuntimeId != 0 && filter.SkillRuntimeId != diagnosticEvent.SkillRuntime.RuntimeId) return false;
            if (filter.AttackId != 0 && filter.AttackId != diagnosticEvent.AttackId) return false;
            if (filter.FailuresOnly && !diagnosticEvent.IsFailure) return false;
            if (filter.UnfinishedOnly && !diagnosticEvent.IsUnfinished) return false;
            if (filter.HasTextSearch &&
                diagnosticEvent.Summary.IndexOf(filter.SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (!filter.HasActorFilter)
            {
                return true;
            }

            switch (filter.ActorRelation)
            {
                case BattleDiagnosticActorRelation.Source:
                    return diagnosticEvent.SourceActorId == filter.ActorId;
                case BattleDiagnosticActorRelation.Target:
                    return diagnosticEvent.TargetActorId == filter.ActorId;
                case BattleDiagnosticActorRelation.Either:
                    return diagnosticEvent.SourceActorId == filter.ActorId ||
                           diagnosticEvent.TargetActorId == filter.ActorId;
                default:
                    return diagnosticEvent.SourceActorId == filter.ActorId ||
                           diagnosticEvent.TargetActorId == filter.ActorId;
            }
        }
    }
}
