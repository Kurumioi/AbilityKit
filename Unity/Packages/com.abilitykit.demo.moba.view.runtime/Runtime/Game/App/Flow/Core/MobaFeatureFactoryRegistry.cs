using System;
using System.Collections.Generic;
using AbilityKit.Game.View.Flow;

namespace AbilityKit.Game.Flow
{
    public sealed class MobaFeatureFactoryRegistry
    {
        private readonly Dictionary<string, PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>.FeatureFactory> _factories =
            new Dictionary<string, PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>.FeatureFactory>(StringComparer.Ordinal);

        public MobaFeatureFactoryRegistry Register(
            string featureId,
            PhaseFeaturePlan<GamePhaseContext, IGamePhaseFeature>.FeatureFactory factory)
        {
            if (string.IsNullOrWhiteSpace(featureId)) throw new ArgumentException("Feature id is required.", nameof(featureId));
            _factories[featureId] = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        public IGamePhaseFeature Create(string featureId, in GamePhaseContext ctx)
        {
            if (!_factories.TryGetValue(featureId, out var factory))
            {
                throw new InvalidOperationException($"Moba feature factory is not registered: {featureId}");
            }

            return factory(in ctx);
        }
    }
}
