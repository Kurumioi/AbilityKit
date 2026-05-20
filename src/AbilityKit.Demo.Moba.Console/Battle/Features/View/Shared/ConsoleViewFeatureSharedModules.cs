using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console ViewFeature standard SubModules
    /// Provides interpolation, dirty entity sync, floating text, etc.
    /// </summary>
    public static class ConsoleViewFeatureSharedModules
    {
    /// <summary>
    /// Create standard view SubFeature list
    /// </summary>
    public static void AddStandardViewModules(List<IConsoleViewSubFeature> subFeatures)
    {
        // Feature-specific modules (added separately)
        subFeatures.Add(new ConsoleInterpolationModule());
        subFeatures.Add(new ConsoleFloatingTextModule());
        subFeatures.Add(new ConsoleDirtySyncModule());
        subFeatures.Add(new ConsoleTimelineModule());
        subFeatures.Add(new ConsoleVfxTickModule());
    }
    }

    /// <summary>
    /// Dirty entity sync module
    /// Refreshes views when DirtyEntities change
    /// </summary>
    public sealed class ConsoleDirtySyncModule : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
            var host = ctx.Feature;
            if (host?.Context?.DirtyEntities == null) return;
            if (host.Context.DirtyEntities.Count == 0) return;

            host.RefreshDirtyViews();
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
            var host = ctx.Feature;
            if (host?.Context?.EcsWorld == null) return;
            host.RebindAllViews();
        }
    }

    /// <summary>
    /// Interpolation module
    /// Responsible for view binder interpolation rendering
    /// </summary>
    public sealed class ConsoleInterpolationModule : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
            var host = ctx.Feature;
            var binder = host?.Binder;
            if (binder == null) return;
            var battleCtx = host.Context;
            if (battleCtx == null) return;

            binder.TickRender(deltaTime, battleCtx.LogicTimeSeconds);
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }

    /// <summary>
    /// Floating text module
    /// Responsible for floating text system Tick
    /// </summary>
    public sealed class ConsoleFloatingTextModule : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
            ctx.Feature?.TickFloatingTexts(deltaTime);
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }

    /// <summary>
    /// Timeline module
    /// Manages frame seeking and synchronization
    /// </summary>
    public sealed class ConsoleTimelineModule : IConsoleViewFeatureModule
    {
        private int _lastSeenFrame = int.MinValue;

        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
            _lastSeenFrame = int.MinValue;
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
            var host = ctx.Feature;
            var battleCtx = host?.Context;
            if (battleCtx == null) return;

            var frame = battleCtx.LastFrame;
            if (frame == _lastSeenFrame) return;
            _lastSeenFrame = frame;

            host.SeekAllToCurrentFrame();
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
            var host = ctx.Feature;
            if (host == null) return;

            _lastSeenFrame = int.MinValue;
            host.RegisterAllSeekables();
            host.SeekAllToCurrentFrame();
        }
    }

    /// <summary>
    /// VFX Tick module (placeholder for Console)
    /// Tick VFX manager in Console environment
    /// </summary>
    public sealed class ConsoleVfxTickModule : IConsoleViewFeatureModule
    {
        public void OnAttach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void OnDetach(in ConsoleFeatureModuleContext ctx)
        {
        }

        public void Tick(in ConsoleFeatureModuleContext ctx, float deltaTime)
        {
            // VFX Tick is handled separately via ConsoleVfxManager
            // This is a placeholder for architecture alignment
        }

        public void Rebind(in ConsoleFeatureModuleContext ctx)
        {
        }
    }
}
