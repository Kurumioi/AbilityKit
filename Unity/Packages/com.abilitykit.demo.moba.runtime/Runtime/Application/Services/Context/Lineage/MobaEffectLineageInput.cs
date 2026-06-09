using System;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaEffectLineageInput
    {
        public MobaEffectLineageInput(
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
        public bool HasExecutionSource => SourceActorId > 0 && ParentContextId != 0;
        public bool IsValid => ContextKind != EffectContextKind.Unknown
                               || SourceActorId != 0
                               || TargetActorId != 0
                               || ParentContextId != 0
                               || RootContextId != 0
                               || OwnerKey != 0
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
                OwnerKey,
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
