using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Variables.Numeric;
using AbilityKit.Core.Markers;

namespace AbilityKit.Triggering.Runtime.Executable
{
    /// <summary>
    /// 行为类型描述符
    /// </summary>
    public sealed class ExecutableDescriptor
    {
        public int TypeId;
        public string TypeName;
        public ExecutableMetadata Metadata;
        public bool IsScheduled => Metadata.IsScheduled;
        public bool IsPeriodic => Metadata.IsScheduled && Metadata.DefaultPeriodMs.HasValue;
        public Func<IExecutable> Factory;
    }

    /// <summary>
    /// 条件类型描述符
    /// </summary>
    public sealed class ConditionDescriptor
    {
        public int TypeId;
        public string TypeName;
        public Func<ICondition> Factory;
    }

    /// <summary>
    /// 行为类型注册表。
    /// 旧 Runtime/Executable 兼容路径默认只注册内建类型，外部扩展需要显式注册或显式触发 Attribute 扫描。
    /// </summary>
    public sealed class ExecutableRegistry : IMarkerRegistry
    {
        private static readonly Lazy<ExecutableRegistry> _instance = new(() => new ExecutableRegistry());
        public static ExecutableRegistry Instance => _instance.Value;

        private readonly Dictionary<int, ExecutableDescriptor> _executables = new();
        private readonly Dictionary<string, int> _nameToId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, ConditionDescriptor> _conditions = new();
        private readonly Dictionary<string, int> _conditionNameToId = new(StringComparer.OrdinalIgnoreCase);

        private ExecutableRegistry()
        {
            RegisterBuiltin();
        }

        /// <summary>
        /// 注册行为类型（通过显式 TypeId）
        /// </summary>
        public void Register<TExecutable>(int typeId, string typeName, ExecutableMetadata metadata = default)
            where TExecutable : IExecutable, new()
        {
            _executables[typeId] = new ExecutableDescriptor
            {
                TypeId = typeId,
                TypeName = typeName,
                Metadata = metadata,
                Factory = () => new TExecutable()
            };
            _nameToId[typeName] = typeId;
        }

        public IExecutable CreateExecutable(int typeId)
        {
            if (_executables.TryGetValue(typeId, out var descriptor))
                return descriptor.Factory();
            throw new KeyNotFoundException($"Executable type {typeId} not found");
        }

        public TExecutable CreateExecutable<TExecutable>(int typeId) where TExecutable : IExecutable
            => (TExecutable)CreateExecutable(typeId);

        public bool TryGetDescriptor(int typeId, out ExecutableDescriptor descriptor)
            => _executables.TryGetValue(typeId, out descriptor);

        public ExecutableDescriptor GetDescriptor(int typeId)
        {
            if (_executables.TryGetValue(typeId, out var descriptor))
                return descriptor;
            throw new KeyNotFoundException($"Executable type {typeId} not found");
        }

        public bool TryGetTypeIdByName(string typeName, out int typeId)
            => _nameToId.TryGetValue(typeName, out typeId);

        public void RegisterCondition<TCondition>(int typeId, string typeName)
            where TCondition : ICondition, new()
        {
            _conditions[typeId] = new ConditionDescriptor
            {
                TypeId = typeId,
                TypeName = typeName,
                Factory = () => new TCondition()
            };
            _conditionNameToId[typeName] = typeId;
        }

        public ICondition CreateCondition(int typeId)
        {
            if (_conditions.TryGetValue(typeId, out var descriptor))
                return descriptor.Factory();
            throw new KeyNotFoundException($"Condition type {typeId} not found");
        }

        public ICondition CreateCondition(string typeName)
        {
            if (TryGetConditionTypeIdByName(typeName, out var typeId))
                return CreateCondition(typeId);
            throw new KeyNotFoundException($"Condition type '{typeName}' not found");
        }

        public bool TryGetConditionDescriptor(int typeId, out ConditionDescriptor descriptor)
            => _conditions.TryGetValue(typeId, out descriptor);

        public bool TryGetConditionTypeIdByName(string typeName, out int typeId)
            => _conditionNameToId.TryGetValue(typeName, out typeId);

        public IEnumerable<ExecutableDescriptor> GetAllExecutables()
            => _executables.Values;

        public IEnumerable<ConditionDescriptor> GetAllConditions()
            => _conditions.Values;

