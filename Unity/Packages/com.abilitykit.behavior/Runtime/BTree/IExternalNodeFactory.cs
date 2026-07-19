using System;
using System.Collections.Generic;

namespace AbilityKit.Ability.Behavior.BTree
{
    /// <summary>
    /// 外部节点类型
    /// </summary>
    public enum ExternalNodeType
    {
        Action,
        Condition
    }

    /// <summary>
    /// 外部节点描述符
    /// </summary>
    public readonly struct ExternalNodeDescriptor
    {
        public string TypeName { get; }
        public ExternalNodeType NodeType { get; }
        public Dictionary<string, string> Properties { get; }
        public Type ImplementationType { get; }

        public ExternalNodeDescriptor(string typeName, ExternalNodeType nodeType, Dictionary<string, string> properties, Type implementationType)
        {
            TypeName = typeName;
            NodeType = nodeType;
            Properties = properties ?? new Dictionary<string, string>();
            ImplementationType = implementationType;
        }
    }

    /// <summary>
    /// 外部节点工厂接口
    /// 用于动态创建 BTCore 的 ExternalAction 和 ExternalCondition
    ///
    /// 使用方式：
    /// ```csharp
    /// // 1. 注册实现
    /// factory.RegisterAction<MyPatrolAction>("Patrol");
    /// factory.RegisterCondition<MyHpCheckCondition>("HpLow");
    ///
    /// // 2. 创建节点
    /// var action = factory.CreateAction("Patrol", new Dictionary<string, string> { { "Speed", "3.0" } });
    /// ```
    /// </summary>
    public interface IExternalNodeFactory
    {
        /// <summary>
        /// 注册 Action 节点类型
        /// </summary>
        void RegisterAction<T>(string typeName) where T : BTCore.Runtime.Actions.Action, new();

        /// <summary>
        /// 注册 Condition 节点类型
        /// </summary>
        void RegisterCondition<T>(string typeName) where T : BTCore.Runtime.Conditions.Condition, new();

        /// <summary>
        /// 创建 Action 节点
        /// </summary>
        BTCore.Runtime.Actions.Action CreateAction(string typeName, Dictionary<string, string> properties);

        /// <summary>
        /// 创建 Condition 节点
        /// </summary>
        BTCore.Runtime.Conditions.Condition CreateCondition(string typeName, Dictionary<string, string> properties);

        /// <summary>
        /// 检查类型是否已注册
        /// </summary>
        bool IsActionRegistered(string typeName);
        bool IsConditionRegistered(string typeName);

        /// <summary>
        /// 获取所有已注册的描述符
        /// </summary>
        IReadOnlyList<ExternalNodeDescriptor> GetAllDescriptors();
    }

    /// <summary>
    /// 默认的外部节点工厂实现
    /// </summary>
    public sealed class DefaultExternalNodeFactory : IExternalNodeFactory
    {
        private readonly Dictionary<string, Type> _actionTypes;
        private readonly Dictionary<string, Type> _conditionTypes;

        public DefaultExternalNodeFactory()
        {
            _actionTypes = new Dictionary<string, Type>();
            _conditionTypes = new Dictionary<string, Type>();
        }

        public void RegisterAction<T>(string typeName) where T : BTCore.Runtime.Actions.Action, new()
        {
            _actionTypes[typeName] = typeof(T);
        }

        public void RegisterCondition<T>(string typeName) where T : BTCore.Runtime.Conditions.Condition, new()
        {
            _conditionTypes[typeName] = typeof(T);
        }

        public BTCore.Runtime.Actions.Action CreateAction(string typeName, Dictionary<string, string> properties)
        {
            if (!_actionTypes.TryGetValue(typeName, out var type))
            {
                throw new InvalidOperationException($"Action type '{typeName}' is not registered");
            }

            var action = (BTCore.Runtime.Actions.Action)Activator.CreateInstance(type);
            if (action is BTCore.Runtime.Externals.ExternalAction externalAction)
            {
                externalAction.TypeName = typeName;
                externalAction.Properties = properties ?? new Dictionary<string, string>();
            }

            return action;
        }

        public BTCore.Runtime.Conditions.Condition CreateCondition(string typeName, Dictionary<string, string> properties)
        {
            if (!_conditionTypes.TryGetValue(typeName, out var type))
            {
                throw new InvalidOperationException($"Condition type '{typeName}' is not registered");
            }

            var condition = (BTCore.Runtime.Conditions.Condition)Activator.CreateInstance(type);
            if (condition is BTCore.Runtime.Externals.ExternalCondition externalCondition)
            {
                externalCondition.TypeName = typeName;
                externalCondition.Properties = properties ?? new Dictionary<string, string>();
            }

            return condition;
        }

        public bool IsActionRegistered(string typeName)
        {
            return _actionTypes.ContainsKey(typeName);
        }

        public bool IsConditionRegistered(string typeName)
        {
            return _conditionTypes.ContainsKey(typeName);
        }

        public IReadOnlyList<ExternalNodeDescriptor> GetAllDescriptors()
        {
            var descriptors = new List<ExternalNodeDescriptor>();

            foreach (var kvp in _actionTypes)
            {
                descriptors.Add(new ExternalNodeDescriptor(
                    kvp.Key,
                    ExternalNodeType.Action,
                    null,
                    kvp.Value));
            }

            foreach (var kvp in _conditionTypes)
            {
                descriptors.Add(new ExternalNodeDescriptor(
                    kvp.Key,
                    ExternalNodeType.Condition,
                    null,
                    kvp.Value));
            }

            return descriptors;
        }
    }

    /// <summary>
    /// 外部节点注册特性
    /// 用于自动注册到工厂
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class BTreeNodeAttribute : Attribute
    {
        public string TypeName { get; }
        public ExternalNodeType NodeType { get; }

        public BTreeNodeAttribute(string typeName, ExternalNodeType nodeType)
        {
            TypeName = typeName;
            NodeType = nodeType;
        }
    }

    /// <summary>
    /// 外部节点工厂扩展方法
    /// </summary>
    public static class ExternalNodeFactoryExtensions
    {
        /// <summary>
        /// 从程序集扫描并注册所有带有 BTreeNode 特性的类型
        /// </summary>
        public static void ScanFromAssembly(this IExternalNodeFactory factory, params System.Reflection.Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttributes(typeof(BTreeNodeAttribute), false) as BTreeNodeAttribute[];
                    if (attr != null && attr.Length > 0)
                    {
                        var nodeAttr = attr[0];
                        if (nodeAttr.NodeType == ExternalNodeType.Action)
                        {
                            RegisterActionFromType(factory, type, nodeAttr.TypeName);
                        }
                        else
                        {
                            RegisterConditionFromType(factory, type, nodeAttr.TypeName);
                        }
                    }
                }
            }
        }

        private static void RegisterActionFromType(IExternalNodeFactory factory, Type type, string typeName)
        {
            var method = typeof(IExternalNodeFactory).GetMethod(nameof(IExternalNodeFactory.RegisterAction));
            var genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(factory, new object[] { typeName });
        }

        private static void RegisterConditionFromType(IExternalNodeFactory factory, Type type, string typeName)
        {
            var method = typeof(IExternalNodeFactory).GetMethod(nameof(IExternalNodeFactory.RegisterCondition));
            var genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(factory, new object[] { typeName });
        }
    }
}
