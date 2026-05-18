using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Events;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Feature 容器
    /// 统一管理所有 Feature 的创建、依赖关联和生命周期
    /// </summary>
    public sealed class FeatureContainer : IDisposable
    {
        private readonly ConsoleSyncFeature _syncFeature;
        private readonly ConsoleInputFeature _inputFeature;
        private readonly ConsoleHudFeature _hudFeature;

        private bool _disposed;

        public ConsoleSyncFeature SyncFeature => _syncFeature;
        public ConsoleInputFeature InputFeature => _inputFeature;
        public ConsoleHudFeature HudFeature => _hudFeature;

        public FeatureContainer()
        {
            _syncFeature = new ConsoleSyncFeature();
            _inputFeature = new ConsoleInputFeature();
            _hudFeature = new ConsoleHudFeature();
        }

        /// <summary>
        /// 注入服务依赖
        /// </summary>
        public void InjectServices(ViewActorRepository viewActorRepository, IConsoleBattleView battleView)
        {
            _hudFeature.SetBattleView(battleView);
        }

        /// <summary>
        /// 附加到 Context
        /// </summary>
        public void OnAttach(ConsoleBattleContext ctx)
        {
            _syncFeature.OnAttach(ctx);
            _inputFeature.OnAttach(ctx);
            _hudFeature.OnAttach(ctx);
        }

        /// <summary>
        /// 从 Context 分离
        /// </summary>
        public void OnDetach(ConsoleBattleContext ctx)
        {
            _syncFeature.OnDetach(ctx);
            _inputFeature.OnDetach(ctx);
            _hudFeature.OnDetach(ctx);
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
