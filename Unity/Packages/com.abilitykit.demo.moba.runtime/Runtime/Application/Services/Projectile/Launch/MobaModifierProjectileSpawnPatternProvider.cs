using AbilityKit.Core.Common.Projectile;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    public sealed class MobaModifierProjectileSpawnPatternProvider : IProjectileSpawnPatternProvider
    {
        private readonly MobaSkillParamModifierService _paramModifiers;
        private readonly int _baseCountPerShot;
        private readonly float _baseFanAngleDeg;
        private readonly IProjectileSpawnPattern _fallbackPattern;

        public MobaModifierProjectileSpawnPatternProvider(
            MobaSkillParamModifierService paramModifiers,
            int baseCountPerShot,
            float baseFanAngleDeg,
            int fallbackCountPerShot,
            float fallbackFanAngleDeg)
        {
            _paramModifiers = paramModifiers;
            _baseCountPerShot = baseCountPerShot < 1 ? 1 : baseCountPerShot;
            _baseFanAngleDeg = baseFanAngleDeg < 0f ? 0f : baseFanAngleDeg;
            _fallbackPattern = CreatePattern(fallbackCountPerShot, fallbackFanAngleDeg);
        }

        public IProjectileSpawnPattern GetPattern(in ProjectileSpawnParams baseSpawn, int frame)
        {
            if (_paramModifiers == null || baseSpawn.RootActorId <= 0)
            {
                return _fallbackPattern;
            }

            var resolveContext = new MobaModifierResolveContext(
                actorId: baseSpawn.RootActorId,
                launcherActorId: baseSpawn.LauncherActorId);
            var countPerShot = _paramModifiers.Projectile.ResolveCountPerShotFromLauncher(
                resolveContext,
                _baseCountPerShot);
            var fanAngleDeg = _paramModifiers.Projectile.ResolveFanAngleDegFromLauncher(
                resolveContext,
                _baseFanAngleDeg);

            return CreatePattern(countPerShot, fanAngleDeg);
        }

        private static IProjectileSpawnPattern CreatePattern(int countPerShot, float fanAngleDeg)
        {
            var bulletsPerShot = System.Math.Max(1, countPerShot);
            var resolvedFanAngleDeg = fanAngleDeg < 0f ? 0f : fanAngleDeg;

            if (bulletsPerShot <= 1)
            {
                return new SingleShotPattern();
            }

            return resolvedFanAngleDeg > 0f
                ? (IProjectileSpawnPattern)new FanPattern(bulletsPerShot, resolvedFanAngleDeg)
                : new BurstPattern(bulletsPerShot);
        }
    }
}
