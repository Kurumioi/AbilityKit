using System;
using AbilityKit.Game.Battle;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;

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
            if (_gatewayConnectionFactory != null)
            {
                var connection = _gatewayConnectionFactory(_plan);
                if (connection == null)
                {
                    throw new InvalidOperationException("Gateway connection factory returned null.");
                }

                return connection;
            }

            var connOptions = new ConnectionOptions
            {
                FrameCodec = LengthPrefixedFrameCodec.Instance,
                KickPushOpCode = 9000
            };

            return new ConnectionManager(() => new TcpTransport(), connOptions, _unityDispatcher, _networkIoDispatcher);
        }
    }
}
