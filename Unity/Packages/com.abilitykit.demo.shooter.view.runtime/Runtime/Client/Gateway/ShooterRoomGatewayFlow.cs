#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using AbilityKit.Ability.Host.Extensions.Session;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterRoomGatewayFlow
    {
        private readonly RoomGatewaySessionFlow _flow;

        public ShooterRoomGatewayFlow(IShooterRoomGatewayRoomClient roomClient)
        {
            if (roomClient == null) throw new ArgumentNullException(nameof(roomClient));
            _flow = new RoomGatewaySessionFlow(new ShooterRoomGatewaySessionClient(roomClient));
        }

        public async Task<ShooterRoomGatewayFlowResult> CreateReadyStartAndSubscribeAsync(
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _flow.CreateReadyStartAndSubscribeAsync(
                sessionToken,
                ToLaunchSpec(in launchSpec),
                playerId,
                timeout,
                cancellationToken).ConfigureAwait(false);
            return ToShooterResult(in result);
        }

        public async Task<ShooterRoomGatewayFlowResult> JoinReadyStartAndSubscribeAsync(
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _flow.JoinReadyStartAndSubscribeAsync(
                sessionToken,
                roomId,
                ToLaunchSpec(in launchSpec),
                playerId,
                timeout,
                cancellationToken).ConfigureAwait(false);
            return ToShooterResult(in result);
        }

        public async Task<ShooterRoomGatewayFlowResult> RestoreRoomAsync(
            string sessionToken,
            string region,
            string serverId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _flow.RestoreRoomAsync(
                sessionToken,
                region,
                serverId,
                ToLaunchSpec(in launchSpec),
                playerId,
                timeout,
                cancellationToken).ConfigureAwait(false);
            return ToShooterResult(in result);
        }

        private static RoomGatewayLaunchSpec ToLaunchSpec(in ShooterRoomLaunchSpec launchSpec)
        {
            return new RoomGatewayLaunchSpec(
                launchSpec.Region,
                launchSpec.ServerId,
                ShooterGameplay.RoomType,
                launchSpec.RoomTitle,
                launchSpec.MaxPlayers,
                launchSpec.GameplayId,
                launchSpec.RuleSetId,
                launchSpec.ConfigVersion,
                launchSpec.ProtocolVersion,
                launchSpec.WorldType,
                launchSpec.ClientId,
                launchSpec.Tags);
        }

        private static ShooterRoomGatewayFlowResult ToShooterResult(in RoomGatewaySessionFlowResult result)
        {
            var anchor = ToShooterAnchor(result.WorldStartAnchor);
            return new ShooterRoomGatewayFlowResult(
                result.SessionToken,
                result.RoomId,
                result.NumericRoomId,
                result.BattleId,
                result.WorldId,
                result.PlayerId,
                in anchor,
                result.ServerNowTicks,
                ToShooterEntryKind(result.EntryKind),
                result.CanStart,
                result.Started,
                result.Subscribed,
                result.Message,
                ToShooterRestoreStatus(result.RestoreStatus),
                ToShooterRestoreErrorCode(result.RestoreErrorCode));
        }

        private static RoomGatewayWorldStartAnchor ToRoomAnchor(in ShooterGatewayWorldStartAnchor anchor)
        {
            return new RoomGatewayWorldStartAnchor(anchor.StartServerTicks, anchor.ServerTickFrequency, anchor.StartFrame, anchor.FixedDeltaSeconds);
        }

        private static ShooterGatewayWorldStartAnchor ToShooterAnchor(RoomGatewayWorldStartAnchor anchor)
        {
            return new ShooterGatewayWorldStartAnchor(anchor.StartServerTicks, anchor.ServerTickFrequency, anchor.StartFrame, anchor.FixedDeltaSeconds);
        }

        private static ShooterRoomGatewayEntryKind ToShooterEntryKind(RoomGatewaySessionEntryKind entryKind)
        {
            return entryKind switch
            {
                RoomGatewaySessionEntryKind.Reconnect => ShooterRoomGatewayEntryKind.Reconnect,
                RoomGatewaySessionEntryKind.LateJoin => ShooterRoomGatewayEntryKind.LateJoin,
                _ => ShooterRoomGatewayEntryKind.TeamLobby
            };
        }

        private static RoomGatewaySessionEntryKind ToRoomEntryKind(ShooterGatewayRoomJoinKind joinKind)
        {
            return joinKind switch
            {
                ShooterGatewayRoomJoinKind.Reconnect => RoomGatewaySessionEntryKind.Reconnect,
                ShooterGatewayRoomJoinKind.LateJoin => RoomGatewaySessionEntryKind.LateJoin,
                _ => RoomGatewaySessionEntryKind.TeamLobby
            };
        }

        private static ShooterGatewayRoomRestoreStatus ToShooterRestoreStatus(RoomGatewaySessionRestoreStatus status)
        {
            return status switch
            {
                RoomGatewaySessionRestoreStatus.NoActiveRoom => ShooterGatewayRoomRestoreStatus.NoActiveRoom,
                RoomGatewaySessionRestoreStatus.NotMember => ShooterGatewayRoomRestoreStatus.NotMember,
                RoomGatewaySessionRestoreStatus.RoomClosed => ShooterGatewayRoomRestoreStatus.RoomClosed,
                RoomGatewaySessionRestoreStatus.RoomExpired => ShooterGatewayRoomRestoreStatus.RoomExpired,
                RoomGatewaySessionRestoreStatus.InvalidSession => ShooterGatewayRoomRestoreStatus.InvalidSession,
                RoomGatewaySessionRestoreStatus.Failed => ShooterGatewayRoomRestoreStatus.Failed,
                _ => ShooterGatewayRoomRestoreStatus.Restored
            };
        }

        private static RoomGatewaySessionRestoreStatus ToRoomRestoreStatus(ShooterGatewayRoomRestoreStatus status)
        {
            return status switch
            {
                ShooterGatewayRoomRestoreStatus.NoActiveRoom => RoomGatewaySessionRestoreStatus.NoActiveRoom,
                ShooterGatewayRoomRestoreStatus.NotMember => RoomGatewaySessionRestoreStatus.NotMember,
                ShooterGatewayRoomRestoreStatus.RoomClosed => RoomGatewaySessionRestoreStatus.RoomClosed,
                ShooterGatewayRoomRestoreStatus.RoomExpired => RoomGatewaySessionRestoreStatus.RoomExpired,
                ShooterGatewayRoomRestoreStatus.InvalidSession => RoomGatewaySessionRestoreStatus.InvalidSession,
                ShooterGatewayRoomRestoreStatus.Failed => RoomGatewaySessionRestoreStatus.Failed,
                _ => RoomGatewaySessionRestoreStatus.Restored
            };
        }

        private static ShooterGatewayRoomRestoreErrorCode ToShooterRestoreErrorCode(RoomGatewaySessionRestoreErrorCode errorCode)
        {
            return errorCode switch
            {
                RoomGatewaySessionRestoreErrorCode.NoAccountRoomMapping => ShooterGatewayRoomRestoreErrorCode.NoAccountRoomMapping,
                RoomGatewaySessionRestoreErrorCode.AccountNotInRoom => ShooterGatewayRoomRestoreErrorCode.AccountNotInRoom,
                RoomGatewaySessionRestoreErrorCode.RoomClosed => ShooterGatewayRoomRestoreErrorCode.RoomClosed,
                RoomGatewaySessionRestoreErrorCode.RoomExpired => ShooterGatewayRoomRestoreErrorCode.RoomExpired,
                RoomGatewaySessionRestoreErrorCode.InvalidSession => ShooterGatewayRoomRestoreErrorCode.InvalidSession,
                RoomGatewaySessionRestoreErrorCode.InternalError => ShooterGatewayRoomRestoreErrorCode.InternalError,
                _ => ShooterGatewayRoomRestoreErrorCode.None
            };
        }

        private static RoomGatewaySessionRestoreErrorCode ToRoomRestoreErrorCode(ShooterGatewayRoomRestoreErrorCode errorCode)
        {
            return errorCode switch
            {
                ShooterGatewayRoomRestoreErrorCode.NoAccountRoomMapping => RoomGatewaySessionRestoreErrorCode.NoAccountRoomMapping,
                ShooterGatewayRoomRestoreErrorCode.AccountNotInRoom => RoomGatewaySessionRestoreErrorCode.AccountNotInRoom,
                ShooterGatewayRoomRestoreErrorCode.RoomClosed => RoomGatewaySessionRestoreErrorCode.RoomClosed,
                ShooterGatewayRoomRestoreErrorCode.RoomExpired => RoomGatewaySessionRestoreErrorCode.RoomExpired,
                ShooterGatewayRoomRestoreErrorCode.InvalidSession => RoomGatewaySessionRestoreErrorCode.InvalidSession,
                ShooterGatewayRoomRestoreErrorCode.InternalError => RoomGatewaySessionRestoreErrorCode.InternalError,
                _ => RoomGatewaySessionRestoreErrorCode.None
            };
        }

        private sealed class ShooterRoomGatewaySessionClient : IRoomGatewaySessionClient
        {
            private readonly IShooterRoomGatewayRoomClient _roomClient;

            public ShooterRoomGatewaySessionClient(IShooterRoomGatewayRoomClient roomClient)
            {
                _roomClient = roomClient ?? throw new ArgumentNullException(nameof(roomClient));
            }

            public async Task<RoomGatewayCreateResult> CreateRoomAsync(RoomGatewayCreateRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.CreateRoomAsync(
                    new ShooterGatewayCreateRoomRequest(request.SessionToken, request.Region, request.ServerId, request.RoomType, request.Title, request.IsPublic, request.MaxPlayers, request.Tags),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayCreateResult(result.Success, result.RoomId, result.NumericRoomId, result.Message);
            }

            public async Task<RoomGatewayJoinResult> JoinRoomAsync(RoomGatewayJoinRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.JoinRoomAsync(
                    new ShooterGatewayJoinRoomRequest(request.SessionToken, request.Region, request.ServerId, request.RoomId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayJoinResult(
                    result.Success,
                    result.RoomId,
                    result.NumericRoomId,
                    ToRoomAnchor(in result.WorldStartAnchor),
                    result.Message,
                    result.BattleId,
                    result.CanStart,
                    ToRoomEntryKind(result.JoinKind),
                    result.ServerNowTicks,
                    result.WorldId);
            }

            public async Task<RoomGatewayReadyResult> SetReadyAsync(RoomGatewayReadyRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.SetReadyAsync(
                    new ShooterGatewayReadyRequest(request.SessionToken, request.RoomId, request.Ready),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayReadyResult(result.Success, result.BattleId, result.CanStart, result.Message);
            }

            public async Task<RoomGatewayStartBattleResult> StartBattleAsync(RoomGatewayStartBattleRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.StartBattleAsync(
                    new ShooterGatewayStartBattleRequest(request.SessionToken, request.RoomId, request.GameplayId, request.RuleSetId, request.ConfigVersion, request.ProtocolVersion, request.WorldType, request.ClientId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayStartBattleResult(result.Success, result.BattleId, result.WorldId, result.Started, ToRoomAnchor(in result.WorldStartAnchor), result.ServerNowTicks, result.Message);
            }

            public async Task<RoomGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(RoomGatewayStateSyncSubscriptionRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.SubscribeStateSyncAsync(
                    new ShooterGatewayStateSyncSubscriptionRequest(request.SessionToken, request.BattleId, request.RoomId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayStateSyncSubscriptionResult(result.Success, result.Message);
            }

            public async Task<RoomGatewayRestoreRoomResult> RestoreRoomAsync(RoomGatewayRestoreRoomRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            {
                var result = await _roomClient.RestoreRoomAsync(
                    new ShooterGatewayRestoreRoomRequest(request.SessionToken, request.Region, request.ServerId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                return new RoomGatewayRestoreRoomResult(
                    result.Success,
                    result.HasActiveRoom,
                    result.IsInBattle,
                    result.RoomId,
                    result.NumericRoomId,
                    ToRoomAnchor(in result.WorldStartAnchor),
                    result.Message,
                    result.BattleId,
                    result.CanStart,
                    ToRoomEntryKind(result.JoinKind),
                    result.ServerNowTicks,
                    result.WorldId,
                    ToRoomRestoreStatus(result.Status),
                    ToRoomRestoreErrorCode(result.ErrorCode));
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
        public readonly ShooterRemoteTimeAnchorProjection RemoteTimeAnchorProjection;
        public readonly ShooterRoomGatewayEntryKind EntryKind;
        public readonly bool CanStart;
        public readonly bool Started;
        public readonly bool Subscribed;
        public readonly string Message;
        public readonly ShooterGatewayRoomRestoreStatus RestoreStatus;
        public readonly ShooterGatewayRoomRestoreErrorCode RestoreErrorCode;

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
            : this(sessionToken, roomId, numericRoomId, battleId, worldId, playerId, in worldStartAnchor, serverNowTicks, entryKind, canStart, started, subscribed, message, ShooterGatewayRoomRestoreStatus.Restored, ShooterGatewayRoomRestoreErrorCode.None)
        {
        }

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
            string message,
            ShooterGatewayRoomRestoreStatus restoreStatus,
            ShooterGatewayRoomRestoreErrorCode restoreErrorCode)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            PlayerId = playerId;
            WorldStartAnchor = worldStartAnchor;
            ServerNowTicks = serverNowTicks;
            RemoteTimeAnchorProjection = ShooterTimeAnchorCoordinator.ProjectRemote(in worldStartAnchor, serverNowTicks);
            TargetFrame = RemoteTimeAnchorProjection.TargetFrame;
            CatchUpFrames = RemoteTimeAnchorProjection.CatchUpFrames;
            EntryKind = entryKind;
            CanStart = canStart;
            Started = started;
            Subscribed = subscribed;
            Message = message ?? string.Empty;
            RestoreStatus = restoreStatus;
            RestoreErrorCode = restoreErrorCode;
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
                Message,
                RestoreStatus,
                RestoreErrorCode);
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
            : this(roomId, numericRoomId, battleId, worldId, playerId, targetFrame, catchUpFrames, entryKind, canStart, started, subscribed, message, ShooterGatewayRoomRestoreStatus.Restored, ShooterGatewayRoomRestoreErrorCode.None)
        {
        }

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
            string message,
            ShooterGatewayRoomRestoreStatus restoreStatus,
            ShooterGatewayRoomRestoreErrorCode restoreErrorCode)
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
            RestoreStatus = restoreStatus;
            RestoreErrorCode = restoreErrorCode;
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

        public ShooterGatewayRoomRestoreStatus RestoreStatus { get; }

        public ShooterGatewayRoomRestoreErrorCode RestoreErrorCode { get; }

        public bool IsRunningEntry => EntryKind == ShooterRoomGatewayEntryKind.Reconnect || EntryKind == ShooterRoomGatewayEntryKind.LateJoin;

        public bool IsClosed => !string.IsNullOrWhiteSpace(RoomId)
            && !string.IsNullOrWhiteSpace(BattleId)
            && WorldId != 0UL
            && PlayerId != 0U
            && Started
            && Subscribed;
    }
}
