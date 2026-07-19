using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.Projectile
{
    public enum ProjectileHitPolicyKind
    {
        ExitOnHit = 0,
        Pierce = 1,
    }

    public readonly struct ProjectileSpawnParams
    {
        public readonly int OwnerId;
        public readonly int TemplateId;
        public readonly int LauncherActorId;
        public readonly int RootActorId;
        public readonly int SpawnFrame;

        public readonly Vec3 Position;
        public readonly Vec3 Direction;
        public readonly float Speed;
        public readonly int TrackingTargetActorId;

        public readonly int ReturnAfterFrames;
        public readonly float ReturnSpeed;
        public readonly float ReturnStopDistance;

        public readonly int LifetimeFrames;
        public readonly float MaxDistance;

        public readonly int CollisionLayerMask;
        public readonly ColliderId IgnoreCollider;
        public readonly Vec3 CollisionHalfExtents;

        public readonly IProjectileHitPolicy HitPolicy;
        public readonly int HitsRemaining;

        public readonly ProjectileHitPolicyKind HitPolicyKind;
        public readonly int HitPolicyParam;

        public readonly int TickIntervalFrames;

        public readonly IProjectileHitFilter HitFilter;
        public readonly int HitCooldownFrames;

        public readonly ProjectileLifecycleSpec Lifecycle;
        public readonly int PatternSlotIndex;
        public readonly int PatternSlotCount;

        public ProjectileSpawnParams(
            int ownerId,
            int templateId,
            int launcherActorId,
            int rootActorId,
            int spawnFrame,
            in Vec3 position,
            in Vec3 direction,
            float speed,
            int returnAfterFrames,
            float returnSpeed,
            float returnStopDistance,
            int lifetimeFrames,
            float maxDistance,
            int collisionLayerMask,
            ColliderId ignoreCollider,
            IProjectileHitPolicy hitPolicy = null,
            int hitsRemaining = 1,
            ProjectileHitPolicyKind hitPolicyKind = ProjectileHitPolicyKind.ExitOnHit,
            int hitPolicyParam = 0,
            int tickIntervalFrames = 0,
            IProjectileHitFilter hitFilter = null,
            int hitCooldownFrames = 0,
            ProjectileLifecycleSpec lifecycle = default,
            int patternSlotIndex = 0,
            int patternSlotCount = 1,
            int trackingTargetActorId = 0,
            Vec3 collisionHalfExtents = default)
        {
            OwnerId = ownerId;
            TemplateId = templateId;
            LauncherActorId = launcherActorId;
            RootActorId = rootActorId;
            SpawnFrame = spawnFrame;
            Position = position;
            Direction = direction;
            Speed = speed;
            TrackingTargetActorId = trackingTargetActorId;
            ReturnAfterFrames = returnAfterFrames;
            ReturnSpeed = returnSpeed;
            ReturnStopDistance = returnStopDistance;
            LifetimeFrames = lifetimeFrames;
            MaxDistance = maxDistance;
            CollisionLayerMask = collisionLayerMask;
            IgnoreCollider = ignoreCollider;
            CollisionHalfExtents = new Vec3(
                MathUtil.Max(0f, collisionHalfExtents.X),
                MathUtil.Max(0f, collisionHalfExtents.Y),
                MathUtil.Max(0f, collisionHalfExtents.Z));

            HitPolicy = hitPolicy;
            HitsRemaining = hitsRemaining;

            HitPolicyKind = hitPolicyKind;
            HitPolicyParam = hitPolicyParam;

            TickIntervalFrames = tickIntervalFrames;

            HitFilter = hitFilter;
            HitCooldownFrames = hitCooldownFrames;

            Lifecycle = lifecycle;
            PatternSlotIndex = patternSlotIndex < 0 ? 0 : patternSlotIndex;
            PatternSlotCount = patternSlotCount <= 0 ? 1 : patternSlotCount;
        }

        public ProjectileSpawnParams WithDirection(in Vec3 direction)
        {
            return new ProjectileSpawnParams(
                ownerId: OwnerId,
                templateId: TemplateId,
                launcherActorId: LauncherActorId,
                rootActorId: RootActorId,
                spawnFrame: SpawnFrame,
                position: Position,
                direction: direction,
                speed: Speed,
                returnAfterFrames: ReturnAfterFrames,
                returnSpeed: ReturnSpeed,
                returnStopDistance: ReturnStopDistance,
                lifetimeFrames: LifetimeFrames,
                maxDistance: MaxDistance,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: IgnoreCollider,
                hitPolicy: HitPolicy,
                hitsRemaining: HitsRemaining,
                hitPolicyKind: HitPolicyKind,
                hitPolicyParam: HitPolicyParam,
                tickIntervalFrames: TickIntervalFrames,
                hitFilter: HitFilter,
                hitCooldownFrames: HitCooldownFrames,
                lifecycle: Lifecycle,
                patternSlotIndex: PatternSlotIndex,
                patternSlotCount: PatternSlotCount,
                trackingTargetActorId: TrackingTargetActorId,
                collisionHalfExtents: CollisionHalfExtents);
        }

        public ProjectileSpawnParams WithPosition(in Vec3 position)
        {
            return new ProjectileSpawnParams(
                ownerId: OwnerId,
                templateId: TemplateId,
                launcherActorId: LauncherActorId,
                rootActorId: RootActorId,
                spawnFrame: SpawnFrame,
                position: position,
                direction: Direction,
                speed: Speed,
                returnAfterFrames: ReturnAfterFrames,
                returnSpeed: ReturnSpeed,
                returnStopDistance: ReturnStopDistance,
                lifetimeFrames: LifetimeFrames,
                maxDistance: MaxDistance,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: IgnoreCollider,
                hitPolicy: HitPolicy,
                hitsRemaining: HitsRemaining,
                hitPolicyKind: HitPolicyKind,
                hitPolicyParam: HitPolicyParam,
                tickIntervalFrames: TickIntervalFrames,
                hitFilter: HitFilter,
                hitCooldownFrames: HitCooldownFrames,
                lifecycle: Lifecycle,
                patternSlotIndex: PatternSlotIndex,
                patternSlotCount: PatternSlotCount,
                trackingTargetActorId: TrackingTargetActorId,
                collisionHalfExtents: CollisionHalfExtents);
        }

        public ProjectileSpawnParams WithLifecycle(in ProjectileLifecycleSpec lifecycle)
        {
            return new ProjectileSpawnParams(
                ownerId: OwnerId,
                templateId: TemplateId,
                launcherActorId: LauncherActorId,
                rootActorId: RootActorId,
                spawnFrame: SpawnFrame,
                position: Position,
                direction: Direction,
                speed: Speed,
                returnAfterFrames: ReturnAfterFrames,
                returnSpeed: ReturnSpeed,
                returnStopDistance: ReturnStopDistance,
                lifetimeFrames: LifetimeFrames,
                maxDistance: MaxDistance,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: IgnoreCollider,
                hitPolicy: HitPolicy,
                hitsRemaining: HitsRemaining,
                hitPolicyKind: HitPolicyKind,
                hitPolicyParam: HitPolicyParam,
                tickIntervalFrames: TickIntervalFrames,
                hitFilter: HitFilter,
                hitCooldownFrames: HitCooldownFrames,
                lifecycle: lifecycle,
                patternSlotIndex: PatternSlotIndex,
                patternSlotCount: PatternSlotCount,
                trackingTargetActorId: TrackingTargetActorId,
                collisionHalfExtents: CollisionHalfExtents);
        }

        public ProjectileSpawnParams WithPatternSlot(int slotIndex, int slotCount)
        {
            return new ProjectileSpawnParams(
                ownerId: OwnerId,
                templateId: TemplateId,
                launcherActorId: LauncherActorId,
                rootActorId: RootActorId,
                spawnFrame: SpawnFrame,
                position: Position,
                direction: Direction,
                speed: Speed,
                returnAfterFrames: ReturnAfterFrames,
                returnSpeed: ReturnSpeed,
                returnStopDistance: ReturnStopDistance,
                lifetimeFrames: LifetimeFrames,
                maxDistance: MaxDistance,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: IgnoreCollider,
                hitPolicy: HitPolicy,
                hitsRemaining: HitsRemaining,
                hitPolicyKind: HitPolicyKind,
                hitPolicyParam: HitPolicyParam,
                tickIntervalFrames: TickIntervalFrames,
                hitFilter: HitFilter,
                hitCooldownFrames: HitCooldownFrames,
                lifecycle: Lifecycle,
                patternSlotIndex: slotIndex,
                patternSlotCount: slotCount,
                trackingTargetActorId: TrackingTargetActorId,
                collisionHalfExtents: CollisionHalfExtents);
        }

        public ProjectileSpawnParams WithSpawnFrame(int spawnFrame)
        {
            return new ProjectileSpawnParams(
                ownerId: OwnerId,
                templateId: TemplateId,
                launcherActorId: LauncherActorId,
                rootActorId: RootActorId,
                spawnFrame: spawnFrame,
                position: Position,
                direction: Direction,
                speed: Speed,
                returnAfterFrames: ReturnAfterFrames,
                returnSpeed: ReturnSpeed,
                returnStopDistance: ReturnStopDistance,
                lifetimeFrames: LifetimeFrames,
                maxDistance: MaxDistance,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: IgnoreCollider,
                hitPolicy: HitPolicy,
                hitsRemaining: HitsRemaining,
                hitPolicyKind: HitPolicyKind,
                hitPolicyParam: HitPolicyParam,
                tickIntervalFrames: TickIntervalFrames,
                hitFilter: HitFilter,
                hitCooldownFrames: HitCooldownFrames,
                lifecycle: Lifecycle,
                patternSlotIndex: PatternSlotIndex,
                patternSlotCount: PatternSlotCount,
                trackingTargetActorId: TrackingTargetActorId,
                collisionHalfExtents: CollisionHalfExtents);
        }
    }
}
