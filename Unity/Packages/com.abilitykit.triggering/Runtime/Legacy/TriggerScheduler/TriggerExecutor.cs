using System;
using AbilityKit.Triggering.Runtime.Executable;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.ActionScheduler;

namespace AbilityKit.Triggering.Runtime.TriggerScheduler
{
    /// <summary>
    /// 旧 TriggerScheduler 兼容类型已收口为显式失败入口。
    /// </summary>
    [Obsolete("Runtime.TriggerScheduler is a removed compatibility surface. Use TriggerRunner + PlannedTrigger with ActionScheduler/RuleScheduler for formal runtime integration.")]
    public readonly struct TriggerExecutionContext<TCtx>
    {
        public TriggerExecutionContext(TCtx context)
        {
            Context = context;
        }

        public TCtx Context { get; }
    }

    /// <summary>
    /// 旧 TriggerScheduler 执行器兼容接口已收口为显式失败入口。
    /// </summary>
    [Obsolete("Runtime.TriggerScheduler is a removed compatibility surface. Use TriggerRunner + PlannedTrigger with ActionScheduler/RuleScheduler for formal runtime integration.")]
    public interface ITriggerExecutor<TCtx>
    {
        ExecutionResult Execute(in TriggerPlan<object> plan, in TriggerExecutionContext<TCtx> context);
    }

    /// <summary>
    /// 旧 TriggerScheduler 默认执行器已收口为显式失败入口。
    /// </summary>
    [Obsolete("Runtime.TriggerScheduler.DefaultTriggerExecutor is a removed compatibility surface. Use TriggerRunner + PlannedTrigger and ActionRegistry resolution instead.")]
    public sealed class DefaultTriggerExecutor<TCtx> : ITriggerExecutor<TCtx>
    {
        public DefaultTriggerExecutor(IActionExecutor defaultActionExecutor = null)
        {
        }

        public ExecutionResult Execute(in TriggerPlan<object> plan, in TriggerExecutionContext<TCtx> context)
        {
            throw new NotSupportedException("Runtime.TriggerScheduler is removed. Use TriggerRunner + PlannedTrigger and ActionScheduler/RuleScheduler on the formal runtime path.");
        }
    }
}
