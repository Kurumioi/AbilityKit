namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaEffectTraceInput
    {
        public MobaEffectTraceInput(
            EffectContextKind contextKind,
            MobaTraceKind originKind,
            int sourceActorId,
            int targetActorId,
            long parentContextId,
            long rootContextId,
            long ownerKey,
            int originConfigId)
        {
            ContextKind = contextKind;
            OriginKind = originKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerKey;
            OriginConfigId = originConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind OriginKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long ParentContextId { get; }
        public long RootContextId { get; }
        public long OwnerKey { get; }
        public int OriginConfigId { get; }

        public long EffectiveRootContextId => RootContextId != 0 ? RootContextId : ParentContextId;

        public MobaEffectLineageInput ToLineageInput()
        {
            return new MobaEffectLineageInput(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                ParentContextId,
                RootContextId,
                OwnerKey,
                OriginConfigId);
        }

        public static MobaEffectTraceInput FromInvocation(IMobaTriggerInvocationContext invocation)
        {
            return MobaEffectLineageInput.FromInvocation(invocation).ToTraceInput();
        }
    }
}
