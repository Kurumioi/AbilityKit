using System;
using System.Collections.Generic;

namespace AbilityKit.Game.Flow
{
    internal static class ViewFeatureSubFeatureBuilder
    {
        public static void AddBattleViewSubFeatures(List<IViewSubFeature<BattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            subFeatures.Add(new ViewContextBindingSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewTimelineSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewVfxSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewBindingSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewFloatingTextSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewAreaViewsSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewEventSinkSubFeature<BattleViewFeature>());
            subFeatures.Add(new ViewEventAdaptersSubFeature<BattleViewFeature>());
        }

        public static void AddConfirmedViewSubFeatures(List<IViewSubFeature<ConfirmedBattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            subFeatures.Add(new ViewContextBindingSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewTimelineSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewVfxSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewBindingSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewFloatingTextSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewAreaViewsSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewEventSinkSubFeature<ConfirmedBattleViewFeature>());
            subFeatures.Add(new ViewEventAdaptersSubFeature<ConfirmedBattleViewFeature>());
        }
    }
}
