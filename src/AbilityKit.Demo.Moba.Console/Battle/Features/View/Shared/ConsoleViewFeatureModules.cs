using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Session;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console ViewFeature module host interface
    /// Defines capabilities that ViewFeature modules need to access
    /// 对齐 Unity IViewFeatureModulesHost
    /// </summary>
    public interface IConsoleViewFeatureModulesHost
    {
        /// <summary>
        /// Battle context
        /// </summary>
        ConsoleBattleContext Context { get; }

        /// <summary>
        /// Session hooks for ViewBinder events
        /// </summary>
        ConsoleSessionHooks? Hooks { get; }

        /// <summary>
        /// View binder
        /// </summary>
        IConsoleViewBinder Binder { get; }

        /// <summary>
        /// Battle view
        /// </summary>
        IConsoleBattleView BattleView { get; }

        /// <summary>
        /// VFX manager
        /// </summary>
        ConsoleVfxManager VfxManager { get; }

        /// <summary>
        /// View event sink
        /// </summary>
        ConsoleBattleViewEventSink EventSink { get; }

        /// <summary>
        /// Register EventSink created by EventSinkModule
        /// </summary>
        void RegisterEventSink(ConsoleBattleViewEventSink eventSink);

        /// <summary>
        /// Unregister EventSink
        /// </summary>
        void UnregisterEventSink(ConsoleBattleViewEventSink eventSink);

        /// <summary>
        /// Register binder created by BindingModule
        /// </summary>
        void RegisterBinder(IConsoleViewBinder binder);

        /// <summary>
        /// Unregister binder
        /// </summary>
        void UnregisterBinder(IConsoleViewBinder binder);

        /// <summary>
        /// Refresh dirty entity views
        /// </summary>
        void RefreshDirtyViews();

        /// <summary>
        /// Register all seekable objects
        /// </summary>
        void RegisterAllSeekables();

        /// <summary>
        /// Seek to current frame
        /// </summary>
        void SeekAllToCurrentFrame();

        /// <summary>
        /// Rebind all views
        /// </summary>
        void RebindAllViews();

        /// <summary>
        /// Tick floating text system
        /// </summary>
        void TickFloatingTexts(float deltaTime);
    }

    /// <summary>
    /// Console ViewFeature module interface
    /// 继承 ModuleHost 所需的接口，支持嵌套 ModuleHost 管理
    /// 对齐 Unity IViewSubFeature (统一为 Module 命名)
    /// </summary>
    public interface IConsoleViewModule : 
        IModuleId, 
        IModuleDependencies,
        IGameModule<IConsoleViewFeatureModulesHost>,
        IGameModuleTick<IConsoleViewFeatureModulesHost>,
        IGameModuleRebind<IConsoleViewFeatureModulesHost>
    {
        // 接口方法由基类提供，此处无需重复定义
    }

    /// <summary>
    /// View module context
    /// Provides context for modules
    /// </summary>
    public sealed class ConsoleViewModuleContext
    {
        public IConsoleViewFeatureModulesHost Host { get; }
        public ConsoleBattleContext? BattleContext => Host.Context;
        public ConsoleSessionHooks? Hooks => Host.Hooks;

        public ConsoleViewModuleContext(IConsoleViewFeatureModulesHost host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }
    }
}
