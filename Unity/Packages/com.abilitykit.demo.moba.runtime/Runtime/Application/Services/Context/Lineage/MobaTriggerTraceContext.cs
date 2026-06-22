namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Trace-facing context snapshot for trigger-origin propagation. New code should use OwnerContextId terminology for ownership identity.
    /// </summary>
    public readonly struct MobaTriggerTraceContext
    {
        public MobaTriggerTraceContext(
            EffectContextKind contextKind,
            MobaTraceKind traceKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long rootContextId,
            long ownerContextId,
            int sourceConfigId)
        {
            ContextKind = contextKind;
            TraceKind = traceKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerContextId;
            SourceConfigId = sourceConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind TraceKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        /// <summary>Trace context that represents the source/parent node for downstream trigger or effect execution.</summary>
        public long SourceContextId { get; }
        /// <summary>Known root trace context for the propagated chain. When zero, SourceContextId is the effective root.</summary>
        public long RootContextId { get; }
        /// <summary>Compatibility name for ownership context identity. Prefer OwnerContextId in new code.</summary>
        public long OwnerKey { get; }
        /// <summary>Preferred ownership context identity name.</summary>
        public long OwnerContextId => OwnerKey;
        public int SourceConfigId { get; }

        public MobaTriggerLineageContext ToLineageContext()
        {
            return new MobaTriggerLineageContext(
                ContextKind,
                TraceKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId,
                OwnerContextId,
                SourceConfigId);
        }

        public MobaEffectTraceInput ToEffectTraceInput()
        {
            return ToLineageContext().ToLineageInput().ToTraceInput();
        }
    }
}
