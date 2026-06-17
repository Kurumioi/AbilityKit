using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Rooms;

namespace AbilityKit.Orleans.Grains.Rooms.Gameplay;

internal sealed class MobaRoomGameplayAdapter : IRoomGameplayAdapter
{
    public const string DefaultRoomType = "battle";

    private static readonly DefaultMobaRoomGameStartSpecBuilder StartSpecBuilder = new();

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

    public void Join(object state, RoomSummary summary, HashSet<string> members, string accountId)
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

    public void PickHero(object state, RoomPickHeroRequest request)
    {
        var roomState = RequireMobaState(state);
        var playerId = new PlayerId(request.AccountId);
        roomState.TrySetTeam(playerId, request.TeamId);
        roomState.TrySetSpawnPoint(playerId, request.SpawnPointId);
        var ok = roomState.TryPickHero(
            playerId,
            request.HeroId,
            request.AttributeTemplateId,
            request.Level,
            request.BasicAttackSkillId,
            request.SkillIds?.ToArray());
        if (!ok)
        {
            throw new InvalidOperationException("Invalid MOBA room pick hero request. Full player loadout fields are required.");
        }
    }

    public bool CanStart(object state)
    {
        return RequireMobaState(state).CanStart();
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
                slot.SkillIds == null ? null : slot.SkillIds.ToList()));
        }

        return players;
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
}
