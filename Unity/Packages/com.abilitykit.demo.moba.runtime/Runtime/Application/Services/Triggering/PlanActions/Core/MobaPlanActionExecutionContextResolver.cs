using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    internal static class MobaPlanActionExecutionContextResolver
    {
        public static bool TryResolve(object triggerArgs, ExecCtx<IWorldResolver> ctx, out MobaCombatExecutionContext executionContext)
        {
            if (ctx.Context.TryResolve<MobaEffectExecutionService>(out var effects)
                && effects != null
                && effects.TryGetCurrentExecutionContext(out var currentContext))
            {
                executionContext = currentContext;
                return true;
            }

            if (triggerArgs.TryResolveCombatExecutionContext(out var payloadContext))
            {
                executionContext = payloadContext;
                return true;
            }

            executionContext = default;
            return false;
        }

        public static MobaCombatExecutionContext Resolve(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            if (TryResolve(triggerArgs, ctx, out var executionContext))
            {
                return executionContext;
            }

            Log.Warning("[MobaPlanActionExecutionContextResolver] Creating fallback execution context. Action should run inside MobaEffectExecutionService session or provide IMobaCombatExecutionContextProvider.");
            return CreateFallback(triggerArgs);
        }

        public static bool TryResolveTraceScope(ExecCtx<IWorldResolver> ctx, out MobaEffectTraceScopeSnapshot traceScope)
        {
            traceScope = default;
            return ctx.Context.TryResolve<MobaEffectExecutionService>(out var effects)
                   && effects != null
                   && effects.TryGetCurrentTraceScope(out traceScope)
                   && traceScope.EffectContextId != 0;
        }

        private static MobaCombatExecutionContext CreateFallback(object triggerArgs)
        {
            var lineageInput = MobaEffectLineageInputResolver.Resolve(triggerArgs);
            var snapshot = MobaTriggerExecutionSnapshotBuilder.Create()
                .FromLineage(in lineageInput)
                .FromPayload(triggerArgs)
                .Build();
            return MobaCombatExecutionContextFactory.Create(triggerArgs, in lineageInput, in snapshot, snapshot.Frame);
        }
    }
}
