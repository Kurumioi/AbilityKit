using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Timeline SubFeature
    /// Manages ViewTimeline instance lifecycle
    /// </summary>
    public sealed class ConsoleTimelineSubFeature : IConsoleViewFeatureModule
    {
        private ConsoleViewTimeline? _timeline;

        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
            _timeline = new ConsoleViewTimeline();
            Platform.Log.View("[TimelineSubFeature] Created ViewTimeline");
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
            _timeline?.Dispose();
            _timeline = null;
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }

    /// <summary>
    /// VFX SubFeature
    /// Manages VFX database and VfxManager instance
    /// </summary>
    public sealed class ConsoleVfxSubFeature : IConsoleViewFeatureModule
    {
        private ConsoleVfxManager? _vfxManager;

        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
            var database = new ConsoleVfxDatabase();
            _vfxManager = new ConsoleVfxManager(database);
            Platform.Log.View("[VfxSubFeature] Created VfxManager");
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
            _vfxManager?.Dispose();
            _vfxManager = null;
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }

    /// <summary>
    /// Binding SubFeature
    /// Manages ViewBinder instance lifecycle
    /// </summary>
    public sealed class ConsoleBindingSubFeature : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
            Platform.Log.View("[BindingSubFeature] Attached");
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
            Platform.Log.View("[BindingSubFeature] Detached");
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }

    /// <summary>
    /// EventSink SubFeature
    /// Creates and manages BattleViewEventSink instance
    /// </summary>
    public sealed class ConsoleEventSinkSubFeature : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
            Platform.Log.View("[EventSinkSubFeature] Attached");
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
            Platform.Log.View("[EventSinkSubFeature] Detached");
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }
}
