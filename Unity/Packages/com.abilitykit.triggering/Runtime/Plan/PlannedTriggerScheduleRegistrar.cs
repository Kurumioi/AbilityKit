using System;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Dispatcher;
using RuntimeActionScheduler = AbilityKit.Triggering.Runtime.ActionScheduler.ActionScheduler;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// PlannedTrigger 的调度注册辅助，统一封装 ActionScheduler 注册与执行器选择。
    /// </summary>
    internal static class PlannedTriggerScheduleRegistrar<TArgs, TCtx>
        where TArgs : class
    {
        public static void RegisterOrReplace(
            RuntimeActionScheduler actionScheduler,
            in ActionCallPlan call,
            int planIndex,
            in TArgs args,
            ExecutionControl control,
            Func<int, Action<object, ITriggerDispatcherContext>> actionDelegateFactory,
            TriggerPredicate<object> conditionDelegate)
        {
            if (actionScheduler == null) throw new ArgumentNullException(nameof(actionScheduler));
            if (actionDelegateFactory == null) throw new ArgumentNullException(nameof(actionDelegateFactory));

            var actionDelegate = actionDelegateFactory(planIndex);
            var executor = CreateExecutor(in call, actionDelegate, control);

            actionScheduler.RegisterOrReplace(
                planIndex: planIndex,
                plan: call,
                actionDelegate: actionDelegate,
                conditionDelegate: conditionDelegate,
                boundArgs: args,
                executor: executor
            );
        }

        private static IActionExecutor CreateExecutor(in ActionCallPlan plan, Action<object, ITriggerDispatcherContext> action, ExecutionControl control)
        {
            var baseExecutor = new DefaultActionExecutor(action);

            return plan.ExecutionPolicy switch
            {
                Config.EActionExecutionPolicy.Queued => new QueuedActionExecutor(baseExecutor),
                Config.EActionExecutionPolicy.Parallel => baseExecutor,
                Config.EActionExecutionPolicy.WithRetry => new RetryActionExecutor(baseExecutor, plan.RetryMaxRetries, plan.RetryDelayMs),
                Config.EActionExecutionPolicy.Conditional => baseExecutor,
                _ => baseExecutor
            };
        }
    }
}
