using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Core.Math;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 发射投射物的Plan Action模块
    /// 使用强类型参�?Schema API
    /// </summary>
    [PlanActionModule(order: 10)]
    public sealed class ShootProjectilePlanActionModule : MobaPlanActionModuleBase<ShootProjectileArgs, ShootProjectilePlanActionModule>
    {
        protected override IActionSchema<ShootProjectileArgs, IWorldResolver> Schema => ShootProjectileSchema.Instance;

        protected override void Execute(object triggerArgs, ShootProjectileArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<MobaProjectileService>(out var projectileSvc) || projectileSvc == null) return;
            if (!ctx.Context.TryResolve<MobaConfigDatabase>(out var configs) || configs == null) return;

            var input = MobaPlanActionInputResolver.ResolveProjectile(triggerArgs, ctx);
            if (!input.HasCasterActor) return;

            var casterActorId = input.CasterActorId;
            var launcherId = args.LauncherId;
            var projectileId = args.ProjectileId;
            var paramResolver = ctx.Context.TryResolve<MobaSkillParamModifierService>(out var resolvedParamResolver)
                ? resolvedParamResolver
                : null;

            if (paramResolver != null)
            {
                launcherId = paramResolver.Projectile.ResolveLauncherId(casterActorId, launcherId);
                projectileId = paramResolver.Projectile.ResolveProjectileId(casterActorId, projectileId);
            }

            if (launcherId <= 0 || projectileId <= 0)
            {
                Log.Warning($"[Plan] shoot_projectile invalid args. launcherId={launcherId} projectileId={projectileId}");
                return;
            }

            var aimPos = input.HasAimPosition ? input.AimPosition : Vec3.Zero;
            var aimDir = input.HasAimDirection ? input.AimDirection : Vec3.Zero;

            ProjectileLauncherMO launcher = null;
            ProjectileMO projectile = null;
            if (!configs.TryGetProjectileLauncher(launcherId, out launcher)) return;
            if (!configs.TryGetProjectile(projectileId, out projectile)) return;
            if (launcher == null || projectile == null) return;

            var countPerShot = paramResolver != null
                ? paramResolver.Projectile.ResolveCountPerShot(casterActorId, launcher.CountPerShot)
                : launcher.CountPerShot;
            var fanAngleDeg = paramResolver != null
                ? paramResolver.Projectile.ResolveFanAngleDeg(casterActorId, launcher.FanAngleDeg)
                : launcher.FanAngleDeg;
            var launchParams = new MobaResolvedShootProjectileParams(launcherId, projectileId, countPerShot, fanAngleDeg);

            var casterPos = Vec3.Zero;
            if (ctx.Context.TryResolve<MobaActorRegistry>(out var actorRegistry)
                && actorRegistry != null
                && actorRegistry.TryGet(casterActorId, out var casterEntity)
                && casterEntity != null
                && casterEntity.hasTransform)
            {
                casterPos = casterEntity.transform.Value.Position;
            }

            if (!aimPos.Equals(Vec3.Zero))
            {
                var sqr = aimPos.SqrMagnitude;
                if (sqr > 1000f * 1000f)
                {
                    Log.Warning($"[Plan] shoot_projectile aimPos looks like world-space (will be treated as offset). casterActorId={casterActorId} aimPos={aimPos}");
                }
            }

            if (!aimPos.Equals(Vec3.Zero)) aimPos = casterPos + aimPos;
            if (!aimDir.Equals(Vec3.Zero)) aimDir = aimDir.Normalized;

            var sourceContext = input.CreateSourceContext(casterActorId, 0, projectile.Id);
            if (!projectileSvc.Launch(casterActorId, launcher, projectile, launchParams.CountPerShot, launchParams.FanAngleDeg, in aimPos, in aimDir, in sourceContext))
            {
                Log.Warning($"[Plan] shoot_projectile launch failed. launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId}");
            }
        }
    }
}
