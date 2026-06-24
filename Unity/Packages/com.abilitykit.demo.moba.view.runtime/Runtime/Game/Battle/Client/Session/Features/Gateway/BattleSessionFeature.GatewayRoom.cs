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

        private bool ShouldPrepareGatewayRoom() => GatewayRoomPreparationHelper.ShouldPrepareGatewayRoom(_plan);

        private void StartGatewayRoomPreparation()
        {
            StopGatewayRoomPreparation();

            var gateway = _plan.Gateway;
            _gatewayConn = CreateGatewayRoomConnection(_plan);
            _gatewayConn.Open(gateway.Host, gateway.Port);

            var opCodes = new GatewayRoomOpCodes(gateway.CreateRoomOpCode, gateway.JoinRoomOpCode);
            _gatewayClient = _gatewayRoomClientFactory.CreateGatewayRoomClient(_gatewayConn, opCodes);

            _gatewayTask = PrepareRoomAsync();
        }

        private void StopGatewayRoomPreparation()
        {
            _gatewayTask = null;
            _gatewayClient = null;

            StopTimeSyncLoop();
            GatewayRoomCleanupHelper.ClearWorldStartAnchors(_gatewayWorldStartAnchors);
            GatewayRoomCleanupHelper.RemoveGatewayReliableConnection(_connectionRegistry);

            _gatewayConn = null;
        }
    }
}
