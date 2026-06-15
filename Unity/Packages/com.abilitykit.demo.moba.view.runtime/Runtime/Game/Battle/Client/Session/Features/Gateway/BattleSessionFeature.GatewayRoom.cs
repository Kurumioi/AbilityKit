using System.Threading.Tasks;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private bool HasGatewayRoomConnection => _gatewayConn != null;

        private void TickGatewayRoomConnection(float deltaTime) => _gatewayConn?.Tick(deltaTime);

        private Task GatewayRoomPreparationTask => _gatewayTask;

        private bool ShouldPrepareGatewayRoom()
        {
            var gateway = _plan.Gateway;
            if (_plan.HostMode != BattleStartConfig.BattleHostMode.GatewayRemote) return false;
            if (!gateway.UseGatewayTransport) return false;
            if (!gateway.AutoCreateRoom && !gateway.AutoJoinRoom) return false;
            return true;
        }

        private void StartGatewayRoomPreparation()
        {
            StopGatewayRoomPreparation();

            var gateway = _plan.Gateway;
            _gatewayConn = CreateGatewayRoomConnection(_plan);
            _gatewayConn.Open(gateway.Host, gateway.Port);

            var opCodes = new GatewayRoomOpCodes(gateway.CreateRoomOpCode, gateway.JoinRoomOpCode);
            _gatewayClient = new GatewayRoomClient(_gatewayConn, opCodes);

            _gatewayTask = PrepareRoomAsync();
        }

        private void StopGatewayRoomPreparation()
        {
            _gatewayTask = null;
            _gatewayClient = null;

            StopTimeSyncLoop();
            _gatewayWorldStartAnchors.Clear();

            if (_connectionRegistry != null)
            {
                _connectionRegistry.Remove(AbilityKitConnectionRole.GatewayReliable);
            }

            _gatewayConn = null;
        }
    }
}
