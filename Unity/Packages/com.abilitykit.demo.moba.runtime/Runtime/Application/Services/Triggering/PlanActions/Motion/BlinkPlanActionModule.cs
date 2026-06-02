using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.MotionSystem.Core;
using AbilityKit.Core.Math;
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
    /// 闪烁位移的Plan Action模块
    /// 瞬间传送到指定方向和距离的位置
    /// </summary>
    [PlanActionModule(order: 14)]
    public sealed class BlinkPlanActionModule : MobaPlanActionModuleBase<BlinkArgs, BlinkPlanActionModule>
    {
        protected override IActionSchema<BlinkArgs, IWorldResolver> Schema => BlinkSchema.Instance;

        protected override void Execute(object triggerArgs, BlinkArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.Distance <= 0f)
            {
                Log.Warning($"[Plan] blink requires positive distance. distance={args.Distance}");
                return;
            }

            var input = MobaMovementActionInputResolver.Resolve(triggerArgs, ctx);
            var actorId = input.ResolveActorId(args.ApplyToCaster);

            if (actorId <= 0)
            {
                Log.Warning($"[Plan] blink cannot resolve actor. applyToCaster={args.ApplyToCaster}");
                return;
            }

            var registry = input.Actors;
            if (registry == null)
            {
                Log.Warning($"[Plan] blink requires MobaActorRegistry service");
                return;
            }

            if (!registry.TryGet(actorId, out var entity) || entity == null || !entity.hasMotion)
            {
                Log.Warning($"[Plan] blink requires actor has Motion component. actorId={actorId}");
                return;
            }

            if (!entity.hasTransform)
            {
                Log.Warning($"[Plan] blink requires actor has Transform component. actorId={actorId}");
                return;
            }

            var m = entity.motion;
            var t = entity.transform.Value;

            var dir = input.ResolveDashOrBlinkDirection(args.DirectionMode, actorId);
            if (dir.SqrMagnitude <= 0f)
            {
                dir = t.Forward;
            }

            var delta = dir * args.Distance;
            var newPos = new Vec3(t.Position.X + delta.X, t.Position.Y + delta.Y, t.Position.Z + delta.Z);

            var state = m.State;
            var newState = new MotionState(in newPos)
            {
                Velocity = state.Velocity,
                Forward = state.Forward,
                Time = state.Time
            };

            entity.ReplaceMotion(
                m.Pipeline,
                newState,
                m.Output,
                m.Solver,
                m.Policy,
                m.Events,
                m.Initialized);

            var newTransform = new Transform3(in newPos, in t.Rotation, in t.Scale);
            entity.ReplaceTransform(newTransform);
        }

    }
}
