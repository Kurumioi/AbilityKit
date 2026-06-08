using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;

namespace AbilityKit.Orleans.Grains.Rooms.Gameplay;

internal sealed class ShooterRoomGameplayAdapter : IRoomGameplayAdapter
{
    public string RoomType => ShooterGameplay.RoomType;

    public object CreateState(RoomSummary summary)
    {
        return new ShooterRoomState(summary.MaxPlayers > 0 ? summary.MaxPlayers : ShooterGameplay.DefaultMaxPlayers);
    }

    public void Join(object state, RoomSummary summary, HashSet<string> members, string accountId)
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

    public void PickHero(object state, RoomPickHeroRequest request)
    {
        // Shooter keeps room loadout intentionally tiny; hero selection is ignored by this gameplay.
    }

    public bool CanStart(object state)
    {
        return RequireState(state).CanStart();
    }

    public List<RoomPlayerSnapshot> BuildPlayerSnapshots(object state)
    {
        var roomState = RequireState(state);
        var players = new List<RoomPlayerSnapshot>(roomState.Players.Count);
        var index = 0;
        foreach (var kv in roomState.Players)
        {
            index++;
            players.Add(new RoomPlayerSnapshot(
                kv.Key,
                TeamId: 1,
                Ready: kv.Value.Ready,
                HeroId: index,
                SpawnPointId: index,
                Level: 1,
                AttributeTemplateId: 0,
                BasicAttackSkillId: 0,
                SkillIds: null));
        }

        return players;
    }

    public BattleInitParams BuildBattleInitParams(object state, RoomSummary summary, StartRoomBattleRequest request)
    {
        var roomState = RequireState(state);
        var players = new List<PlayerInitInfo>(roomState.Players.Count);
        var index = 0;
        foreach (var kv in roomState.Players)
        {
            index++;
            players.Add(new PlayerInitInfo
            {
                PlayerId = (uint)index,
                ActorId = index,
                HeroId = index,
                PosX = (index - 1) * 2f,
                PosY = 0f,
                PosZ = 0f,
                TeamId = 1
            });
        }

        return new BattleInitParams
        {
            WorldId = CreateNumericWorldId(summary.RoomId),
            TickRate = ReadIntTag(summary, "tickRate", ShooterGameplay.DefaultTickRate),
            MapId = ReadIntTag(summary, "mapId", 1),
            RandomSeed = ReadIntTag(summary, "randomSeed", Environment.TickCount),
            InputDelayFrames = 0,
            Players = players,
            GameplayId = request.GameplayId > 0 ? request.GameplayId : ShooterGameplay.GameplayId,
            RuleSetId = request.RuleSetId,
            ConfigVersion = request.ConfigVersion,
            ProtocolVersion = request.ProtocolVersion,
            WorldType = string.IsNullOrWhiteSpace(request.WorldType) ? ShooterGameplay.WorldType : request.WorldType,
            ClientId = request.ClientId,
            RoomType = summary.RoomType
        };
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

        public void Join(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return;
            if (Players.ContainsKey(accountId)) return;
            if (Players.Count >= MaxPlayers) throw new InvalidOperationException("Shooter room is full.");

            Players[accountId] = new ShooterRoomPlayer();
        }

        public void Leave(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return;
            Players.Remove(accountId);
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
    }

    private sealed class ShooterRoomPlayer
    {
        public bool Ready;
    }
}
