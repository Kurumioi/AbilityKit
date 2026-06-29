using AbilityKit.Core.Logging;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public sealed class ActionCallTriggerPlanExecutable : TriggerPlanExecutableBase
    {
        private readonly ActionCallPlan _action;
        private readonly TriggerPlan<object> _plan;

        public ActionCallPlan Action => _action;

        public override string Name => "ActionCall";
        public override ETriggerPlanExecutableKind Kind => ETriggerPlanExecutableKind.Action;

        public ActionCallTriggerPlanExecutable(ActionCallPlan action, ITriggerPlanCondition condition = null, float weight = 1f)
            : base(condition, weight)
        {
            _action = action;
            _plan = new TriggerPlan<object>(phase: 0, priority: 0, triggerId: 0, actions: new[] { _action });
        }

        protected override TriggerPlanExecutionResult ExecuteCore<TCtx>(object args, in ExecCtx<TCtx> ctx)
        {
            Log.Warning($"[ActionCallTriggerPlanExecutable] executing actionId={_action.Id.Value} arity={_action.Arguments.Arity} hasNamedArgs={_action.Arguments.HasNamedArgs} triggerArgsType={args?.GetType().Name ?? "<null>"} ctxType={typeof(TCtx).Name}");
            var trigger = new PlannedTrigger<object, TCtx>(_plan);
            trigger.Execute(args, in ctx);
            Log.Warning($"[ActionCallTriggerPlanExecutable] executed actionId={_action.Id.Value} arity={_action.Arguments.Arity}");
            return TriggerPlanExecutionResult.Success(_plan.Actions?.Length ?? 0);
        }
    }
}
