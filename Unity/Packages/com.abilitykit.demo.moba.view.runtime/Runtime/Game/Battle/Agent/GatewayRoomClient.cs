using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;
using AbilityKit.Protocol.Moba.GatewayTimeSync;
using AbilityKit.Protocol.Moba.Room;

namespace AbilityKit.Game.Battle.Agent
{
    public sealed class GatewayRoomClient
    {
        private readonly IConnection _connection;
        private readonly RequestClient _request;
        private readonly GatewayRoomOpCodes _opCodes;

        public GatewayRoomClient(IConnection connection, GatewayRoomOpCodes opCodes)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _opCodes = opCodes;
            _request = new RequestClient(connection);
        }

        public Task<ArraySegment<byte>> SendRawRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return _request.SendRequestAsync(opCode, payload, timeout, cancellationToken);
        }

        public async Task<GatewayTimeSyncResult> TimeSyncAsync(uint timeSyncOpCode, long clientSendTicks, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var req = new WireTimeSyncReq(clientSendTicks);
            var payload = WireTimeSyncBinary.Serialize(in req);
            var resp = await _request.SendRequestAsync(timeSyncOpCode, payload, timeout, cancellationToken);
            var wire = WireTimeSyncBinary.DeserializeTimeSyncRes(resp);
            return new GatewayTimeSyncResult(wire.ClientSendTicks, wire.ServerNowTicks, wire.ServerTickFrequency);
        }

        public async Task<string> GuestLoginAsync(uint guestLoginOpCode, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var req = new WireRoomGuestLoginReq
            {
                GuestId = Guid.NewGuid().ToString("N")
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var resp = await _request.SendRequestAsync(guestLoginOpCode, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireRoomGuestLoginRes>(resp);
            return wire.Success ? wire.SessionToken ?? string.Empty : string.Empty;
        }

        public async Task<GatewayCreateRoomResult> CreateRoomAsync(
            string sessionToken,
            string region,
            string serverId,
            string roomType,
            string title,
            bool isPublic,
            int maxPlayers,
            IReadOnlyDictionary<string, string> tags,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("region is required.", nameof(region));
            if (string.IsNullOrWhiteSpace(serverId)) throw new ArgumentException("serverId is required.", nameof(serverId));
            if (string.IsNullOrWhiteSpace(roomType)) roomType = "battle";
            if (title == null) title = string.Empty;

            var req = new WireCreateRoomReq
            {
                SessionToken = sessionToken,
                Region = region,
                ServerId = serverId,
                RoomType = roomType,
                Title = title,
                IsPublic = isPublic,
                MaxPlayers = maxPlayers,
                Tags = ToDictionary(tags)
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _request.SendRequestAsync(_opCodes.CreateRoom, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireCreateRoomRes>(respPayload);
            return new GatewayCreateRoomResult(wire.RoomId ?? string.Empty, wire.NumericRoomId);
        }

        public async Task<GatewayJoinRoomResult> JoinRoomAsync(
            string sessionToken,
            string region,
            string serverId,
            string roomId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("region is required.", nameof(region));
            if (string.IsNullOrWhiteSpace(serverId)) throw new ArgumentException("serverId is required.", nameof(serverId));
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required.", nameof(roomId));

            var req = new WireJoinRoomReq
            {
                SessionToken = sessionToken,
                Region = region,
                ServerId = serverId,
                RoomId = roomId
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _request.SendRequestAsync(_opCodes.JoinRoom, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireJoinRoomRes>(respPayload);
            var anchor = ToGatewayAnchor(wire.WorldStartAnchor);
            return new GatewayJoinRoomResult(wire.NumericRoomId, string.Empty, in anchor);
        }

        public async Task<GatewayRoomSnapshotResult> SetReadyAsync(
            string sessionToken,
            string roomId,
            bool ready,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required.", nameof(roomId));

            var req = new WireRoomReadyReq
            {
                SessionToken = sessionToken,
                RoomId = roomId,
                Ready = ready
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _request.SendRequestAsync(_opCodes.SetReady, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireRoomSnapshotRes>(respPayload);
            return new GatewayRoomSnapshotResult(wire.RoomId ?? string.Empty, wire.NumericRoomId);
        }

        public async Task<GatewayRoomSnapshotResult> PickHeroAsync(
            string sessionToken,
            string roomId,
            int heroId,
            int teamId,
            int spawnPointId,
            int level,
            int attributeTemplateId,
            int basicAttackSkillId,
            IReadOnlyList<int> skillIds,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required.", nameof(roomId));

            var req = new WireRoomPickHeroReq
            {
                SessionToken = sessionToken,
                RoomId = roomId,
                HeroId = heroId,
                TeamId = teamId,
                SpawnPointId = spawnPointId,
                Level = level,
                AttributeTemplateId = attributeTemplateId,
                BasicAttackSkillId = basicAttackSkillId,
                SkillIds = ToList(skillIds)
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _request.SendRequestAsync(_opCodes.PickHero, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireRoomSnapshotRes>(respPayload);
            return new GatewayRoomSnapshotResult(wire.RoomId ?? string.Empty, wire.NumericRoomId);
        }

        public async Task<GatewayStartBattleResult> StartBattleAsync(
            string sessionToken,
            string roomId,
            int gameplayId,
            int ruleSetId,
            int configVersion,
            int protocolVersion,
            string worldType,
            string clientId,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("sessionToken is required.", nameof(sessionToken));
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required.", nameof(roomId));

            var req = new WireStartRoomBattleReq
            {
                SessionToken = sessionToken,
                RoomId = roomId,
                GameplayId = gameplayId,
                RuleSetId = ruleSetId,
                ConfigVersion = configVersion,
                ProtocolVersion = protocolVersion,
                WorldType = worldType ?? string.Empty,
                ClientId = clientId ?? string.Empty
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var respPayload = await _request.SendRequestAsync(_opCodes.StartBattle, payload, timeout, cancellationToken);
            var wire = WireRoomGatewayBinary.Deserialize<WireStartRoomBattleRes>(respPayload);
            return new GatewayStartBattleResult(wire.BattleId ?? string.Empty, wire.WorldId, wire.Started);
        }

        private static Dictionary<string, string> ToDictionary(IReadOnlyDictionary<string, string> source)
        {
            if (source == null || source.Count == 0)
            {
                return null;
            }

            var result = new Dictionary<string, string>(source.Count);
            foreach (var kv in source)
            {
                result[kv.Key ?? string.Empty] = kv.Value ?? string.Empty;
            }

            return result;
        }

        private static List<int> ToList(IReadOnlyList<int> source)
        {
            if (source == null || source.Count == 0)
            {
                return null;
            }

            var result = new List<int>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                result.Add(source[i]);
            }

            return result;
        }

        private static GatewayWorldStartAnchor ToGatewayAnchor(in WireWorldStartAnchor anchor)
        {
            return new GatewayWorldStartAnchor(
                anchor.StartServerTicks,
                anchor.ServerTickFrequency,
                anchor.StartFrame,
                anchor.FixedDeltaSeconds);
        }
    }
}
