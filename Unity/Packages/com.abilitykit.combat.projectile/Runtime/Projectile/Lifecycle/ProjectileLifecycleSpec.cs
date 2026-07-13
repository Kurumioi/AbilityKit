using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.Projectile
{
    public enum ProjectileLifecycleState
    {
        Flying = 0,
        Preparing = 1,
        Holding = 2,
        Finished = 3,
    }

    public enum ProjectilePrepareMotionMode
    {
        None = 0,
        MoveToRelativeOffset = 1,
    }

    public readonly struct ProjectileLifecycleSpec
    {
        public readonly ProjectilePrepareMotionMode PrepareMotionMode;
        public readonly int PrepareFrames;
        public readonly int HoldFrames;
        public readonly Vec3 PrepareOffset;
        public readonly float PrepareSlotSpacing;
        public readonly bool ConsumeLifetimeBeforeFlying;
        public readonly bool ArmedBeforeFlying;

        public ProjectileLifecycleSpec(
            ProjectilePrepareMotionMode prepareMotionMode,
            int prepareFrames,
            int holdFrames,
            in Vec3 prepareOffset,
            float prepareSlotSpacing,
            bool consumeLifetimeBeforeFlying,
            bool armedBeforeFlying)
        {
            PrepareMotionMode = prepareMotionMode;
            PrepareFrames = prepareFrames > 0 ? prepareFrames : 0;
            HoldFrames = holdFrames > 0 ? holdFrames : 0;
            PrepareOffset = prepareOffset;
            PrepareSlotSpacing = prepareSlotSpacing;
            ConsumeLifetimeBeforeFlying = consumeLifetimeBeforeFlying;
            ArmedBeforeFlying = armedBeforeFlying;
        }

        public bool HasPreFlight => PrepareMotionMode != ProjectilePrepareMotionMode.None && (PrepareFrames > 0 || HoldFrames > 0);

        public ProjectileLifecycleSpec WithPrepareOffset(in Vec3 prepareOffset)
        {
            return new ProjectileLifecycleSpec(
                PrepareMotionMode,
                PrepareFrames,
                HoldFrames,
                in prepareOffset,
                PrepareSlotSpacing,
                ConsumeLifetimeBeforeFlying,
                ArmedBeforeFlying);
        }

        public static ProjectileLifecycleSpec Default => new ProjectileLifecycleSpec(
            ProjectilePrepareMotionMode.None,
            0,
            0,
            Vec3.Zero,
            0f,
            true,
            true);
    }
}
