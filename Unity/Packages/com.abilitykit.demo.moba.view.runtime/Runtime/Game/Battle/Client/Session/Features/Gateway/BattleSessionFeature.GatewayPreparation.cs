using System;
using System.Threading.Tasks;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Common.Log;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private async Task PrepareRoomAsync()
        {
            await WaitForGatewayConnectionAsync();
            await EnsureGatewaySessionTokenAsync();

            if (_plan.GatewayAutoCreateRoom)
            {
                await CreateAndJoinGatewayRoomAsync();
                return;
            }

            if (_plan.GatewayAutoJoinRoom)
            {
                await JoinGatewayRoomAsync();
            }
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

            Log.Info($"[BattleSessionFeature] GatewayRoom connected: {_plan.GatewayHost}:{_plan.GatewayPort}");
        }

        private async Task EnsureGatewaySessionTokenAsync()
        {
            const uint GuestLoginOpCode = 100;
            var sessionToken = _plan.GatewaySessionToken;
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
            Log.Info("[BattleSessionFeature] GatewayRoom CreateRoom...");
            var result = await _gatewayClient.CreateRoomAsync(
                sessionToken: _plan.GatewaySessionToken,
                region: _plan.GatewayRegion,
                serverId: _plan.GatewayServerId,
                roomType: string.IsNullOrEmpty(_plan.WorldType) ? "battle" : _plan.WorldType,
                title: string.Empty,
                isPublic: true,
                maxPlayers: 10,
                tags: null);

            Log.Info($"[BattleSessionFeature] GatewayRoom CreateRoom ok. roomId='{result.RoomId}' numericRoomId={result.NumericRoomId}");

            if (result.NumericRoomId == 0)
            {
                throw new InvalidOperationException($"Gateway CreateRoom returned invalid NumericRoomId. roomId='{result.RoomId}'");
            }

            var worldId = result.NumericRoomId.ToString();
            _plan = _plan.WithGatewayRoom(worldId, result.NumericRoomId);

            var joinResult = await _gatewayClient.JoinRoomAsync(
                sessionToken: _plan.GatewaySessionToken,
                region: _plan.GatewayRegion,
                serverId: _plan.GatewayServerId,
                roomId: string.IsNullOrWhiteSpace(result.RoomId) ? _plan.NumericRoomId.ToString() : result.RoomId);

            var wid = new WorldId(_plan.WorldId);
            if (joinResult.WorldStartAnchor.ServerTickFrequency != 0)
            {
                _gatewayWorldStartAnchors[wid] = joinResult.WorldStartAnchor;
            }

            StartTimeSyncLoop();

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom ok. numericRoomId={_plan.NumericRoomId}");
        }

        private async Task JoinGatewayRoomAsync()
        {
            var joinRoomId = _plan.GatewayJoinRoomId;
            if (string.IsNullOrWhiteSpace(joinRoomId))
            {
                joinRoomId = _plan.NumericRoomId != 0 ? _plan.NumericRoomId.ToString() : _plan.WorldId;
            }
            if (string.IsNullOrWhiteSpace(joinRoomId))
            {
                throw new InvalidOperationException("GatewayAutoJoinRoom requires JoinRoomId or WorldId.");
            }

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom... roomId='{joinRoomId}'");
            var result = await _gatewayClient.JoinRoomAsync(
                sessionToken: _plan.GatewaySessionToken,
                region: _plan.GatewayRegion,
                serverId: _plan.GatewayServerId,
                roomId: joinRoomId);

            var tmpWid = new WorldId(_plan.WorldId);
            if (result.WorldStartAnchor.ServerTickFrequency != 0)
            {
                _gatewayWorldStartAnchors[tmpWid] = result.WorldStartAnchor;
            }

            StartTimeSyncLoop();

            Log.Info($"[BattleSessionFeature] GatewayRoom JoinRoom ok. numericRoomId={result.NumericRoomId}");

            if (result.NumericRoomId == 0)
            {
                throw new InvalidOperationException($"Gateway JoinRoom returned invalid NumericRoomId. roomId='{joinRoomId}'");
            }

            var worldId = result.NumericRoomId.ToString();
            _plan = _plan.WithGatewayRoom(worldId, result.NumericRoomId);
        }
    }
}
