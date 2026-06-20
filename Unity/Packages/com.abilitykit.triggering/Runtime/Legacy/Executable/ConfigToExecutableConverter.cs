using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Schedule;
using AbilityKit.Triggering.Registry;

namespace AbilityKit.Triggering.Runtime.Executable
{
    /// <summary>
    /// 配置到可执行节点的转换器。
    /// </summary>
    [Obsolete("Runtime/Executable config converter is legacy compatibility only. Use Runtime.Plan.Json and Plan/Executables instead.")]
    public sealed class ConfigToExecutableConverter
    {
        private readonly ExecutableConverterStrategyRegistry _strategyRegistry;
        private readonly IExecutableConverterStrategy _defaultStrategy;
        private readonly bool _allowLegacyTypeIdFallback;

        public ConfigToExecutableConverter()
            : this(ExecutableConverterStrategyRegistry.Default, null, false)
        {
        }

        public ConfigToExecutableConverter(ExecutableConverterStrategyRegistry strategyRegistry, IExecutableConverterStrategy defaultStrategy = null, bool allowLegacyTypeIdFallback = false)
        {
            _strategyRegistry = strategyRegistry ?? ExecutableConverterStrategyRegistry.Default;
            _defaultStrategy = defaultStrategy;
            _allowLegacyTypeIdFallback = allowLegacyTypeIdFallback;
        }

        public ISimpleExecutable Convert(ExecutableConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!string.IsNullOrWhiteSpace(config.TypeName))
                return ConvertByTypeName(config);

            if (_allowLegacyTypeIdFallback && config.TypeId > 0)
                return ConvertByTypeId(config);

            if (config.TypeId > 0)
                throw new NotSupportedException($"Executable config typeId={config.TypeId} must not use legacy TypeId. Use a formal TypeName instead.");

            throw new InvalidOperationException("Executable config must specify a formal executable TypeName.");
        }

        public ISimpleExecutable ConvertToSequence(List<ExecutableConfig> children)
        {
            var sequence = new SequenceExecutable();
            if (children == null)
                return sequence;

            foreach (var childConfig in children)
            {
                var child = Convert(childConfig);
                if (child != null)
                    sequence.Add(child);
            }

            return sequence;
        }

        public ICondition ConvertCondition(ConditionConfig condition)
        {
            if (condition == null)
                return null;

            var normalized = condition.TypeName?.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "payloadcompare":
                    return new PayloadCompareCondition();
                case "hastarget":
                    return new HasTargetCondition();
                case "not":
                    return new NotCondition();
                case "and":
                    return new AndCondition();
                case "or":
                    return new OrCondition();
                case "numericcompare":
                    return new NumericCompareCondition();
                case "const":
                    return new ConstCondition();
                default:
                    throw new NotSupportedException($"Condition TypeName '{condition.TypeName}' is not supported by the formal triggering runtime.");
            }
        }

        public NumericValueRef ConvertNumericValueRef(NumericValueRefDto valueRef)
        {
            switch (valueRef.Kind)
            {
                case "Const":
                    return NumericValueRef.Const(valueRef.ConstValue);
                case "Var":
                    return NumericValueRef.Var(valueRef.DomainId, valueRef.Key);
                case "Blackboard":
                    return NumericValueRef.Blackboard(valueRef.BoardId, valueRef.KeyId);
                case "PayloadField":
                    return NumericValueRef.PayloadField(valueRef.FieldId);
                case "Expr":
                    return NumericValueRef.Expr(valueRef.ExprText);
                default:
                    return default;
            }
        }

        public ScheduleConfig ConvertSchedule(ScheduleConfig schedule)
        {
            return schedule;
        }

        public object EvaluateValueSelector(object selector, object ctx)
        {
            return selector;
        }

        public static ECompareOp ParseCompareOp(string compareOp)
        {
            if (string.IsNullOrWhiteSpace(compareOp))
                return ECompareOp.Equal;

            switch (compareOp.Trim().ToLowerInvariant())
            {
                case "eq":
                case "equal":
                    return ECompareOp.Equal;
                case "ne":
                case "notequal":
                    return ECompareOp.NotEqual;
                case "gt":
                case "greaterthan":
                    return ECompareOp.GreaterThan;
                case "ge":
                case "greaterthanorequal":
                    return ECompareOp.GreaterThanOrEqual;
                case "lt":
                case "lessthan":
                    return ECompareOp.LessThan;
                case "le":
                case "lessthanorequal":
                    return ECompareOp.LessThanOrEqual;
                default:
                    return ECompareOp.Equal;
            }
        }

        private ISimpleExecutable ConvertByTypeName(ExecutableConfig config)
        {
            var normalized = config.TypeName.Trim().ToLowerInvariant();
            var strategy = _strategyRegistry != null && _strategyRegistry.TryGetByTypeName(normalized, out var matched)
                ? matched
                : _defaultStrategy;

            if (strategy == null)
                throw new NotSupportedException($"Executable TypeName '{config.TypeName}' is not supported by the formal triggering runtime. Use Runtime.Plan executables or registered action logic instead.");

            return strategy.Convert(config, this);
        }

        private ISimpleExecutable ConvertByTypeId(ExecutableConfig config)
        {
            if (_strategyRegistry != null && _strategyRegistry.TryGetByTypeId(config.TypeId, out var strategy))
                return strategy.Convert(config, this);

            if (_defaultStrategy != null)
                return _defaultStrategy.Convert(config, this);

            throw new NotSupportedException($"Executable TypeId {config.TypeId} is not supported by the formal triggering runtime.");
        }
    }
}
