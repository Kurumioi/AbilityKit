using System;
using AbilityKit.Combat.MotionSystem.Core;
using AbilityKit.Combat.MotionSystem.Generic;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 冲刺位移的Plan Action模块
    /// 使用 FixedDeltaMotionSource 实现朝指定方向的冲刺
    /// </summary>
    [PlanActionModule(order: MobaPlanActionModuleOrders.Dash)]
    public sealed class DashPlanActionModule : MobaPlanActionModuleBase<DashArgs, DashPlanActionModule>
    {
        protected override IActionSchema<DashArgs, IWorldResolver> Schema => DashSchema.Instance;

        protected override void Execute(object triggerArgs, DashArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.Speed <= 0f || args.DurationMs <= 0f)
            {
                LogRejected(ctx, $"requires positive speed and duration. speed={args.Speed} duration={args.DurationMs}");
                return;
            }

            var input = MobaMovementActionInputResolver.Resolve(triggerArgs, ctx);
            var actorId = input.ResolveActorId(args.ApplyToCaster);

            if (actorId <= 0)
            {
                LogRejected(ctx, $"cannot resolve actor. applyToCaster={args.ApplyToCaster}");
                return;
            }

            var registry = input.Actors;
            if (registry == null)
            {
                LogRejected(ctx, "requires MobaActorRegistry service");
                return;
            }

            if (!registry.TryGet(actorId, out var entity) || entity == null || !entity.hasMotion)
            {
                LogRejected(ctx, $"requires actor has Motion component. actorId={actorId}");
                return;
            }

            var m = entity.motion;
            if (!m.Initialized || m.Pipeline == null)
            {
                LogRejected(ctx, $"requires Motion initialized. actorId={actorId}");
                return;
            }

            var dir = input.ResolveDashOrBlinkDirection(args.DirectionMode, actorId);
            if (dir.SqrMagnitude <= 0f)
            {
                dir = entity.hasTransform ? entity.transform.Value.Forward : Vec3.Forward;
            }

            var velocity = dir * args.Speed;
            var duration = args.DurationMs / 1000f;
            var source = new FixedDeltaMotionSource(velocity, duration, args.Priority, MotionGroups.Control, MotionStacking.OverrideLowerPriority);

            m.Pipeline.AddSource(source);
        }

    }
}
