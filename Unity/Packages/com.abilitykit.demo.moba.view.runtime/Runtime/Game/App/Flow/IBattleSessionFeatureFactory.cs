using System;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public interface IBattleSessionFeatureFactory
    {
        IBattleSessionFeature Create(
            IBattleBootstrapper bootstrapper,
            Func<BattleStartPlan, IConnection> gatewayConnectionFactory);
    }

    public sealed class DefaultBattleSessionFeatureFactory : IBattleSessionFeatureFactory
    {
        public IBattleSessionFeature Create(
            IBattleBootstrapper bootstrapper,
            Func<BattleStartPlan, IConnection> gatewayConnectionFactory)
        {
            return new BattleSessionFeature(bootstrapper, gatewayConnectionFactory);
        }
    }
}
