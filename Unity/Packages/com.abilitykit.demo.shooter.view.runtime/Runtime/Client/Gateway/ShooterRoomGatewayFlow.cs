#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterRoomGatewayFlow
    {
        private readonly IShooterRoomGatewayRoomClient _roomClient;

        public ShooterRoomGatewayFlow(IShooterRoomGatewayRoomClient roomClient)
        {
            _roomClient = roomClient ?? throw new ArgumentNullException(nameof(roomClient));
        }

        public async Task<ShooterRoomGatewayFlowResult> CreateReadyStartAndSubscribeAsync(
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateSessionToken(sessionToken);
            if (playerId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId));
            }

            var create = await _roomClient.CreateRoomAsync(
                new ShooterGatewayCreateRoomRequest(
                    sessionToken,
                    launchSpec.Region,
                    launchSpec.ServerId,
                    ShooterGameplay.RoomType,
                    launchSpec.RoomTitle,
                    isPublic: true,
                    launchSpec.MaxPlayers,
                    launchSpec.Tags),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(create.Success, create.Message, "create room");

            var join = await _roomClient.JoinRoomAsync(
                new ShooterGatewayJoinRoomRequest(sessionToken, launchSpec.Region, launchSpec.ServerId, create.RoomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(join.Success, join.Message, "join room");

            var ready = await _roomClient.SetReadyAsync(
                new ShooterGatewayReadyRequest(sessionToken, create.RoomId, ready: true),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(ready.Success, ready.Message, "set ready");

            var start = await _roomClient.StartBattleAsync(
                new ShooterGatewayStartBattleRequest(
                    sessionToken,
                    create.RoomId,
                    launchSpec.GameplayId,
                    launchSpec.RuleSetId,
                    launchSpec.ConfigVersion,
                    launchSpec.ProtocolVersion,
                    launchSpec.WorldType,
                    launchSpec.ClientId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(start.Success, start.Message, "start battle");

            var battleId = string.IsNullOrWhiteSpace(start.BattleId) ? ready.BattleId : start.BattleId;
            if (string.IsNullOrWhiteSpace(battleId))
            {
                battleId = join.BattleId;
            }

            if (string.IsNullOrWhiteSpace(battleId))
            {
                throw new InvalidOperationException("start battle did not return a battle id.");
            }

            var subscribe = await _roomClient.SubscribeStateSyncAsync(
                new ShooterGatewayStateSyncSubscriptionRequest(sessionToken, battleId, create.RoomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(subscribe.Success, subscribe.Message, "subscribe state sync");

            return new ShooterRoomGatewayFlowResult(
                sessionToken,
                create.RoomId,
                create.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                in join.WorldStartAnchor,
                ready.CanStart,
                start.Started,
                subscribe.Success,
                subscribe.Message);
        }

        public async Task<ShooterRoomGatewayFlowResult> JoinReadyStartAndSubscribeAsync(
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateSessionToken(sessionToken);
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("roomId is required.", nameof(roomId));
            }

            if (playerId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId));
            }

            var join = await _roomClient.JoinRoomAsync(
                new ShooterGatewayJoinRoomRequest(sessionToken, launchSpec.Region, launchSpec.ServerId, roomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(join.Success, join.Message, "join room");

            var ready = await _roomClient.SetReadyAsync(
                new ShooterGatewayReadyRequest(sessionToken, roomId, ready: true),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(ready.Success, ready.Message, "set ready");

            var start = await _roomClient.StartBattleAsync(
                new ShooterGatewayStartBattleRequest(
                    sessionToken,
                    roomId,
                    launchSpec.GameplayId,
                    launchSpec.RuleSetId,
                    launchSpec.ConfigVersion,
                    launchSpec.ProtocolVersion,
                    launchSpec.WorldType,
                    launchSpec.ClientId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(start.Success, start.Message, "start battle");

            var battleId = string.IsNullOrWhiteSpace(start.BattleId) ? ready.BattleId : start.BattleId;
            if (string.IsNullOrWhiteSpace(battleId))
            {
                battleId = join.BattleId;
            }

            if (string.IsNullOrWhiteSpace(battleId))
            {
                throw new InvalidOperationException("start battle did not return a battle id.");
            }

            var subscribe = await _roomClient.SubscribeStateSyncAsync(
                new ShooterGatewayStateSyncSubscriptionRequest(sessionToken, battleId, roomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(subscribe.Success, subscribe.Message, "subscribe state sync");

            return new ShooterRoomGatewayFlowResult(
                sessionToken,
                roomId,
                join.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                in join.WorldStartAnchor,
                ready.CanStart,
                start.Started,
                subscribe.Success,
                subscribe.Message);
        }

        private static void ValidateSessionToken(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            }
        }

        private static void EnsureSuccess(bool success, string message, string operation)
        {
            if (!success)
            {
                throw new InvalidOperationException($"Shooter room gateway {operation} failed: {message}");
            }
        }
    }

    public readonly struct ShooterRoomGatewayFlowResult
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly string BattleId;
        public readonly ulong WorldId;
        public readonly uint PlayerId;
        public readonly ShooterGatewayWorldStartAnchor WorldStartAnchor;
        public readonly bool CanStart;
        public readonly bool Started;
        public readonly bool Subscribed;
        public readonly string Message;

        public ShooterRoomGatewayFlowResult(
            string sessionToken,
            string roomId,
            ulong numericRoomId,
            string battleId,
            ulong worldId,
            uint playerId,
            in ShooterGatewayWorldStartAnchor worldStartAnchor,
            bool canStart,
            bool started,
            bool subscribed,
            string message)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            PlayerId = playerId;
            WorldStartAnchor = worldStartAnchor;
            CanStart = canStart;
            Started = started;
            Subscribed = subscribed;
            Message = message ?? string.Empty;
        }

        public ShooterGatewayBattleInputContext CreateBattleInputContext(int frame)
        {
            return new ShooterGatewayBattleInputContext(SessionToken, BattleId, WorldId, frame, PlayerId);
        }
    }
}
