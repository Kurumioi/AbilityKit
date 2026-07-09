using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Contracts.Shooter;
using AbilityKit.Orleans.Grains.Rooms;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;

namespace AbilityKit.Orleans.Grains.Gameplays.Shooter.Rooms;

internal sealed class ShooterRoomGameplayAdapter : IRoomGameplayAdapter
{
    public string RoomType => ShooterGameplay.RoomType;

    public object CreateState(RoomSummary summary)
    {
        return new ShooterRoomState(summary.MaxPlayers > 0 ? summary.MaxPlayers : ShooterGameplay.DefaultMaxPlayers);
    }

    public void Join(object state, RoomSummary summary, IReadOnlyCollection<string> members, string accountId)
    {
        var roomState = RequireState(state);
        roomState.Join(accountId);
    }

    public void Leave(object state, string accountId)
    {
        RequireState(state).Leave(accountId);
    }

    public void SetReady(object state, RoomReadyRequest request)
    {
        RequireState(state).SetReady(request.AccountId, request.Ready);
    }

    public void SubmitCommand(object state, RoomGameplayCommandRequest request)
    {
        // Shooter 刻意保持房间配置极简；该玩法会忽略房间玩法命令。
    }

    public bool CanStart(object state)
    {
        return RequireState(state).CanStart();
    }

    public List<RoomPlayerSnapshot> BuildPlayerSnapshots(object state)
    {
        var roomState = RequireState(state);
        var slots = BuildOrderedPlayerSlots(roomState);
        var players = new List<RoomPlayerSnapshot>(slots.Count);
        foreach (var kv in slots)
        {
            var slot = kv.Value;
            players.Add(new RoomPlayerSnapshot(
                kv.Key,
                TeamId: 1,
                Ready: slot.Ready,
                HeroId: slot.PlayerId,
                SpawnPointId: slot.PlayerId,
                Level: 1,
                AttributeTemplateId: 0,
                BasicAttackSkillId: 0,
                SkillIds: null,
                PlayerId: (uint)slot.PlayerId));
        }

        return players;
    }

    public PlayerInitInfo? BuildLateJoinPlayer(object state, RoomSummary summary, string accountId)
    {
        var roomState = RequireState(state);
        if (string.IsNullOrWhiteSpace(accountId) || !roomState.Players.TryGetValue(accountId, out var slot))
        {
            return null;
        }

        return CreatePlayerInitInfo(slot.PlayerId, accountId);
    }

    public BattleInitParams BuildBattleInitParams(object state, RoomSummary summary, StartRoomBattleRequest request)
    {
        var roomState = RequireState(state);
        var slots = BuildOrderedPlayerSlots(roomState);
        var players = new List<PlayerInitInfo>(slots.Count);
        foreach (var kv in slots)
        {
            players.Add(CreatePlayerInitInfo(kv.Value.PlayerId, kv.Key));
        }

        var syncOptions = RoomBattleSyncOptionsMapper.Resolve(summary, request);
        return new BattleInitParams
        {
            WorldId = CreateNumericWorldId(summary.RoomId),
            TickRate = ReadIntTag(summary, ShooterRoomTagKeys.TickRate, ShooterGameplay.DefaultTickRate),
            MapId = ReadIntTag(summary, ShooterRoomTagKeys.MapId, 1),
            RandomSeed = ReadIntTag(summary, ShooterRoomTagKeys.RandomSeed, Environment.TickCount),
            InputDelayFrames = syncOptions.InputDelayFrames,
            DurationFrames = ReadIntTag(summary, ShooterRoomTagKeys.DurationFrames, 0),
            Players = players,
            GameplayId = request.GameplayId > 0 ? request.GameplayId : ShooterGameplay.GameplayId,
            RuleSetId = request.RuleSetId,
            ConfigVersion = request.ConfigVersion,
            ProtocolVersion = request.ProtocolVersion,
            WorldType = string.IsNullOrWhiteSpace(request.WorldType) ? ShooterGameplay.WorldType : request.WorldType,
            ClientId = request.ClientId,
            RoomType = summary.RoomType,
            SyncOptions = syncOptions
        };
    }

    private static PlayerInitInfo CreatePlayerInitInfo(int playerId, string accountId)
    {
        return new PlayerInitInfo
        {
            PlayerId = (uint)playerId,
            AccountId = accountId,
            ActorId = playerId,
            HeroId = playerId,
            PosX = (playerId - 1) * 2f,
            PosY = 0f,
            PosZ = 0f,
            TeamId = 1,
            Level = 1,
            AttributeTemplateId = 1,
            BasicAttackSkillId = 1,
            SkillIds = new List<int> { 1 }
        };
    }

    private static List<KeyValuePair<string, ShooterRoomPlayer>> BuildOrderedPlayerSlots(ShooterRoomState roomState)
    {
        var slots = new List<KeyValuePair<string, ShooterRoomPlayer>>(roomState.Players);
        slots.Sort(static (left, right) => left.Value.PlayerId.CompareTo(right.Value.PlayerId));
        return slots;
    }

    private static ShooterRoomState RequireState(object state)
    {
        return state as ShooterRoomState
            ?? throw new InvalidOperationException("Room gameplay state is not a Shooter room state.");
    }

    private static int ReadIntTag(RoomSummary summary, string key, int fallback)
    {
        if (summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static ulong CreateNumericWorldId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;

        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        for (int i = 0; i < value.Length; i++)
        {
            hash ^= value[i];
            hash *= prime;
        }

        return hash == 0 ? 1 : hash;
    }

    private sealed class ShooterRoomState
    {
        public ShooterRoomState(int maxPlayers)
        {
            MaxPlayers = Math.Max(1, maxPlayers);
        }

        public int MaxPlayers { get; }

        public Dictionary<string, ShooterRoomPlayer> Players { get; } = new(StringComparer.Ordinal);

        private SortedSet<int> ReleasedPlayerIds { get; } = new();

        private int NextPlayerId { get; set; } = 1;

        public void Join(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return;
            if (Players.ContainsKey(accountId)) return;
            if (Players.Count >= MaxPlayers) throw new InvalidOperationException("Shooter room is full.");

            Players[accountId] = new ShooterRoomPlayer(AllocatePlayerId());
        }

        public void Leave(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return;
            if (Players.Remove(accountId, out var player))
            {
                ReleasedPlayerIds.Add(player.PlayerId);
            }
        }

        public void SetReady(string accountId, bool ready)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return;
            if (Players.TryGetValue(accountId, out var player))
            {
                player.Ready = ready;
            }
        }

        public bool CanStart()
        {
            if (Players.Count == 0) return false;
            foreach (var kv in Players)
            {
                if (!kv.Value.Ready) return false;
            }

            return true;
        }

        private int AllocatePlayerId()
        {
            if (ReleasedPlayerIds.Count > 0)
            {
                var playerId = ReleasedPlayerIds.Min;
                ReleasedPlayerIds.Remove(playerId);
                return playerId;
            }

            return NextPlayerId++;
        }
    }

    private sealed class ShooterRoomPlayer
    {
        public ShooterRoomPlayer(int playerId)
        {
            PlayerId = playerId;
        }

        public int PlayerId { get; }

        public bool Ready;
    }
}
