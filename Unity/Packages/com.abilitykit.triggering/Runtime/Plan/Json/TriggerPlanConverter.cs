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
        private static readonly System.Collections.Generic.Dictionary<ETriggerPlanExecutableKind, ExecutionNodeConverterBase> _executionNodeConverters = BuildExecutionNodeConverters();
        private System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.NumericValueRefDto> _templateBindings;
        private System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.ExecutionNodeDto> _behaviorCatalog;
        private System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.ExecutionNodeDto> _nodeCatalog;
        private System.Collections.Generic.HashSet<string> _executionNodeResolving;

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
            return ConvertExecutionRoot(dto, null);
        }

        internal ITriggerPlanExecutable ConvertExecutionRoot(
            TriggerPlanJsonDatabase.TriggerPlanDto dto,
            TriggerPlanJsonDatabase.TriggerPlanDatabaseDto databaseDto)
        {
            if (dto == null) return null;

            var previousBindings = _templateBindings;
            var previousBehaviorCatalog = _behaviorCatalog;
            var previousNodeCatalog = _nodeCatalog;
            var previousExecutionNodeResolving = _executionNodeResolving;
            _templateBindings = BuildTemplateBindings(dto.Template);
            _behaviorCatalog = BuildExecutionNodeCatalog(databaseDto?.Behaviors);
            _nodeCatalog = BuildExecutionNodeCatalog(databaseDto?.Nodes);
            _executionNodeResolving = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                return ConvertExecutionRootCore(dto);
            }
            finally
            {
                _templateBindings = previousBindings;
                _behaviorCatalog = previousBehaviorCatalog;
                _nodeCatalog = previousNodeCatalog;
                _executionNodeResolving = previousExecutionNodeResolving;
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

            var resolvingKeys = new System.Collections.Generic.List<string>();
            dto = ResolveExecutionNodeReference(dto, resolvingKeys);
            try
            {
                if (!Enum.TryParse<ETriggerPlanExecutableKind>(NormalizeKind(dto.Kind), true, out var kind))
                {
                    throw new InvalidOperationException($"Execution node kind not supported: {dto.Kind}");
                }

                if (!_executionNodeConverters.TryGetValue(kind, out var converter) || converter == null)
                {
                    throw new InvalidOperationException($"Execution node kind not supported: {kind}");
                }

                return converter.Convert(this, dto);
            }
            finally
            {
                EndResolveExecutionNodeReference(resolvingKeys);
            }
        }

        private static System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.ExecutionNodeDto> BuildExecutionNodeCatalog(
            System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.ExecutionNodeDto> catalog)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            return new System.Collections.Generic.Dictionary<string, TriggerPlanJsonDatabase.ExecutionNodeDto>(catalog, StringComparer.OrdinalIgnoreCase);
        }

        private TriggerPlanJsonDatabase.ExecutionNodeDto ResolveExecutionNodeReference(
            TriggerPlanJsonDatabase.ExecutionNodeDto dto,
            System.Collections.Generic.List<string> resolvingKeys)
        {
            while (dto != null && TryGetExecutionNodeReference(dto, out var id, out var kind))
            {
                var key = kind + ":" + id;
                if (_executionNodeResolving == null)
                {
                    _executionNodeResolving = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                if (!_executionNodeResolving.Add(key))
                {
                    throw new InvalidOperationException($"Cyclic execution node reference detected: {key}");
                }

                resolvingKeys?.Add(key);
                dto = ResolveExecutionNodeReferenceTarget(id, kind);
            }

            return dto;
        }

        private TriggerPlanJsonDatabase.ExecutionNodeDto ResolveExecutionNodeReferenceTarget(string id, string kind)
        {
            if (string.Equals(kind, "behavior", StringComparison.OrdinalIgnoreCase))
            {
                if (_behaviorCatalog != null && _behaviorCatalog.TryGetValue(id, out var behavior) && behavior != null)
                {
                    return behavior;
                }

                throw new InvalidOperationException($"Behavior reference not found: {id}");
            }

            if (string.Equals(kind, "node", StringComparison.OrdinalIgnoreCase))
            {
                if (_nodeCatalog != null && _nodeCatalog.TryGetValue(id, out var node) && node != null)
                {
                    return node;
                }

                throw new InvalidOperationException($"Node reference not found: {id}");
            }

            if (_behaviorCatalog != null && _behaviorCatalog.TryGetValue(id, out var behaviorRef) && behaviorRef != null)
            {
                return behaviorRef;
            }

            if (_nodeCatalog != null && _nodeCatalog.TryGetValue(id, out var nodeRef) && nodeRef != null)
            {
                return nodeRef;
            }

            throw new InvalidOperationException($"Execution node reference not found: {id}");
        }

        private static bool TryGetExecutionNodeReference(TriggerPlanJsonDatabase.ExecutionNodeDto dto, out string id, out string kind)
        {
            id = null;
            kind = null;
            if (dto == null) return false;

            if (!string.IsNullOrEmpty(dto.BehaviorRef))
            {
                id = dto.BehaviorRef;
                kind = "behavior";
                return true;
            }

            if (!string.IsNullOrEmpty(dto.BehaviorId))
            {
                id = dto.BehaviorId;
                kind = "behavior";
                return true;
            }

            if (!string.IsNullOrEmpty(dto.NodeRef))
            {
                id = dto.NodeRef;
                kind = "node";
                return true;
            }

            if (!string.IsNullOrEmpty(dto.NodeId))
            {
                id = dto.NodeId;
                kind = "node";
                return true;
            }

            if (!string.IsNullOrEmpty(dto.Ref))
            {
                id = dto.Ref;
                kind = "any";
                return true;
            }

            return false;
        }

        private void EndResolveExecutionNodeReference(System.Collections.Generic.List<string> resolvingKeys)
        {
            if (resolvingKeys == null || _executionNodeResolving == null)
            {
                return;
            }

            for (int i = resolvingKeys.Count - 1; i >= 0; i--)
            {
                _executionNodeResolving.Remove(resolvingKeys[i]);
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

        private static System.Collections.Generic.Dictionary<ETriggerPlanExecutableKind, ExecutionNodeConverterBase> BuildExecutionNodeConverters()
        {
            return new System.Collections.Generic.Dictionary<ETriggerPlanExecutableKind, ExecutionNodeConverterBase>
            {
                [ETriggerPlanExecutableKind.Action] = new ActionExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Sequence] = new SequenceExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Selector] = new SelectorExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Random] = new RandomExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Parallel] = new ParallelExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.If] = new IfExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Repeat] = new RepeatExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Until] = new UntilExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Invert] = new InvertExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Succeed] = new SucceedExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Fail] = new FailExecutionNodeConverter(),
                [ETriggerPlanExecutableKind.Metadata] = new MetadataExecutionNodeConverter()
            };
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
                case "metadata":
                case "decorator":
                case "tags":
                case "tag":
                case "modifiers":
                case "modifier":
                case "stack":
                case "hierarchy":
                case "capability":
                case "duration":
                case "continuous":
                    return nameof(ETriggerPlanExecutableKind.Metadata);
                default:
                    return kind;
            }
        }

        private ActionCallPlan ConvertAction(TriggerPlanJsonDatabase.ActionCallPlanDto dto)
        {
            if (dto == null) return default;

            var id = new ActionId(dto.ActionId);
            var scheduleMode = ParseActionScheduleMode(dto.ScheduleMode);
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
                    scheduleMode,
                    dto.ScheduleParam,
                    dto.MaxExecutions,
                    dto.CanBeInterrupted,
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
                scheduleMode,
                dto.ScheduleParam,
                dto.MaxExecutions,
                dto.CanBeInterrupted,
                executionPolicy,
                retryMaxRetries,
                dto.RetryDelayMs);
        }

        private static EActionScheduleMode ParseActionScheduleMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return EActionScheduleMode.Immediate;
            }

            if (Enum.TryParse<EActionScheduleMode>(value, true, out var mode))
            {
                return mode;
            }

            throw new InvalidOperationException($"Unknown action schedule mode: {value}");
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

            var valueRef = kind switch
            {
                ENumericValueRefKind.Const => NumericValueRef.Const(dto.ConstValue),
                ENumericValueRefKind.Blackboard => NumericValueRef.Blackboard(dto.BoardId, dto.KeyId),
                ENumericValueRefKind.PayloadField => NumericValueRef.PayloadField(dto.FieldId),
                ENumericValueRefKind.Var => NumericValueRef.Var(dto.DomainId, dto.Key),
                ENumericValueRefKind.Expr => NumericValueRef.Expr(dto.ExprText),
                _ => throw new InvalidOperationException($"Unsupported NumericValueRef kind: {kind}")
            };

            if (dto.Required) valueRef = valueRef.AsRequired();
            if (dto.HasFallback) valueRef = valueRef.WithFallback(dto.FallbackValue);
            if (dto.HasMin) valueRef = valueRef.WithMin(dto.MinValue);
            if (dto.HasMax) valueRef = valueRef.WithMax(dto.MaxValue);
            if (dto.HasScale) valueRef = valueRef.WithScale(dto.Scale);
            if (dto.Offset != 0d) valueRef = valueRef.WithOffset(dto.Offset);
            if (!string.IsNullOrEmpty(dto.Label)) valueRef = valueRef.WithLabel(dto.Label);
            if (!string.IsNullOrEmpty(dto.Scope)) valueRef = valueRef.WithScope(dto.Scope);

            return valueRef;
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

        private abstract class ExecutionNodeConverterBase
        {
            public abstract ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto);

            protected static ITriggerPlanCondition Condition(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return context.ConvertCondition(dto.Condition);
            }

            protected static ITriggerPlanExecutable[] Children(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return context.ConvertExecutionNodes(dto.Children);
            }

            protected static ITriggerPlanExecutable Branch(
                ITriggerPlanExecutable[] children)
            {
                return BuildBranch(children);
            }
        }

        private sealed class ActionExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                if (dto.Action == null)
                {
                    throw new InvalidOperationException("Action execution node requires Action payload.");
                }

                return new ActionCallTriggerPlanExecutable(context.ConvertAction(dto.Action), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class SequenceExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new SequenceTriggerPlanExecutable(Children(context, dto), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class SelectorExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new SelectorTriggerPlanExecutable(Children(context, dto), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class RandomExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new RandomTriggerPlanExecutable(Children(context, dto), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class ParallelExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new ParallelTriggerPlanExecutable(Children(context, dto), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class IfExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new IfTriggerPlanExecutable(
                    Condition(context, dto),
                    Branch(Children(context, dto)),
                    Branch(context.ConvertExecutionNodes(dto.ElseChildren)),
                    guardCondition: null,
                    weight: dto.Weight);
            }
        }

        private sealed class RepeatExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new RepeatTriggerPlanExecutable(Branch(Children(context, dto)), dto.Count, Condition(context, dto), dto.Weight);
            }
        }

        private sealed class UntilExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new UntilTriggerPlanExecutable(
                    Branch(Children(context, dto)),
                    context.ConvertCondition(dto.UntilCondition),
                    dto.MaxIterations,
                    Condition(context, dto),
                    dto.Weight);
            }
        }

        private sealed class InvertExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new InvertTriggerPlanExecutable(Branch(Children(context, dto)), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class SucceedExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new SucceedTriggerPlanExecutable(Branch(Children(context, dto)), Condition(context, dto), dto.Weight);
            }
        }

        private sealed class FailExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                return new FailTriggerPlanExecutable(Branch(Children(context, dto)), dto.Reason, Condition(context, dto), dto.Weight);
            }
        }

        private sealed class MetadataExecutionNodeConverter : ExecutionNodeConverterBase
        {
            public override ITriggerPlanExecutable Convert(
                TriggerPlanConverter context,
                TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                var metadataKind = ParseMetadataKind(dto);
                return new MetadataTriggerPlanExecutable(
                    Branch(Children(context, dto)),
                    metadataKind,
                    dto.Values,
                    Condition(context, dto),
                    dto.Weight);
            }

            private static ETriggerPlanMetadataKind ParseMetadataKind(TriggerPlanJsonDatabase.ExecutionNodeDto dto)
            {
                var value = !string.IsNullOrWhiteSpace(dto.MetadataKind) ? dto.MetadataKind : dto.Kind;
                if (string.IsNullOrWhiteSpace(value))
                    return ETriggerPlanMetadataKind.Generic;

                switch (value.Trim().ToLowerInvariant())
                {
                    case "tag":
                    case "tags":
                        return ETriggerPlanMetadataKind.Tags;
                    case "modifier":
                    case "modifiers":
                        return ETriggerPlanMetadataKind.Modifiers;
                    case "stack":
                        return ETriggerPlanMetadataKind.Stack;
                    case "hierarchy":
                        return ETriggerPlanMetadataKind.Hierarchy;
                    case "capability":
                        return ETriggerPlanMetadataKind.Capability;
                    case "duration":
                        return ETriggerPlanMetadataKind.Duration;
                    case "continuous":
                        return ETriggerPlanMetadataKind.Continuous;
                }

                return Enum.TryParse<ETriggerPlanMetadataKind>(value, true, out var kind)
                    ? kind
                    : ETriggerPlanMetadataKind.Generic;
            }
        }
    }
}
