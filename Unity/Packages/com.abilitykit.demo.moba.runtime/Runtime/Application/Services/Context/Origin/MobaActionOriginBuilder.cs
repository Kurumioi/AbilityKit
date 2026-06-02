namespace AbilityKit.Demo.Moba.Services
{
    internal static class MobaActionOriginBuilder
    {
        public static MobaGameplayOrigin Build(
            in MobaCombatExecutionContext executionContext,
            in MobaEffectTraceScopeSnapshot traceScope,
            int sourceActorId,
            int targetActorId,
            MobaTraceKind fallbackKind,
            int fallbackConfigId)
        {
            var origin = executionContext.TryGetOrigin(out var contextOrigin)
                ? contextOrigin
                : MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, fallbackKind, fallbackConfigId, 0);

            return BuildFromOrigin(in origin, in executionContext, in traceScope, sourceActorId, targetActorId);
        }

        public static MobaGameplayOrigin BuildFromOrigin(
            in MobaGameplayOrigin sourceOrigin,
            in MobaCombatExecutionContext executionContext,
            in MobaEffectTraceScopeSnapshot traceScope,
            int sourceActorId,
            int targetActorId)
        {
            var origin = sourceOrigin.WithActors(sourceActorId, targetActorId);
            if (traceScope.EffectContextId != 0)
            {
                var handle = executionContext.SkillRuntimeHandle;
                return MobaGameplayOriginBuilder.Create()
                    .FromOrigin(in origin)
                    .WithActors(sourceActorId, targetActorId)
                    .WithImmediate(MobaTraceKind.EffectExecution, traceScope.EffectConfigId, traceScope.EffectContextId)
                    .WithRootContext(origin.EffectiveRootContextId)
                    .WithOwnerContext(origin.OwnerContextId)
                    .WithSkillRuntimeIfMissing(in handle)
                    .Build();
            }

            if (!origin.SkillRuntimeHandle.IsValid && executionContext.SkillRuntimeHandle.IsValid)
            {
                var handle = executionContext.SkillRuntimeHandle;
                origin = origin.WithSkillRuntime(in handle);
            }

            return origin;
        }
    }
}
