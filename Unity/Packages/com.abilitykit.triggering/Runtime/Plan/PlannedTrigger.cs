using System;
using AbilityKit.Core.Logging;
using AbilityKit.Triggering.Blackboard;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Dispatcher;
using RuntimeActionScheduler = AbilityKit.Triggering.Runtime.ActionScheduler.ActionScheduler;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// 触发器计划执行器
    /// 从 TriggerPlan 解析委托，并在满足条件时执行 Actions
    /// </summary>
    public sealed class PlannedTrigger<TArgs, TCtx> : ITrigger<TArgs, TCtx>, ITriggerWithId
        where TArgs : class
    {
        // 按 arity 拆分的 Action 委托数组
        private NamedAction0<TArgs, object, TCtx>[] _actions0;
        private NamedAction1<TArgs, object, TCtx>[] _actions1;
        private NamedAction2<TArgs, object, TCtx>[] _actions2;

        /// <summary>
        /// 标记哪些 Action 使用了具名参数模式（与 actions 数组索引对应）
        /// </summary>
        private bool[] _useNamedArgs;

        /// <inheritdoc />
        public ITriggerCue Cue => _plan.Cue;

        /// <inheritdoc />
        public int TriggerId => _plan.TriggerId;

        public PlannedTrigger(in TriggerPlan<TArgs> plan)
        {
            _plan = plan;
            _resolved = false;
            _actions0 = null;
            _actions1 = null;
            _actions2 = null;
            _useNamedArgs = null;
            _execCtx = default;
        }

        private readonly TriggerPlan<TArgs> _plan;
        private bool _resolved;
        private ExecCtx<TCtx> _execCtx;
        private int _executionCount;
        private float _lastExecutionTimeMs;
        private bool _hasLastExecutionTime;

        private ExecCtx<TCtx> ExecCtx => _execCtx;

        public bool Evaluate(in TArgs args, in ExecCtx<TCtx> ctx)
        {
            Resolve(ctx);
            return PlannedTriggerPredicateEvaluator<TArgs, TCtx>.Evaluate(in _plan, in args, in ctx);
        }

        public void Execute(in TArgs args, in ExecCtx<TCtx> ctx)
        {
            Resolve(ctx);
            var actions = _plan.Actions;
            var hasActions = actions != null && actions.Length > 0;

            if (!hasActions || !CanExecuteByControl(in ctx)) return;

            if (!HasScheduledActions(in ctx, actions))
            {
                ExecuteImmediate(in args, in ctx);
                return;
            }

            ExecuteMixedActions(in args, in ctx, actions);
        }

        private bool HasScheduledActions(in ExecCtx<TCtx> ctx, ActionCallPlan[] actions)
        {
            if (ctx.ActionSchedulerManager == null || actions == null || actions.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < actions.Length; i++)
            {
                if (IsScheduledAction(actions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsScheduledAction(in ActionCallPlan call)
        {
            return call.ScheduleMode != Config.EActionScheduleMode.Immediate;
        }

        private void ExecuteMixedActions(in TArgs args, in ExecCtx<TCtx> ctx, ActionCallPlan[] actions)
        {
            var actionScheduler = ctx.ActionSchedulerManager.GetOrCreateScheduler(_plan.TriggerId);
            var control = ctx.Control ?? new ExecutionControl();

            try
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    var call = actions[i];
                    if (IsScheduledAction(in call))
                    {
                        RegisterScheduledAction(actionScheduler, call, i, in args, control);
                    }
                    else
                    {
                        ExecuteImmediateAction(in args, in call, in ctx, i);
                    }

                    if (ctx.Control != null && ctx.Control.IsHardStopped)
                    {
                        return;
                    }
                }
            }
            finally
            {
                MarkExecutedByControl(in ctx);
            }

            ApplyInterruptControl(control);
        }

        /// <summary>
        /// 将单个 Action 计划注册到触发器级调度器，并按计划索引替换旧实例。
        /// </summary>
        private void RegisterScheduledAction(RuntimeActionScheduler actionScheduler, ActionCallPlan call, int planIndex, in TArgs args, ExecutionControl control)
        {
            PlannedTriggerScheduleRegistrar<TArgs, TCtx>.RegisterOrReplace(
                actionScheduler,
                in call,
                planIndex,
                in args,
                control,
                CreateActionDelegate,
                CreateConditionDelegate());
        }

        /// <summary>
        /// 执行成功后，根据当前触发器优先级中断更低优先级的触发链路。
        /// </summary>
        private void ApplyInterruptControl(ExecutionControl control)
        {
            if (control == null || _plan.InterruptPriority <= 0)
            {
                return;
            }

            control.StopBelowPriority(
                _plan.InterruptPriority,
                conditionPassed: true,
                _plan.TriggerId,
                $"Trigger[{_plan.TriggerId}]"
            );
        }

        /// <summary>
        /// 立即执行模式
        /// </summary>
        private void ExecuteImmediate(in TArgs args, in ExecCtx<TCtx> ctx)
        {
            var actions = _plan.Actions;
            try
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    var call = actions[i];
                    ExecuteImmediateAction(in args, in call, in ctx, i);

                    if (ctx.Control != null && ctx.Control.IsHardStopped) return;
                }
            }
            finally
            {
                MarkExecutedByControl(in ctx);
            }
        }

        private void ExecuteImmediateAction(in TArgs args, in ActionCallPlan call, in ExecCtx<TCtx> ctx, int index)
        {
            if (_useNamedArgs[index])
            {
                var rawArgs = PlannedTriggerArgumentResolver<TArgs, TCtx>.ResolveNamedArgs(in args, in call, in ctx);
                switch (call.Arity)
                {
                    case 0:
                        if (_actions0[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                        _actions0[index].Invoke(args, rawArgs, ctx);
                        break;
                    case 1:
                        if (_actions1[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                        _actions1[index].Invoke(args, rawArgs, ctx);
                        break;
                    case 2:
                        if (_actions2[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                        _actions2[index].Invoke(args, rawArgs, ctx);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported named action arity during execute. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity={call.Arity}");
                }
                return;
            }

            ExecuteLegacy(in args, in call, in ctx, index);
        }

        private bool CanExecuteByControl(in ExecCtx<TCtx> ctx)
        {
            var control = _plan.ExecutionControl;
            switch (control.Mode)
            {
                case ETriggerExecutionMode.Once:
                    return _executionCount <= 0;
                case ETriggerExecutionMode.Repeat:
                    return control.MaxExecutions <= 0 || _executionCount < control.MaxExecutions;
                case ETriggerExecutionMode.Cooldown:
                    if (control.CooldownMs <= 0f || !_hasLastExecutionTime)
                    {
                        return true;
                    }
                    return ctx.Policy.TotalTimeMs - _lastExecutionTimeMs >= control.CooldownMs;
                case ETriggerExecutionMode.Always:
                default:
                    return true;
            }
        }

        private void MarkExecutedByControl(in ExecCtx<TCtx> ctx)
        {
            var control = _plan.ExecutionControl;
            if (control.Mode == ETriggerExecutionMode.Always && control.MaxExecutions <= 0 && control.CooldownMs <= 0f)
            {
                return;
            }

            _executionCount++;
            _lastExecutionTimeMs = ctx.Policy.TotalTimeMs;
            _hasLastExecutionTime = true;
        }

        /// <summary>
        /// 向后兼容的位置参数执行（使用 Arg0/Arg1）
        /// </summary>
        private void ExecuteLegacy(in TArgs args, in ActionCallPlan call, in ExecCtx<TCtx> ctx, int index)
        {
            switch (call.Arity)
            {
                case 0:
                    if (_actions0[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                    _actions0[index].Invoke(args, null, ctx);
                    break;
                case 1:
                    {
                        if (_actions1[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                        var v0 = PlannedTriggerArgumentResolver<TArgs, TCtx>.ResolveNumeric(in args, in call.Arg0, in ctx);
                        var argsDict = PlannedTriggerArgumentResolver<TArgs, TCtx>.CreatePositionalArgs(v0);
                        _actions1[index].Invoke(args, argsDict, ctx);
                        break;
                    }
                case 2:
                    {
                        if (_actions2[index] == null) ThrowActionSlotMissing(in call, in ctx, index);
                        var v0 = PlannedTriggerArgumentResolver<TArgs, TCtx>.ResolveNumeric(in args, in call.Arg0, in ctx);
                        var v1 = PlannedTriggerArgumentResolver<TArgs, TCtx>.ResolveNumeric(in args, in call.Arg1, in ctx);
                        var argsDict = PlannedTriggerArgumentResolver<TArgs, TCtx>.CreatePositionalArgs(v0, v1);
                        _actions2[index].Invoke(args, argsDict, ctx);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Unsupported action arity during execute. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity={call.Arity}");
            }
        }

        private void ThrowActionSlotMissing(in ActionCallPlan call, in ExecCtx<TCtx> ctx, int index)
        {
            throw new InvalidOperationException($"Action slot missing. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity={call.Arity}");
        }

        /// <summary>
        /// 创建 Action 委托适配器。
        /// 调度器执行时复用立即执行路径中的单 Action 解析与调用逻辑，避免调度路径维护另一套参数解析分支。
        /// </summary>
        private Action<object, ITriggerDispatcherContext> CreateActionDelegate(int index)
        {
            var call = _plan.Actions[index];

            return (argsObj, _) =>
            {
                var args = (TArgs)argsObj;
                ExecuteImmediateAction(in args, in call, ExecCtx, index);
            };
        }

        /// <summary>
        /// 创建条件委托（如果 TriggerPlan 包含 Predicate）
        /// </summary>
        private TriggerPredicate<object> CreateConditionDelegate()
        {
            return PlannedTriggerPredicateEvaluator<TArgs, TCtx>.CreateConditionDelegate(in _plan, in _execCtx);
        }

        private void Resolve(in ExecCtx<TCtx> ctx)
        {
            if (_resolved) return;

            InitializeActionBindings();
            PlannedTriggerActionBindingResolver<TArgs, TCtx>.ResolveAll(
                _plan.Actions,
                in ctx,
                _actions0,
                _actions1,
                _actions2,
                _useNamedArgs);
            _execCtx = ctx;
            _resolved = true;
        }

        /// <summary>
        /// 初始化按 Action 计划索引缓存的委托绑定数组。
        /// </summary>
        private void InitializeActionBindings()
        {
            var len = _plan.Actions?.Length ?? 0;
            _actions0 = len > 0 ? new NamedAction0<TArgs, object, TCtx>[len] : null;
            _actions1 = len > 0 ? new NamedAction1<TArgs, object, TCtx>[len] : null;
            _actions2 = len > 0 ? new NamedAction2<TArgs, object, TCtx>[len] : null;
            _useNamedArgs = len > 0 ? new bool[len] : null;
        }

        private static string FormatActionId(in ExecCtx<TCtx> ctx, ActionId id)
        {
            return PlannedTriggerArgumentResolver<TArgs, TCtx>.FormatActionId(in ctx, id);
        }
    }
}

