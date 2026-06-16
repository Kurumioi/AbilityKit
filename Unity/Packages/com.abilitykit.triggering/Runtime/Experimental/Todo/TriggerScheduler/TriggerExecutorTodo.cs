using System;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Dispatcher;
using AbilityKit.Triggering.Runtime.Executable;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Triggering.Runtime.Experimental.Todo.TriggerScheduler
{
    /// <summary>
    /// Experimental mirror of the non-mainline TriggerScheduler executor concept.
    ///
    /// This file intentionally does not replace AbilityKit.Triggering.Runtime.TriggerScheduler.
    /// It documents the migration target for a future trigger execution strategy layer.
    /// The current stable runtime path remains TriggerRunner + PlannedTrigger + Plan/Executables.
    /// </summary>
    public readonly struct ExperimentalTriggerExecutionContext<TCtx>
    {
        public readonly TCtx Context;
        public readonly ITriggerDispatcherContext DispatcherContext;
        public readonly ExecutionControl Control;
        public readonly ActionSchedulerManager ActionSchedulerManager;

        public ExperimentalTriggerExecutionContext(
            TCtx context,
            ITriggerDispatcherContext dispatcherContext,
            ExecutionControl control,
            ActionSchedulerManager actionSchedulerManager)
        {
            Context = context;
            DispatcherContext = dispatcherContext;
            Control = control;
            ActionSchedulerManager = actionSchedulerManager;
        }
    }

    /// <summary>
    /// TODO migration target for trigger-level execution strategies.
    ///
    /// Planned mainline integration:
    /// - resolve actions through ActionRegistry instead of placeholder delegates;
    /// - resolve predicates through FunctionRegistry / PredicateExprPlan;
    /// - integrate with Plan/Executables rather than creating a parallel trigger runtime;
    /// - reuse ActionScheduler only for scheduled actions, not as a competing trigger dispatcher.
    /// </summary>
    public interface IExperimentalTriggerExecutor<TCtx>
    {
        ExecutionResult Execute<TArgs>(TriggerPlan<TArgs> plan, ExperimentalTriggerExecutionContext<TCtx> ctx) where TArgs : class;
    }

    /// <summary>
    /// Preserved value implementation for future migration.
    /// Not wired into the stable runtime yet.
    /// </summary>
    public sealed class ExperimentalDefaultTriggerExecutor<TCtx> : IExperimentalTriggerExecutor<TCtx>
    {
        private readonly IActionExecutor _defaultActionExecutor;

        public ExperimentalDefaultTriggerExecutor(IActionExecutor defaultActionExecutor = null)
        {
            _defaultActionExecutor = defaultActionExecutor;
        }

        public ExecutionResult Execute<TArgs>(TriggerPlan<TArgs> plan, ExperimentalTriggerExecutionContext<TCtx> ctx) where TArgs : class
        {
            if (ctx.ActionSchedulerManager == null)
                return ExecutionResult.Failed("ActionSchedulerManager is required for experimental trigger execution.");

            var actions = plan.Actions;
            if (actions == null || actions.Length == 0)
                return ExecutionResult.Success(0);

            var actionScheduler = ctx.ActionSchedulerManager.GetOrCreateScheduler(plan.TriggerId);
            var registeredCount = 0;

            for (var i = 0; i < actions.Length; i++)
            {
                var actionPlan = actions[i];
                var actionDelegate = CreateUnresolvedActionDelegate(actionPlan.Id);
                var conditionDelegate = CreateUnresolvedConditionDelegate<TArgs>(plan);
                var executor = _defaultActionExecutor ?? new DefaultActionExecutor(actionDelegate);

                actionScheduler.Register(
                    plan: actionPlan,
                    actionDelegate: actionDelegate,
                    conditionDelegate: conditionDelegate,
                    boundArgs: ctx.Context,
                    executor: executor);

                registeredCount++;
            }

            return ExecutionResult.Success(registeredCount);
        }

        private static Action<object, ITriggerDispatcherContext> CreateUnresolvedActionDelegate(ActionId actionId)
        {
            return (_, _) =>
            {
                throw new InvalidOperationException(
                    $"Experimental TriggerScheduler action [{actionId}] is unresolved. " +
                    "Migrate this path to ActionRegistry/PlannedTrigger resolution before wiring it into mainline.");
            };
        }

        private static TriggerPredicate<object> CreateUnresolvedConditionDelegate<TArgs>(TriggerPlan<TArgs> plan) where TArgs : class
        {
            return null;
        }
    }
}
