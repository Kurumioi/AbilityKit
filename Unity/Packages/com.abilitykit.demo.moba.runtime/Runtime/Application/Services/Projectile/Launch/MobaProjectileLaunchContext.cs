using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    public readonly struct MobaProjectileLaunchContext
    {
        public MobaProjectileLaunchContext(
            in MobaProjectileLaunchRequest request,
            ProjectileLauncherMO launcher,
            ProjectileMO projectile,
            int launcherActorId,
            global::ActorEntity launcherEntity,
            in ProjectileSpawnParams baseSpawn,
            in ProjectileSourceContext launcherSource,
            int startFrame,
            long endTimeMs,
            int intervalFrames,
            int repeatCount,
            int bulletsPerShot,
            float fallbackFanAngleDeg,
            IProjectileService projectiles,
            MobaProjectileLinkService links,
            MobaSkillParamModifierService skillParamModifiers,
            IMobaProjectileLaunchRuntime runtime)
        {
            Request = request;
            Launcher = launcher;
            Projectile = projectile;
            LauncherActorId = launcherActorId;
            LauncherEntity = launcherEntity;
            BaseSpawn = baseSpawn;
            LauncherSource = launcherSource;
            StartFrame = startFrame;
            EndTimeMs = endTimeMs;
            IntervalFrames = intervalFrames;
            RepeatCount = repeatCount;
            BulletsPerShot = bulletsPerShot < 1 ? 1 : bulletsPerShot;
            FallbackFanAngleDeg = fallbackFanAngleDeg < 0f ? 0f : fallbackFanAngleDeg;
            Projectiles = projectiles;
            Links = links;
            SkillParamModifiers = skillParamModifiers;
            Runtime = runtime;
        }

        public MobaProjectileLaunchRequest Request { get; }
        public ProjectileLauncherMO Launcher { get; }
        public ProjectileMO Projectile { get; }
        public int LauncherActorId { get; }
        public global::ActorEntity LauncherEntity { get; }
        public ProjectileSpawnParams BaseSpawn { get; }
        public ProjectileSourceContext LauncherSource { get; }
        public int StartFrame { get; }
        public long EndTimeMs { get; }
        public int IntervalFrames { get; }
        public int RepeatCount { get; }
        public int BulletsPerShot { get; }
        public float FallbackFanAngleDeg { get; }
        public IProjectileService Projectiles { get; }
        public MobaProjectileLinkService Links { get; }
        public MobaSkillParamModifierService SkillParamModifiers { get; }
        public IMobaProjectileLaunchRuntime Runtime { get; }
        public int TotalCount => RepeatCount * BulletsPerShot;
    }
}
