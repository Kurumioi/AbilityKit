#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using System.Threading.Tasks;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterRoomGatewayRoomClient
    {
        Task<ShooterGatewayCreateRoomResult> CreateRoomAsync(
            ShooterGatewayCreateRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayJoinRoomResult> JoinRoomAsync(
            ShooterGatewayJoinRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayRoomSnapshotResult> SetReadyAsync(
            ShooterGatewayReadyRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayStartBattleResult> StartBattleAsync(
            ShooterGatewayStartBattleRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(
            ShooterGatewayStateSyncSubscriptionRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayFullStateSyncRequestResult> RequestFullStateSyncAsync(
            ShooterGatewayFullStateSyncRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<ShooterGatewayRestoreRoomResult> RestoreRoomAsync(
            ShooterGatewayRestoreRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);
    }

    public sealed class ShooterRoomGatewayRoomClient : IShooterRoomGatewayRoomClient
    {
        private readonly IShooterRoomGatewayRequestTransport _transport;
        private readonly ShooterRoomGatewayRoomOpCodes _opCodes;

        public ShooterRoomGatewayRoomClient(IShooterRoomGatewayRequestTransport transport)
            : this(transport, ShooterRoomGatewayRoomOpCodes.Default)
        {
        }

        public ShooterRoomGatewayRoomClient(IShooterRoomGatewayRequestTransport transport, ShooterRoomGatewayRoomOpCodes opCodes)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _opCodes = opCodes;
        }

        public async Task<ShooterGatewayCreateRoomResult> CreateRoomAsync(
            ShooterGatewayCreateRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateRoom(in request);

            var req = new WireCreateRoomReq
            {
                SessionToken = request.SessionToken,
                Region = request.Region,
                ServerId = request.ServerId,
                RoomType = request.RoomType,
                Title = request.Title,
                IsPublic = request.IsPublic,
                MaxPlayers = request.MaxPlayers,
                Tags = ToDictionary(request.Tags)
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.CreateRoom, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireCreateRoomRes>(respPayload);
            return new ShooterGatewayCreateRoomResult(wire.Success, wire.RoomId ?? string.Empty, wire.NumericRoomId, wire.Message ?? string.Empty);
        }

        public async Task<ShooterGatewayJoinRoomResult> JoinRoomAsync(
            ShooterGatewayJoinRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateJoinRoom(in request);

            var req = new WireJoinRoomReq
            {
                SessionToken = request.SessionToken,
                Region = request.Region,
                ServerId = request.ServerId,
                RoomId = request.RoomId
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.JoinRoom, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireJoinRoomRes>(respPayload);
            var worldStartAnchor = wire.WorldStartAnchor;
            var anchor = ToAnchor(in worldStartAnchor);
            return new ShooterGatewayJoinRoomResult(
                wire.Success,
                wire.RoomId ?? string.Empty,
                wire.NumericRoomId,
                in anchor,
                wire.Message ?? string.Empty,
                wire.Snapshot.BattleId ?? string.Empty,
                wire.Snapshot.CanStart,
                ToJoinKind(wire.JoinKind),
                wire.ServerNowTicks,
                wire.Snapshot.WorldId);
        }

        public async Task<ShooterGatewayRoomSnapshotResult> SetReadyAsync(
            ShooterGatewayReadyRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateReady(in request);

            var req = new WireRoomReadyReq
            {
                SessionToken = request.SessionToken,
                RoomId = request.RoomId,
                Ready = request.Ready
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.SetReady, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireRoomSnapshotRes>(respPayload);
            return new ShooterGatewayRoomSnapshotResult(wire.Success, wire.RoomId ?? string.Empty, wire.NumericRoomId, wire.Message ?? string.Empty, wire.Snapshot.BattleId ?? string.Empty, wire.Snapshot.CanStart);
        }

        public async Task<ShooterGatewayStartBattleResult> StartBattleAsync(
            ShooterGatewayStartBattleRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateStartBattle(in request);

            var req = new WireStartRoomBattleReq
            {
                SessionToken = request.SessionToken,
                RoomId = request.RoomId,
                GameplayId = request.GameplayId,
                RuleSetId = request.RuleSetId,
                ConfigVersion = request.ConfigVersion,
                ProtocolVersion = request.ProtocolVersion,
                WorldType = request.WorldType,
                ClientId = request.ClientId
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.StartBattle, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireStartRoomBattleRes>(respPayload);
            var worldStartAnchor = wire.WorldStartAnchor;
            return new ShooterGatewayStartBattleResult(
                wire.Success,
                wire.BattleId ?? string.Empty,
                wire.WorldId,
                wire.Started,
                ToAnchor(in worldStartAnchor),
                wire.ServerNowTicks,
                wire.Message ?? string.Empty);
        }

        public async Task<ShooterGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(
            ShooterGatewayStateSyncSubscriptionRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateStateSyncSubscription(in request);

            var req = new WireSubscribeStateSyncReq
            {
                SessionToken = request.SessionToken,
                BattleId = request.BattleId,
                RoomId = request.RoomId
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.SubscribeStateSync, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireSubscribeStateSyncRes>(respPayload);
            return new ShooterGatewayStateSyncSubscriptionResult(wire.Success, wire.Message ?? string.Empty);
        }

        public async Task<ShooterGatewayFullStateSyncRequestResult> RequestFullStateSyncAsync(
            ShooterGatewayFullStateSyncRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateFullStateSyncRequest(in request);

            var req = new WireRequestFullStateSyncReq
            {
                SessionToken = request.SessionToken,
                BattleId = request.BattleId,
                RoomId = request.RoomId,
                WorldId = request.WorldId,
                ClientFrame = request.ClientFrame,
                LastAuthoritativeFrame = request.LastAuthoritativeFrame,
                ClientStateHash = request.ClientStateHash,
                AuthoritativeStateHash = request.AuthoritativeStateHash,
                Reason = request.Reason
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.RequestFullStateSync, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireRequestFullStateSyncRes>(respPayload);
            return new ShooterGatewayFullStateSyncRequestResult(wire.Success, wire.Accepted, wire.Message ?? string.Empty, wire.ServerTicks);
        }

        public async Task<ShooterGatewayRestoreRoomResult> RestoreRoomAsync(
            ShooterGatewayRestoreRoomRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ValidateRestoreRoom(in request);

            var req = new WireRestoreRoomReq
            {
                SessionToken = request.SessionToken,
                Region = request.Region,
                ServerId = request.ServerId
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _transport.SendRequestAsync(_opCodes.RestoreRoom, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireRestoreRoomRes>(respPayload);
            var worldStartAnchor = wire.WorldStartAnchor;
            var anchor = ToAnchor(in worldStartAnchor);
            return new ShooterGatewayRestoreRoomResult(
                wire.Success,
                wire.HasActiveRoom,
                wire.IsInBattle,
                wire.RoomId ?? string.Empty,
                wire.NumericRoomId,
                in anchor,
                wire.Message ?? string.Empty,
                wire.Snapshot.BattleId ?? string.Empty,
                wire.Snapshot.CanStart,
                ToJoinKind(wire.JoinKind),
                wire.ServerNowTicks,
                wire.Snapshot.WorldId,
                ToRestoreStatus(wire.Status),
                ToRestoreErrorCode(wire.ErrorCode));
        }

        private static void ValidateCreateRoom(in ShooterGatewayCreateRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Region)) throw new ArgumentException("region is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.ServerId)) throw new ArgumentException("serverId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomType)) throw new ArgumentException("roomType is required.", nameof(request));
            if (request.MaxPlayers <= 0) throw new ArgumentOutOfRangeException(nameof(request));
        }

        private static void ValidateJoinRoom(in ShooterGatewayJoinRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Region)) throw new ArgumentException("region is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.ServerId)) throw new ArgumentException("serverId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomId)) throw new ArgumentException("roomId is required.", nameof(request));
        }

        private static void ValidateReady(in ShooterGatewayReadyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomId)) throw new ArgumentException("roomId is required.", nameof(request));
        }

        private static void ValidateStartBattle(in ShooterGatewayStartBattleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomId)) throw new ArgumentException("roomId is required.", nameof(request));
            if (request.GameplayId <= 0) throw new ArgumentOutOfRangeException(nameof(request));
            if (request.ProtocolVersion <= 0) throw new ArgumentOutOfRangeException(nameof(request));
        }

        private static void ValidateStateSyncSubscription(in ShooterGatewayStateSyncSubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.BattleId)) throw new ArgumentException("battleId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomId)) throw new ArgumentException("roomId is required.", nameof(request));
        }

        private static void ValidateFullStateSyncRequest(in ShooterGatewayFullStateSyncRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.BattleId)) throw new ArgumentException("battleId is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.RoomId)) throw new ArgumentException("roomId is required.", nameof(request));
        }

        private static void ValidateRestoreRoom(in ShooterGatewayRestoreRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Region)) throw new ArgumentException("region is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.ServerId)) throw new ArgumentException("serverId is required.", nameof(request));
        }

        private static Dictionary<string, string>? ToDictionary(IReadOnlyDictionary<string, string>? source)
        {
            if (source == null || source.Count == 0)
            {
                return null;
            }

            var result = new Dictionary<string, string>(source.Count);
            foreach (var kv in source)
            {
                result[kv.Key] = kv.Value ?? string.Empty;
            }

            return result;
        }

        private static ShooterGatewayRoomJoinKind ToJoinKind(WireRoomJoinKind joinKind)
        {
            return joinKind switch
            {
                WireRoomJoinKind.Reconnect => ShooterGatewayRoomJoinKind.Reconnect,
                WireRoomJoinKind.LateJoin => ShooterGatewayRoomJoinKind.LateJoin,
                _ => ShooterGatewayRoomJoinKind.TeamLobby
            };
        }

        private static ShooterGatewayRoomRestoreStatus ToRestoreStatus(WireRoomRestoreStatus status)
        {
            switch (status)
            {
                case WireRoomRestoreStatus.NoActiveRoom:
                    return ShooterGatewayRoomRestoreStatus.NoActiveRoom;
                case WireRoomRestoreStatus.NotMember:
                    return ShooterGatewayRoomRestoreStatus.NotMember;
                case WireRoomRestoreStatus.RoomClosed:
                    return ShooterGatewayRoomRestoreStatus.RoomClosed;
                case WireRoomRestoreStatus.RoomExpired:
                    return ShooterGatewayRoomRestoreStatus.RoomExpired;
                case WireRoomRestoreStatus.InvalidSession:
                    return ShooterGatewayRoomRestoreStatus.InvalidSession;
                case WireRoomRestoreStatus.Failed:
                    return ShooterGatewayRoomRestoreStatus.Failed;
                default:
                    return ShooterGatewayRoomRestoreStatus.Restored;
            }
        }

        private static ShooterGatewayRoomRestoreErrorCode ToRestoreErrorCode(WireRoomRestoreErrorCode errorCode)
        {
            switch (errorCode)
            {
                case WireRoomRestoreErrorCode.NoAccountRoomMapping:
                    return ShooterGatewayRoomRestoreErrorCode.NoAccountRoomMapping;
                case WireRoomRestoreErrorCode.AccountNotInRoom:
                    return ShooterGatewayRoomRestoreErrorCode.AccountNotInRoom;
                case WireRoomRestoreErrorCode.RoomClosed:
                    return ShooterGatewayRoomRestoreErrorCode.RoomClosed;
                case WireRoomRestoreErrorCode.RoomExpired:
                    return ShooterGatewayRoomRestoreErrorCode.RoomExpired;
                case WireRoomRestoreErrorCode.InvalidSession:
                    return ShooterGatewayRoomRestoreErrorCode.InvalidSession;
                case WireRoomRestoreErrorCode.InternalError:
                    return ShooterGatewayRoomRestoreErrorCode.InternalError;
                default:
                    return ShooterGatewayRoomRestoreErrorCode.None;
            }
        }

        private static ShooterGatewayWorldStartAnchor ToAnchor(in WireWorldStartAnchor anchor)
        {
            return new ShooterGatewayWorldStartAnchor(anchor.StartServerTicks, anchor.ServerTickFrequency, anchor.StartFrame, anchor.FixedDeltaSeconds);
        }
    }

    public readonly struct ShooterRoomGatewayRoomOpCodes
    {
        public static ShooterRoomGatewayRoomOpCodes Default => new ShooterRoomGatewayRoomOpCodes(
            RoomGatewayOpCodes.CreateRoom,
            RoomGatewayOpCodes.JoinRoom,
            RoomGatewayOpCodes.SubscribeStateSync,
            RoomGatewayOpCodes.SetReady,
            RoomGatewayOpCodes.StartBattle,
            RoomGatewayOpCodes.RequestFullStateSync,
            RoomGatewayOpCodes.RestoreRoom);

        public readonly uint CreateRoom;
        public readonly uint JoinRoom;
        public readonly uint SubscribeStateSync;
        public readonly uint SetReady;
        public readonly uint StartBattle;
        public readonly uint RequestFullStateSync;
        public readonly uint RestoreRoom;

        public ShooterRoomGatewayRoomOpCodes(uint createRoom, uint joinRoom, uint subscribeStateSync, uint setReady, uint startBattle)
            : this(createRoom, joinRoom, subscribeStateSync, setReady, startBattle, RoomGatewayOpCodes.RequestFullStateSync)
        {
        }

        public ShooterRoomGatewayRoomOpCodes(uint createRoom, uint joinRoom, uint subscribeStateSync, uint setReady, uint startBattle, uint requestFullStateSync)
            : this(createRoom, joinRoom, subscribeStateSync, setReady, startBattle, requestFullStateSync, RoomGatewayOpCodes.RestoreRoom)
        {
        }

        public ShooterRoomGatewayRoomOpCodes(uint createRoom, uint joinRoom, uint subscribeStateSync, uint setReady, uint startBattle, uint requestFullStateSync, uint restoreRoom)
        {
            CreateRoom = createRoom;
            JoinRoom = joinRoom;
            SubscribeStateSync = subscribeStateSync;
            SetReady = setReady;
            StartBattle = startBattle;
            RequestFullStateSync = requestFullStateSync;
            RestoreRoom = restoreRoom;
        }
    }

    public readonly struct ShooterGatewayCreateRoomRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;
        public readonly string RoomType;
        public readonly string Title;
        public readonly bool IsPublic;
        public readonly int MaxPlayers;
        public readonly IReadOnlyDictionary<string, string>? Tags;

        public ShooterGatewayCreateRoomRequest(string sessionToken, string region, string serverId, string roomType, string title, bool isPublic, int maxPlayers, IReadOnlyDictionary<string, string>? tags = null)
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

    public readonly struct ShooterGatewayJoinRoomRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;
        public readonly string RoomId;

        public ShooterGatewayJoinRoomRequest(string sessionToken, string region, string serverId, string roomId)
        {
            SessionToken = sessionToken ?? string.Empty;
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayRestoreRoomRequest
    {
        public readonly string SessionToken;
        public readonly string Region;
        public readonly string ServerId;

        public ShooterGatewayRestoreRoomRequest(string sessionToken, string region, string serverId)
        {
            SessionToken = sessionToken ?? string.Empty;
            Region = region ?? string.Empty;
            ServerId = serverId ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayReadyRequest
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly bool Ready;

        public ShooterGatewayReadyRequest(string sessionToken, string roomId, bool ready)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            Ready = ready;
        }
    }

    public readonly struct ShooterGatewayStartBattleRequest
    {
        public readonly string SessionToken;
        public readonly string RoomId;
        public readonly int GameplayId;
        public readonly int RuleSetId;
        public readonly int ConfigVersion;
        public readonly int ProtocolVersion;
        public readonly string WorldType;
        public readonly string ClientId;

        public ShooterGatewayStartBattleRequest(string sessionToken, string roomId, int gameplayId, int ruleSetId, int configVersion, int protocolVersion, string worldType, string clientId)
        {
            SessionToken = sessionToken ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            GameplayId = gameplayId;
            RuleSetId = ruleSetId;
            ConfigVersion = configVersion;
            ProtocolVersion = protocolVersion;
            WorldType = worldType ?? string.Empty;
            ClientId = clientId ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayStateSyncSubscriptionRequest
    {
        public readonly string SessionToken;
        public readonly string BattleId;
        public readonly string RoomId;

        public ShooterGatewayStateSyncSubscriptionRequest(string sessionToken, string battleId, string roomId)
        {
            SessionToken = sessionToken ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayFullStateSyncRequest
    {
        public readonly string SessionToken;
        public readonly string BattleId;
        public readonly string RoomId;
        public readonly ulong WorldId;
        public readonly int ClientFrame;
        public readonly int LastAuthoritativeFrame;
        public readonly uint ClientStateHash;
        public readonly uint AuthoritativeStateHash;
        public readonly string Reason;

        public ShooterGatewayFullStateSyncRequest(
            string sessionToken,
            string battleId,
            string roomId,
            ulong worldId,
            int clientFrame,
            int lastAuthoritativeFrame,
            uint clientStateHash,
            uint authoritativeStateHash,
            string reason)
        {
            SessionToken = sessionToken ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            WorldId = worldId;
            ClientFrame = clientFrame;
            LastAuthoritativeFrame = lastAuthoritativeFrame;
            ClientStateHash = clientStateHash;
            AuthoritativeStateHash = authoritativeStateHash;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayCreateRoomResult
    {
        public readonly bool Success;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly string Message;

        public ShooterGatewayCreateRoomResult(bool success, string roomId, ulong numericRoomId, string message)
        {
            Success = success;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            Message = message ?? string.Empty;
        }
    }

    public enum ShooterGatewayRoomJoinKind
    {
        TeamLobby = 0,
        Reconnect = 1,
        LateJoin = 2
    }

    public enum ShooterGatewayRoomRestoreStatus
    {
        Restored = 0,
        NoActiveRoom = 1,
        NotMember = 2,
        RoomClosed = 3,
        RoomExpired = 4,
        InvalidSession = 5,
        Failed = 100
    }

    public enum ShooterGatewayRoomRestoreErrorCode
    {
        None = 0,
        NoAccountRoomMapping = 1,
        AccountNotInRoom = 2,
        RoomClosed = 3,
        RoomExpired = 4,
        InvalidSession = 5,
        InternalError = 100
    }

    public readonly struct ShooterGatewayJoinRoomResult
    {
        public readonly bool Success;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly ShooterGatewayWorldStartAnchor WorldStartAnchor;
        public readonly string Message;
        public readonly string BattleId;
        public readonly bool CanStart;
        public readonly ShooterGatewayRoomJoinKind JoinKind;
        public readonly long ServerNowTicks;
        public readonly ulong WorldId;

        public ShooterGatewayJoinRoomResult(bool success, string roomId, ulong numericRoomId, in ShooterGatewayWorldStartAnchor worldStartAnchor, string message, string battleId, bool canStart)
            : this(success, roomId, numericRoomId, in worldStartAnchor, message, battleId, canStart, ShooterGatewayRoomJoinKind.TeamLobby, 0L, 0ul)
        {
        }

        public ShooterGatewayJoinRoomResult(bool success, string roomId, ulong numericRoomId, in ShooterGatewayWorldStartAnchor worldStartAnchor, string message, string battleId, bool canStart, ShooterGatewayRoomJoinKind joinKind, long serverNowTicks, ulong worldId)
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

    public readonly struct ShooterGatewayRestoreRoomResult
    {
        public readonly bool Success;
        public readonly bool HasActiveRoom;
        public readonly bool IsInBattle;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly ShooterGatewayWorldStartAnchor WorldStartAnchor;
        public readonly string Message;
        public readonly string BattleId;
        public readonly bool CanStart;
        public readonly ShooterGatewayRoomJoinKind JoinKind;
        public readonly long ServerNowTicks;
        public readonly ulong WorldId;
        public readonly ShooterGatewayRoomRestoreStatus Status;
        public readonly ShooterGatewayRoomRestoreErrorCode ErrorCode;

        public ShooterGatewayRestoreRoomResult(
            bool success,
            bool hasActiveRoom,
            bool isInBattle,
            string roomId,
            ulong numericRoomId,
            in ShooterGatewayWorldStartAnchor worldStartAnchor,
            string message,
            string battleId,
            bool canStart,
            ShooterGatewayRoomJoinKind joinKind,
            long serverNowTicks,
            ulong worldId)
            : this(success, hasActiveRoom, isInBattle, roomId, numericRoomId, in worldStartAnchor, message, battleId, canStart, joinKind, serverNowTicks, worldId, ShooterGatewayRoomRestoreStatus.Restored, ShooterGatewayRoomRestoreErrorCode.None)
        {
        }

        public ShooterGatewayRestoreRoomResult(
            bool success,
            bool hasActiveRoom,
            bool isInBattle,
            string roomId,
            ulong numericRoomId,
            in ShooterGatewayWorldStartAnchor worldStartAnchor,
            string message,
            string battleId,
            bool canStart,
            ShooterGatewayRoomJoinKind joinKind,
            long serverNowTicks,
            ulong worldId,
            ShooterGatewayRoomRestoreStatus status,
            ShooterGatewayRoomRestoreErrorCode errorCode)
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

    public readonly struct ShooterGatewayRoomSnapshotResult
    {
        public readonly bool Success;
        public readonly string RoomId;
        public readonly ulong NumericRoomId;
        public readonly string Message;
        public readonly string BattleId;
        public readonly bool CanStart;

        public ShooterGatewayRoomSnapshotResult(bool success, string roomId, ulong numericRoomId, string message, string battleId, bool canStart)
        {
            Success = success;
            RoomId = roomId ?? string.Empty;
            NumericRoomId = numericRoomId;
            Message = message ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            CanStart = canStart;
        }
    }

    public readonly struct ShooterGatewayStartBattleResult
    {
        public readonly bool Success;
        public readonly string BattleId;
        public readonly ulong WorldId;
        public readonly bool Started;
        public readonly ShooterGatewayWorldStartAnchor WorldStartAnchor;
        public readonly long ServerNowTicks;
        public readonly string Message;

        public ShooterGatewayStartBattleResult(bool success, string battleId, ulong worldId, bool started, string message)
            : this(success, battleId, worldId, started, default, 0L, message)
        {
        }

        public ShooterGatewayStartBattleResult(bool success, string battleId, ulong worldId, bool started, in ShooterGatewayWorldStartAnchor worldStartAnchor, long serverNowTicks, string message)
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

    public readonly struct ShooterGatewayStateSyncSubscriptionResult
    {
        public readonly bool Success;
        public readonly string Message;

        public ShooterGatewayStateSyncSubscriptionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct ShooterGatewayFullStateSyncRequestResult
    {
        public static readonly ShooterGatewayFullStateSyncRequestResult NotRequested = new ShooterGatewayFullStateSyncRequestResult(false, false, "not requested", 0L);

        public readonly bool Success;
        public readonly bool Accepted;
        public readonly string Message;
        public readonly long ServerTicks;

        public ShooterGatewayFullStateSyncRequestResult(bool success, bool accepted, string message, long serverTicks)
        {
            Success = success;
            Accepted = accepted;
            Message = message ?? string.Empty;
            ServerTicks = serverTicks;
        }
    }

    public readonly struct ShooterGatewayWorldStartAnchor
    {
        public readonly long StartServerTicks;
        public readonly long ServerTickFrequency;
        public readonly int StartFrame;
        public readonly double FixedDeltaSeconds;

        public ShooterGatewayWorldStartAnchor(long startServerTicks, long serverTickFrequency, int startFrame, double fixedDeltaSeconds)
        {
            StartServerTicks = startServerTicks;
            ServerTickFrequency = serverTickFrequency;
            StartFrame = startFrame;
            FixedDeltaSeconds = fixedDeltaSeconds;
        }

        public bool IsValid => StartServerTicks > 0L && ServerTickFrequency > 0L && FixedDeltaSeconds > 0d;

        public WorldStartFrameAnchor ToFrameStartAnchor()
        {
            return new WorldStartFrameAnchor(StartServerTicks, ServerTickFrequency, StartFrame, FixedDeltaSeconds);
        }

        public int CalculateTargetFrame(long serverNowTicks)
        {
            return WorldStartFrameCatchUpCalculator.Calculate(ToFrameStartAnchor(), serverNowTicks).TargetFrame;
        }
    }
}