        /// <summary>
        /// 兼容旧扩展的 Attribute 扫描入口。主线与内建类型不依赖运行时扫描。
        /// </summary>
        public void ScanAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0) return;
            MarkerScanner<ExecutableTypeIdAttribute>.Scan(assemblies, this);
            MarkerScanner<ConditionTypeIdAttribute>.Scan(assemblies, this);
        }

        /// <summary>
        /// 扫描旧 Runtime/Executable 所在程序集。仅为兼容旧 Attribute 扩展保留。
        /// </summary>
        public void ScanRuntimeExecutableAssembly()
        {
            ScanAssemblies(typeof(SequenceExecutable).Assembly);
        }

        /// <summary>
        /// 通过 Attribute 注册 Executable 类型（供 MarkerAttribute.OnScanned 调用）
        /// </summary>
        internal void RegisterByAttribute(ExecutableTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            var factory = CreateExecutableFactory(implType);
            if (factory != null)
            {
                _executables[attr.TypeId] = new ExecutableDescriptor
                {
                    TypeId = attr.TypeId,
                    TypeName = attr.TypeName,
                    Metadata = new ExecutableMetadata(attr.TypeId, attr.TypeName, isComposite: attr.IsComposite),
                    Factory = factory
                };
                _nameToId[attr.TypeName] = attr.TypeId;
            }
        }

        /// <summary>
        /// 通过 Attribute 注册 Condition 类型（供 MarkerAttribute.OnScanned 调用）
        /// </summary>
        internal void RegisterConditionByAttribute(ConditionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            var factory = CreateConditionFactory(implType);
            if (factory != null)
            {
                _conditions[attr.TypeId] = new ConditionDescriptor
                {
                    TypeId = attr.TypeId,
                    TypeName = attr.TypeName,
                    Factory = factory
                };
                _conditionNameToId[attr.TypeName] = attr.TypeId;
            }
        }

        private Func<IExecutable> CreateExecutableFactory(Type type)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                return () => (IExecutable)ctor.Invoke(null);
            }
            return null;
        }

        private Func<ICondition> CreateConditionFactory(Type type)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                return () => (ICondition)ctor.Invoke(null);
            }
            return null;
        }

        private void RegisterBuiltin()
        {
            Register<SequenceExecutable>(TypeIdRegistry.Executable.Sequence, "Sequence", new ExecutableMetadata(TypeIdRegistry.Executable.Sequence, "Sequence", isComposite: true));
            Register<SelectorExecutable>(TypeIdRegistry.Executable.Selector, "Selector", new ExecutableMetadata(TypeIdRegistry.Executable.Selector, "Selector", isComposite: true));
            Register<ParallelExecutable>(TypeIdRegistry.Executable.Parallel, "Parallel", new ExecutableMetadata(TypeIdRegistry.Executable.Parallel, "Parallel", isComposite: true));
            Register<IfExecutable>(TypeIdRegistry.Executable.If, "If", new ExecutableMetadata(TypeIdRegistry.Executable.If, "If", isComposite: true));
            Register<IfElseExecutable>(TypeIdRegistry.Executable.IfElse, "IfElse", new ExecutableMetadata(TypeIdRegistry.Executable.IfElse, "IfElse", isComposite: true));
            Register<SwitchExecutable>(TypeIdRegistry.Executable.Switch, "Switch", new ExecutableMetadata(TypeIdRegistry.Executable.Switch, "Switch", isComposite: true));
            Register<RandomSelectorExecutable>(TypeIdRegistry.Executable.RandomSelector, "RandomSelector", new ExecutableMetadata(TypeIdRegistry.Executable.RandomSelector, "RandomSelector", isComposite: true));
            Register<RepeatExecutable>(TypeIdRegistry.Executable.Repeat, "Repeat", new ExecutableMetadata(TypeIdRegistry.Executable.Repeat, "Repeat", isComposite: true));
            Register<UntilExecutable>(TypeIdRegistry.Executable.Until, "Until", new ExecutableMetadata(TypeIdRegistry.Executable.Until, "Until", isComposite: true));
            Register<ActionCallExecutable>(TypeIdRegistry.Executable.ActionCall, "ActionCall", new ExecutableMetadata(TypeIdRegistry.Executable.ActionCall, "ActionCall"));
            Register<DelayExecutable>(TypeIdRegistry.Executable.Delay, "Delay", new ExecutableMetadata(TypeIdRegistry.Executable.Delay, "Delay"));

            RegisterCondition<ConstCondition>(TypeIdRegistry.Condition.Const, "Const");
            RegisterCondition<AndCondition>(TypeIdRegistry.Condition.And, "And");
            RegisterCondition<OrCondition>(TypeIdRegistry.Condition.Or, "Or");
            RegisterCondition<NotCondition>(TypeIdRegistry.Condition.Not, "Not");
            RegisterCondition<NumericCompareCondition>(TypeIdRegistry.Condition.NumericCompare, "NumericCompare");
            RegisterCondition<PayloadCompareCondition>(TypeIdRegistry.Condition.PayloadCompare, "PayloadCompare");
            RegisterCondition<HasTargetCondition>(TypeIdRegistry.Condition.HasTarget, "HasTarget");
            RegisterCondition<MultiCondition>(TypeIdRegistry.Condition.Multi, "Multi");
        }

        #region IMarkerRegistry 实现

        private readonly List<Type> _types = new();
        public int Count => _types.Count;
        public IReadOnlyList<Type> Types => _types;

        public void Register(Type implType)
        {
            if (implType == null) return;
            if (implType.IsAbstract) return;
            if (implType.IsInterface) return;
            _types.Add(implType);
        }

        public void ForEach(Action<Type> action)
        {
            for (int i = 0; i < _types.Count; i++)
            {
                action(_types[i]);
            }
        }

        public IEnumerable<Type> Where(Func<Type, bool> predicate)
        {
            for (int i = 0; i < _types.Count; i++)
            {
                if (predicate(_types[i]))
                    yield return _types[i];
            }
        }

        public Type Find(Func<Type, bool> predicate)
        {
            for (int i = 0; i < _types.Count; i++)
            {
                if (predicate(_types[i]))
                    return _types[i];
            }
            return null;
        }

        #endregion
    }
}
