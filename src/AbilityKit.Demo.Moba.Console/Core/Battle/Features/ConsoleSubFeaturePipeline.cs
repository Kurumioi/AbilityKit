using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Battle;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// Console SubFeature 管道
    /// 统一管理 SubFeature 的创建、排序和生命周期
    /// </summary>
    public static class ConsoleSubFeaturePipeline
    {
        /// <summary>
        /// 创建标准 SubFeature 列表
        /// </summary>
        public static List<IConsoleSubFeature> CreatePipeline()
        {
            var features = new List<IConsoleSubFeature>();
            AddStandardSubFeatures(features);
            return features;
        }

        /// <summary>
        /// 添加标准 SubFeatures
        /// </summary>
        public static void AddStandardSubFeatures(List<IConsoleSubFeature> features)
        {
            // 视图相关
            features.Add(new ConsoleViewFeatureSubFeature());
            features.Add(new ConsoleInterpolationFeatureSubFeature());
            features.Add(new ConsoleFloatingTextFeatureSubFeature());

            // 同步相关
            features.Add(new ConsoleSyncFeature());

            // 输入相关
            features.Add(new ConsoleInputFeature());

            // HUD
            features.Add(new ConsoleHudFeature());
        }

        /// <summary>
        /// 按依赖关系排序
        /// </summary>
        public static bool SortByDependencies(List<IConsoleSubFeature> features)
        {
            var sorted = new List<IConsoleSubFeature>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var feature in features)
            {
                if (!Visit(feature, features, sorted, visited, visiting))
                {
                    Log.Warn($"[SubFeaturePipeline] Circular dependency detected involving: {feature.Id}");
                    return false;
                }
            }

            features.Clear();
            features.AddRange(sorted);
            return true;
        }

        private static bool Visit(
            IConsoleSubFeature feature,
            List<IConsoleSubFeature> allFeatures,
            List<IConsoleSubFeature> sorted,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (visited.Contains(feature.Id))
                return true;

            if (visiting.Contains(feature.Id))
                return false;

            visiting.Add(feature.Id);

            foreach (var depId in feature.Dependencies)
            {
                var dep = FindFeature(depId, allFeatures);
                if (dep != null && !Visit(dep, allFeatures, sorted, visited, visiting))
                    return false;
            }

            visiting.Remove(feature.Id);
            visited.Add(feature.Id);
            sorted.Add(feature);

            return true;
        }

        private static IConsoleSubFeature? FindFeature(string id, List<IConsoleSubFeature> features)
        {
            foreach (var f in features)
            {
                if (f.Id == id)
                    return f;
            }
            return null;
        }

        /// <summary>
        /// 创建 SubFeature 模块宿主
        /// </summary>
        public static ConsoleSubFeatureHost CreateModuleHost(List<IConsoleSubFeature> features)
        {
            return new ConsoleSubFeatureHost(features);
        }
    }

    /// <summary>
    /// SubFeature 模块宿主
    /// 管理 SubFeature 的生命周期
    /// </summary>
    public sealed class ConsoleSubFeatureHost : IDisposable
    {
        private readonly List<IConsoleSubFeature> _features;
        private readonly ConsoleHooks _hooks;
        private bool _disposed;

        public ConsoleHooks Hooks => _hooks;

        public ConsoleSubFeatureHost(List<IConsoleSubFeature> features)
        {
            _features = features;
            _hooks = new ConsoleHooks();
        }

        public void OnAttach(ConsoleBattleContext ctx)
        {
            foreach (var feature in _features)
            {
                feature.OnAttach(ctx);
            }
        }

        public void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
            _hooks.InvokePreTick(deltaTime);

            foreach (var feature in _features)
            {
                feature.Tick(ctx, deltaTime);
            }

            _hooks.InvokePostTick(deltaTime);
        }

        public void OnDetach(ConsoleBattleContext ctx)
        {
            // 反向 detach
            for (int i = _features.Count - 1; i >= 0; i--)
            {
                _features[i].OnDetach(ctx);
            }
            _hooks.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _hooks.Dispose();
        }
    }
}
