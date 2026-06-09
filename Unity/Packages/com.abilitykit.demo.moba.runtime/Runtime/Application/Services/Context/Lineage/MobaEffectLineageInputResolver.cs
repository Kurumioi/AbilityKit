using System;
using AbilityKit.Effect;

namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaEffectLineageInputResolver
    {
        public static MobaEffectLineageInput Resolve(object payload)
        {
            if (payload.TryResolveCombatExecutionContext(out var executionContext))
            {
                return executionContext.LineageInput;
            }

            if (payload.TryResolveOrigin(out var origin))
            {
                var originLineageContext = origin.ToLineageContext(payload is IMobaTriggerInvocationContext invocationContext ? invocationContext.Kind : EffectContextKind.Unknown);
                if (originLineageContext.HasExecutionSource)
                {
                    return originLineageContext.ToLineageInput();
                }
            }

            if (payload.TryResolveLineageContext(out var lineageContext) && lineageContext.HasExecutionSource)
            {
                return lineageContext.ToLineageInput();
            }

            if (payload is IMobaTriggerInvocationContext invocation)
            {
                var lineageInput = MobaEffectLineageInput.FromInvocation(invocation);
                if (lineageInput.HasExecutionSource)
                {
                    return lineageInput;
                }
            }

            if (payload is IEffectContext effectCtx)
            {
                var lineageInput = new MobaEffectLineageInput(
                    effectCtx.Kind,
                    MobaTraceKind.EffectExecution,
                    effectCtx.SourceActorId,
                    effectCtx.TargetActorId,
                    effectCtx.SourceContextId,
                    effectCtx.SourceContextId,
                    0,
                    0);
                if (lineageInput.HasExecutionSource)
                {
                    return lineageInput;
                }
            }

            var payloadType = payload != null ? payload.GetType().FullName : "null";
            throw new InvalidOperationException($"[MobaEffectLineageInputResolver] Missing complete effect lineage context. payloadType={payloadType}. Effect execution payload must provide sourceActorId and sourceContextId through IMobaCombatContextSource, IMobaCombatExecutionContextProvider, IMobaOriginContextProvider, IMobaTriggerLineageContextProvider, IMobaTriggerInvocationContext, or IEffectContext.");
        }
    }
}
