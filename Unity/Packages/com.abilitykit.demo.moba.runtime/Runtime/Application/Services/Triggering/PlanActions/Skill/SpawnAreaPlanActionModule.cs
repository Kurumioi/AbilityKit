using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.DI;
using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: MobaPlanActionModuleOrders.SpawnArea)]
    public sealed class SpawnAreaPlanActionModule : MobaPlanActionModuleBase<SpawnAreaArgs, SpawnAreaPlanActionModule>
    {
        protected override IActionSchema<SpawnAreaArgs, IWorldResolver> Schema => SpawnAreaSchema.Instance;

        protected override void Execute(object triggerArgs, SpawnAreaArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<IProjectileService>(out var projectiles) || projectiles == null)
            {
                LogRejected(ctx, "cannot resolve IProjectileService.");
                return;
            }

            if (!ctx.Context.TryResolve<MobaConfigDatabase>(out var configs) || configs == null || !configs.TryGetAoe(args.AreaId, out var aoe) || aoe == null)
            {
                LogRejected(ctx, $"cannot resolve area config. areaId={args.AreaId}");
                return;
            }

            var input = MobaPlanActionInputResolver.ResolveSummon(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                LogRejected(ctx, "requires caster actor.");
                return;
            }

            var positionMode = (SpawnSummonPositionMode)args.PositionMode;
            if (!input.TryResolveSpawnPosition(positionMode, out var center))
            {
                LogRejected(ctx, $"cannot resolve spawn position. mode={positionMode}");
                return;
            }

            center = center + new Vec3(aoe.OffsetX + args.OffsetX, aoe.OffsetY + args.OffsetY, aoe.OffsetZ + args.OffsetZ);
            var radius = args.RadiusOverride > 0f ? args.RadiusOverride : aoe.Radius;
            var lifetimeFrames = ResolveLifetimeFrames(args, aoe.DurationMs, ctx.Context);
            var collisionLayerMask = args.CollisionLayerMaskOverride != 0 ? args.CollisionLayerMaskOverride : aoe.CollisionLayerMask;
            var stayIntervalFrames = ResolveStayIntervalFrames(args, aoe.IntervalMs, ctx.Context);
            var frame = ResolveFrame(ctx.Context);

            ctx.Context.TryResolve<MobaAreaRuntimeService>(out var areaRuntime);
            var sourceContextId = 0L;
            var rootContextId = 0L;
            var ownerContextId = 0L;
            if (areaRuntime != null)
            {
                var origin = input.BuildOrigin(input.CasterActorId, input.TargetActorId, MobaTraceKind.AreaSpawn, args.AreaId);
                if (origin.EffectiveParentContextId == 0L)
                {
                    LogRejected(ctx, $"requires source context. areaId={args.AreaId} caster={input.CasterActorId}");
                    return;
                }

                sourceContextId = origin.EffectiveParentContextId;
                rootContextId = origin.EffectiveRootContextId;
                ownerContextId = origin.OwnerContextId;
            }

            var spawnParams = new AreaSpawnParams(input.CasterActorId, in center, radius, lifetimeFrames, collisionLayerMask, stayIntervalFrames);
            var areaId = projectiles.SpawnArea(in spawnParams, frame);
            if (areaId.Value <= 0)
            {
                LogRejected(ctx, $"spawn failed. areaId={args.AreaId} caster={input.CasterActorId}");
                return;
            }

            if (areaRuntime != null)
            {
                areaRuntime.RegisterSpawn(
                    areaId,
                    args.AreaId,
                    input.CasterActorId,
                    in center,
                    radius,
                    collisionLayerMask,
                    aoe.MaxTargets,
                    frame,
                    sourceContextId,
                    rootContextId,
                    ownerContextId);
            }

            LogApplied(ctx, $"templateId={args.AreaId} runtimeId={areaId.Value} caster={input.CasterActorId} radius={radius} lifetimeFrames={lifetimeFrames}");
        }

        private static int ResolveLifetimeFrames(SpawnAreaArgs args, int configDurationMs, IWorldResolver services)
        {
            if (args.DurationFrames > 0) return args.DurationFrames;

            var durationMs = args.DurationMs > 0 ? args.DurationMs : configDurationMs;
            if (durationMs <= 0)
            {
                throw new InvalidOperationException($"SpawnArea requires a positive duration. areaId={args.AreaId}");
            }

            var frameTime = ResolveFrameTime(services);
            var seconds = durationMs / 1000f;
            var now = frameTime.Frame.Value;
            return Math.Max(1, frameTime.TimeToFrame(frameTime.Time + seconds).Value - now);
        }

        private static int ResolveStayIntervalFrames(SpawnAreaArgs args, int configIntervalMs, IWorldResolver services)
        {
            if (args.StayIntervalFrames > 0) return args.StayIntervalFrames;
            if (configIntervalMs <= 0) return 0;

            var frameTime = ResolveFrameTime(services);
            var seconds = configIntervalMs / 1000f;
            var now = frameTime.Frame.Value;
            return Math.Max(1, frameTime.TimeToFrame(frameTime.Time + seconds).Value - now);
        }

        private static int ResolveFrame(IWorldResolver services)
        {
            return ResolveFrameTime(services).Frame.Value;
        }

        private static IFrameTime ResolveFrameTime(IWorldResolver services)
        {
            if (services != null && services.TryResolve<IFrameTime>(out var frameTime) && frameTime != null)
            {
                return frameTime;
            }

            throw new InvalidOperationException("SpawnArea requires IFrameTime for deterministic frame resolution.");
        }
    }
}
