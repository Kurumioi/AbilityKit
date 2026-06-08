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
            if (_plan.HostMode != BattleStartConfig.BattleHostMode.GatewayRemote) return false;
            if (!_plan.UseGatewayTransport) return false;
            if (!_plan.GatewayAutoCreateRoom && !_plan.GatewayAutoJoinRoom) return false;
            return true;
        }

        private void StartGatewayRoomPreparation()
        {
            StopGatewayRoomPreparation();

            _gatewayConn = CreateGatewayRoomConnection(_plan);
            _gatewayConn.Open(_plan.GatewayHost, _plan.GatewayPort);

            var opCodes = new GatewayRoomOpCodes(_plan.GatewayCreateRoomOpCode, _plan.GatewayJoinRoomOpCode);
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
