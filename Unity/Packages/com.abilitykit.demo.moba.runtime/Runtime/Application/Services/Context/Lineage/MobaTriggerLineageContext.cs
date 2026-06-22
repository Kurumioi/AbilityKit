namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Canonical lineage context propagated by trigger providers before it is converted into effect execution input or trace snapshots.
    /// </summary>
    public readonly struct MobaTriggerLineageContext
    {
        public MobaTriggerLineageContext(
            EffectContextKind contextKind,
            MobaTraceKind originKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long rootContextId,
            long ownerContextId,
            int sourceConfigId)
        {
            ContextKind = contextKind;
            OriginKind = originKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerKey = ownerContextId;
            SourceConfigId = sourceConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind OriginKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        /// <summary>
        /// Source trace context for downstream trigger/effect execution. It usually becomes the parent context of the next execution node.
        /// </summary>
        public long SourceContextId { get; }
        /// <summary>
        /// Known root trace context for this lineage. When zero, SourceContextId is used as the effective root by converters.
        /// </summary>
        public long RootContextId { get; }
        /// <summary>
        /// Compatibility name for ownership context identity. Prefer <see cref="OwnerContextId"/> in new code.
        /// </summary>
        public long OwnerKey { get; }

        /// <summary>
        /// Preferred ownership context identity name.
        /// </summary>
        public long OwnerContextId => OwnerKey;
        public int SourceConfigId { get; }

        public bool HasExecutionSource => SourceActorId > 0 && SourceContextId != 0;

        public MobaEffectLineageInput ToLineageInput()
        {
            return new MobaEffectLineageInput(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId,
                OwnerContextId,
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
                OwnerContextId,
                SourceConfigId);
        }
    }
}
