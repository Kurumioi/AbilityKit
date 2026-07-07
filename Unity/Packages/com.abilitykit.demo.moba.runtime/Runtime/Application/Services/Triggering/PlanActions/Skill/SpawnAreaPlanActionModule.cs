using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.DI;
using AbilityKit.Combat.Projectile;
using AbilityKit.Trace;
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
            LogInvestigation(ctx,
                $"begin areaId={args.AreaId} positionMode={(SpawnSummonPositionMode)args.PositionMode} radiusOverride={args.RadiusOverride:0.###} durationMs={args.DurationMs} durationFrames={args.DurationFrames}");

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
            LogInvestigation(ctx,
                $"resolved input caster={input.CasterActorId} hasCaster={input.HasCasterActor} target={input.TargetActorId} hasTarget={input.HasTargetActor} hasTraceScope={input.HasTraceScope} hasAimPos={input.HasAimPosition} hasAimDir={input.HasAimDirection}");
            if (!input.HasCasterActor)
            {
                LogRejected(ctx, "requires caster actor.");
                return;
            }

            var positionMode = (SpawnSummonPositionMode)args.PositionMode;
            if (!input.TryResolveSpawnPosition(positionMode, out var center))
            {
                AbilityKit.Core.Logging.Log.Warning($"[SpawnAreaPlanActionModule] rejected resolve spawn position areaId={args.AreaId} mode={positionMode} caster={input.CasterActorId} target={input.TargetActorId} hasAimPos={input.HasAimPosition} hasAimDir={input.HasAimDirection} aimPos=({input.AimPosition.X:0.###},{input.AimPosition.Y:0.###},{input.AimPosition.Z:0.###}) aimDir=({input.AimDirection.X:0.###},{input.AimDirection.Y:0.###},{input.AimDirection.Z:0.###})");
                LogRejected(ctx, $"cannot resolve spawn position. mode={positionMode} caster={input.CasterActorId} target={input.TargetActorId} hasAimPos={input.HasAimPosition} hasAimDir={input.HasAimDirection}");
                return;
            }

            center = center + new Vec3(aoe.OffsetX + args.OffsetX, aoe.OffsetY + args.OffsetY, aoe.OffsetZ + args.OffsetZ);
            var radius = args.RadiusOverride > 0f ? args.RadiusOverride : aoe.Radius;
            var lifetimeFrames = ResolveLifetimeFrames(args, aoe.DurationMs, ctx.Context);
            var collisionLayerMask = args.CollisionLayerMaskOverride != 0 ? args.CollisionLayerMaskOverride : aoe.CollisionLayerMask;
            var stayIntervalFrames = ResolveStayIntervalFrames(args, aoe.IntervalMs, ctx.Context);
            var delayFrames = ResolveDelayFrames(aoe.DelayMs, ctx.Context);
            var frame = ResolveFrame(ctx.Context);
            LogInvestigation(ctx,
                $"resolved area params center=({center.X:0.###},{center.Y:0.###},{center.Z:0.###}) radius={radius:0.###} lifetimeFrames={lifetimeFrames} stayIntervalFrames={stayIntervalFrames} delayFrames={delayFrames} collisionMask={collisionLayerMask} frame={frame}");

            ctx.Context.TryResolve<MobaAreaRuntimeService>(out var areaRuntime);
            ctx.Context.TryResolve<MobaTraceRegistry>(out var trace);
            var sourceContextId = 0L;
            var rootContextId = 0L;
            var ownerContextId = 0L;
            if (areaRuntime != null)
            {
                var origin = input.BuildOrigin(input.CasterActorId, input.TargetActorId, MobaTraceKind.AreaSpawn, args.AreaId);
                LogInvestigation(ctx,
                    $"resolved origin immediate={origin.ImmediateContextId} parent={origin.EffectiveParentContextId} root={origin.EffectiveRootContextId} owner={origin.OwnerContextId}");
                if (origin.EffectiveParentContextId == 0L)
                {
                    AbilityKit.Core.Logging.Log.Warning($"[SpawnAreaPlanActionModule] rejected missing source context areaId={args.AreaId} caster={input.CasterActorId} target={input.TargetActorId} immediate={origin.ImmediateContextId} parent={origin.EffectiveParentContextId} root={origin.EffectiveRootContextId} owner={origin.OwnerContextId}");
                    LogRejected(ctx, $"requires source context. areaId={args.AreaId} caster={input.CasterActorId} target={input.TargetActorId}");
                    return;
                }

                sourceContextId = trace != null
                    ? trace.CreateChildContext(
                        origin.EffectiveParentContextId,
                        MobaTraceKind.AreaSpawn,
                        args.AreaId,
                        input.CasterActorId,
                        input.TargetActorId,
                        TraceEndpoint.Config(MobaRuntimeKindNames.Area, args.AreaId),
                        TraceEndpoint.Actor(input.TargetActorId))
                    : 0L;
                if (sourceContextId == 0L)
                {
                    sourceContextId = origin.ImmediateContextId != 0L
                        ? origin.ImmediateContextId
                        : origin.EffectiveParentContextId;
                }

                rootContextId = origin.EffectiveRootContextId != 0L
                    ? origin.EffectiveRootContextId
                    : sourceContextId;
                ownerContextId = origin.OwnerContextId != 0L
                    ? origin.OwnerContextId
                    : sourceContextId;

                LogInvestigation(ctx,
                    $"resolved area trace source={sourceContextId} parent={origin.EffectiveParentContextId} root={rootContextId} owner={ownerContextId}");
            }
            else
            {
                LogInvestigation(ctx, "area runtime not available; continuing without trace registration.");
            }

            var spawnParams = new AreaSpawnParams(input.CasterActorId, in center, radius, lifetimeFrames, collisionLayerMask, stayIntervalFrames);
            var areaId = projectiles.SpawnArea(in spawnParams, frame);
            if (areaId.Value <= 0)
            {
                AbilityKit.Core.Logging.Log.Warning($"[SpawnAreaPlanActionModule] rejected spawn failed areaId={args.AreaId} caster={input.CasterActorId} center=({center.X:0.###},{center.Y:0.###},{center.Z:0.###}) radius={radius:0.###} lifetimeFrames={lifetimeFrames} collisionMask={collisionLayerMask} frame={frame}");
                LogRejected(ctx, $"spawn failed. areaId={args.AreaId} caster={input.CasterActorId} center=({center.X:0.###},{center.Y:0.###},{center.Z:0.###}) radius={radius:0.###} lifetimeFrames={lifetimeFrames}");
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
                    delayFrames,
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

        private static int ResolveDelayFrames(int configDelayMs, IWorldResolver services)
        {
            if (configDelayMs <= 0) return 0;

            var frameTime = ResolveFrameTime(services);
            var seconds = configDelayMs / 1000f;
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
