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
    /// 拉拽位移的Plan Action模块
    /// 将目标拉向释放者或指定位置
    /// </summary>
    [PlanActionModule(order: MobaPlanActionModuleOrders.Pull)]
    public sealed class PullPlanActionModule : MobaPlanActionModuleBase<PullArgs, PullPlanActionModule>
    {
        protected override IActionSchema<PullArgs, IWorldResolver> Schema => PullSchema.Instance;

        protected override void Execute(object triggerArgs, PullArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.Speed <= 0f || args.DurationMs <= 0f)
            {
                LogRejected(ctx, $"requires positive speed and duration. speed={args.Speed} duration={args.DurationMs}");
                return;
            }

            var input = MobaMovementActionInputResolver.Resolve(triggerArgs, ctx);
            var targetId = input.TargetActorId;
            if (targetId <= 0)
            {
                LogRejected(ctx, "requires valid target actor");
                return;
            }

            var registry = input.Actors;
            if (registry == null)
            {
                LogRejected(ctx, "requires MobaActorRegistry service");
                return;
            }

            if (!registry.TryGet(targetId, out var targetEntity) || targetEntity == null || !targetEntity.hasMotion)
            {
                LogRejected(ctx, $"requires target has Motion component. targetId={targetId}");
                return;
            }

            var m = targetEntity.motion;
            if (!m.Initialized || m.Pipeline == null)
            {
                LogRejected(ctx, $"requires target Motion initialized. targetId={targetId}");
                return;
            }

            var pullDir = input.ResolvePullDirection(args.DirectionMode, targetId);
            if (pullDir.SqrMagnitude <= 0f)
            {
                LogRejected(ctx, $"cannot resolve pull direction. mode={args.DirectionMode}");
                return;
            }

            var velocity = pullDir * args.Speed;
            var duration = args.DurationMs / 1000f;
            var source = new FixedDeltaMotionSource(velocity, duration, args.Priority, MotionGroups.Control, MotionStacking.OverrideLowerPriority);

            m.Pipeline.AddSource(source);
        }

    }
}
