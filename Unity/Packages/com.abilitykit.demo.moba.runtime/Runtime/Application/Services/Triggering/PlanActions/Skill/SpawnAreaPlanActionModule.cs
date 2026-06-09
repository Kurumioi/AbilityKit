using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 24)]
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
            var lifetimeFrames = ResolveLifetimeFrames(args, aoe.DelayMs, ctx.Context);
            var collisionLayerMask = args.CollisionLayerMaskOverride != 0 ? args.CollisionLayerMaskOverride : aoe.CollisionLayerMask;
            var stayIntervalFrames = Math.Max(0, args.StayIntervalFrames);
            var frame = ResolveFrame(ctx.Context);

            var spawnParams = new AreaSpawnParams(input.CasterActorId, in center, radius, lifetimeFrames, collisionLayerMask, stayIntervalFrames);
            var areaId = projectiles.SpawnArea(in spawnParams, frame);
            if (areaId.Value <= 0)
            {
                LogRejected(ctx, $"spawn failed. areaId={args.AreaId} caster={input.CasterActorId}");
                return;
            }

            if (ctx.Context.TryResolve<MobaAreaRuntimeService>(out var areaRuntime) && areaRuntime != null)
            {
                var origin = input.BuildOrigin(input.CasterActorId, input.TargetActorId, MobaTraceKind.AreaSpawn, args.AreaId);
                if (origin.EffectiveParentContextId == 0L)
                {
                    throw new InvalidOperationException($"SpawnArea requires source context. areaId={args.AreaId} caster={input.CasterActorId}");
                }

                areaRuntime.RegisterSpawn(
                    areaId,
                    args.AreaId,
                    input.CasterActorId,
                    in center,
                    radius,
                    collisionLayerMask,
                    aoe.MaxTargets,
                    frame,
                    origin.EffectiveParentContextId,
                    origin.EffectiveRootContextId,
                    origin.OwnerContextId);
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
