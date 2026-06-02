namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaTriggerLineageContext
    {
        public MobaTriggerLineageContext(
            EffectContextKind contextKind,
            MobaTraceKind originKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long rootContextId,
            long ownerKey,
            int sourceConfigId)
        {
            ContextKind = contextKind;
            OriginKind = originKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerKey;
            SourceConfigId = sourceConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind OriginKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long RootContextId { get; }
        public long OwnerKey { get; }
        public int SourceConfigId { get; }

        public MobaEffectLineageInput ToLineageInput()
        {
            return new MobaEffectLineageInput(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId,
                OwnerKey,
                SourceConfigId);
        }

        public MobaTriggerTraceContext ToTraceContext()
        {
            return new MobaTriggerTraceContext(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId,
                OwnerKey,
                SourceConfigId);
        }
    }
}
