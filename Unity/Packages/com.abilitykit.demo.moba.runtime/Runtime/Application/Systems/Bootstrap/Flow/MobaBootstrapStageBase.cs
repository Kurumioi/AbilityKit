using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow
{
    /// <summary>
    /// Bootstrap Stage 基类
    /// 所有引导阶段继承此类，实现具体配置逻辑
    /// </summary>
    public abstract class MobaBootstrapStageBase
    {
        /// <summary>
        /// Stage 名称
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// 依赖的其他 Stage 名称
        /// </summary>
        public virtual string[] Dependencies => Array.Empty<string>();

        /// <summary>
        /// 配置阶段 - 添加服务到容器
        /// </summary>
        /// <param name="builder">世界容器构建器</param>
        protected internal virtual void Configure(WorldContainerBuilder builder)
        {
        }

        /// <summary>
        /// 安装阶段 - 安装系统
        /// </summary>
        /// <param name="contexts">Entitas 上下文</param>
        /// <param name="systems">Entitas 系统</param>
        /// <param name="services">世界解析器</param>
        protected internal virtual void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
        }

        /// <summary>
        /// 执行配置阶段
        /// </summary>
        protected internal void ExecuteConfigure(WorldContainerBuilder builder)
        {
            try
            {
                Configure(builder);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaBootstrap] Configure stage failed: {Name}");
                throw;
            }
        }

        /// <summary>
        /// 执行安装阶段
        /// </summary>
        protected internal void ExecuteInstall(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            try
            {
                Install(contexts, systems, services);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaBootstrap] Install stage failed: {Name}");
                throw;
            }
        }
    }

    /// <summary>
    /// Stage 注册表
    /// 管理所有 Bootstrap Stage
    /// </summary>
    public static class MobaBootstrapStageRegistry
    {
        private static readonly List<MobaBootstrapStageBase> _stages = new();
        /// <summary>
        /// 注册 Stage
        /// </summary>
        public static void Register(MobaBootstrapStageBase stage)
        {
            if (stage == null) return;

            var name = stage.Name;
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("[MobaBootstrapStageRegistry] Stage has no name, skipping registration");
                return;
            }

            _stages.Add(stage);
        }

        /// <summary>
        /// 获取所有 Stage
        /// </summary>
        public static IEnumerable<MobaBootstrapStageBase> GetAllStages()
        {
            return _stages;
        }

        /// <summary>
        /// 获取配置阶段的 Stage
        /// </summary>
        public static IEnumerable<MobaBootstrapStageBase> GetConfigureStages()
        {
            return GetSortedStages();
        }

        /// <summary>
        /// 获取安装阶段的 Stage
        /// </summary>
        public static IEnumerable<MobaBootstrapStageBase> GetInstallStages()
        {
            return GetSortedStages();
        }

        private static IReadOnlyList<MobaBootstrapStageBase> GetSortedStages()
        {
            var sorted = new List<MobaBootstrapStageBase>(_stages.Count);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var byName = new Dictionary<string, MobaBootstrapStageBase>(StringComparer.Ordinal);

            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                if (!byName.ContainsKey(stage.Name))
                {
                    byName.Add(stage.Name, stage);
                }
            }

            for (int i = 0; i < _stages.Count; i++)
            {
                Visit(_stages[i], byName, visiting, visited, sorted);
            }

            return sorted;
        }

        private static void Visit(
            MobaBootstrapStageBase stage,
            Dictionary<string, MobaBootstrapStageBase> byName,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<MobaBootstrapStageBase> sorted)
        {
            var name = stage.Name;
            if (visited.Contains(name)) return;
            if (!visiting.Add(name))
            {
                Log.Warning($"[MobaBootstrapStageRegistry] Circular dependency detected at stage: {name}");
                return;
            }

            var dependencies = stage.Dependencies;
            if (dependencies != null)
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    var dependencyName = dependencies[i];
                    if (string.IsNullOrEmpty(dependencyName)) continue;

                    if (byName.TryGetValue(dependencyName, out var dependency))
                    {
                        Visit(dependency, byName, visiting, visited, sorted);
                    }
                    else
                    {
                        Log.Warning($"[MobaBootstrapStageRegistry] Stage dependency not found: {name} -> {dependencyName}");
                    }
                }
            }

            visiting.Remove(name);
            visited.Add(name);
            sorted.Add(stage);
        }

        /// <summary>
        /// 获取 Stage 数量
        /// </summary>
        public static int Count => _stages.Count;
    }

    /// <summary>
    /// Stage 自动注册特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MobaBootstrapStageAttribute : Attribute
    {
    }
}
