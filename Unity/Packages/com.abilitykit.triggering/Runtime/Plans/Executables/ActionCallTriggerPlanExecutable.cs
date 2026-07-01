using AbilityKit.Core.Logging;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public sealed class ActionCallTriggerPlanExecutable : TriggerPlanExecutableBase
    {
        private readonly ActionCallPlan _action;
        private readonly TriggerPlan<object> _plan;
        private readonly int _originalActionIndex;

        public ActionCallPlan Action => _action;

        public override string Name => "ActionCall";
        public override ETriggerPlanExecutableKind Kind => ETriggerPlanExecutableKind.Action;

        public ActionCallTriggerPlanExecutable(ActionCallPlan action, int originalActionIndex = -1, ITriggerPlanCondition condition = null, float weight = 1f)
            : base(condition, weight)
        {
            _action = action;
            _originalActionIndex = originalActionIndex;
            _plan = new TriggerPlan<object>(phase: 0, priority: 0, triggerId: 0, actions: new[] { _action });
        }

        protected override TriggerPlanExecutionResult ExecuteCore<TCtx>(object args, in ExecCtx<TCtx> ctx)
            where TCtx : class
        {
            Log.Warning($"[ActionCallTriggerPlanExecutable] executing actionId={_action.Id.Value} originalIndex={_originalActionIndex} planTriggerId={_plan.TriggerId} wrappedActionCount={_plan.Actions?.Length ?? 0} arity={_action.Arguments.Arity} hasNamedArgs={_action.Arguments.HasNamedArgs} triggerArgsType={args?.GetType().Name ?? "<null>"} ctxType={typeof(TCtx).Name}");
            var executor = new PlannedTriggerActionExecutor<object, TCtx>(in _plan);
            executor.Resolve(in ctx);
            executor.ExecuteWithScopeIndex(args, in ctx, 0, _originalActionIndex >= 0 ? _originalActionIndex : 0);
            Log.Warning($"[ActionCallTriggerPlanExecutable] executed actionId={_action.Id.Value} originalIndex={_originalActionIndex} arity={_action.Arguments.Arity}");
            return TriggerPlanExecutionResult.Success(_plan.Actions?.Length ?? 0);
        }
    }
}
