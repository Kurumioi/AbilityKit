using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using ActorSnapshot = AbilityKit.Demo.Moba.Console.Battle.Sync.ActorStateSnapshot;
using BattleView = AbilityKit.Demo.Moba.Console.View;
using ConsoleView = AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console ViewFeature module host interface
    /// Defines capabilities that ViewFeature submodules need to access
    /// </summary>
    public interface IConsoleViewFeatureModulesHost
    {
        /// <summary>
        /// Battle context
        /// </summary>
        ConsoleBattleContext Context { get; }

        /// <summary>
        /// View binder
        /// </summary>
        BattleView.IConsoleViewBinder Binder { get; }

        /// <summary>
        /// Battle view
        /// </summary>
        ConsoleView.IConsoleBattleView BattleView { get; }

        /// <summary>
        /// VFX manager
        /// </summary>
        ConsoleView.ConsoleVfxManager VfxManager { get; }

        /// <summary>
        /// View event sink
        /// </summary>
        ConsoleView.ConsoleBattleViewEventSink EventSink { get; }

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
    /// Console SubFeature base interface
    /// </summary>
    public interface IConsoleViewSubFeature
    {
        void OnAttach(in ConsoleFeatureModuleContext ctx);
        void OnDetach(in ConsoleFeatureModuleContext ctx);
        void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime);
        void Rebind(in ConsoleFeatureModuleContext ctx);
    }

    /// <summary>
    /// Console ViewFeature module interface
    /// </summary>
    public interface IConsoleViewFeatureModule : IConsoleViewSubFeature
    {
    }

    /// <summary>
    /// Feature module context
    /// </summary>
    public readonly struct ConsoleFeatureModuleContext
    {
        public IConsoleViewFeatureModulesHost Feature { get; }

        public ConsoleFeatureModuleContext(IConsoleViewFeatureModulesHost feature)
        {
            Feature = feature;
        }
    }
}
