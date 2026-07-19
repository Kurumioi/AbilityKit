using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Persistence;
using AbilityKit.Orleans.Grains.Rooms;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;

namespace AbilityKit.Orleans.Grains.Gameplays.Moba.Rooms;

internal sealed class MobaRoomGameplayAdapter : IRoomGameplayAdapter
{
    public const string DefaultRoomType = GameplayRoomTypes.Moba;

    private static readonly DefaultMobaRoomGameStartSpecBuilder StartSpecBuilder = new();

    private const string PersistentFormat = "moba.room.v1";

    public string RoomType => DefaultRoomType;

    public object CreateState(RoomSummary summary)
    {
        var roomState = new MobaRoomState(
            summary.RoomId,
            ReadIntTag(summary, "mapId", 1),
            ReadIntTag(summary, "randomSeed", Environment.TickCount),
            ReadIntTag(summary, "tickRate", 30),
            ReadIntTag(summary, "inputDelayFrames", 0));
        roomState.Configure(ReadIntTag(summary, "minPlayers", 1), summary.MaxPlayers);
        return roomState;
    }

    public RoomGameplayPersistentState ExportPersistentState(object state)
    {
        var snapshot = RequireMobaState(state).ExportPersistentState();
        return new RoomGameplayPersistentState(PersistentFormat, 1, JsonSerializer.SerializeToUtf8Bytes(snapshot));
    }

    public object RestorePersistentState(RoomSummary summary, RoomGameplayPersistentState persistentState)
    {
        if (persistentState is null ||
            persistentState.Version != 1 ||
            !string.Equals(persistentState.Format, PersistentFormat, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported MOBA room persistent state format.");
        }

        var snapshot = JsonSerializer.Deserialize<MobaRoomPersistentSnapshot>(persistentState.Payload)
            ?? throw new InvalidOperationException("MOBA room persistent state payload is empty.");
        return MobaRoomState.RestorePersistentState(snapshot);
    }

    public void Join(object state, RoomSummary summary, IReadOnlyCollection<string> members, string accountId)
    {
        var roomState = RequireMobaState(state);
        roomState.TryJoin(new PlayerId(accountId), GuessTeamId(members.Count));
    }

    public void Leave(object state, string accountId)
    {
        var roomState = RequireMobaState(state);
        roomState.TryLeave(new PlayerId(accountId));
    }

    public void SetReady(object state, RoomReadyRequest request)
    {
        var roomState = RequireMobaState(state);
        roomState.TrySetReady(new PlayerId(request.AccountId), request.Ready);
    }

