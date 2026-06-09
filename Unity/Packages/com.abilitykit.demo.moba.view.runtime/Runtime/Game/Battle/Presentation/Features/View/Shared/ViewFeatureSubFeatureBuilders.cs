using System;
using System.Collections.Generic;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewFeatureSubFeatureBuilder
    {
        private readonly ViewSubFeatureFactory _factory;

        public ViewFeatureSubFeatureBuilder(ViewSubFeatureFactory factory = null)
        {
            _factory = factory ?? new ViewSubFeatureFactory();
        }

        public void AddBattleViewSubFeatures(List<IViewSubFeature<BattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            _factory.AddDefaultViewSubFeatures(subFeatures);
        }

        public void AddConfirmedViewSubFeatures(List<IViewSubFeature<ConfirmedBattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            _factory.AddDefaultViewSubFeatures(subFeatures);
        }
    }

    internal sealed class ViewSubFeatureFactory
    {
        public void AddDefaultViewSubFeatures<TFeature>(List<IViewSubFeature<TFeature>> subFeatures)
            where TFeature : class, IViewFeatureRuntime
        {
            subFeatures.Add(new ViewContextBindingSubFeature<TFeature>());
            subFeatures.Add(new ViewTimelineSubFeature<TFeature>());
            subFeatures.Add(new ViewVfxSubFeature<TFeature>());
            subFeatures.Add(new ViewBindingSubFeature<TFeature>());
            subFeatures.Add(new ViewFloatingTextSubFeature<TFeature>());
            subFeatures.Add(new ViewAreaViewsSubFeature<TFeature>());
            subFeatures.Add(new ViewEventSinkSubFeature<TFeature>());
            subFeatures.Add(new ViewEventAdaptersSubFeature<TFeature>());
        }
    }
}
