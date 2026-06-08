using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SharedVfxTickSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewSharedSubFeatureHost
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx) { }
        public void OnDetach(in FeatureModuleContext<TFeature> ctx) { }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            ctx.Feature?.TickVfx();
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }

    internal sealed class SharedInterpolationSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewSharedSubFeatureHost
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

    internal sealed class SharedFloatingTextSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewSharedSubFeatureHost
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
