using System;
using System.Threading.Tasks;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Logging;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private Task PrepareRoomAsync()
        {
            return GatewayRoomPreparationController.RunAsync(
                getPlan: () => _plan,
                waitForConnectionAsync: WaitForGatewayConnectionAsync,
                ensureSessionTokenAsync: EnsureGatewaySessionTokenAsync,
                createAndJoinRoomAsync: CreateAndJoinGatewayRoomAsync,
                joinRoomAsync: JoinGatewayRoomAsync);
        }

        private async Task WaitForGatewayConnectionAsync()
        {
            var conn = _gatewayConn;

            while (conn != null && conn.State == ConnectionState.Connecting)
            {
                await Task.Yield();
            }

            if (conn == null || conn.State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Gateway room connection not connected. state={conn?.State}");
            }

            var gateway = _plan.Gateway;
            Log.Info($"[BattleSessionFeature] GatewayRoom connected: {gateway.Host}:{gateway.Port}");
        }

        private async Task EnsureGatewaySessionTokenAsync()
        {
            const uint GuestLoginOpCode = 100;
            var sessionToken = _plan.Gateway.SessionToken;
            if (!string.IsNullOrWhiteSpace(sessionToken)) return;

            Log.Info("[BattleSessionFeature] GatewayRoom GuestLogin...");
            sessionToken = await _gatewayClient.GuestLoginAsync(GuestLoginOpCode);
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new InvalidOperationException("Gateway guest login failed: sessionToken is empty.");
            }

            Log.Info("[BattleSessionFeature] GatewayRoom GuestLogin ok.");

            _plan = _plan.WithGatewaySessionToken(sessionToken);
        }

        private async Task CreateAndJoinGatewayRoomAsync()
        {
            var world = _plan.World;
            var gateway = _plan.Gateway;

            Log.Info("[BattleSessionFeature] GatewayRoom CreateRoom...");
            var result = await _gatewayClient.CreateRoomAsync(
                sessionToken: gateway.SessionToken,
                region: gateway.Region,
                serverId: gateway.ServerId,
                roomType: string.IsNullOrEmpty(world.WorldType) ? "battle" : world.WorldType,
                title: string.Empty,
                isPublic: true,
                maxPlayers: 10,
                tags: null);

            Log.Info($"[BattleSessionFeature] GatewayRoom CreateRoom ok. roomId='{result.RoomId}' numericRoomId={result.NumericRoomId}");

            var worldId = GatewayRoomPreparationHelper.ResolveCreatedRoomWorldId(in result);
            _plan = _plan.WithGatewayRoom(worldId, result.NumericRoomId);

            var updatedGateway = _plan.Gateway;
            var joinResult = await _gatewayClient.JoinRoomAsync(
                sessionToken: updatedGateway.SessionToken,
                region: updatedGateway.Region,
                serverId: updatedGateway.ServerId,
                roomId: GatewayRoomPreparationHelper.ResolveCreatedRoomJoinRoomId(in result, updatedGateway.NumericRoomId));

            GatewayRoomPreparationHelper.TryRecordWorldStartAnchor(
                _gatewayWorldStartAnchors,
                new WorldId(_plan.World.WorldId),
                in joinResult.WorldStartAnchor);

            StartTimeSyncLoop();

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom ok. numericRoomId={_plan.Gateway.NumericRoomId}");
        }

        private async Task JoinGatewayRoomAsync()
        {
            var world = _plan.World;
            var gateway = _plan.Gateway;
            var joinRoomId = GatewayRoomPreparationHelper.ResolveJoinRoomId(_plan);

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom... roomId='{joinRoomId}'");
            var result = await _gatewayClient.JoinRoomAsync(
                sessionToken: gateway.SessionToken,
                region: gateway.Region,
                serverId: gateway.ServerId,
                roomId: joinRoomId);

            GatewayRoomPreparationHelper.TryRecordWorldStartAnchor(
                _gatewayWorldStartAnchors,
                new WorldId(world.WorldId),
                in result.WorldStartAnchor);

            StartTimeSyncLoop();

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom ok. numericRoomId={result.NumericRoomId}");

            var worldId = GatewayRoomPreparationHelper.ResolveJoinedRoomWorldId(in result, joinRoomId);
            _plan = _plan.WithGatewayRoom(worldId, result.NumericRoomId);
        }
    }
}
