using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    /// <summary>
    /// 逻辑世界创建器注册表
    /// 基于反射自动发现并注册所有 ILogicWorldCreator 实现
    ///
    /// 使用方式:
    /// 1. 创建实现类实现 ILogicWorldCreator 接口并添加 [WorldCreator] 特性标记
    /// 2. 调用 LogicWorldRegistry.Initialize() 初始化（自动调用）
    /// 3. 调用 LogicWorldRegistry.GetCreator(worldType) 获取创建器
    /// </summary>
    public static class LogicWorldRegistry
    {
        private static readonly Dictionary<string, ILogicWorldCreator> _creators = new Dictionary<string, ILogicWorldCreator>();
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// 特性标记，用于标记 ILogicWorldCreator 实现类
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class WorldCreatorAttribute : Attribute
        {
            public string WorldType { get; }

            public WorldCreatorAttribute(string worldType)
            {
                WorldType = worldType ?? throw new ArgumentNullException(nameof(worldType));
            }
        }

        /// <summary>
        /// 初始化注册表
        /// 自动扫描程序集中所有 ILogicWorldCreator 实现并注册
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                Log.Info("[LogicWorldRegistry] Initializing...");

                // 扫描所有程序集
                var assemblies = new List<Assembly> { typeof(LogicWorldRegistry).Assembly };

                // 扫描 AbilityKit.Demo.ET.Logic 程序集
                var logicAsm = Assembly.Load("AbilityKit.Demo.ET.Logic");
                if (logicAsm != null && !assemblies.Contains(logicAsm))
                {
                    assemblies.Add(logicAsm);
                }

                foreach (var asm in assemblies)
                {
                    if (asm == null || asm.IsDynamic || string.IsNullOrEmpty(asm.FullName))
                        continue;

                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var type in types)
                        {
                            if (type == null || type.IsInterface || type.IsAbstract)
                                continue;

                            if (!typeof(ILogicWorldCreator).IsAssignableFrom(type))
                                continue;

                            // 检查 [WorldCreator] 特性
                            var attrs = type.GetCustomAttributes(typeof(WorldCreatorAttribute), false);
                            if (attrs == null || attrs.Length == 0)
                            {
                                Log.Warning($"[LogicWorldRegistry] {type.Name} implements ILogicWorldCreator but missing [WorldCreator] attribute");
                                continue;
                            }

                            var attr = (WorldCreatorAttribute)attrs[0];
                            try
                            {
                                var creator = Activator.CreateInstance(type) as ILogicWorldCreator;
                                if (creator != null)
                                {
                                    Register(attr.WorldType, creator);
                                    Log.Info($"[LogicWorldRegistry] Registered creator: {type.Name} for world type '{attr.WorldType}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[LogicWorldRegistry] Failed to create instance of {type.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[LogicWorldRegistry] Failed to scan assembly {asm.FullName}: {ex.Message}");
                    }
                }

                _isInitialized = true;
                Log.Info($"[LogicWorldRegistry] Initialized with {_creators.Count} creators");
            }
        }

        /// <summary>
        /// 注册创建器
        /// </summary>
        public static void Register(string worldType, ILogicWorldCreator creator)
        {
            if (string.IsNullOrEmpty(worldType))
                throw new ArgumentNullException(nameof(worldType));
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));

            lock (_lock)
            {
                if (_creators.ContainsKey(worldType))
                {
                    Log.Warning($"[LogicWorldRegistry] Overwriting existing creator for world type '{worldType}'");
                }
                _creators[worldType] = creator;
            }
        }

        /// <summary>
        /// 获取指定类型的创建器
        /// </summary>
        public static ILogicWorldCreator GetCreator(string worldType)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            lock (_lock)
            {
                if (_creators.TryGetValue(worldType, out var creator))
                {
                    return creator;
                }
                Log.Warning($"[LogicWorldRegistry] No creator registered for world type '{worldType}'");
                return null;
            }
        }

        /// <summary>
        /// 获取所有已注册的世界类型
        /// </summary>
        public static IReadOnlyCollection<string> GetRegisteredWorldTypes()
        {
            lock (_lock)
            {
                return new List<string>(_creators.Keys);
            }
        }

        /// <summary>
        /// 检查指定类型是否已注册
        /// </summary>
        public static bool IsRegistered(string worldType)
        {
            lock (_lock)
            {
                return _creators.ContainsKey(worldType);
            }
        }

        /// <summary>
        /// 清除所有注册（主要用于测试）
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _creators.Clear();
                _isInitialized = false;
            }
        }
    }
}
