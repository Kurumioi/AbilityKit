using System;
using AbilityKit.Game.View.Flow;

namespace AbilityKit.Game.Flow
{
    internal sealed class MobaFeaturePlanFactory
    {
        private readonly MobaFeatureFactoryRegistry _featureFactories;

        public MobaFeaturePlanFactory(MobaFeatureFactoryRegistry featureFactories)
        {
            _featureFactories = featureFactories ?? throw new ArgumentNullException(nameof(featureFactories));
        }

        public PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> CreateBootFeaturePlan(int capacity = 2)
        {
            return new PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>(capacity)
                .Add("demo_lobby", (in GamePhaseContext ctx) => _featureFactories.Create("demo_lobby", in ctx))
                .Add("root_debug", (in GamePhaseContext ctx) => _featureFactories.Create("root_debug", in ctx));
        }

        public PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature> CreateBattleFeaturePlan(int capacity = 8)
        {
            return new PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>(capacity)
                .Add("context", (in GamePhaseContext ctx) => _featureFactories.Create("context", in ctx))
                .Add("session", (in GamePhaseContext ctx) => _featureFactories.Create("session", in ctx))
                .Add("entity", (in GamePhaseContext ctx) => _featureFactories.Create("entity", in ctx))
                .Add("sync", (in GamePhaseContext ctx) => _featureFactories.Create("sync", in ctx))
                .Add("input", (in GamePhaseContext ctx) => _featureFactories.Create("input", in ctx))
                .Add("view", (in GamePhaseContext ctx) => _featureFactories.Create("view", in ctx))
                .Add("hud", (in GamePhaseContext ctx) => _featureFactories.Create("hud", in ctx))
                .Add("debug_ongui", (in GamePhaseContext ctx) => _featureFactories.Create("debug_ongui", in ctx));
        }
    }
}
