using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Math;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Resolves only core action execution facts. New action-specific requirements should be
    /// modeled by args, payload providers, or specialized action input resolvers.
    /// </summary>
    internal static class MobaPlanActionInputResolver
    {
        public static bool TryResolve(object triggerArgs, ExecCtx<IWorldResolver> ctx, out MobaPlanActionInput input)
        {
            if (!MobaPlanActionExecutionContextResolver.TryResolve(triggerArgs, ctx, out var executionContext))
            {
                input = default;
                return false;
            }

            input = Create(triggerArgs, ctx, in executionContext);
            return true;
        }

        public static MobaPlanActionInput Resolve(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            if (TryResolve(triggerArgs, ctx, out var input))
            {
                return input;
            }

            var executionContext = MobaPlanActionExecutionContextResolver.Resolve(triggerArgs, ctx);
            return Create(triggerArgs, ctx, in executionContext);
        }

        public static MobaEffectActionInput ResolveEffect(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var core = Resolve(triggerArgs, ctx);
            return new MobaEffectActionInput(in core);
        }

        public static MobaProjectileActionInput ResolveProjectile(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var core = Resolve(triggerArgs, ctx);
            var effect = new MobaEffectActionInput(in core);
            return new MobaProjectileActionInput(in effect, in core);
        }

        public static MobaSummonActionInput ResolveSummon(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var core = Resolve(triggerArgs, ctx);
            var effect = new MobaEffectActionInput(in core);
            ctx.Context.TryResolve<MobaActorLookupService>(out var actors);
            return new MobaSummonActionInput(in effect, in core, actors);
        }

        private static MobaPlanActionInput Create(object triggerArgs, ExecCtx<IWorldResolver> ctx, in MobaCombatExecutionContext executionContext)
        {
            MobaPlanActionExecutionContextResolver.TryResolveTraceScope(ctx, out var traceScope);

            var casterActorId = executionContext.SourceActorId;
            var targetActorId = executionContext.TargetActorId;
            var aimPosition = Vec3.Zero;
            var aimDirection = Vec3.Zero;
            var hasAimPosition = false;
            var hasAimDirection = false;

            return new MobaPlanActionInput(
                executionContext,
                traceScope,
                casterActorId,
                targetActorId,
                aimPosition,
                aimDirection,
                hasAimPosition,
                hasAimDirection);
        }
    }
}
