using System;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Canonical lineage input for effect execution. It describes how a new effect execution attaches to an existing trace/context chain.
    /// </summary>
    public readonly struct MobaEffectLineageInput
    {
        public MobaEffectLineageInput(
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
        /// <summary>
        /// Parent trace context that the effect execution should attach to. Zero means the execution may create a new root.
        /// </summary>
        public long ParentContextId { get; }
        /// <summary>
        /// Known root trace context for this lineage. When zero, ParentContextId is used as the effective root.
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
        public int OriginConfigId { get; }

        public long EffectiveRootContextId => RootContextId != 0 ? RootContextId : ParentContextId;
        public bool HasExecutionSource => SourceActorId > 0 && ParentContextId != 0;
        public bool IsValid => ContextKind != EffectContextKind.Unknown
                               || SourceActorId != 0
                               || TargetActorId != 0
                               || ParentContextId != 0
                               || RootContextId != 0
                               || OwnerContextId != 0
                               || OriginConfigId != 0;

        public MobaEffectTraceInput ToTraceInput()
        {
            return new MobaEffectTraceInput(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                ParentContextId,
                RootContextId,
                OwnerContextId,
                OriginConfigId);
        }

        public static MobaEffectLineageInput FromInvocation(IMobaTriggerInvocationContext invocation)
        {
            if (invocation == null) throw new ArgumentNullException(nameof(invocation));
            return new MobaEffectLineageInput(
                invocation.Kind,
                MobaTraceKind.EffectExecution,
                invocation.SourceActorId,
                invocation.TargetActorId,
                invocation.SourceContextId,
                invocation.SourceContextId,
                0,
                0);
        }
    }
}
