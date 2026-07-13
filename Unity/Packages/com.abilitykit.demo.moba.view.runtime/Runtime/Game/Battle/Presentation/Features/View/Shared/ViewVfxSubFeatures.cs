using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Battle.Vfx;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Modules;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewVfxSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        private readonly ViewVfxRuntimeFactory _factory;

        public ViewVfxSubFeature(ViewVfxRuntimeFactory factory = null)
        {
            _factory = factory ?? new ViewVfxRuntimeFactory();
        }

        public void OnAttach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.Vfx = _factory.CreateManager(runtime.Resources);
            runtime.VfxNode = _factory.CreateNode(runtime.Context, runtime.IsConfirmed);
            if (runtime.Context != null && !runtime.IsConfirmed)
            {
                runtime.Context.ViewVfxManager = runtime.Vfx;
                runtime.Context.ViewVfxNode = runtime.VfxNode;
            }
        }

        public void OnDetach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            if (runtime.Context != null && !runtime.IsConfirmed)
            {
                runtime.Context.ViewVfxManager = null;
                runtime.Context.ViewVfxNode = default;
            }

            runtime.Vfx = null;
            runtime.VfxNode = default;
        }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }

    internal sealed class ViewVfxRuntimeFactory
    {
        public BattleVfxManager CreateManager(BattleViewResourceProvider resources)
        {
            return new BattleVfxManager(resources.GetOrLoadVfxDb());
        }

        public IEntity CreateNode(BattleContext ctx, bool isConfirmed)
        {
            if (ctx == null || !ctx.EntityNode.IsValid)
            {
                return default;
            }

            var vfxNode = ctx.EntityNode.World.CreateChild(ctx.EntityNode);
            vfxNode.SetName(isConfirmed ? "BattleVfx_confirmed" : "BattleVfx");
            return vfxNode;
        }
    }
}
