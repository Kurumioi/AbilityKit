using System;
using System.Collections.Generic;

namespace AbilityKit.Ability.StateSync.Aoi
{
    public readonly struct AoiEntityKey : IEquatable<AoiEntityKey>
    {
        public AoiEntityKey(int kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        public int Kind { get; }

        public int Id { get; }

        public bool IsValid => Id > 0;

        public bool Equals(AoiEntityKey other)
        {
            return Kind == other.Kind && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AoiEntityKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Kind * 397) ^ Id;
            }
        }

        public override string ToString()
        {
            return Kind + ":" + Id;
        }
    }

    public readonly struct AoiInterestScope
    {
        public AoiInterestScope(float centerX, float centerY, float visibleRadius, float boundaryRadius = 0f, int maxEntities = 0)
        {
            CenterX = centerX;
            CenterY = centerY;
            VisibleRadius = Math.Max(0f, visibleRadius);
            BoundaryRadius = Math.Max(VisibleRadius, boundaryRadius <= 0f ? visibleRadius : boundaryRadius);
            MaxEntities = maxEntities;
        }

        public float CenterX { get; }

        public float CenterY { get; }

        public float VisibleRadius { get; }

        public float BoundaryRadius { get; }

        public int MaxEntities { get; }

        public bool HasRadius => VisibleRadius > 0f;
    }

    public readonly struct AoiEntitySample
    {
        public AoiEntitySample(AoiEntityKey key, float x, float y, int priority = 0, int layer = 0, int ownerId = 0, byte flags = 0)
        {
            Key = key;
            X = x;
            Y = y;
            Priority = priority;
            Layer = layer;
            OwnerId = ownerId;
            Flags = flags;
        }

        public AoiEntityKey Key { get; }

        public float X { get; }

        public float Y { get; }

        public int Priority { get; }

        public int Layer { get; }

        public int OwnerId { get; }

        public byte Flags { get; }
    }

    public enum AoiInterestTransition
    {
        None = 0,
        Enter = 1,
        Stay = 2,
        Leave = 3
    }

    public readonly struct AoiInterestChange
    {
        public AoiInterestChange(AoiEntityKey key, AoiInterestTransition transition, float distanceSquared)
            : this(key, transition, distanceSquared, 0, 0, 0)
        {
        }

        public AoiInterestChange(AoiEntityKey key, AoiInterestTransition transition, float distanceSquared, int layer, int ownerId, byte flags)
        {
            Key = key;
            Transition = transition;
            DistanceSquared = distanceSquared;
            Layer = layer;
            OwnerId = ownerId;
            Flags = flags;
        }

        public AoiEntityKey Key { get; }

        public AoiInterestTransition Transition { get; }

        public float DistanceSquared { get; }

        public int Layer { get; }

        public int OwnerId { get; }

        public byte Flags { get; }

        public bool IsVisible => Transition == AoiInterestTransition.Enter || Transition == AoiInterestTransition.Stay;
    }

    public sealed class AoiInterestEvaluation
    {
        private static readonly IReadOnlyList<AoiInterestChange> EmptyChanges = Array.Empty<AoiInterestChange>();

        public AoiInterestEvaluation(IReadOnlyList<AoiInterestChange> changes, int visibleCount)
        {
            Changes = changes ?? EmptyChanges;
            VisibleCount = visibleCount;
        }

        public IReadOnlyList<AoiInterestChange> Changes { get; }

        public int VisibleCount { get; }
    }

    public sealed class AoiInterestSet
    {
        private readonly HashSet<AoiEntityKey> _visible = new HashSet<AoiEntityKey>();
        private readonly HashSet<AoiEntityKey> _seenThisFrame = new HashSet<AoiEntityKey>();
        private readonly Dictionary<AoiEntityKey, AoiEntitySample> _lastVisibleSamples = new Dictionary<AoiEntityKey, AoiEntitySample>();

        public int VisibleCount => _visible.Count;

        public bool IsVisible(AoiEntityKey key)
        {
            return key.IsValid && _visible.Contains(key);
        }

        public void Clear()
        {
            _visible.Clear();
            _seenThisFrame.Clear();
            _lastVisibleSamples.Clear();
        }

        public AoiInterestEvaluation Evaluate(IReadOnlyList<AoiEntitySample> samples, AoiInterestScope scope, bool forceFullBaseline = false)
        {
            _seenThisFrame.Clear();

            if (forceFullBaseline)
            {
                _visible.Clear();
                _lastVisibleSamples.Clear();
            }

            if (samples == null || samples.Count == 0)
            {
                return RemoveUnseenEntities();
            }

            var changes = new List<AoiInterestChange>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                if (!sample.Key.IsValid)
                {
                    continue;
                }

                var distanceSquared = ComputeDistanceSquared(sample.X, sample.Y, scope);
                var wasVisible = _visible.Contains(sample.Key);
                var shouldBeVisible = ShouldBeVisible(wasVisible, distanceSquared, scope);
                if (!shouldBeVisible)
                {
                    continue;
                }

                _seenThisFrame.Add(sample.Key);
                if (wasVisible)
                {
                    _lastVisibleSamples[sample.Key] = sample;
                    changes.Add(new AoiInterestChange(sample.Key, AoiInterestTransition.Stay, distanceSquared, sample.Layer, sample.OwnerId, sample.Flags));
                    continue;
                }

                _visible.Add(sample.Key);
                _lastVisibleSamples[sample.Key] = sample;
                changes.Add(new AoiInterestChange(sample.Key, AoiInterestTransition.Enter, distanceSquared, sample.Layer, sample.OwnerId, sample.Flags));
            }

            AppendUnseenLeaves(changes);
            return new AoiInterestEvaluation(changes, _visible.Count);
        }

        private static bool ShouldBeVisible(bool wasVisible, float distanceSquared, AoiInterestScope scope)
        {
            if (!scope.HasRadius)
            {
                return true;
            }

            var radius = wasVisible ? scope.BoundaryRadius : scope.VisibleRadius;
            return distanceSquared <= radius * radius;
        }

        private static float ComputeDistanceSquared(float x, float y, AoiInterestScope scope)
        {
            if (!scope.HasRadius)
            {
                return 0f;
            }

            var dx = x - scope.CenterX;
            var dy = y - scope.CenterY;
            return dx * dx + dy * dy;
        }

        private AoiInterestEvaluation RemoveUnseenEntities()
        {
            if (_visible.Count == 0)
            {
                return new AoiInterestEvaluation(Array.Empty<AoiInterestChange>(), 0);
            }

            var changes = new List<AoiInterestChange>(_visible.Count);
            AppendUnseenLeaves(changes);
            return new AoiInterestEvaluation(changes, _visible.Count);
        }

        private void AppendUnseenLeaves(List<AoiInterestChange> changes)
        {
            if (_visible.Count == 0)
            {
                return;
            }

            var leaves = new List<AoiEntityKey>();
            foreach (var key in _visible)
            {
                if (!_seenThisFrame.Contains(key))
                {
                    leaves.Add(key);
                }
            }

            for (int i = 0; i < leaves.Count; i++)
            {
                var key = leaves[i];
                _visible.Remove(key);
                if (_lastVisibleSamples.TryGetValue(key, out var sample))
                {
                    _lastVisibleSamples.Remove(key);
                    changes.Add(new AoiInterestChange(key, AoiInterestTransition.Leave, 0f, sample.Layer, sample.OwnerId, sample.Flags));
                    continue;
                }

                changes.Add(new AoiInterestChange(key, AoiInterestTransition.Leave, 0f));
            }
        }
    }
}
