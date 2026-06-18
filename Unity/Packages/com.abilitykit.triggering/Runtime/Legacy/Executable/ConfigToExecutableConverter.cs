using System;
using System.Collections.Generic;
using AbilityKit.Modifiers;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Executable
{
    /// <summary>
    /// 配置转行为实例的转换器。
    /// 仅用于旧 Runtime/Executable 配置兼容；新配置加载请走 Runtime.Plan.Json 与 Plan/Executables。
    /// </summary>
    [Obsolete("Runtime/Executable config converter is legacy compatibility only. Use Runtime.Plan.Json and Plan/Executables instead.")]
    public sealed class ConfigToExecutableConverter
    {
        private readonly FunctionRegistry _functions;
        private readonly ActionRegistry _actions;
        private readonly IIdNameRegistry _idNames;

        public ConfigToExecutableConverter(
            FunctionRegistry functions,
            ActionRegistry actions,
            IIdNameRegistry idNames = null)
        {
            _functions = functions ?? throw new ArgumentNullException(nameof(functions));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _idNames = idNames;
        }

        /// <summary>
        /// 从配置创建行为实例
        /// </summary>
        public ISimpleExecutable Convert(ExecutableConfig config)
        {
            if (config.TypeId > 0)
                return ConvertByTypeId(config);
            if (!string.IsNullOrEmpty(config.TypeName))
                return ConvertByTypeName(config);
            return InferFromConfig(config);
        }

        /// <summary>
        /// 从配置列表创建 Sequence
        /// </summary>
        public SequenceExecutable ConvertToSequence(List<ExecutableConfig> configs)
        {
            var sequence = new SequenceExecutable();
            if (configs == null || configs.Count == 0)
                return sequence;
            foreach (var config in configs)
            {
                var executable = Convert(config);
                if (executable != null)
                    sequence.Add(executable);
            }
            return sequence;
        }

        private ISimpleExecutable ConvertByTypeId(ExecutableConfig config)
        {
            if (!TryCreateFromRegistry(config.TypeId, out var executable))
            {
                // 回退到硬编码的兼容处理
                return ConvertByTypeIdLegacy(config);
            }
            return executable;
        }

        private bool TryCreateFromRegistry(int typeId, out ISimpleExecutable executable)
        {
            executable = null;
            if (ExecutableRegistry.Instance.TryGetDescriptor(typeId, out var descriptor))
            {
                executable = descriptor.Factory() as ISimpleExecutable;
                return executable != null;
            }
            return false;
        }

        /// <summary>
        /// 兼容旧代码：通过类型 ID 创建实例。
        /// 下线条件：旧 JSON/旧示例不再依赖硬编码 TypeId fallback 后删除。
        /// </summary>
        [Obsolete("Legacy hardcoded executable TypeId fallback. Prefer ExecutableRegistry descriptors or Runtime.Plan.Json conversion.")]
        private ISimpleExecutable ConvertByTypeIdLegacy(ExecutableConfig config)
        {
            return config.TypeId switch
            {
                ExecutableModule.ExecutableTypeIds.Sequence => ConvertSequence(config),
                ExecutableModule.ExecutableTypeIds.Selector => ConvertSelector(config),
                ExecutableModule.ExecutableTypeIds.Parallel => ConvertParallel(config),
                ExecutableModule.ExecutableTypeIds.If => ConvertIf(config),
                ExecutableModule.ExecutableTypeIds.IfElse => ConvertIfElse(config),
                ExecutableModule.ExecutableTypeIds.Switch => ConvertSwitch(config),
                ExecutableModule.ExecutableTypeIds.RandomSelector => ConvertRandomSelector(config),
                ExecutableModule.ExecutableTypeIds.Repeat => ConvertRepeat(config),
                ExecutableModule.ExecutableTypeIds.ActionCall => ConvertActionCall(config),
                ExecutableModule.ExecutableTypeIds.Delay => ConvertDelay(config),
                ExecutableModule.ExecutableTypeIds.Schedule => ConvertSchedule(config),
                _ => throw new NotSupportedException($"Executable type id {config.TypeId} not supported")
            };
        }

        private ISimpleExecutable ConvertByTypeName(ExecutableConfig config)
        {
            if (ExecutableRegistry.Instance.TryGetTypeIdByName(config.TypeName, out var typeId))
            {
                if (TryCreateFromRegistry(typeId, out var executable))
                {
                    return ConvertByNameWithConfig(executable, config);
                }
            }

            // 回退到字符串匹配
            return config.TypeName?.ToLowerInvariant() switch
            {
                "sequence" => ConvertSequence(config),
                "selector" => ConvertSelector(config),
                "parallel" => ConvertParallel(config),
                "if" => ConvertIf(config),
                "ifelse" => ConvertIfElse(config),
                "elseif" => ConvertIfElse(config),
                "switch" => ConvertSwitch(config),
                "randomselector" => ConvertRandomSelector(config),
                "repeat" => ConvertRepeat(config),
                "actioncall" => ConvertActionCall(config),
                "action" => ConvertActionCall(config),
                "delay" => ConvertDelay(config),
                "schedule" => ConvertSchedule(config),
                _ => throw new NotSupportedException($"Executable type name '{config.TypeName}' not supported")
            };
        }

        private ISimpleExecutable ConvertByNameWithConfig(ISimpleExecutable executable, ExecutableConfig config)
        {
            switch (executable)
            {
                case SequenceExecutable seq:
                    ConvertSequenceTo(seq, config);
                    return seq;
                case SelectorExecutable sel:
                    ConvertSelectorTo(sel, config);
                    return sel;
                case IfExecutable @if:
                    ConvertIfTo(@if, config);
                    return @if;
                // ... 其他类型类似处理
                default:
                    return executable;
            }
        }

        private void ConvertSequenceTo(SequenceExecutable seq, ExecutableConfig config)
        {
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        seq.Add(child);
                }
            }
        }

        private void ConvertSelectorTo(SelectorExecutable sel, ExecutableConfig config)
        {
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        sel.Add(child);
                }
            }
        }

        private void ConvertIfTo(IfExecutable @if, ExecutableConfig config)
        {
            if (config.Condition != null)
                @if.Condition = ConvertCondition(config.Condition);
            if (config.Children != null && config.Children.Count > 0)
                @if.Body = ConvertToSequence(config.Children);
        }

        private ISimpleExecutable InferFromConfig(ExecutableConfig config)
        {
            if (config.Children != null && config.Children.Count > 0)
                return ConvertSequence(config);
            if (config.ActionCall.HasValue)
                return ConvertActionCall(config);
            if (config.Delay.HasValue)
                return ConvertDelay(config);
            if (config.Switch != null)
                return ConvertSwitch(config);
            if (!string.IsNullOrEmpty(config.Schedule.ScheduleMode))
                return ConvertSchedule(config);
            return new SequenceExecutable();
        }

        private SequenceExecutable ConvertSequence(ExecutableConfig config)
        {
            var sequence = new SequenceExecutable();
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        sequence.Add(child);
                }
            }
            return sequence;
        }

        private SelectorExecutable ConvertSelector(ExecutableConfig config)
        {
            var selector = new SelectorExecutable();
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        selector.Add(child);
                }
            }
            return selector;
        }

        private ParallelExecutable ConvertParallel(ExecutableConfig config)
        {
            var parallel = new ParallelExecutable();
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        parallel.Add(child);
                }
            }
            return parallel;
        }

        private RandomSelectorExecutable ConvertRandomSelector(ExecutableConfig config)
        {
            var random = new RandomSelectorExecutable();
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    var child = Convert(childConfig);
                    if (child != null)
                        random.Children.Add(child);
                }
            }
            return random;
        }

        private RepeatExecutable ConvertRepeat(ExecutableConfig config)
        {
            var repeat = new RepeatExecutable();
            if (config.Children != null && config.Children.Count > 0)
            {
                repeat.Child = ConvertToSequence(config.Children);
            }
            return repeat;
        }

        private IfExecutable ConvertIf(ExecutableConfig config)
        {
            ICondition condition = null;
            if (config.Condition != null)
                condition = ConvertCondition(config.Condition);
            ISimpleExecutable body = null;
            if (config.Children != null && config.Children.Count > 0)
                body = ConvertToSequence(config.Children);
            return new IfExecutable { Condition = condition, Body = body };
        }

        private IfElseExecutable ConvertIfElse(ExecutableConfig config)
        {
            var ifElse = new IfElseExecutable();
            if (config.Children != null)
            {
                foreach (var childConfig in config.Children)
                {
                    if (childConfig.TypeId == ExecutableModule.ExecutableTypeIds.If || childConfig.TypeName == "If")
                    {
                        ICondition condition = null;
                        if (childConfig.Condition != null)
                            condition = ConvertCondition(childConfig.Condition);
                        ISimpleExecutable body = null;
                        if (childConfig.Children != null && childConfig.Children.Count > 0)
                            body = ConvertToSequence(childConfig.Children);
                        ifElse.If(condition, body);
                    }
                    else if (childConfig.TypeName == "Else")
                    {
                        ISimpleExecutable body = null;
                        if (childConfig.Children != null && childConfig.Children.Count > 0)
                            body = ConvertToSequence(childConfig.Children);
                        ifElse.Else(body);
                    }
                }
            }
            return ifElse;
        }

        private SwitchExecutable ConvertSwitch(ExecutableConfig config)
        {
            var switchExec = new SwitchExecutable();
            if (config.Switch != null)
            {
                if (!string.IsNullOrEmpty(config.Switch.ValueSelector))
                    switchExec.ValueSelector = ctx => EvaluateValueSelector(config.Switch.ValueSelector, ctx);
                if (config.Switch.Cases != null)
                {
                    foreach (var caseConfig in config.Switch.Cases)
                    {
                        ISimpleExecutable body = null;
                        if (caseConfig.Body != null)
                            body = Convert(caseConfig.Body);
                        switchExec.Case(caseConfig.Value, body);
                    }
                }
                if (config.Switch.DefaultCase != null)
                    switchExec.Default(Convert(config.Switch.DefaultCase));
            }
            return switchExec;
        }

        private ActionCallExecutable ConvertActionCall(ExecutableConfig config)
        {
            var actionConfig = config.ActionCall.Value;
            var actionId = new ActionId(actionConfig.ActionId);

            // 构建参数引用
            var arg0 = ConvertNumericValueRef(actionConfig.Arg0);
            var arg1 = ConvertNumericValueRef(actionConfig.Arg1);

            // 根据 arity 创建对应的 ActionCallExecutable
            // 注意：这里我们创建一个包装器，在 Execute 时从 ActionRegistry 解析
            // 未来可以优化为转换期直接绑定委托（需要 ActionRegistry 已注册完成）
            return new ActionCallExecutable
            {
                ActionId = actionId,
                Arity = actionConfig.Arity,
                Arg0 = arg0,
                Arg1 = arg1
            };
        }

        private DelayExecutable ConvertDelay(ExecutableConfig config)
        {
            var delayConfig = config.Delay.Value;
            return new DelayExecutable { DelayMs = delayConfig.DelayMs };
        }

        /// <summary>
        /// 转换调度行为。
        /// 仅保留旧配置兼容映射；新配置请使用 Runtime.Plan.Json 与 Plan/Executables 的 Scheduled 节点。
        /// </summary>
        internal IScheduledExecutable ConvertSchedule(ExecutableConfig config)
        {
            var mode = config.Schedule.ScheduleMode ?? "external";

            ISimpleExecutable body = null;
            if (config.Children != null && config.Children.Count > 0)
            {
                body = ConvertToSequence(config.Children);
            }

            var scheduledExec = CreateLegacyScheduledExecutable(mode, config.Schedule, body);
            if (scheduledExec == null)
                return null;

            SetModifiersIfPossible(scheduledExec, config.Schedule.Modifiers, config.TypeId);
            return scheduledExec;
        }

        private static IScheduledExecutable CreateLegacyScheduledExecutable(string mode, ScheduleConfig config, ISimpleExecutable body)
        {
            switch ((mode ?? "external").ToLowerInvariant())
            {
                case "timed":
                case "delay":
                    return ScheduledExecutableFactory.WrapTimed(body, config.DurationMs);
                case "periodic":
                case "interval":
                    return ScheduledExecutableFactory.WrapPeriodic(body, config.PeriodMs, config.MaxExecutions);
                case "external":
                case "conditional":
                case "continuous":
                    return ScheduledExecutableFactory.WrapExternal(body);
                default:
                    return ScheduledExecutableFactory.WrapExternal(body);
            }
        }

        private static void SetModifiersIfPossible(IScheduledExecutable exec, List<ModifierDataConfig> configs, int sourceId)
        {
            if (exec is ModifierApplyingPeriodicExecutable modifierExec && configs != null)
            {
                modifierExec.Modifiers = ConvertModifiers(configs, sourceId);
                modifierExec.SourceId = sourceId;
            }
        }

        /// <summary>
        /// 将 ModifierDataConfig 列表转换为 ModifierData 数组
        /// </summary>
        private static ModifierData[] ConvertModifiers(List<ModifierDataConfig> configs, int sourceId)
        {
            if (configs == null || configs.Count == 0)
                return null;

            var modifiers = new ModifierData[configs.Count];
            for (int i = 0; i < configs.Count; i++)
            {
                modifiers[i] = ConvertModifierData(configs[i], sourceId);
            }
            return modifiers;
        }

        /// <summary>
        /// 将单个 ModifierDataConfig 转换为 ModifierData
        /// </summary>
        private static ModifierData ConvertModifierData(ModifierDataConfig config, int sourceId)
        {
            var op = ParseModifierOp(config.ModifierType);
            var key = ParseModifierKey(config.Key);

            return new ModifierData
            {
                Key = key,
                Op = op,
                Magnitude = MagnitudeSource.Fixed(config.Value),
                SourceId = sourceId
            };
        }

        private static ModifierOp ParseModifierOp(string modifierType)
        {
            if (string.IsNullOrEmpty(modifierType))
                return ModifierOp.Add;

            return modifierType.ToLowerInvariant() switch
            {
                "add" or "+" => ModifierOp.Add,
                "mul" or "*" or "multiply" => ModifierOp.Mul,
                "override" or "=" or "set" => ModifierOp.Override,
                "percentadd" or "%" or "percent" => ModifierOp.PercentAdd,
                _ => ModifierOp.Add
            };
        }

        private static ModifierKey ParseModifierKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return ModifierKey.None;

            // 支持预定义键的字符串映射
            return key.ToLowerInvariant() switch
            {
                "movespeed" or "movespeed%max" => ModifierKey.MoveSpeed,
                "shieldmax" or "shield" => ModifierKey.ShieldMax,
                "shieldregen" => ModifierKey.ShieldRegen,
                "dotdamage" or "dot" => ModifierKey.Create(100),
                "hotheal" or "hot" => ModifierKey.Create(101),
                "healtaken" => ModifierKey.Create(102),
                _ => ModifierKey.Create((byte)(key.GetHashCode() & 0xFF))
            };
        }

        /// <summary>
        /// 转换条件
        /// </summary>
        public ICondition ConvertCondition(ConditionConfig config)
        {
            if (config.TypeId > 0)
                return ConvertConditionByTypeId(config);
            if (!string.IsNullOrEmpty(config.TypeName))
                return ConvertConditionByTypeName(config);
            return InferCondition(config);
        }

        private ICondition ConvertConditionByTypeId(ConditionConfig config)
        {
            if (TryCreateConditionFromRegistry(config.TypeId, config, out var condition))
                return condition;

            return config.TypeId switch
            {
                ExecutableModule.ConditionTypeIds.Const => new ConstCondition { Value = true },
                ExecutableModule.ConditionTypeIds.And => ConvertAndCondition(config),
                ExecutableModule.ConditionTypeIds.Or => ConvertOrCondition(config),
                ExecutableModule.ConditionTypeIds.Not => ConvertNotCondition(config),
                ExecutableModule.ConditionTypeIds.NumericCompare => ConvertNumericCompare(config),
                ExecutableModule.ConditionTypeIds.PayloadCompare => ConvertPayloadCompare(config),
                ExecutableModule.ConditionTypeIds.HasTarget => ConvertHasTarget(config),
                ExecutableModule.ConditionTypeIds.Multi => ConvertMultiCondition(config),
                _ => throw new NotSupportedException($"Condition type id {config.TypeId} not supported")
            };
        }

        private ICondition ConvertConditionByTypeName(ConditionConfig config)
        {
            if (ExecutableRegistry.Instance.TryGetConditionTypeIdByName(config.TypeName, out var typeId)
                && TryCreateConditionFromRegistry(typeId, config, out var condition))
                return condition;

            return config.TypeName?.ToLowerInvariant() switch
            {
                "const" => new ConstCondition { Value = true },
                "and" => ConvertAndCondition(config),
                "or" => ConvertOrCondition(config),
                "not" => ConvertNotCondition(config),
                "numericcompare" => ConvertNumericCompare(config),
                "payloadcompare" => ConvertPayloadCompare(config),
                "hastarget" => ConvertHasTarget(config),
                "multi" => ConvertMultiCondition(config),
                _ => throw new NotSupportedException($"Condition type name '{config.TypeName}' not supported")
            };
        }

        private bool TryCreateConditionFromRegistry(int typeId, ConditionConfig config, out ICondition condition)
        {
            condition = null;
            if (!ExecutableRegistry.Instance.TryGetConditionDescriptor(typeId, out _))
                return false;

            condition = ExecutableRegistry.Instance.CreateCondition(typeId);
            if (condition is IConfigurableCondition configurable)
                configurable.Configure(config, this);
            return true;
        }

        private ICondition InferCondition(ConditionConfig config)
        {
            if (config.Children != null && config.Children.Count > 0)
            {
                var combinator = ParseConditionCombinator(config.Combinator);
                var multi = new MultiCondition
                {
                    Combinator = combinator
                };
                foreach (var childConfig in config.Children)
                    multi.Conditions.Add(ConvertCondition(childConfig));
                return multi;
            }
            if (config.FieldId > 0)
                return ConvertPayloadCompare(config);
            if (config.Left.HasValue && config.Right.HasValue)
                return ConvertNumericCompare(config);
            throw new InvalidOperationException("Legacy condition config cannot infer a valid condition. Specify TypeId, TypeName, children, or numeric compare operands explicitly.");
        }

        private AndCondition ConvertAndCondition(ConditionConfig config)
        {
            EnsureConditionChildCount(config, 2, "And");
            return new AndCondition
            {
                Left = ConvertCondition(config.Children[0]),
                Right = ConvertCondition(config.Children[1])
            };
        }

        private OrCondition ConvertOrCondition(ConditionConfig config)
        {
            EnsureConditionChildCount(config, 2, "Or");
            return new OrCondition
            {
                Left = ConvertCondition(config.Children[0]),
                Right = ConvertCondition(config.Children[1])
            };
        }

        private NotCondition ConvertNotCondition(ConditionConfig config)
        {
            EnsureConditionChildCount(config, 1, "Not");
            return new NotCondition { Inner = ConvertCondition(config.Children[0]) };
        }

        private NumericCompareCondition ConvertNumericCompare(ConditionConfig config)
        {
            var op = ParseCompareOp(config.CompareOp);
            return new NumericCompareCondition
            {
                Op = op,
                Left = ConvertNumericValueRef(config.Left),
                Right = ConvertNumericValueRef(config.Right)
            };
        }

        private ICondition ConvertPayloadCompare(ConditionConfig config)
        {
            throw new NotSupportedException("Legacy PayloadCompare config is not supported by the formal triggering runtime. Use TriggerPlan payload predicates or a registered condition extension instead.");
        }

        private ICondition ConvertHasTarget(ConditionConfig config)
        {
            throw new NotSupportedException("Legacy HasTarget config is not supported in the triggering package. Target lookup belongs to the targeting framework package and should be exposed as a formal predicate extension.");
        }

        private MultiCondition ConvertMultiCondition(ConditionConfig config)
        {
            if (config.Children == null || config.Children.Count == 0)
                throw new InvalidOperationException("Multi condition requires at least one child condition.");

            var multi = new MultiCondition
            {
                Combinator = ParseConditionCombinator(config.Combinator)
            };
            foreach (var childConfig in config.Children)
                multi.Conditions.Add(ConvertCondition(childConfig));
            return multi;
        }

        internal NumericValueRef ConvertNumericValueRef(NumericValueRefDto dto)
        {
            if (!dto.HasValue)
                throw new InvalidOperationException("Numeric value reference is required but was not specified.");
            return dto.Kind?.ToLowerInvariant() switch
            {
                "const" => NumericValueRef.Const(dto.ConstValue),
                "blackboard" => NumericValueRef.Blackboard(dto.BoardId, dto.KeyId),
                "payload" or "payloadfield" => NumericValueRef.PayloadField(dto.FieldId),
                "var" => NumericValueRef.Var(dto.DomainId, dto.Key),
                _ => throw new NotSupportedException($"Numeric value reference kind '{dto.Kind}' is not supported.")
            };
        }

        internal static ECompareOp ParseCompareOp(string op)
        {
            if (string.IsNullOrEmpty(op))
                throw new InvalidOperationException("Compare operator is required for numeric conditions.");
            return op.ToLowerInvariant() switch
            {
                "eq" or "equal" or "==" => ECompareOp.Equal,
                "ne" or "notequal" or "!=" => ECompareOp.NotEqual,
                "gt" or "greaterthan" or ">" => ECompareOp.GreaterThan,
                "ge" or "greaterthanorequal" or ">=" => ECompareOp.GreaterThanOrEqual,
                "lt" or "lessthan" or "<" => ECompareOp.LessThan,
                "le" or "lessthanorequal" or "<=" => ECompareOp.LessThanOrEqual,
                _ => throw new NotSupportedException($"Compare operator '{op}' is not supported.")
            };
        }

        private static EConditionCombinator ParseConditionCombinator(string combinator)
        {
            if (string.IsNullOrEmpty(combinator))
                throw new InvalidOperationException("Condition combinator is required for multi-child legacy conditions.");
            return combinator.ToLowerInvariant() switch
            {
                "and" => EConditionCombinator.And,
                "or" => EConditionCombinator.Or,
                _ => throw new NotSupportedException($"Condition combinator '{combinator}' is not supported.")
            };
        }

        private static void EnsureConditionChildCount(ConditionConfig config, int requiredCount, string conditionName)
        {
            if (config.Children == null || config.Children.Count < requiredCount)
                throw new InvalidOperationException($"{conditionName} condition requires at least {requiredCount} child condition(s).");
        }

        internal int EvaluateValueSelector(string expression, object ctx)
        {
            throw new NotSupportedException("Legacy switch value selector expression evaluation is not supported. Use Runtime.Plan executables or registered action logic instead.");
        }

        // ========================================================================
        // 新增行为类型转换方法
    }
}
