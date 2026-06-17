using System;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// 触发器计划转换器
    /// 统一 TriggerPlanJsonDatabase 的转换逻辑
    /// </summary>
    internal sealed class TriggerPlanConverter
    {
        private const string TemplateParamKind = "TemplateParam";
        private System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.NumericValueRefDto> _templateBindings;

        internal TriggerPlan<object> Convert(TriggerPlanJsonDatabase.TriggerPlanDto dto, ITriggerCue cue = null)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var previousBindings = _templateBindings;
            _templateBindings = BuildTemplateBindings(dto.Template);
            try
            {
                return ConvertCore(dto, cue);
            }
            finally
            {
                _templateBindings = previousBindings;
            }
        }

        private TriggerPlan<object> ConvertCore(TriggerPlanJsonDatabase.TriggerPlanDto dto, ITriggerCue cue)
        {
            var actions = ConvertActions(dto.Actions);
            var pred = dto.Predicate;

            if (pred == null || string.Equals(pred.Kind, "none", StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerPlan<object>(
                    phase: dto.Phase,
                    priority: dto.Priority,
                    triggerId: dto.TriggerId,
                    actions: actions,
                    interruptPriority: 0,
                    cue: cue,
                    schedule: default,
                    executionControl: ConvertExecutionControl(dto.ExecutionControl));
            }

            if (string.Equals(pred.Kind, "expr", StringComparison.OrdinalIgnoreCase))
            {
                var expr = new PredicateExprPlan(BuildExprNodes(pred.Nodes));
                return new TriggerPlan<object>(
                    phase: dto.Phase,
                    priority: dto.Priority,
                    triggerId: dto.TriggerId,
                    predicateExpr: expr,
                    actions: actions,
                    interruptPriority: 0,
                    cue: cue,
                    schedule: default,
                    executionControl: ConvertExecutionControl(dto.ExecutionControl));
            }

            throw new NotSupportedException($"Predicate kind not supported: {pred.Kind}");
        }

        private static TriggerExecutionControlPlan ConvertExecutionControl(TriggerPlanJsonDatabase.ExecutionControlPlanDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Mode))
            {
                return TriggerExecutionControlPlan.Always;
            }

            var modeText = dto.Mode.Trim().ToLowerInvariant();
            switch (modeText)
            {
                case "once":
                    return new TriggerExecutionControlPlan(ETriggerExecutionMode.Once, maxExecutions: 1);
                case "cooldown":
                    return new TriggerExecutionControlPlan(ETriggerExecutionMode.Cooldown, cooldownMs: Math.Max(0f, dto.CooldownMs));
                case "repeat":
                    return new TriggerExecutionControlPlan(ETriggerExecutionMode.Repeat, maxExecutions: Math.Max(0, dto.MaxExecutions));
                case "always":
                default:
                    return TriggerExecutionControlPlan.Always;
            }
        }

        internal ActionCallPlan[] ConvertActions(System.Collections.Generic.List<TriggerPlanJsonDatabase.ActionCallPlanDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return Array.Empty<ActionCallPlan>();

            var arr = new ActionCallPlan[dtos.Count];
            for (int i = 0; i < dtos.Count; i++)
            {
                arr[i] = ConvertAction(dtos[i]);
            }
            return arr;
        }

        internal ITriggerPlanExecutable ConvertExecutionRoot(TriggerPlanJsonDatabase.TriggerPlanDto dto)
        {
            if (dto == null) return null;

            var previousBindings = _templateBindings;
            _templateBindings = BuildTemplateBindings(dto.Template);
            try
            {
                return ConvertExecutionRootCore(dto);
            }
            finally
            {
                _templateBindings = previousBindings;
            }
        }

        private ITriggerPlanExecutable ConvertExecutionRootCore(TriggerPlanJsonDatabase.TriggerPlanDto dto)
        {
            var explicitRoot = ConvertExecutionNode(dto.ExecutionRoot);
            if (explicitRoot != null)
                return explicitRoot;

            var actions = ConvertActions(dto.Actions);
            if (actions.Length == 0)
                return null;

            var children = new ITriggerPlanExecutable[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                children[i] = new ActionCallTriggerPlanExecutable(actions[i]);
            }

            return new SequenceTriggerPlanExecutable(children);
        }

        private ITriggerPlanExecutable ConvertExecutionNode(TriggerPlanJsonDatabase.ExecutionNodeDto dto)
        {
            if (dto == null) return null;

            if (!Enum.TryParse<ETriggerPlanExecutableKind>(NormalizeKind(dto.Kind), true, out var kind))
            {
                throw new InvalidOperationException($"Execution node kind not supported: {dto.Kind}");
            }

            var condition = ConvertCondition(dto.Condition);
            var children = ConvertExecutionNodes(dto.Children);
            var elseChildren = ConvertExecutionNodes(dto.ElseChildren);

            switch (kind)
            {
                case ETriggerPlanExecutableKind.Action:
                    if (dto.Action == null)
                    {
                        throw new InvalidOperationException("Action execution node requires Action payload.");
                    }
                    return new ActionCallTriggerPlanExecutable(ConvertAction(dto.Action), condition, dto.Weight);
                case ETriggerPlanExecutableKind.Sequence:
                    return new SequenceTriggerPlanExecutable(children, condition, dto.Weight);
                case ETriggerPlanExecutableKind.Selector:
                    return new SelectorTriggerPlanExecutable(children, condition, dto.Weight);
                case ETriggerPlanExecutableKind.Random:
                    return new RandomTriggerPlanExecutable(children, condition, dto.Weight);
                case ETriggerPlanExecutableKind.Parallel:
                    return new ParallelTriggerPlanExecutable(children, condition, dto.Weight);
                case ETriggerPlanExecutableKind.If:
                    return new IfTriggerPlanExecutable(
                        condition,
                        BuildBranch(children),
                        BuildBranch(elseChildren),
                        guardCondition: null,
                        weight: dto.Weight);
                case ETriggerPlanExecutableKind.Repeat:
                    return new RepeatTriggerPlanExecutable(BuildBranch(children), dto.Count, condition, dto.Weight);
                case ETriggerPlanExecutableKind.Until:
                    return new UntilTriggerPlanExecutable(
                        BuildBranch(children),
                        ConvertCondition(dto.UntilCondition),
                        dto.MaxIterations,
                        condition,
                        dto.Weight);
                case ETriggerPlanExecutableKind.Invert:
                    return new InvertTriggerPlanExecutable(BuildBranch(children), condition, dto.Weight);
                case ETriggerPlanExecutableKind.Succeed:
                    return new SucceedTriggerPlanExecutable(BuildBranch(children), condition, dto.Weight);
                case ETriggerPlanExecutableKind.Fail:
                    return new FailTriggerPlanExecutable(BuildBranch(children), dto.Reason, condition, dto.Weight);
                default:
                    throw new InvalidOperationException($"Execution node kind not supported: {kind}");
            }
        }

        private ITriggerPlanExecutable[] ConvertExecutionNodes(System.Collections.Generic.List<TriggerPlanJsonDatabase.ExecutionNodeDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return Array.Empty<ITriggerPlanExecutable>();

            var nodes = new ITriggerPlanExecutable[dtos.Count];
            for (int i = 0; i < dtos.Count; i++)
            {
                nodes[i] = ConvertExecutionNode(dtos[i]);
            }

            return nodes;
        }

        private static ITriggerPlanExecutable BuildBranch(ITriggerPlanExecutable[] children)
        {
            if (children == null || children.Length == 0) return null;
            return children.Length == 1 ? children[0] : new SequenceTriggerPlanExecutable(children);
        }

        private ITriggerPlanCondition ConvertCondition(TriggerPlanJsonDatabase.PredicatePlanDto dto)
        {
            var expr = ConvertPredicateExpr(dto);
            return expr.Nodes == null || expr.Nodes.Length == 0 ? null : new PredicateExprTriggerPlanCondition(expr);
        }

        private static string NormalizeKind(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return nameof(ETriggerPlanExecutableKind.Sequence);

            switch (kind.Trim().ToLowerInvariant())
            {
                case "action":
                    return nameof(ETriggerPlanExecutableKind.Action);
                case "sequence":
                case "seq":
                    return nameof(ETriggerPlanExecutableKind.Sequence);
                case "selector":
                case "select":
                    return nameof(ETriggerPlanExecutableKind.Selector);
                case "random":
                case "random_selector":
                case "randomselector":
                    return nameof(ETriggerPlanExecutableKind.Random);
                case "if":
                case "ifelse":
                case "if_else":
                    return nameof(ETriggerPlanExecutableKind.If);
                case "parallel":
                case "all":
                    return nameof(ETriggerPlanExecutableKind.Parallel);
                case "repeat":
                case "loop":
                    return nameof(ETriggerPlanExecutableKind.Repeat);
                case "until":
                case "repeat_until":
                case "repeatuntil":
                    return nameof(ETriggerPlanExecutableKind.Until);
                case "invert":
                case "not":
                    return nameof(ETriggerPlanExecutableKind.Invert);
                case "succeed":
                case "success":
                case "always_success":
                case "alwayssuccess":
                    return nameof(ETriggerPlanExecutableKind.Succeed);
                case "fail":
                case "failure":
                case "always_fail":
                case "alwaysfail":
                    return nameof(ETriggerPlanExecutableKind.Fail);
                default:
                    return kind;
            }
        }

        private ActionCallPlan ConvertAction(TriggerPlanJsonDatabase.ActionCallPlanDto dto)
        {
            if (dto == null) return default;

            var id = new ActionId(dto.ActionId);
            var executionPolicy = ParseActionExecutionPolicy(dto.ExecutionPolicy);
            var retryMaxRetries = dto.RetryMaxRetries;

            if (dto.RetryMaxRetries < 0)
            {
                throw new InvalidOperationException($"RetryMaxRetries cannot be negative: {dto.RetryMaxRetries} actionId={dto.ActionId}");
            }

            if (dto.RetryDelayMs < 0f)
            {
                throw new InvalidOperationException($"RetryDelayMs cannot be negative: {dto.RetryDelayMs} actionId={dto.ActionId}");
            }

            if (dto.Args != null && dto.Args.Count > 0)
            {
                if (dto.Arity > 2)
                {
                    throw new InvalidOperationException($"Unsupported named action arity: {dto.Arity} actionId={dto.ActionId}. PlannedTrigger currently supports arity 0/1/2; use Action schema named args for additional business parameters.");
                }

                var namedArgs = new System.Collections.Generic.Dictionary<string, ActionArgValue>(dto.Args.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in dto.Args)
                {
                    namedArgs[kv.Key] = new ActionArgValue(ConvertNumericValueRef(kv.Value), kv.Key);
                }

                var arity = dto.Arity;
                var arg0 = arity > 0 ? ConvertNumericValueRef(dto.Arg0) : default;
                var arg1 = arity > 1 ? ConvertNumericValueRef(dto.Arg1) : default;
                return new ActionCallPlan(
                    id,
                    (byte)arity,
                    arg0,
                    arg1,
                    namedArgs,
                    EActionScheduleMode.Immediate,
                    0,
                    -1,
                    true,
                    executionPolicy,
                    retryMaxRetries,
                    dto.RetryDelayMs);
            }

            ActionCallPlan plan;
            switch (dto.Arity)
            {
                case 0:
                    plan = new ActionCallPlan(id);
                    break;
                case 1:
                    plan = new ActionCallPlan(id, ConvertNumericValueRef(dto.Arg0));
                    break;
                case 2:
                    plan = new ActionCallPlan(id, ConvertNumericValueRef(dto.Arg0), ConvertNumericValueRef(dto.Arg1));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported action arity: {dto.Arity} actionId={dto.ActionId}");
            }

            return new ActionCallPlan(
                plan.Id,
                plan.Arity,
                plan.Arg0,
                plan.Arg1,
                plan.Args,
                plan.ScheduleMode,
                plan.ScheduleParam,
                plan.MaxExecutions,
                plan.CanBeInterrupted,
                executionPolicy,
                retryMaxRetries,
                dto.RetryDelayMs);
        }

        private static EActionExecutionPolicy ParseActionExecutionPolicy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return EActionExecutionPolicy.Immediate;
            }

            if (Enum.TryParse<EActionExecutionPolicy>(value, true, out var policy))
            {
                return policy;
            }

            throw new InvalidOperationException($"Unknown action execution policy: {value}");
        }

        private NumericValueRef ConvertNumericValueRef(TriggerPlanJsonDatabase.NumericValueRefDto dto)
        {
            if (dto == null) return default;

            dto = ResolveTemplateParam(dto, new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase));

            if (!Enum.TryParse<ENumericValueRefKind>(dto.Kind, out var kind))
            {
                throw new InvalidOperationException($"Unknown NumericValueRef kind: {dto.Kind}");
            }

            return kind switch
            {
                ENumericValueRefKind.Const => NumericValueRef.Const(dto.ConstValue),
                ENumericValueRefKind.Blackboard => NumericValueRef.Blackboard(dto.BoardId, dto.KeyId),
                ENumericValueRefKind.PayloadField => NumericValueRef.PayloadField(dto.FieldId),
                ENumericValueRefKind.Var => NumericValueRef.Var(dto.DomainId, dto.Key),
                ENumericValueRefKind.Expr => NumericValueRef.Expr(dto.ExprText),
                _ => throw new InvalidOperationException($"Unsupported NumericValueRef kind: {kind}")
            };
        }

        private static System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.NumericValueRefDto> BuildTemplateBindings(TriggerPlanJsonDatabase.TriggerTemplateBindingDto dto)
        {
            if (dto == null || dto.Bindings == null || dto.Bindings.Count == 0)
            {
                return null;
            }

            return new System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.NumericValueRefDto>(dto.Bindings, StringComparer.OrdinalIgnoreCase);
        }

        private TriggerPlanJsonDatabase.NumericValueRefDto ResolveTemplateParam(
            TriggerPlanJsonDatabase.NumericValueRefDto dto,
            System.Collections.Generic.HashSet<string> resolving)
        {
            while (dto != null && string.Equals(dto.Kind, TemplateParamKind, StringComparison.OrdinalIgnoreCase))
            {
                var key = dto.Key;
                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException("TemplateParam NumericValueRef requires Key.");
                }

                if (_templateBindings == null || !_templateBindings.TryGetValue(key, out var bound) || bound == null)
                {
                    throw new InvalidOperationException($"Template parameter is not bound: {key}");
                }

                if (!resolving.Add(key))
                {
                    throw new InvalidOperationException($"Cyclic template parameter binding detected: {key}");
                }

                dto = bound;
            }

            return dto;
        }

        private PredicateExprPlan ConvertPredicateExpr(TriggerPlanJsonDatabase.PredicatePlanDto dto)
        {
            if (dto == null || string.Equals(dto.Kind, "none", StringComparison.OrdinalIgnoreCase))
                return default;

            if (!string.Equals(dto.Kind, "expr", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Predicate kind not supported: {dto.Kind}");
            }

            return new PredicateExprPlan(BuildExprNodes(dto.Nodes));
        }

        private BoolExprNode[] BuildExprNodes(System.Collections.Generic.List<TriggerPlanJsonDatabase.BoolExprNodeDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return Array.Empty<BoolExprNode>();

            var arr = new BoolExprNode[dtos.Count];
            for (int i = 0; i < dtos.Count; i++)
            {
                var d = dtos[i];
                if (d == null)
                {
                    arr[i] = BoolExprNode.Const(true);
                    continue;
                }

                if (!Enum.TryParse<EBoolExprNodeKind>(d.Kind, out var kind))
                {
                    throw new InvalidOperationException($"Unknown expr node kind: {d.Kind}");
                }

                switch (kind)
                {
                    case EBoolExprNodeKind.Const:
                        arr[i] = BoolExprNode.Const(d.ConstValue);
                        break;
                    case EBoolExprNodeKind.Not:
                        arr[i] = BoolExprNode.Not();
                        break;
                    case EBoolExprNodeKind.And:
                        arr[i] = BoolExprNode.And();
                        break;
                    case EBoolExprNodeKind.Or:
                        arr[i] = BoolExprNode.Or();
                        break;
                    case EBoolExprNodeKind.CompareNumeric:
                        if (!Enum.TryParse<ECompareOp>(d.CompareOp, out var op))
                        {
                            throw new InvalidOperationException($"Unknown compare op: {d.CompareOp}");
                        }
                        arr[i] = BoolExprNode.Compare(op, ConvertNumericValueRef(d.Left), ConvertNumericValueRef(d.Right));
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported expr node kind: {kind}");
                }
            }

            return arr;
        }
    }
}
