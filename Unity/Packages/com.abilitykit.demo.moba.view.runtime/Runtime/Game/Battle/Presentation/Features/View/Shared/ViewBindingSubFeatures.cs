using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Game.Flow.Modules;
using AbilityKit.World.ECS;
using System;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewBindingSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        private static void ApplyInterpolationSettingsIfAny(FeatureModuleContext<TFeature> ctx, BattleViewBinder binder)
        {
            if (binder == null) return;

            var flow = ctx.Phase.Entry != null ? ctx.Phase.Entry.Get<GameFlowDomain>() : null;
            var settings = flow?.Settings;
            if (settings == null) return;

            if (settings.TryGetBool("View.Interp.Enabled", out var enabled)) binder.InterpolationEnabled = enabled;
            if (settings.TryGetFloat("View.Interp.BackTimeTicks", out var backTicks)) binder.BackTimeTicks = backTicks;
            if (settings.TryGetFloat("View.Interp.MaxLagTicks", out var maxLagTicks)) binder.MaxLagTicks = maxLagTicks;
        }

        public void OnAttach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.Binder?.Clear();
            runtime.Binder = new BattleViewBinder(runtime.Vfx, runtime.VfxNode);
            ApplyInterpolationSettingsIfAny(ctx, runtime.Binder);

            runtime.EntityDestroyedSubscription?.Dispose();
            if (runtime.Context?.EntityWorld != null)
            {
                runtime.EntityDestroyedSubscription = runtime.Context.EntityWorld.EntityDestroyed(runtime.OnEntityDestroyed);
            }
        }

        public void OnDetach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.EntityDestroyedSubscription?.Dispose();
            runtime.EntityDestroyedSubscription = null;

            runtime.Binder?.Clear();
            runtime.Binder = null;
        }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;
            if (runtime.Binder == null) return;
            ApplyInterpolationSettingsIfAny(ctx, runtime.Binder);
        }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }
}
