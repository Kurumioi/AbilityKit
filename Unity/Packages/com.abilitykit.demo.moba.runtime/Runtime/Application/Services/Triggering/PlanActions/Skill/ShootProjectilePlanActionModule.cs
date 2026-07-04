using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 发射投射物的 Plan Action 模块，使用强类型参数 Schema API。
    /// </summary>
    [PlanActionModule(order: MobaPlanActionModuleOrders.ShootProjectile)]
    public sealed class ShootProjectilePlanActionModule : MobaPlanActionModuleBase<ShootProjectileArgs, ShootProjectilePlanActionModule>
    {
        protected override IActionSchema<ShootProjectileArgs, IWorldResolver> Schema => ShootProjectileSchema.Instance;

        protected override void Execute(object triggerArgs, ShootProjectileArgs args, ExecCtx<IWorldResolver> ctx)
        {
            Log.Warning($"[ShootProjectilePlanActionModule] entered triggerArgsType={triggerArgs?.GetType().Name ?? "<null>"} rawLauncherId={args.LauncherId} rawProjectileId={args.ProjectileId} continuousProcessId={args.ContinuousProcessId}");
            LogInvestigation(ctx, $"entered. triggerArgsType={triggerArgs?.GetType().Name ?? "<null>"} rawLauncherId={args.LauncherId} rawProjectileId={args.ProjectileId} continuousProcessId={args.ContinuousProcessId}");

            if (!ctx.Context.TryResolve<MobaProjectileService>(out var projectileSvc) || projectileSvc == null)
            {
                LogRejected(ctx, "cannot resolve MobaProjectileService.");
                return;
            }

            if (!ctx.Context.TryResolve<MobaConfigDatabase>(out var configs) || configs == null)
            {
                LogRejected(ctx, "cannot resolve MobaConfigDatabase.");
                return;
            }

            var input = MobaPlanActionInputResolver.ResolveProjectile(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                LogRejected(ctx, "caster actor not found");
                return;
            }

            var casterActorId = input.CasterActorId;
            var launcherId = args.LauncherId;
            var projectileId = args.ProjectileId;
            var paramResolver = ctx.Context.TryResolve<MobaSkillParamModifierService>(out var resolvedParamResolver)
                ? resolvedParamResolver
                : null;

            if (paramResolver != null)
            {
                var resolvedLauncherId = paramResolver.Projectile.ResolveLauncherId(casterActorId, launcherId);
                var resolvedProjectileId = paramResolver.Projectile.ResolveProjectileId(casterActorId, projectileId);
                Log.Warning($"[ShootProjectilePlanActionModule] resolved projectile params casterActorId={casterActorId} rawLauncherId={launcherId} resolvedLauncherId={resolvedLauncherId} rawProjectileId={projectileId} resolvedProjectileId={resolvedProjectileId}");
                LogInvestigation(ctx, $"resolved projectile params. casterActorId={casterActorId} rawLauncherId={launcherId} resolvedLauncherId={resolvedLauncherId} rawProjectileId={projectileId} resolvedProjectileId={resolvedProjectileId}");
                launcherId = resolvedLauncherId;
                projectileId = resolvedProjectileId;
            }
            else
            {
                Log.Warning($"[ShootProjectilePlanActionModule] no param resolver. casterActorId={casterActorId} rawLauncherId={launcherId} rawProjectileId={projectileId}");
                LogInvestigation(ctx, $"no param resolver. casterActorId={casterActorId} rawLauncherId={launcherId} rawProjectileId={projectileId}");
            }

            if (launcherId <= 0 || projectileId <= 0)
            {
                LogRejected(ctx, $"invalid args. launcherId={launcherId} projectileId={projectileId}");
                return;
            }

            var aimPos = input.HasAimPosition ? input.AimPosition : Vec3.Zero;
            var aimDir = input.HasAimDirection ? input.AimDirection : Vec3.Zero;

            ProjectileLauncherMO launcher = null;
            ProjectileMO projectile = null;
            if (!configs.TryGetProjectileLauncher(launcherId, out launcher) || launcher == null)
            {
                LogRejected(ctx, $"launcher config not found. launcherId={launcherId}");
                return;
            }

            if (!configs.TryGetProjectile(projectileId, out projectile) || projectile == null)
            {
                LogRejected(ctx, $"projectile config not found. projectileId={projectileId}");
                return;
            }

            var countPerShot = paramResolver != null
                ? paramResolver.Projectile.ResolveCountPerShot(casterActorId, launcher.CountPerShot)
                : launcher.CountPerShot;
            var fanAngleDeg = paramResolver != null
                ? paramResolver.Projectile.ResolveFanAngleDeg(casterActorId, launcher.FanAngleDeg)
                : launcher.FanAngleDeg;
            var durationMs = paramResolver != null
                ? paramResolver.Projectile.ResolveDurationMs(casterActorId, launcher.DurationMs)
                : launcher.DurationMs;
            MobaResolvedShootProjectileParams launchParams;
            try
            {
                launchParams = new MobaResolvedShootProjectileParams(launcherId, projectileId, countPerShot, fanAngleDeg, durationMs);
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                LogRejected(ctx, ex.Message);
                return;
            }
 
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
                    LogInvestigation(ctx, $"aimPos looks like world-space (will be treated as offset). casterActorId={casterActorId} aimPos={aimPos}");
                }
            }

            if (!aimPos.Equals(Vec3.Zero)) aimPos = casterPos + aimPos;
            if (!aimDir.Equals(Vec3.Zero)) aimDir = aimDir.Normalized;

            Log.Warning($"[ShootProjectilePlanActionModule] launching casterActorId={casterActorId} launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId} countPerShot={launchParams.CountPerShot} fanAngleDeg={launchParams.FanAngleDeg:0.###} durationMs={launchParams.DurationMs} continuousProcessId={args.ContinuousProcessId} hasAimPos={input.HasAimPosition} hasAimDir={input.HasAimDirection} aimPos={aimPos} aimDir={aimDir}");
            var sourceContext = input.CreateSourceContext(casterActorId, 0, projectile.Id);
            if (!projectileSvc.Launch(casterActorId, launcher, projectile, launchParams.CountPerShot, launchParams.FanAngleDeg, launchParams.DurationMs, args.ContinuousProcessId, in aimPos, in aimDir, in sourceContext))
            {
                Log.Warning($"[ShootProjectilePlanActionModule] launch failed casterActorId={casterActorId} launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId}");
                LogRejected(ctx, $"launch failed. launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId}");
                return;
            }

            Log.Warning($"[ShootProjectilePlanActionModule] launch succeeded casterActorId={casterActorId} launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId}");
            LogApplied(ctx, $"launch requested. casterActorId={casterActorId} launcherId={launchParams.LauncherId} projectileId={launchParams.ProjectileId} countPerShot={launchParams.CountPerShot} fanAngleDeg={launchParams.FanAngleDeg:0.###} durationMs={launchParams.DurationMs} continuousProcessId={args.ContinuousProcessId} hasAimPos={input.HasAimPosition} hasAimDir={input.HasAimDirection}");
        }
    }
}
