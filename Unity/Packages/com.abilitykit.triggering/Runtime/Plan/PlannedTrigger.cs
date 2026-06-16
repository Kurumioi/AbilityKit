using System;
using System.Buffers;
using AbilityKit.Core.Logging;
using AbilityKit.Triggering.Blackboard;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Dispatcher;
using AbilityKit.Triggering.Variables.Numeric;
using AbilityKit.Triggering.Variables.Numeric.Expression;
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
            if (_plan.PredicateKind == EPredicateKind.None || !_plan.HasPredicate) return true;

            if (_plan.PredicateKind == EPredicateKind.Expr)
            {
                return EvaluateExpr(in args, in ctx);
            }

            if (_plan.PredicateKind != EPredicateKind.Function)
            {
                throw new InvalidOperationException($"Unsupported predicate kind: {_plan.PredicateKind}");
            }

            return EvaluatePredicate(in args, in ctx);
        }

        /// <summary>
        /// 评估函数谓词
        /// </summary>
        private bool EvaluatePredicate(in TArgs args, in ExecCtx<TCtx> ctx)
        {
            switch (_plan.PredicateArity)
            {
                case 0:
                    if (ctx.Functions.TryGet<Predicate0<TArgs, TCtx>>(_plan.PredicateId, out var p0, out var p0Det))
                    {
                        if (ctx.Policy.RequireDeterministic && !p0Det)
                            throw new InvalidOperationException($"Non-deterministic predicate is not allowed by policy. id={FormatFunctionId(in ctx, _plan.PredicateId)}");
                        return p0?.Invoke(args, ctx) ?? true;
                    }
                    throw new InvalidOperationException($"Predicate function not found. id={FormatFunctionId(in ctx, _plan.PredicateId)} arity=0");

                case 1:
                    if (ctx.Functions.TryGet<Predicate1<TArgs, TCtx>>(_plan.PredicateId, out var p1, out var p1Det))
                    {
                        if (ctx.Policy.RequireDeterministic && !p1Det)
                            throw new InvalidOperationException($"Non-deterministic predicate is not allowed by policy. id={FormatFunctionId(in ctx, _plan.PredicateId)}");
                        var v0 = ResolveNumeric(in args, in _plan.PredicateArg0, in ctx);
                        var argsDict = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0")
                        });
                        return p1?.Invoke(args, argsDict, ctx) ?? true;
                    }
                    throw new InvalidOperationException($"Predicate function not found. id={FormatFunctionId(in ctx, _plan.PredicateId)} arity=1");

                case 2:
                    if (ctx.Functions.TryGet<Predicate2<TArgs, TCtx>>(_plan.PredicateId, out var p2, out var p2Det))
                    {
                        if (ctx.Policy.RequireDeterministic && !p2Det)
                            throw new InvalidOperationException($"Non-deterministic predicate is not allowed by policy. id={FormatFunctionId(in ctx, _plan.PredicateId)}");
                        var v0 = ResolveNumeric(in args, in _plan.PredicateArg0, in ctx);
                        var v1 = ResolveNumeric(in args, in _plan.PredicateArg1, in ctx);
                        var argsDict = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0"),
                            ["_1"] = ActionArgValue.OfConst(v1, "_1")
                        });
                        return p2?.Invoke(args, argsDict, ctx) ?? true;
                    }
                    throw new InvalidOperationException($"Predicate function not found. id={FormatFunctionId(in ctx, _plan.PredicateId)} arity=2");

                default:
                    throw new InvalidOperationException($"Unsupported predicate arity: {_plan.PredicateArity}");
            }
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
            var actionDelegate = CreateActionDelegate(planIndex);
            var conditionDelegate = CreateConditionDelegate();
            var executor = CreateExecutor(call, actionDelegate, control);

            actionScheduler.RegisterOrReplace(
                planIndex: planIndex,
                plan: call,
                actionDelegate: actionDelegate,
                conditionDelegate: conditionDelegate,
                boundArgs: args,
                executor: executor
            );
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
                var rawArgs = ResolveNamedArgs(in args, in call, in ctx);
                switch (call.Arity)
                {
                    case 0:
                        if (_actions0[index] == null) Log.Warning($"[PlannedTrigger] Named action slot missing. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity=0");
                        _actions0[index]?.Invoke(args, rawArgs, ctx);
                        break;
                    case 1:
                        if (_actions1[index] == null) Log.Warning($"[PlannedTrigger] Named action slot missing. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity=1");
                        _actions1[index]?.Invoke(args, rawArgs, ctx);
                        break;
                    case 2:
                        if (_actions2[index] == null) Log.Warning($"[PlannedTrigger] Named action slot missing. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity=2");
                        _actions2[index]?.Invoke(args, rawArgs, ctx);
                        break;
                    default:
                        Log.Warning($"[PlannedTrigger] Unsupported named action arity during execute. triggerId={_plan.TriggerId}, index={index}, id={FormatActionId(in ctx, call.Id)}, arity={call.Arity}");
                        break;
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
                    _actions0[index]?.Invoke(args, null, ctx);
                    break;
                case 1:
                    {
                        var v0 = ResolveNumeric(in args, in call.Arg0, in ctx);
                        var argsDict = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0")
                        });
                        _actions1[index]?.Invoke(args, argsDict, ctx);
                        break;
                    }
                case 2:
                    {
                        var v0 = ResolveNumeric(in args, in call.Arg0, in ctx);
                        var v1 = ResolveNumeric(in args, in call.Arg1, in ctx);
                        var argsDict = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0"),
                            ["_1"] = ActionArgValue.OfConst(v1, "_1")
                        });
                        _actions2[index]?.Invoke(args, argsDict, ctx);
                        break;
                    }
            }
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
            if (!_plan.HasPredicate || _plan.PredicateKind != EPredicateKind.Function)
                return null;

            var plan = _plan;
            var ctx = _execCtx;

            return (argsObj, _) =>
            {
                var args = (TArgs)argsObj;
                return EvaluatePredicate(in args, in ctx);
            };
        }

        /// <summary>
        /// 根据 ExecutionPolicy 创建执行器
        /// </summary>
        private IActionExecutor CreateExecutor(ActionCallPlan plan, Action<object, ITriggerDispatcherContext> action, ExecutionControl control)
        {
            var baseExecutor = new DefaultActionExecutor(action);

            return plan.ExecutionPolicy switch
            {
                Config.EActionExecutionPolicy.Queued => new QueuedActionExecutor(baseExecutor),
                Config.EActionExecutionPolicy.Parallel => baseExecutor,
                Config.EActionExecutionPolicy.WithRetry => new RetryActionExecutor(baseExecutor),
                Config.EActionExecutionPolicy.Conditional => baseExecutor,
                _ => baseExecutor
            };
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

        /// <summary>
        /// 将具名参数字典解析为可传递给 NamedAction 委托的 args 对象
        /// </summary>
        private NamedArgsDict ResolveNamedArgs(in TArgs args, in ActionCallPlan call, in ExecCtx<TCtx> ctx)
        {
            if (call.Args == null || call.Args.Count == 0)
            {
                return NamedArgsDict.Empty;
            }

            // 通过 ActionSchemaRegistry 解析参数
            var parsed = ActionSchemaRegistry.GetParsedArgs<TArgs, TCtx>(call.Id, call.Args, ctx);
            return ConvertToNamedArgsDict(parsed, call.Args);
        }

        /// <summary>
        /// 将 Schema 解析结果规整为 NamedArgsDict；暂不支持的强类型结果回退到原始参数字典。
        /// </summary>
        private static NamedArgsDict ConvertToNamedArgsDict(object parsed, System.Collections.Generic.Dictionary<string, ActionArgValue> rawArgs)
        {
            if (parsed is NamedArgsDict namedDict)
            {
                return namedDict;
            }

            if (rawArgs == null || rawArgs.Count == 0)
            {
                return NamedArgsDict.Empty;
            }

            return new NamedArgsDict(rawArgs);
        }

        private bool EvaluateExpr(in TArgs args, in ExecCtx<TCtx> ctx)
        {
            var nodes = _plan.PredicateExpr.Nodes;
            if (nodes == null || nodes.Length == 0) return true;

            if (nodes.Length <= 64)
            {
                Span<bool> stack = stackalloc bool[64];
                var sp = 0;
                EvalNodes(nodes, in args, in ctx, ref stack, ref sp);
                if (sp != 1) throw new InvalidOperationException($"Invalid expr stack depth: {sp}");
                return stack[0];
            }

            var rented = ArrayPool<bool>.Shared.Rent(nodes.Length);
            try
            {
                Span<bool> stack = rented;
                var sp = 0;
                EvalNodes(nodes, in args, in ctx, ref stack, ref sp);
                if (sp != 1) throw new InvalidOperationException($"Invalid expr stack depth: {sp}");
                return stack[0];
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(rented);
            }
        }

        private void EvalNodes(BoolExprNode[] nodes, in TArgs args, in ExecCtx<TCtx> ctx, ref Span<bool> stack, ref int sp)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i];
                switch (n.Kind)
                {
                    case EBoolExprNodeKind.Const:
                        stack[sp++] = n.ConstValue;
                        break;
                    case EBoolExprNodeKind.Not:
                        if (sp < 1) throw new InvalidOperationException("Invalid expr: NOT stack underflow");
                        stack[sp - 1] = !stack[sp - 1];
                        break;
                    case EBoolExprNodeKind.And:
                    {
                        if (sp < 2) throw new InvalidOperationException("Invalid expr: AND stack underflow");
                        var b = stack[--sp];
                        var a = stack[--sp];
                        stack[sp++] = a && b;
                        break;
                    }
                    case EBoolExprNodeKind.Or:
                    {
                        if (sp < 2) throw new InvalidOperationException("Invalid expr: OR stack underflow");
                        var b = stack[--sp];
                        var a = stack[--sp];
                        stack[sp++] = a || b;
                        break;
                    }
                    case EBoolExprNodeKind.CompareNumeric:
                    {
                        var left = ResolveNumeric(in args, in n.Left, in ctx);
                        var right = ResolveNumeric(in args, in n.Right, in ctx);
                        stack[sp++] = CompareNumeric(n.CompareOp, left, right);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Unsupported expr node kind: {n.Kind}");
                }
            }
        }

        private static bool CompareNumeric(ECompareOp op, double left, double right)
        {
            switch (op)
            {
                case ECompareOp.Equal: return left == right;
                case ECompareOp.NotEqual: return left != right;
                case ECompareOp.GreaterThan: return left > right;
                case ECompareOp.GreaterThanOrEqual: return left >= right;
                case ECompareOp.LessThan: return left < right;
                case ECompareOp.LessThanOrEqual: return left <= right;
                default:
                    throw new InvalidOperationException($"Unsupported compare op: {op}");
            }
        }

        private static double ResolveNumeric(in TArgs args, in NumericValueRef valueRef, in ExecCtx<TCtx> ctx)
        {
            if (valueRef.Kind == ENumericValueRefKind.Const) return valueRef.ConstValue;

            if (valueRef.Kind == ENumericValueRefKind.Blackboard)
            {
                var resolver = ctx.Blackboards;
                if (resolver == null)
                {
                    throw new InvalidOperationException($"Blackboard resolver is null. boardId={FormatBoardId(in ctx, valueRef.BoardId)} keyId={FormatKeyId(in ctx, valueRef.KeyId)}");
                }

                if (!resolver.TryResolve(valueRef.BoardId, out var bb) || bb == null)
                {
                    throw new InvalidOperationException($"Blackboard not found. boardId={FormatBoardId(in ctx, valueRef.BoardId)}");
                }

                if (!bb.TryGetDouble(valueRef.KeyId, out var v))
                {
                    throw new InvalidOperationException($"Blackboard numeric key not found. boardId={FormatBoardId(in ctx, valueRef.BoardId)} keyId={FormatKeyId(in ctx, valueRef.KeyId)}");
                }

                return v;
            }

            if (valueRef.Kind == ENumericValueRefKind.PayloadField)
            {
                var payloads = ctx.Payloads;
                if (payloads == null)
                {
                    throw new InvalidOperationException($"Payload accessor registry is null. fieldId={FormatFieldId(in ctx, valueRef.FieldId)}");
                }

                if (!payloads.TryGetDouble(in args, valueRef.FieldId, out var v))
                {
                    throw new InvalidOperationException($"Payload numeric field not found. fieldId={FormatFieldId(in ctx, valueRef.FieldId)}");
                }

                return v;
            }

            if (valueRef.Kind == ENumericValueRefKind.Var)
            {
                if (string.IsNullOrEmpty(valueRef.DomainId) || string.IsNullOrEmpty(valueRef.Key))
                {
                    throw new InvalidOperationException("Numeric var ref is empty");
                }

                if (!ctx.TryGetNumericVar(valueRef.DomainId, valueRef.Key, out var v))
                {
                    throw new InvalidOperationException($"Numeric var not found. domainId='{valueRef.DomainId}' key='{valueRef.Key}'");
                }

                return v;
            }

            if (valueRef.Kind == ENumericValueRefKind.Expr)
            {
                if (string.IsNullOrEmpty(valueRef.ExprText))
                {
                    throw new InvalidOperationException("Numeric expr text is empty");
                }

                if (!NumericExpressionCompiler.TryCompileCached(valueRef.ExprText, out var program) || program == null)
                {
                    throw new InvalidOperationException("Numeric expr compile failed: " + valueRef.ExprText);
                }

                if (!NumericExpressionEvaluator.TryEvaluate(in ctx, program, out var v))
                {
                    throw new InvalidOperationException("Numeric expr evaluate failed: " + valueRef.ExprText);
                }

                return v;
            }

            throw new InvalidOperationException($"Unsupported NumericValueRef kind: {valueRef.Kind}");
        }

        private static string FormatFunctionId(in ExecCtx<TCtx> ctx, FunctionId id)
        {
            var names = ctx.IdNames;
            if (names != null && names.TryGetFunctionName(id, out var name) && !string.IsNullOrEmpty(name))
                return $"{id.Value}('{name}')";
            return id.Value.ToString();
        }

        private static string FormatActionId(in ExecCtx<TCtx> ctx, ActionId id)
        {
            var names = ctx.IdNames;
            if (names != null && names.TryGetActionName(id, out var name) && !string.IsNullOrEmpty(name))
                return $"{id.Value}('{name}')";
            return id.Value.ToString();
        }

        private static string FormatBoardId(in ExecCtx<TCtx> ctx, int id)
        {
            var names = ctx.IdNames;
            if (names != null && names.TryGetBoardName(id, out var name) && !string.IsNullOrEmpty(name))
                return $"{id}('{name}')";
            return id.ToString();
        }

        private static string FormatKeyId(in ExecCtx<TCtx> ctx, int id)
        {
            var names = ctx.IdNames;
            if (names != null && names.TryGetKeyName(id, out var name) && !string.IsNullOrEmpty(name))
                return $"{id}('{name}')";
            return id.ToString();
        }

        private static string FormatFieldId(in ExecCtx<TCtx> ctx, int id)
        {
            var names = ctx.IdNames;
            if (names != null && names.TryGetFieldName(id, out var name) && !string.IsNullOrEmpty(name))
                return $"{id}('{name}')";
            return id.ToString();
        }
    }
}

