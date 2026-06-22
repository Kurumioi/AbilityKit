namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 触发执行 payload 的统一契约。
    /// 用于把主动技能、被动触发、投射物命中、Buff 定时/阶段触发、伤害计算前后等触发时机收敛到同一套可溯源模型。
    /// </summary>
    public interface IMobaTriggerExecutionPayload : IMobaTriggerInvocationContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaOriginContextProvider
    {
        MobaTriggerLineageContext LineageContext { get; }
        MobaTriggerTraceContext TraceContext { get; }
        MobaGameplayOrigin Origin { get; }
    }

    /// <summary>
    /// Recommended base class for formal trigger execution payloads.
    /// New trigger timings should inherit this type so origin, lineage, and trace capabilities stay consistent across active skill, passive, buff, projectile, area, summon, and damage pipeline payloads.
    /// </summary>
    public abstract class MobaTriggerInvocationContextBase : IMobaTriggerExecutionPayload
    {
        public int TriggerId { get; set; }
        public abstract EffectContextKind Kind { get; }
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }

        public virtual MobaTriggerLineageContext LineageContext
        {
            get
            {
                TryGetLineageContext(out var context);
                return context;
            }
        }

        public virtual MobaTriggerTraceContext TraceContext
        {
            get
            {
                TryGetTraceContext(out var context);
                return context;
            }
        }

        public virtual MobaGameplayOrigin Origin
        {
            get
            {
                TryGetOrigin(out var origin);
                return origin;
            }
        }

        public abstract bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext);
        public abstract bool TryGetTraceContext(out MobaTriggerTraceContext traceContext);
        public abstract bool TryGetOrigin(out MobaGameplayOrigin origin);
    }
}
