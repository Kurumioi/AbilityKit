#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AbilityKit.Ability.Host.Extensions.Session
{
    public interface IRoomGatewaySessionClient
    {
        Task<RoomGatewayCreateResult> CreateRoomAsync(RoomGatewayCreateRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<RoomGatewayJoinResult> JoinRoomAsync(RoomGatewayJoinRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<RoomGatewayReadyResult> SetReadyAsync(RoomGatewayReadyRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<RoomGatewayStartBattleResult> StartBattleAsync(RoomGatewayStartBattleRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<RoomGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(RoomGatewayStateSyncSubscriptionRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<RoomGatewayRestoreRoomResult> RestoreRoomAsync(RoomGatewayRestoreRoomRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }

    public sealed class RoomGatewaySessionFlow
    {
        private readonly IRoomGatewaySessionClient _client;

        public RoomGatewaySessionFlow(IRoomGatewaySessionClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<RoomGatewaySessionFlowResult> CreateReadyStartAndSubscribeAsync(
            string sessionToken,
            RoomGatewayLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateSessionToken(sessionToken);
            ValidatePlayerId(playerId);

            var create = await _client.CreateRoomAsync(
                new RoomGatewayCreateRequest(sessionToken, launchSpec.Region, launchSpec.ServerId, launchSpec.RoomType, launchSpec.RoomTitle, true, launchSpec.MaxPlayers, launchSpec.Tags),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(create.Success, create.Message, "create room");

            var join = await _client.JoinRoomAsync(
                new RoomGatewayJoinRequest(sessionToken, launchSpec.Region, launchSpec.ServerId, create.RoomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(join.Success, join.Message, "join room");

            var ready = await _client.SetReadyAsync(
                new RoomGatewayReadyRequest(sessionToken, create.RoomId, true),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(ready.Success, ready.Message, "set ready");

            var start = await _client.StartBattleAsync(
                new RoomGatewayStartBattleRequest(
                    sessionToken,
                    create.RoomId,
                    launchSpec.GameplayId,
                    launchSpec.RuleSetId,
                    launchSpec.ConfigVersion,
                    launchSpec.ProtocolVersion,
                    launchSpec.WorldType,
                    launchSpec.ClientId,
                    launchSpec.SyncTemplateId,
                    launchSpec.SyncModel,
                    launchSpec.NetworkEnvironmentId,
                    launchSpec.CarrierName,
                    launchSpec.EnableAuthoritativeWorld,
                    launchSpec.InterpolationEnabled,
                    launchSpec.InputDelayFrames),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(start.Success, start.Message, "start battle");

            var battleId = SelectBattleId(start.BattleId, ready.BattleId, join.BattleId);
            var subscribe = await _client.SubscribeStateSyncAsync(
                new RoomGatewayStateSyncSubscriptionRequest(sessionToken, battleId, create.RoomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(subscribe.Success, subscribe.Message, "subscribe state sync");

            return new RoomGatewaySessionFlowResult(
                sessionToken,
                create.RoomId,
                create.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                SelectWorldStartAnchor(start.WorldStartAnchor, join.WorldStartAnchor),
                start.ServerNowTicks,
                RoomGatewaySessionEntryKind.TeamLobby,
                ready.CanStart,
                start.Started,
                subscribe.Success,
                subscribe.Message);
        }

        public async Task<RoomGatewaySessionFlowResult> JoinReadyStartAndSubscribeAsync(
            string sessionToken,
            string roomId,
            RoomGatewayLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateSessionToken(sessionToken);
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required.", nameof(roomId));
            ValidatePlayerId(playerId);

            var join = await _client.JoinRoomAsync(
                new RoomGatewayJoinRequest(sessionToken, launchSpec.Region, launchSpec.ServerId, roomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(join.Success, join.Message, "join room");

            if (join.JoinKind != RoomGatewaySessionEntryKind.TeamLobby && !string.IsNullOrWhiteSpace(join.BattleId))
            {
                var runningSubscribe = await _client.SubscribeStateSyncAsync(
                    new RoomGatewayStateSyncSubscriptionRequest(sessionToken, join.BattleId, roomId),
                    timeout,
                    cancellationToken).ConfigureAwait(false);
                EnsureSuccess(runningSubscribe.Success, runningSubscribe.Message, "subscribe state sync");

                return new RoomGatewaySessionFlowResult(
                    sessionToken,
                    roomId,
                    join.NumericRoomId,
                    join.BattleId,
                    join.WorldId,
                    playerId,
                    join.WorldStartAnchor,
                    join.ServerNowTicks,
                    join.JoinKind,
                    join.CanStart,
                    started: true,
                    runningSubscribe.Success,
                    runningSubscribe.Message);
            }

            var ready = await _client.SetReadyAsync(
                new RoomGatewayReadyRequest(sessionToken, roomId, true),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(ready.Success, ready.Message, "set ready");

            var start = await _client.StartBattleAsync(
                new RoomGatewayStartBattleRequest(
                    sessionToken,
                    roomId,
                    launchSpec.GameplayId,
                    launchSpec.RuleSetId,
                    launchSpec.ConfigVersion,
                    launchSpec.ProtocolVersion,
                    launchSpec.WorldType,
                    launchSpec.ClientId,
                    launchSpec.SyncTemplateId,
                    launchSpec.SyncModel,
                    launchSpec.NetworkEnvironmentId,
                    launchSpec.CarrierName,
                    launchSpec.EnableAuthoritativeWorld,
                    launchSpec.InterpolationEnabled,
                    launchSpec.InputDelayFrames),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(start.Success, start.Message, "start battle");

            var battleId = SelectBattleId(start.BattleId, ready.BattleId, join.BattleId);
            var subscribe = await _client.SubscribeStateSyncAsync(
                new RoomGatewayStateSyncSubscriptionRequest(sessionToken, battleId, roomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(subscribe.Success, subscribe.Message, "subscribe state sync");

            return new RoomGatewaySessionFlowResult(
                sessionToken,
                roomId,
                join.NumericRoomId,
                battleId,
                start.WorldId,
                playerId,
                SelectWorldStartAnchor(start.WorldStartAnchor, join.WorldStartAnchor),
                start.ServerNowTicks,
                RoomGatewaySessionEntryKind.TeamLobby,
                ready.CanStart,
                start.Started,
                subscribe.Success,
                subscribe.Message);
        }

        public async Task<RoomGatewaySessionFlowResult> RestoreRoomAsync(
            string sessionToken,
            string region,
            string serverId,
            RoomGatewayLaunchSpec launchSpec,
            uint playerId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateSessionToken(sessionToken);
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("region is required.", nameof(region));
            if (string.IsNullOrWhiteSpace(serverId)) throw new ArgumentException("serverId is required.", nameof(serverId));
            ValidatePlayerId(playerId);

            var restored = await _client.RestoreRoomAsync(
                new RoomGatewayRestoreRoomRequest(sessionToken, region, serverId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(restored.Success, restored.Message, "restore room");

            if (!restored.HasActiveRoom || string.IsNullOrWhiteSpace(restored.RoomId))
            {
                throw new InvalidOperationException("restore room did not find an active room.");
            }

            if (!restored.IsInBattle || string.IsNullOrWhiteSpace(restored.BattleId))
            {
                throw new InvalidOperationException("restore room did not find a running battle.");
            }

            var subscribe = await _client.SubscribeStateSyncAsync(
                new RoomGatewayStateSyncSubscriptionRequest(sessionToken, restored.BattleId, restored.RoomId),
                timeout,
                cancellationToken).ConfigureAwait(false);
            EnsureSuccess(subscribe.Success, subscribe.Message, "subscribe state sync");

            return new RoomGatewaySessionFlowResult(
                sessionToken,
                restored.RoomId,
                restored.NumericRoomId,
                restored.BattleId,
                restored.WorldId,
                playerId,
                restored.WorldStartAnchor,
                restored.ServerNowTicks,
                restored.JoinKind,
                restored.CanStart,
                started: true,
                subscribe.Success,
                subscribe.Message,
                restored.Status,
                restored.ErrorCode);
        }

        private static RoomGatewayWorldStartAnchor SelectWorldStartAnchor(RoomGatewayWorldStartAnchor startAnchor, RoomGatewayWorldStartAnchor joinAnchor)
        {
            return startAnchor.IsValid ? startAnchor : joinAnchor;
        }

        private static string SelectBattleId(string startBattleId, string readyBattleId, string joinBattleId)
        {
            var battleId = string.IsNullOrWhiteSpace(startBattleId) ? readyBattleId : startBattleId;
            if (string.IsNullOrWhiteSpace(battleId)) battleId = joinBattleId;
            if (string.IsNullOrWhiteSpace(battleId)) throw new InvalidOperationException("start battle did not return a battle id.");
            return battleId;
        }

        private static void ValidateSessionToken(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
        }

        private static void ValidatePlayerId(uint playerId)
        {
            if (playerId == 0) throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        private static void EnsureSuccess(bool success, string message, string operation)
        {
            if (!success) throw new InvalidOperationException($"Room gateway {operation} failed: {message}");
        }
    }

    public enum RoomGatewaySessionEntryKind
    {
        TeamLobby = 0,
        Reconnect = 1,
        LateJoin = 2
    }

    public enum RoomGatewaySessionRestoreStatus
    {
        Restored = 0,
        NoActiveRoom = 1,
        NotMember = 2,
        RoomClosed = 3,
        RoomExpired = 4,
        InvalidSession = 5,
        Failed = 6
    }

    public enum RoomGatewaySessionRestoreErrorCode
    {
        None = 0,
        NoAccountRoomMapping = 1,
        AccountNotInRoom = 2,
        RoomClosed = 3,
        RoomExpired = 4,
        InvalidSession = 5,
        InternalError = 6
    }

    public readonly struct RoomGatewayWorldStartAnchor
    {
        public readonly long StartServerTicks;
        public readonly long ServerTickFrequency;
        public readonly int StartFrame;
        public readonly double FixedDeltaSeconds;

        public RoomGatewayWorldStartAnchor(long startServerTicks, long serverTickFrequency, int startFrame, double fixedDeltaSeconds)
        {
            StartServerTicks = startServerTicks;
            ServerTickFrequency = serverTickFrequency;
            StartFrame = startFrame;
            FixedDeltaSeconds = fixedDeltaSeconds;
        }

        public bool IsValid => ServerTickFrequency > 0 && FixedDeltaSeconds > 0d;
    }

    public readonly struct RoomGatewayLaunchSpec
    {
        public readonly string Region;
        public readonly string ServerId;
        public readonly string RoomType;
        public readonly string RoomTitle;
        public readonly int MaxPlayers;
        public readonly int GameplayId;
        public readonly int RuleSetId;
        public readonly int ConfigVersion;
        public readonly int ProtocolVersion;
        public readonly string WorldType;
        public readonly string ClientId;
        public readonly IReadOnlyDictionary<string, string>? Tags;
        public readonly string SyncTemplateId;
        public readonly int SyncModel;
        public readonly string NetworkEnvironmentId;
        public readonly string CarrierName;
        public readonly bool EnableAuthoritativeWorld;
        public readonly bool InterpolationEnabled;
        public readonly int InputDelayFrames;

        public RoomGatewayLaunchSpec(
            string region,
            string serverId,
            string roomType,
            string roomTitle,
            int maxPlayers,
            int gameplayId,
            int ruleSetId,
            int configVersion,
            int protocolVersion,
            string worldType,
            string clientId,
            IReadOnlyDictionary<string, string>? tags = null,
            string syncTemplateId = "",
            int syncModel = 0,
            string networkEnvironmentId = "",
            string carrierName = "",
            bool enableAuthoritativeWorld = true,
            bool interpolationEnabled = false,
            int inputDelayFrames = 0)
        {
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
            RoomType = roomType ?? string.Empty;
            RoomTitle = roomTitle ?? string.Empty;
            MaxPlayers = maxPlayers;
            GameplayId = gameplayId;
            RuleSetId = ruleSetId;
            ConfigVersion = configVersion;
            ProtocolVersion = protocolVersion;
            WorldType = worldType ?? string.Empty;
            ClientId = clientId ?? string.Empty;
            Tags = tags;
            SyncTemplateId = syncTemplateId ?? string.Empty;
            SyncModel = syncModel;
            NetworkEnvironmentId = networkEnvironmentId ?? string.Empty;
            CarrierName = carrierName ?? string.Empty;
            EnableAuthoritativeWorld = enableAuthoritativeWorld;
            InterpolationEnabled = interpolationEnabled;
            InputDelayFrames = inputDelayFrames < 0 ? 0 : inputDelayFrames;
        }
    }

    public readonly struct RoomGatewayCreateRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;
        public readonly string RoomType;
        public readonly string Title;
        public readonly bool IsPublic;
        public readonly int MaxPlayers;
        public readonly IReadOnlyDictionary<string, string>? Tags;

        public RoomGatewayCreateRequest(string sessionToken, string region, string serverId, string roomType, string title, bool isPublic, int maxPlayers, IReadOnlyDictionary<string, string>? tags = null)
        {
            SessionToken = sessionToken ?? string.Empty;
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
            RoomType = roomType ?? string.Empty;
            Title = title ?? string.Empty;
            IsPublic = isPublic;
            MaxPlayers = maxPlayers;
            Tags = tags;
        }
    }

    public readonly struct RoomGatewayJoinRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;
        public readonly string RoomId;

        public RoomGatewayJoinRequest(string sessionToken, string region, string serverId, string roomId)
        {
            SessionToken = sessionToken ?? string.Empty;
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayReadyRequest
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly bool Ready;

        public RoomGatewayReadyRequest(string sessionToken, string roomId, bool ready)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            Ready = ready;
        }
    }

    public readonly struct RoomGatewayStartBattleRequest
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly int GameplayId;
        public readonly int RuleSetId;
        public readonly int ConfigVersion;
        public readonly int ProtocolVersion;
        public readonly string WorldType;
        public readonly string ClientId;
        public readonly string SyncTemplateId;
        public readonly int SyncModel;
        public readonly string NetworkEnvironmentId;
        public readonly string CarrierName;
        public readonly bool EnableAuthoritativeWorld;
        public readonly bool InterpolationEnabled;
        public readonly int InputDelayFrames;

        public RoomGatewayStartBattleRequest(
            string sessionToken,
            string roomId,
            int gameplayId,
            int ruleSetId,
            int configVersion,
            int protocolVersion,
            string worldType,
            string clientId,
            string syncTemplateId = "",
            int syncModel = 0,
            string networkEnvironmentId = "",
            string carrierName = "",
            bool enableAuthoritativeWorld = true,
            bool interpolationEnabled = false,
            int inputDelayFrames = 0)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            GameplayId = gameplayId;
            RuleSetId = ruleSetId;
            ConfigVersion = configVersion;
            ProtocolVersion = protocolVersion;
            WorldType = worldType ?? string.Empty;
            ClientId = clientId ?? string.Empty;
            SyncTemplateId = syncTemplateId ?? string.Empty;
            SyncModel = syncModel;
            NetworkEnvironmentId = networkEnvironmentId ?? string.Empty;
            CarrierName = carrierName ?? string.Empty;
            EnableAuthoritativeWorld = enableAuthoritativeWorld;
            InterpolationEnabled = interpolationEnabled;
            InputDelayFrames = inputDelayFrames < 0 ? 0 : inputDelayFrames;
        }
    }

    public readonly struct RoomGatewayStateSyncSubscriptionRequest
    {
        public readonly string SessionToken;
        public readonly string BattleId;
        public readonly string RoomId;

        public RoomGatewayStateSyncSubscriptionRequest(string sessionToken, string battleId, string roomId)
        {
            SessionToken = sessionToken ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayRestoreRoomRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;

        public RoomGatewayRestoreRoomRequest(string sessionToken, string region, string serverId)
        {
            SessionToken = sessionToken ?? string.Empty;
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayCreateResult
    {
        public readonly bool Success;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly string Message;

        public RoomGatewayCreateResult(bool success, string roomId, ulong numericRoomId, string message)
        {
            Success = success;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayJoinResult
    {
        public readonly bool Success;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly RoomGatewayWorldStartAnchor WorldStartAnchor;
        public readonly string Message;
        public readonly string BattleId;
        public readonly bool CanStart;
        public readonly RoomGatewaySessionEntryKind JoinKind;
        public readonly long ServerNowTicks;
        public readonly ulong WorldId;

        public RoomGatewayJoinResult(bool success, string roomId, ulong numericRoomId, RoomGatewayWorldStartAnchor worldStartAnchor, string message, string battleId, bool canStart, RoomGatewaySessionEntryKind joinKind, long serverNowTicks, ulong worldId)
        {
            Success = success;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            WorldStartAnchor = worldStartAnchor;
            Message = message ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            CanStart = canStart;
            JoinKind = joinKind;
            ServerNowTicks = serverNowTicks;
            WorldId = worldId;
        }
    }

    public readonly struct RoomGatewayReadyResult
    {
        public readonly bool Success;
        public readonly string BattleId;
        public readonly bool CanStart;
        public readonly string Message;

        public RoomGatewayReadyResult(bool success, string battleId, bool canStart, string message)
        {
            Success = success;
            BattleId = battleId ?? string.Empty;
            CanStart = canStart;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayStartBattleResult
    {
        public readonly bool Success;
        public readonly string BattleId;
        public readonly ulong WorldId;
        public readonly bool Started;
        public readonly RoomGatewayWorldStartAnchor WorldStartAnchor;
        public readonly long ServerNowTicks;
        public readonly string Message;

        public RoomGatewayStartBattleResult(bool success, string battleId, ulong worldId, bool started, RoomGatewayWorldStartAnchor worldStartAnchor, long serverNowTicks, string message)
        {
            Success = success;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            Started = started;
            WorldStartAnchor = worldStartAnchor;
            ServerNowTicks = serverNowTicks;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayStateSyncSubscriptionResult
    {
        public readonly bool Success;
        public readonly string Message;

        public RoomGatewayStateSyncSubscriptionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct RoomGatewayRestoreRoomResult
    {
        public readonly bool Success;
        public readonly bool HasActiveRoom;
        public readonly bool IsInBattle;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly RoomGatewayWorldStartAnchor WorldStartAnchor;
        public readonly string Message;
        public readonly string BattleId;
        public readonly bool CanStart;
        public readonly RoomGatewaySessionEntryKind JoinKind;
        public readonly long ServerNowTicks;
        public readonly ulong WorldId;
        public readonly RoomGatewaySessionRestoreStatus Status;
        public readonly RoomGatewaySessionRestoreErrorCode ErrorCode;

        public RoomGatewayRestoreRoomResult(bool success, bool hasActiveRoom, bool isInBattle, string roomId, ulong numericRoomId, RoomGatewayWorldStartAnchor worldStartAnchor, string message, string battleId, bool canStart, RoomGatewaySessionEntryKind joinKind, long serverNowTicks, ulong worldId, RoomGatewaySessionRestoreStatus status, RoomGatewaySessionRestoreErrorCode errorCode)
        {
            Success = success;
            HasActiveRoom = hasActiveRoom;
            IsInBattle = isInBattle;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            WorldStartAnchor = worldStartAnchor;
            Message = message ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            CanStart = canStart;
            JoinKind = joinKind;
            ServerNowTicks = serverNowTicks;
            WorldId = worldId;
            Status = status;
            ErrorCode = errorCode;
        }
    }

    public readonly struct RoomGatewaySessionFlowResult
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly string BattleId;
        public readonly ulong WorldId;
        public readonly uint PlayerId;
        public readonly RoomGatewayWorldStartAnchor WorldStartAnchor;
        public readonly long ServerNowTicks;
        public readonly RoomGatewaySessionEntryKind EntryKind;
        public readonly bool CanStart;
        public readonly bool Started;
        public readonly bool Subscribed;
        public readonly string Message;
        public readonly RoomGatewaySessionRestoreStatus RestoreStatus;
        public readonly RoomGatewaySessionRestoreErrorCode RestoreErrorCode;

        public RoomGatewaySessionFlowResult(string sessionToken, string roomId, ulong numericRoomId, string battleId, ulong worldId, uint playerId, RoomGatewayWorldStartAnchor worldStartAnchor, long serverNowTicks, RoomGatewaySessionEntryKind entryKind, bool canStart, bool started, bool subscribed, string message)
            : this(sessionToken, roomId, numericRoomId, battleId, worldId, playerId, worldStartAnchor, serverNowTicks, entryKind, canStart, started, subscribed, message, RoomGatewaySessionRestoreStatus.Restored, RoomGatewaySessionRestoreErrorCode.None)
        {
        }

        public RoomGatewaySessionFlowResult(string sessionToken, string roomId, ulong numericRoomId, string battleId, ulong worldId, uint playerId, RoomGatewayWorldStartAnchor worldStartAnchor, long serverNowTicks, RoomGatewaySessionEntryKind entryKind, bool canStart, bool started, bool subscribed, string message, RoomGatewaySessionRestoreStatus restoreStatus, RoomGatewaySessionRestoreErrorCode restoreErrorCode)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            PlayerId = playerId;
            WorldStartAnchor = worldStartAnchor;
            ServerNowTicks = serverNowTicks;
            EntryKind = entryKind;
            CanStart = canStart;
            Started = started;
            Subscribed = subscribed;
            Message = message ?? string.Empty;
            RestoreStatus = restoreStatus;
            RestoreErrorCode = restoreErrorCode;
        }
    }
}
