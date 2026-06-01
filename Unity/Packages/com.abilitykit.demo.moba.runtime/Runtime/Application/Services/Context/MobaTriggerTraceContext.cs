namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaTriggerTraceContext
    {
        public MobaTriggerTraceContext(
            EffectContextKind contextKind,
            MobaTraceKind traceKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long rootContextId,
            long ownerKey,
            int sourceConfigId)
        {
            ContextKind = contextKind;
            TraceKind = traceKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerKey;
            SourceConfigId = sourceConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind TraceKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long RootContextId { get; }
        public long OwnerKey { get; }
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
                OwnerKey,
                SourceConfigId);
        }

        public MobaEffectTraceInput ToEffectTraceInput()
        {
            return ToLineageContext().ToLineageInput().ToTraceInput();
        }
    }
}
