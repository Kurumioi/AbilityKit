using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Input;
using AbilityKit.Demo.Moba.Console.Battle.Features;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Feature 容器
    /// 统一管理所有 Feature 的创建、依赖关联和生命周期
    /// </summary>
    public sealed class FeatureContainer : IDisposable, IFeatureContainer, IFeatureContextProvider
    {
        private readonly ConsoleSyncFeature _syncFeature;
        private readonly ConsoleInputFeature _inputFeature;
        private readonly ConsoleHudFeature _hudFeature;

        private readonly Dictionary<Type, IFeature> _featuresByType = new();
        private readonly Dictionary<string, IFeature> _featuresById = new();

        private ConsoleBattleContext? _context;
        private bool _disposed;

        public ConsoleSyncFeature SyncFeature => _syncFeature;
        public ConsoleInputFeature InputFeature => _inputFeature;
        public ConsoleHudFeature HudFeature => _hudFeature;

        public ConsoleBattleContext? Context => _context;

        public FeatureContainer()
        {
            _syncFeature = new ConsoleSyncFeature();
            _inputFeature = new ConsoleInputFeature();
            _hudFeature = new ConsoleHudFeature();

            // 注册到字典
            RegisterFeatureInternal(_syncFeature);
            RegisterFeatureInternal(_inputFeature);
            RegisterFeatureInternal(_hudFeature);
        }

        private void RegisterFeatureInternal(IFeature feature)
        {
            _featuresByType[feature.GetType()] = feature;
            _featuresById[feature.Id] = feature;
        }

        #region IFeatureContainer 实现

        public T? GetSubFeature<T>() where T : class, IFeature
        {
            return GetSubFeature<T>(typeof(T).Name);
        }

        public T? GetSubFeature<T>(string id) where T : class, IFeature
        {
            if (_featuresById.TryGetValue(id, out var feature))
            {
                return feature as T;
            }
            return null;
        }

        public T? GetHandler<T>(string subFeatureId) where T : class, IHandler
        {
            // 目前不支持跨 SubFeature 获取 Handler
            // 如果需要，可以在 SubFeature 中实现此方法
            return null;
        }

        public void RegisterSubFeature<T>(T subFeature) where T : IFeature
        {
            RegisterFeatureInternal(subFeature);
        }

        #endregion

        #region IFeatureContextProvider 实现

        public IFeatureContext? GetContext()
        {
            return _context != null ? new FeatureContextAdapter(_context) : null;
        }

        #endregion

        /// <summary>
        /// 注入服务依赖
        /// </summary>
        public void InjectServices(IConsoleBattleView battleView)
        {
            _hudFeature.SetBattleView(battleView);
        }

        /// <summary>
        /// 附加到 Context
        /// </summary>
        public void OnAttach(ConsoleBattleContext ctx)
        {
            _context = ctx;

            // 设置 Feature 引用
            SetFeatureReferences();

            _syncFeature.OnAttach(ctx);
            _inputFeature.OnAttach(ctx);
            _hudFeature.OnAttach(ctx);
        }

        private void SetFeatureReferences()
        {
            // 让 SubFeature 能够通过 GetSibling 获取其他 Feature
            // 目前 _syncFeature, _inputFeature, _hudFeature 不直接支持
            // 如果需要，让它们实现 IFeatureContainer 接口
        }

        /// <summary>
        /// 从 Context 分离
        /// </summary>
        public void OnDetach(ConsoleBattleContext ctx)
        {
            _syncFeature.OnDetach(ctx);
            _inputFeature.OnDetach(ctx);
            _hudFeature.OnDetach(ctx);
            _context = null;
        }

        /// <summary>
        /// Tick 所有 Feature
        /// </summary>
        public void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
            _syncFeature.Tick(ctx, deltaTime);
            _inputFeature.Tick(ctx, deltaTime);
            _hudFeature.Tick(ctx, deltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
