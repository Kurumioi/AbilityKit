using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Builds the movement action input by composing core action facts with movement services.
    /// </summary>
    internal static class MobaMovementActionInputResolver
    {
        public static MobaMovementActionInput Resolve(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            var actionInput = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            ctx.Context.TryResolve<MobaActorRegistry>(out var actors);
            return new MobaMovementActionInput(actionInput, actors);
        }
    }
}
