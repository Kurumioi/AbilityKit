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
                var contextKind = payload is IMobaTriggerInvocationContext invocationContext ? invocationContext.Kind : EffectContextKind.Unknown;
                return origin.ToLineageContext(contextKind).ToLineageInput();
            }

            if (payload.TryResolveLineageContext(out var lineageContext))
            {
                return lineageContext.ToLineageInput();
            }

            if (payload is IMobaTriggerInvocationContext invocation)
            {
                return MobaEffectLineageInput.FromInvocation(invocation);
            }

            if (payload is IEffectContext effectCtx)
            {
                return new MobaEffectLineageInput(
                    effectCtx.Kind,
                    MobaTraceKind.EffectExecution,
                    effectCtx.SourceActorId,
                    effectCtx.TargetActorId,
                    effectCtx.SourceContextId,
                    effectCtx.SourceContextId,
                    0,
                    0);
            }

            return new MobaEffectLineageInput(EffectContextKind.Unknown, MobaTraceKind.EffectExecution, 0, 0, 0, 0, 0, 0);
        }
    }
}
