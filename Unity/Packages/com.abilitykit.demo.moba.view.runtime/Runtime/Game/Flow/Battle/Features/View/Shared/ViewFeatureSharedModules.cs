using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Modules;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal interface IViewFeatureModulesHost
    {
        BattleContext Context { get; }
        BattleViewBinder Binder { get; }

        bool IsConfirmed { get; }
        WorldId WorldId { get; }

        void RefreshDirtyViews();
        void RegisterAllSeekables();
        void SeekAllToCurrentFrame();

        void RebindAllViews();

        void TickVfx();
        void TickFloatingTexts(float deltaTime);
    }

    internal interface IViewFeatureModule<TFeature> :
        IViewSubFeature<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
    }

    internal interface IViewSubFeature<TFeature> :
        IGameModule<FeatureModuleContext<TFeature>>,
        IGameModuleTick<FeatureModuleContext<TFeature>>,
        IGameModuleRebind<FeatureModuleContext<TFeature>>
        where TFeature : class, IViewFeatureModulesHost
    {
    }

    internal sealed class SharedDirtySyncModule<TFeature> : IViewFeatureModule<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx) { }
        public void OnDetach(in FeatureModuleContext<TFeature> ctx) { }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            var f = ctx.Feature;
            if (f?.Context?.DirtyEntities == null) return;
            if (f.Context.DirtyEntities.Count == 0) return;

            f.RefreshDirtyViews();
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx)
        {
            var f = ctx.Feature;
            if (f?.Context?.EntityWorld == null) return;
            f.RebindAllViews();
        }
    }

    internal sealed class SharedTimelineModule<TFeature> : IViewFeatureModule<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
        private Action<ViewBinderReadyEvent> _onReadyHandler;
        private Action<ViewsReboundEvent> _onReboundHandler;

        private int _lastSeenFrame = int.MinValue;

        public void OnAttach(in FeatureModuleContext<TFeature> ctx)
        {
            var f = ctx.Feature;
            var hooks = f?.Context?.Hooks;

            _lastSeenFrame = int.MinValue;

            _onReadyHandler = e =>
            {
                if (f == null) return;
                if (e.IsConfirmed != f.IsConfirmed) return;
                if (!WorldId.Equals(e.WorldId, f.WorldId)) return;
                f.RegisterAllSeekables();
                f.SeekAllToCurrentFrame();
            };

            _onReboundHandler = e =>
            {
                if (f == null) return;
                if (e.IsConfirmed != f.IsConfirmed) return;
                if (!WorldId.Equals(e.WorldId, f.WorldId)) return;
                f.RegisterAllSeekables();
                f.SeekAllToCurrentFrame();
            };

            hooks?.ViewBinderReady.Add(_onReadyHandler);
            hooks?.ViewsRebound.Add(_onReboundHandler);
        }

        public void OnDetach(in FeatureModuleContext<TFeature> ctx)
        {
            var hooks = ctx.Feature?.Context?.Hooks;
            if (_onReadyHandler != null && hooks != null)
            {
                hooks.ViewBinderReady.Remove(_onReadyHandler);
            }
            if (_onReboundHandler != null && hooks != null)
            {
                hooks.ViewsRebound.Remove(_onReboundHandler);
            }

            _onReadyHandler = null;
            _onReboundHandler = null;
        }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            var f = ctx.Feature;
            var battleCtx = f?.Context;
            if (battleCtx?.EntityWorld == null) return;

            var frame = battleCtx.LastFrame;
            if (frame == _lastSeenFrame) return;
            _lastSeenFrame = frame;

            f.SeekAllToCurrentFrame();
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx)
        {
            var f = ctx.Feature;
            if (f == null) return;
            _lastSeenFrame = int.MinValue;
            f.RegisterAllSeekables();
            f.SeekAllToCurrentFrame();
        }
    }

    internal sealed class SharedVfxTickModule<TFeature> : IViewFeatureModule<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx) { }
        public void OnDetach(in FeatureModuleContext<TFeature> ctx) { }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            ctx.Feature?.TickVfx();
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }

    internal sealed class SharedInterpolationModule<TFeature> : IViewFeatureModule<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx) { }
        public void OnDetach(in FeatureModuleContext<TFeature> ctx) { }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            var f = ctx.Feature;
            var binder = f?.Binder;
            if (binder == null) return;
            binder.TickInterpolation(f.Context, deltaTime);
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }

    internal sealed class SharedFloatingTextModule<TFeature> : IViewFeatureModule<TFeature>
        where TFeature : class, IViewFeatureModulesHost
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx) { }
        public void OnDetach(in FeatureModuleContext<TFeature> ctx) { }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            ctx.Feature?.TickFloatingTexts(deltaTime);
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }
}
