#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;

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

            var worldStartAnchor = SelectWorldStartAnchor(in start.WorldStartAnchor, in join.WorldStartAnchor);

            return new ShooterRoomGatewayFlowResult(
                sessionToken,
                create.RoomId,
                create.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                in worldStartAnchor,
                start.ServerNowTicks,
                ShooterRoomGatewayEntryKind.TeamLobby,
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

            if (join.JoinKind != ShooterGatewayRoomJoinKind.TeamLobby && !string.IsNullOrWhiteSpace(join.BattleId))
            {
                var runningSubscribe = await _roomClient.SubscribeStateSyncAsync(
                    new ShooterGatewayStateSyncSubscriptionRequest(sessionToken, join.BattleId, roomId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                EnsureSuccess(runningSubscribe.Success, runningSubscribe.Message, "subscribe state sync");

                return new ShooterRoomGatewayFlowResult(
                    sessionToken,
                    roomId,
                    join.NumericRoomId,
                    join.BattleId,
                    join.WorldId,
                    playerId,
                    in join.WorldStartAnchor,
                    join.ServerNowTicks,
                    ToEntryKind(join.JoinKind),
                    join.CanStart,
                    started: true,
                    runningSubscribe.Success,
                    runningSubscribe.Message);
            }

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

            var worldStartAnchor = SelectWorldStartAnchor(in start.WorldStartAnchor, in join.WorldStartAnchor);

            return new ShooterRoomGatewayFlowResult(
                sessionToken,
                roomId,
                join.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                in worldStartAnchor,
                start.ServerNowTicks,
                ShooterRoomGatewayEntryKind.TeamLobby,
                ready.CanStart,
                start.Started,
                subscribe.Success,
                subscribe.Message);
        }

        private static ShooterRoomGatewayEntryKind ToEntryKind(ShooterGatewayRoomJoinKind joinKind)
        {
            return joinKind switch
            {
                ShooterGatewayRoomJoinKind.Reconnect => ShooterRoomGatewayEntryKind.Reconnect,
                ShooterGatewayRoomJoinKind.LateJoin => ShooterRoomGatewayEntryKind.LateJoin,
                _ => ShooterRoomGatewayEntryKind.TeamLobby
            };
        }

        private static ShooterGatewayWorldStartAnchor SelectWorldStartAnchor(
            in ShooterGatewayWorldStartAnchor startAnchor,
            in ShooterGatewayWorldStartAnchor joinAnchor)
        {
            return startAnchor.IsValid ? startAnchor : joinAnchor;
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

    public enum ShooterRoomGatewayEntryKind
    {
        TeamLobby = 0,
        Reconnect = 1,
        LateJoin = 2
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
        public readonly long ServerNowTicks;
        public readonly int TargetFrame;
        public readonly int CatchUpFrames;
        public readonly ShooterRoomGatewayEntryKind EntryKind;
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
            long serverNowTicks,
            ShooterRoomGatewayEntryKind entryKind,
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
            ServerNowTicks = serverNowTicks;
            var catchUp = WorldStartFrameCatchUpCalculator.Calculate(worldStartAnchor.ToFrameStartAnchor(), serverNowTicks);
            TargetFrame = catchUp.TargetFrame;
            CatchUpFrames = catchUp.CatchUpFrames;
            EntryKind = entryKind;
            CanStart = canStart;
            Started = started;
            Subscribed = subscribed;
            Message = message ?? string.Empty;
        }

        public ShooterRoomGatewayLaunchSummary ToSummary()
        {
            return new ShooterRoomGatewayLaunchSummary(
                RoomId,
                NumericRoomId,
                BattleId,
                WorldId,
                PlayerId,
                TargetFrame,
                CatchUpFrames,
                EntryKind,
                CanStart,
                Started,
                Subscribed,
                Message);
        }

        public ShooterGatewayBattleInputContext CreateBattleInputContext(int frame)
        {
            return new ShooterGatewayBattleInputContext(SessionToken, BattleId, WorldId, frame, PlayerId);
        }
    }

    public readonly struct ShooterRoomGatewayLaunchSummary
    {
        public ShooterRoomGatewayLaunchSummary(
            string roomId,
            ulong numericRoomId,
            string battleId,
            ulong worldId,
            uint playerId,
            int targetFrame,
            int catchUpFrames,
            ShooterRoomGatewayEntryKind entryKind,
            bool canStart,
            bool started,
            bool subscribed,
            string message)
        {
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            PlayerId = playerId;
            TargetFrame = targetFrame;
            CatchUpFrames = catchUpFrames;
            EntryKind = entryKind;
            CanStart = canStart;
            Started = started;
            Subscribed = subscribed;
            Message = message ?? string.Empty;
        }

        public string RoomId { get; }

        public ulong NumericRoomId { get; }

        public string BattleId { get; }

        public ulong WorldId { get; }

        public uint PlayerId { get; }

        public int TargetFrame { get; }

        public int CatchUpFrames { get; }

        public ShooterRoomGatewayEntryKind EntryKind { get; }

        public bool CanStart { get; }

        public bool Started { get; }

        public bool Subscribed { get; }

        public string Message { get; }

        public bool IsRunningEntry => EntryKind == ShooterRoomGatewayEntryKind.Reconnect || EntryKind == ShooterRoomGatewayEntryKind.LateJoin;

        public bool IsClosed => !string.IsNullOrWhiteSpace(RoomId)
            && !string.IsNullOrWhiteSpace(BattleId)
            && WorldId != 0UL
            && PlayerId != 0U
            && Started
            && Subscribed;
    }
}
