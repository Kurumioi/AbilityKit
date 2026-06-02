namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaTriggerContextResolveExtensions
    {
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
