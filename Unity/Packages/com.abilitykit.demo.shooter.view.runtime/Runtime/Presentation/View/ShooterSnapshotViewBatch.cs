#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Game.View.Presentation;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public readonly struct ShooterSnapshotViewBatch : IViewBatch
    {
        private static readonly IReadOnlyList<ShooterViewEntityChange> EmptyEntityChanges = Array.Empty<ShooterViewEntityChange>();
        private static readonly IReadOnlyList<ShooterViewEntityKey> EmptyRemovedEntities = Array.Empty<ShooterViewEntityKey>();
        private static readonly IReadOnlyList<ShooterViewTransformComponentChange> EmptyTransformChanges = Array.Empty<ShooterViewTransformComponentChange>();
        private static readonly IReadOnlyList<ShooterViewHealthComponentChange> EmptyHealthChanges = Array.Empty<ShooterViewHealthComponentChange>();
        private static readonly IReadOnlyList<ShooterViewScoreComponentChange> EmptyScoreChanges = Array.Empty<ShooterViewScoreComponentChange>();
        private static readonly IReadOnlyList<ShooterViewProjectileLifetimeComponentChange> EmptyProjectileLifetimeChanges = Array.Empty<ShooterViewProjectileLifetimeComponentChange>();
        private static readonly IReadOnlyList<ShooterEventSnapshot> EmptyEvents = Array.Empty<ShooterEventSnapshot>();

        public ShooterSnapshotViewBatch(
            ulong worldId,
            int frame,
            ulong sequence,
            ShooterViewSnapshotKind snapshotKind,
            ShooterViewBatchSource source,
            IReadOnlyList<ShooterViewEntityChange> entityChanges,
            IReadOnlyList<ShooterViewEntityKey> removedEntities,
            IReadOnlyList<ShooterViewTransformComponentChange> transformChanges,
            IReadOnlyList<ShooterViewHealthComponentChange> healthChanges,
            IReadOnlyList<ShooterViewScoreComponentChange> scoreChanges,
            IReadOnlyList<ShooterViewProjectileLifetimeComponentChange> projectileLifetimeChanges,
            IReadOnlyList<ShooterEventSnapshot> events)
        {
            WorldId = worldId;
            Frame = frame;
            Sequence = sequence;
            SnapshotKind = snapshotKind;
            Source = source;
            EntityChanges = entityChanges ?? throw new ArgumentNullException(nameof(entityChanges));
            RemovedEntities = removedEntities ?? throw new ArgumentNullException(nameof(removedEntities));
            TransformChanges = transformChanges ?? throw new ArgumentNullException(nameof(transformChanges));
            HealthChanges = healthChanges ?? throw new ArgumentNullException(nameof(healthChanges));
            ScoreChanges = scoreChanges ?? throw new ArgumentNullException(nameof(scoreChanges));
            ProjectileLifetimeChanges = projectileLifetimeChanges ?? throw new ArgumentNullException(nameof(projectileLifetimeChanges));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public ulong WorldId { get; }

        public int Frame { get; }

        public ulong Sequence { get; }

        public ShooterViewSnapshotKind SnapshotKind { get; }

        public ShooterViewBatchSource Source { get; }
 
        public IReadOnlyList<ShooterViewEntityChange> EntityChanges { get; }

        public IReadOnlyList<ShooterViewEntityKey> RemovedEntities { get; }

        public IReadOnlyList<ShooterViewTransformComponentChange> TransformChanges { get; }

        public IReadOnlyList<ShooterViewHealthComponentChange> HealthChanges { get; }

        public IReadOnlyList<ShooterViewScoreComponentChange> ScoreChanges { get; }

        public IReadOnlyList<ShooterViewProjectileLifetimeComponentChange> ProjectileLifetimeChanges { get; }

        public IReadOnlyList<ShooterEventSnapshot> Events { get; }

        public int EntityChangeCount => EntityChanges.Count;

        public int RemovedEntityCount => RemovedEntities.Count;

        public int ComponentChangeCount => TransformChanges.Count + HealthChanges.Count + ScoreChanges.Count + ProjectileLifetimeChanges.Count;

        public bool IsFullSnapshot => SnapshotKind == ShooterViewSnapshotKind.Full;

        public bool ShouldReplaceMissingEntities => IsFullSnapshot &&
            (Source == ShooterViewBatchSource.AuthoritativeCorrection ||
             Source == ShooterViewBatchSource.JoinOrReconnect ||
             Source == ShooterViewBatchSource.LocalAuthoritative);
 
        public static ShooterSnapshotViewBatch Empty { get; } = new ShooterSnapshotViewBatch(
            0UL,
            0,
            0UL,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.DebugSnapshot,
            EmptyEntityChanges,
            EmptyRemovedEntities,
            EmptyTransformChanges,
            EmptyHealthChanges,
            EmptyScoreChanges,
            EmptyProjectileLifetimeChanges,
            EmptyEvents);
    }

    public readonly struct ShooterViewEntityKey : IEquatable<ShooterViewEntityKey>
    {
        public ShooterViewEntityKey(ShooterViewEntityKind kind, int entityId)
        {
            Kind = kind;
            EntityId = entityId;
        }

        public ShooterViewEntityKind Kind { get; }

        public int EntityId { get; }

        public bool Equals(ShooterViewEntityKey other)
        {
            return Kind == other.Kind && EntityId == other.EntityId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ShooterViewEntityKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ EntityId;
            }
        }

        public override string ToString()
        {
            return Kind + ":" + EntityId;
        }
    }

    public enum ShooterViewSnapshotKind
    {
        Full = 1,
        Delta = 2
    }

    public enum ShooterViewBatchSource
    {
        LocalPrediction = 1,
        AuthoritativeCorrection = 2,
        JoinOrReconnect = 3,
        DebugSnapshot = 4,
        LocalAuthoritative = 5
    }

    public readonly struct ShooterViewEntityChange
    {
        public ShooterViewEntityChange(ShooterViewEntityKey key, int ownerEntityId, bool alive)
        {
            Key = key;
            OwnerEntityId = ownerEntityId;
            Alive = alive;
        }

        public ShooterViewEntityKey Key { get; }

        public ShooterViewEntityKind Kind => Key.Kind;

        public int EntityId => Key.EntityId;

        public int OwnerEntityId { get; }

        public bool Alive { get; }
    }

    public readonly struct ShooterViewTransformComponentChange
    {
        public ShooterViewTransformComponentChange(
            ShooterViewEntityKey key,
            float x,
            float y,
            float facingX,
            float facingY,
            float velocityX,
            float velocityY)
        {
            Key = key;
            X = x;
            Y = y;
            FacingX = facingX;
            FacingY = facingY;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        public ShooterViewEntityKey Key { get; }

        public float X { get; }

        public float Y { get; }

        public float FacingX { get; }

        public float FacingY { get; }

        public float VelocityX { get; }

        public float VelocityY { get; }
    }

    public readonly struct ShooterViewHealthComponentChange
    {
        public ShooterViewHealthComponentChange(ShooterViewEntityKey key, int hp)
        {
            Key = key;
            Hp = hp;
        }

        public ShooterViewEntityKey Key { get; }

        public int Hp { get; }
    }

    public readonly struct ShooterViewScoreComponentChange
    {
        public ShooterViewScoreComponentChange(ShooterViewEntityKey key, int score)
        {
            Key = key;
            Score = score;
        }

        public ShooterViewEntityKey Key { get; }

        public int Score { get; }
    }

    public readonly struct ShooterViewProjectileLifetimeComponentChange
    {
        public ShooterViewProjectileLifetimeComponentChange(ShooterViewEntityKey key, int remainingFrames)
        {
            Key = key;
            RemainingFrames = remainingFrames;
        }

        public ShooterViewEntityKey Key { get; }

        public int RemainingFrames { get; }
    }
}
