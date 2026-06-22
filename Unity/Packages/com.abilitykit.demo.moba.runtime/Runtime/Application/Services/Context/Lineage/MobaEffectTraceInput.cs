namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 面向序列化与快照的效果执行 trace 输入。
    /// 新代码在表示所有权标识时应使用 OwnerContextId 这一术语。
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
            OwnerContextId = ownerContextId;
            OriginConfigId = originConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind OriginKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        /// <summary>效果执行应挂接到的父 trace 上下文；为 0 时，本次效果可创建新的 trace 根节点。</summary>
        public long ParentContextId { get; }
        /// <summary>执行链路中已知的 trace 根节点；为 0 时，ParentContextId 作为有效根节点。</summary>
        public long RootContextId { get; }
        /// <summary>通过该 trace 输入传播的所有权上下文标识。</summary>
        public long OwnerContextId { get; }
        /// <summary>所有权上下文标识的兼容别名。</summary>
        public long OwnerKey => OwnerContextId;
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
