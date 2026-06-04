namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaTriggerContextResolveExtensions
    {
        public static bool TryResolveContextSource(this object payload, out MobaContextSourceView source)
        {
            source = default;
            if (payload == null) return false;

            if (payload is MobaContextSourceView direct && direct.IsValid)
            {
                source = direct;
                return true;
            }

            if (payload is IMobaContextSourceProvider sourceProvider && sourceProvider.TryGetContextSource(out source) && source.IsValid)
                return true;

            if (payload.TryResolveCombatExecutionContext(out var executionContext))
            {
                source = new MobaContextSourceView(
                    MobaContextSourceResolveKind.CombatExecutionContext,
                    MobaContextSourceBoundary.Execution,
                    executionContext.ContextKind,
                    executionContext.OriginKind,
                    executionContext.SourceActorId,
                    executionContext.TargetActorId,
                    executionContext.ParentContextId,
                    executionContext.ParentContextId,
                    executionContext.RootContextId,
                    executionContext.OwnerContextId,
                    executionContext.ConfigId,
                    executionContext.TriggerId,
                    executionContext.Frame,
                    null,
                    0,
                    false,
                    executionContext.SkillRuntimeHandle);
                return source.IsValid;
            }

            if (payload.TryResolveOrigin(out var origin))
            {
                source = MobaContextSourceView.FromOrigin(in origin);
                return source.IsValid;
            }

            if (payload.TryResolveLineageContext(out var lineageContext))
            {
                var handle = default(MobaSkillCastRuntimeHandle);
                if (payload is IMobaTriggerSkillRuntimeContext skillRuntimeProvider)
                {
                    skillRuntimeProvider.TryGetSkillRuntimeHandle(out handle);
                }

                source = MobaContextSourceView.FromLineage(in lineageContext, skillRuntimeHandle: handle);
                return source.IsValid;
            }

            if (payload.TryResolveExecutionSnapshot(out var snapshot))
            {
                source = MobaContextSourceView.FromExecutionSnapshot(in snapshot);
                return source.IsValid;
            }

            return false;
        }

        public static bool TryResolveExecutionSnapshot(this object payload, out MobaTriggerExecutionSnapshot snapshot)
        {
            snapshot = default;
            return payload is IMobaTriggerExecutionSnapshotProvider provider
                   && provider.TryGetExecutionSnapshot(out snapshot)
                   && snapshot.IsValid;
        }

        public static bool TryResolveStageSnapshot(this object payload, out MobaTriggerStageSnapshot snapshot)
        {
            return MobaTriggerStageSnapshotResolver.TryResolve(payload, out snapshot);
        }

        public static bool TryResolveLineageContext(this object payload, out MobaTriggerLineageContext lineageContext)
        {
            lineageContext = default;
            if (payload is IMobaTriggerLineageContextProvider lineageProvider && lineageProvider.TryGetLineageContext(out lineageContext))
                return true;

            if (payload is IMobaTriggerTraceContextProvider traceProvider && traceProvider.TryGetTraceContext(out var traceContext))
            {
                lineageContext = traceContext.ToLineageContext();
                return true;
            }

            return false;
        }

        public static bool TryResolveOrigin(this object payload, out MobaGameplayOrigin origin)
        {
            origin = default;
            if (payload is IMobaOriginContextProvider originProvider && originProvider.TryGetOrigin(out origin) && origin.IsValid)
                return true;

            if (payload.TryResolveLineageContext(out var lineageContext))
            {
                var handle = default(MobaSkillCastRuntimeHandle);
                if (payload is IMobaTriggerSkillRuntimeContext skillRuntimeProvider)
                {
                    skillRuntimeProvider.TryGetSkillRuntimeHandle(out handle);
                }

                origin = MobaGameplayOrigin.FromLineageContext(in lineageContext, in handle);
                return origin.IsValid;
            }

            return false;
        }
    }
}
