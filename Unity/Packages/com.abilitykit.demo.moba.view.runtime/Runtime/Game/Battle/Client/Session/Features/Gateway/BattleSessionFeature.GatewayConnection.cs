using AbilityKit.Game.Battle;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private IConnection CreateGatewayRoomConnection(BattleStartPlan plan)
        {
            var gateway = plan.Gateway;
            var descriptor = new AbilityKitConnectionDescriptor(
                AbilityKitConnectionRole.GatewayReliable,
                gateway.Host,
                gateway.Port,
                "tcp");

            return _connectionRegistry.GetOrCreate(descriptor, CreateGatewayRoomConnectionForDescriptor);
        }

        private IConnection CreateGatewayRoomConnectionForDescriptor(AbilityKitConnectionDescriptor descriptor)
        {
            return _gatewayConnectionFactory.CreateGatewayRoomConnection(_plan, _unityDispatcher, _networkIoDispatcher);
        }
    }
}
