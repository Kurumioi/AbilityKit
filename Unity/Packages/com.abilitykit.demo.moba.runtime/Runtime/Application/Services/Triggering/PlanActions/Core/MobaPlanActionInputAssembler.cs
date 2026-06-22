using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Mathematics;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Centralized assembler for action execution input. It keeps execution-context, trace-scope,
    /// actor, aim, target, and origin prerequisites out of individual plan action modules.
    /// </summary>
    internal static class MobaPlanActionInputAssembler
    {
        public static MobaPlanActionInput Assemble(object triggerArgs, ExecCtx<IWorldResolver> ctx, in MobaCombatExecutionContext executionContext)
        {
            if (!MobaPlanActionExecutionContextResolver.TryResolveTraceScope(ctx, out var traceScope))
            {
                var payloadType = triggerArgs != null ? triggerArgs.GetType().FullName : "null";
                throw new System.InvalidOperationException($"[MobaPlanActionInputAssembler] Missing formal effect trace scope. payloadType={payloadType}. Plan actions must execute inside MobaEffectExecutionService with an active effect trace scope.");
            }

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

        public static MobaEffectActionInput AssembleEffect(in MobaPlanActionInput core)
        {
            return new MobaEffectActionInput(in core);
        }

        public static MobaProjectileActionInput AssembleProjectile(in MobaPlanActionInput core)
        {
            var effect = AssembleEffect(in core);
            return new MobaProjectileActionInput(in effect, in core);
        }

        public static MobaSummonActionInput AssembleSummon(in MobaPlanActionInput core, ExecCtx<IWorldResolver> ctx)
        {
            var effect = AssembleEffect(in core);
            ctx.Context.TryResolve<MobaActorLookupService>(out var actors);
            return new MobaSummonActionInput(in effect, in core, actors);
        }

        public static bool TryResolveTargets(
            in MobaActionTargetRequest request,
            in MobaPlanActionInput coreInput,
            in MobaEffectActionInput effectInput,
            ExecCtx<IWorldResolver> ctx,
            string actionName,
            System.Collections.Generic.List<int> results)
        {
            return MobaActionTargetResolver.TryResolveTargets(in request, in coreInput, in effectInput, ctx, actionName, results);
        }
    }
}
