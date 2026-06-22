namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Serializable/snapshot-friendly trace input for effect execution. New code should use OwnerContextId terminology for ownership identity.
    /// </summary>
    public readonly struct MobaEffectTraceInput
    {
        public MobaEffectTraceInput(
            EffectContextKind contextKind,
            MobaTraceKind originKind,
            int sourceActorId,
            int targetActorId,
            long parentContextId,
            long rootContextId,
            long ownerContextId,
            int originConfigId)
        {
            ContextKind = contextKind;
            OriginKind = originKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerContextId;
            OriginConfigId = originConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind OriginKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        /// <summary>Parent trace context that the effect execution should attach to. Zero means this effect may create a new trace root.</summary>
        public long ParentContextId { get; }
        /// <summary>Known trace root for the execution chain. When zero, ParentContextId is treated as the effective root.</summary>
        public long RootContextId { get; }
        /// <summary>Compatibility name for ownership context identity. Prefer OwnerContextId in new code.</summary>
        public long OwnerKey { get; }
        /// <summary>Preferred ownership context identity name.</summary>
        public long OwnerContextId => OwnerKey;
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
                OwnerContextId,
                OriginConfigId);
        }

        public static MobaEffectTraceInput FromInvocation(IMobaTriggerInvocationContext invocation)
        {
            return MobaEffectLineageInput.FromInvocation(invocation).ToTraceInput();
        }
    }
}