    public void SubmitCommand(object state, RoomGameplayCommandRequest request)
    {
        if (!string.Equals(request.CommandName, RoomGameplayCommandNames.ConfigureMobaLoadout, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported MOBA room gameplay command. CommandName={request.CommandName}");
        }

        var roomState = RequireMobaState(state);
        var fields = request.Fields ?? throw new InvalidOperationException("MOBA loadout command fields are required.");
        var playerId = new PlayerId(request.AccountId);
        roomState.TrySetTeam(playerId, ReadIntField(fields, "teamId", 0));
        roomState.TrySetSpawnPoint(playerId, ReadIntField(fields, "spawnPointId", 0));
        var ok = roomState.TryPickHero(
            playerId,
            ReadIntField(fields, "heroId", 0),
            ReadIntField(fields, "attributeTemplateId", 0),
            ReadIntField(fields, "level", 1),
            ReadIntField(fields, "basicAttackSkillId", 0),
            ReadIntArrayField(fields, "skillIds"));
        if (!ok)
        {
            throw new InvalidOperationException("Invalid MOBA room loadout command. Full player loadout fields are required.");
        }
    }

    public bool CanStart(object state)
    {
        return RequireMobaState(state).CanStart();
    }

    public bool ValidateBeginLoading(object state)
    {
        // MOBA: 复用 CanStart 作为玩法级可加载校验（人数满足 + 完整 loadout）。
        return RequireMobaState(state).CanStart();
    }

    public RoomLaunchManifest BuildLaunchManifest(object state, RoomSummary summary)
    {
        var roomState = RequireMobaState(state);
        var references = new List<string>();

        var mapId = ReadIntTag(summary, "mapId", 1);
        references.Add($"map:{mapId}");

        foreach (var kv in roomState.Players)
        {
            var slot = kv.Value;
            references.Add($"hero:{slot.HeroId}");
            references.Add($"attr:{slot.AttributeTemplateId}");
            references.Add($"basic:{slot.BasicAttackSkillId}");
            if (slot.SkillIds is { Length: > 0 })
            {
                foreach (var skillId in slot.SkillIds)
                {
                    references.Add($"skill:{skillId}");
                }
            }
        }

        var metadata = new Dictionary<string, string>
        {
            ["mapId"] = mapId.ToString(),
            ["players"] = roomState.Players.Count.ToString()
        };

        return RoomLaunchManifestBuilder.Build(RoomLaunchManifestBuilder.CurrentManifestVersion, references, metadata);
    }

    public List<RoomPlayerSnapshot> BuildPlayerSnapshots(object state)
    {
        var roomState = RequireMobaState(state);
        if (roomState.Players.Count == 0)
        {
            return new List<RoomPlayerSnapshot>();
        }

        var players = new List<RoomPlayerSnapshot>(roomState.Players.Count);
        foreach (var kv in roomState.Players)
        {
            var slot = kv.Value;
            players.Add(new RoomPlayerSnapshot(
                kv.Key,
                slot.TeamId,
                slot.Ready,
                slot.HeroId,
                slot.SpawnPointId,
                slot.Level,
                slot.AttributeTemplateId,
                slot.BasicAttackSkillId,
                slot.SkillIds == null ? null : slot.SkillIds.ToList(),
                (uint)(players.Count + 1)));
        }

        return players;
    }

    public PlayerInitInfo? BuildLateJoinPlayer(object state, RoomSummary summary, string accountId)
    {
        return null;
    }

    public BattleInitParams BuildBattleInitParams(object state, RoomSummary summary, StartRoomBattleRequest request)
    {
        var roomState = RequireMobaState(state);
        if (!StartSpecBuilder.TryBuild(roomState, out var roomSpec))
        {
            throw new InvalidOperationException("Room is not ready to start battle.");
        }

        roomSpec = new MobaRoomGameStartSpec(
            roomSpec.MatchId,
            roomSpec.MapId,
            roomSpec.RandomSeed,
            roomSpec.TickRate,
            roomSpec.InputDelayFrames,
            roomSpec.Players,
            request.GameplayId);

        var initParams = OrleansRoomBattleStartMapper.ToBattleInitParams(
            summary.RoomId,
            in roomSpec,
            request.RuleSetId,
            request.ConfigVersion,
            request.ProtocolVersion,
            request.WorldType,
            request.ClientId,
            summary.RoomType);
        initParams.SyncOptions = RoomBattleSyncOptionsMapper.Resolve(summary, request);
        initParams.InputDelayFrames = initParams.SyncOptions.InputDelayFrames;
        return initParams;
    }

    private static MobaRoomState RequireMobaState(object state)
    {
        return state as MobaRoomState
            ?? throw new InvalidOperationException("Room gameplay state is not a MOBA room state.");
    }

    private static int GuessTeamId(int memberCount)
    {
        return memberCount % 2 == 0 ? 2 : 1;
    }

    private static int ReadIntTag(RoomSummary summary, string key, int fallback)
    {
        if (summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static int ReadIntField(IReadOnlyDictionary<string, string> fields, string key, int fallback)
    {
        return fields.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int[]? ReadIntArrayField(IReadOnlyDictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, out var parsed) ? parsed : 0)
            .ToArray();
    }
}
