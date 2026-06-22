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
            var origin = ResolveOrigin(
                in executionContext,
                sourceActorId,
                targetActorId,
                fallbackKind,
                fallbackConfigId);

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

        private static MobaGameplayOrigin ResolveOrigin(
            in MobaCombatExecutionContext executionContext,
            int sourceActorId,
            int targetActorId,
            MobaTraceKind fallbackKind,
            int fallbackConfigId)
        {
            if (executionContext.TryGetOrigin(out var contextOrigin) && contextOrigin.IsValid)
            {
                return contextOrigin;
            }

            if (executionContext.TryGetLineageContext(out var lineageContext))
            {
                return MobaGameplayOrigin.FromLineageContext(in lineageContext, executionContext.SkillRuntimeHandle)
                    .WithActors(sourceActorId, targetActorId);
            }

            return MobaGameplayOriginBuilder.Create()
                .WithActors(sourceActorId, targetActorId)
                .WithImmediate(fallbackKind, fallbackConfigId, 0)
                .WithSkillRuntimeIfMissing(executionContext.SkillRuntimeHandle)
                .Build();
        }
    }
}
