using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    internal interface IBattleSessionGatewayRoomClientFactory
    {
        IGatewayRoomClient CreateGatewayRoomClient(IConnection connection, GatewayRoomOpCodes opCodes);
    }

    internal sealed class DefaultBattleSessionGatewayRoomClientFactory : IBattleSessionGatewayRoomClientFactory
    {
        public IGatewayRoomClient CreateGatewayRoomClient(IConnection connection, GatewayRoomOpCodes opCodes)
        {
            return new GatewayRoomClient(connection, opCodes);
        }
    }
}
