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
        public void OnAttach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            if (BattleViewFactory.VfxDb == null) BattleViewFactory.VfxDb = VfxDatabase.LoadFromResources("vfx/vfx");
            runtime.Vfx = new BattleVfxManager(BattleViewFactory.VfxDb);

            runtime.VfxNode = default;
            if (runtime.Context != null && runtime.Context.EntityNode.IsValid)
            {
                var vfxNode = runtime.Context.EntityNode.World.CreateChild(runtime.Context.EntityNode);
                vfxNode.SetName(runtime.IsConfirmed ? "BattleVfx_confirmed" : "BattleVfx");
                runtime.VfxNode = vfxNode;
            }
        }

        public void OnDetach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.Vfx = null;
            runtime.VfxNode = default;
        }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }
}
