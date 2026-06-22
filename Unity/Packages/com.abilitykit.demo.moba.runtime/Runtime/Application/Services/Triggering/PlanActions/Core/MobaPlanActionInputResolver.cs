using AbilityKit.Ability.World.DI;
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
            return MobaPlanActionInputAssembler.AssembleEffect(in core);
        }
 
        public static MobaProjectileActionInput ResolveProjectile(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var core = Resolve(triggerArgs, ctx);
            return MobaPlanActionInputAssembler.AssembleProjectile(in core);
        }
 
        public static MobaSummonActionInput ResolveSummon(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var core = Resolve(triggerArgs, ctx);
            return MobaPlanActionInputAssembler.AssembleSummon(in core, ctx);
        }

        private static MobaPlanActionInput Create(object triggerArgs, ExecCtx<IWorldResolver> ctx, in MobaCombatExecutionContext executionContext)
        {
            return MobaPlanActionInputAssembler.Assemble(triggerArgs, ctx, in executionContext);
        }
    }
}
