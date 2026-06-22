namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 面向 trace 的触发来源上下文快照。
    /// 新代码在表示所有权标识时应使用 OwnerContextId 这一术语。
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
            OwnerContextId = ownerContextId;
            SourceConfigId = sourceConfigId;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind TraceKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        /// <summary>代表下游触发或效果执行来源/父节点的 trace 上下文。</summary>
        public long SourceContextId { get; }
        /// <summary>传播链路中已知的根 trace 上下文；为 0 时，SourceContextId 作为有效根节点。</summary>
        public long RootContextId { get; }
        /// <summary>通过该 trace 上下文传播的所有权上下文标识。</summary>
        public long OwnerContextId { get; }
        /// <summary>所有权上下文标识的兼容别名。</summary>
        public long OwnerKey => OwnerContextId;
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
