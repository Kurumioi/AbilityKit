using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Transport;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private BattleLogicSession StartBattleLogicSession(BattleLogicSessionOptions opts)
        {
            var world = _plan.World;
            var gateway = _plan.Gateway;

            if (_plan.HostMode == BattleStartConfig.BattleHostMode.GatewayRemote && gateway.UseGatewayTransport)
            {
                if (!uint.TryParse(world.PlayerId, out var localPlayerId))
                {
                    throw new InvalidOperationException($"GatewayRemote requires numeric PlayerId. playerId='{world.PlayerId}'");
                }

                var roomId = gateway.NumericRoomId;
                if (roomId == 0 && !ulong.TryParse(world.WorldId, out roomId))
                {
                    throw new InvalidOperationException($"GatewayRemote requires numeric WorldId(roomId). worldId='{world.WorldId}'");
                }

                var gatewayOptions = NetworkTransportOptionsFactory.Create(
                    host: gateway.Host,
                    port: gateway.Port,
                    transportFactory: () => new TcpTransport(),
                    playerIdToUInt: pid => uint.TryParse(pid.Value, out var n) ? n : localPlayerId,
                    playerIdFromUInt: n => new PlayerId(n.ToString()),
                    worldIdToUlong: wid => ulong.TryParse(wid.Value, out var n) ? n : roomId,
                    worldIdFromUlong: n => new WorldId(n.ToString()),
                    roomId: roomId,
                    sessionToken: gateway.SessionToken);

                var transport = new NetworkTransport(gatewayOptions, _unityDispatcher, _networkIoDispatcher);
                return BattleLogicSessionHost.Start(opts, remoteTransport: transport);
            }

            return BattleLogicSessionHost.Start(opts);
        }
    }
}
