using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.Host.Extensions.Moba.Snapshot;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using PlayerId = AbilityKit.Ability.Host.PlayerId;

namespace AbilityKit.Orleans.Grains.Battle.Protocol;

/// <summary>
/// Converts Orleans battle contracts to runtime protocol models, and runtime snapshots back to Orleans contracts.
/// Keep this boundary outside grains so rooms, gateways, and gameplay-specific hosts can swap mapping rules later.
/// </summary>
public interface IOrleansBattleProtocolMapper
{
    MobaGameStartSpec CreateGameStartSpec(string battleId, int tickRate, BattleInitParams initParams);

    IReadOnlyList<PlayerInputCommand> CreatePlayerInputCommands(int frame, IReadOnlyList<BattleInputItem>? inputs);

    StateSyncPush CreateStateSyncPush(ulong worldId, int frame, WorldStateSnapshot? snapshot, IReadOnlyList<LogicWorldEntityState>? readModelStates, bool isFullSnapshot);

    BattleSnapshot CreateBattleSnapshot(int frame, WorldStateSnapshot snapshot, IReadOnlyList<LogicWorldEntityState>? readModelStates);
}

/// <summary>
/// Default MOBA mapping profile used by the sample server. Future gameplay modes can replace this class
/// without changing the battle host grain lifecycle.
/// </summary>
public sealed class DefaultOrleansBattleProtocolMapper : IOrleansBattleProtocolMapper
{
    public static readonly DefaultOrleansBattleProtocolMapper Instance = new();

    private DefaultOrleansBattleProtocolMapper()
    {
    }

    private readonly MobaRuntimeSnapshotMapperRegistry<List<ActorSnapshot>> _actorSnapshotMappers =
        MobaRuntimeSnapshotMapperRegistryBuilder.FromMappers<List<ActorSnapshot>>(new ActorTransformSnapshotMapper());

    public MobaGameStartSpec CreateGameStartSpec(string battleId, int tickRate, BattleInitParams initParams)
    {
        if (initParams == null)
        {
            throw new ArgumentNullException(nameof(initParams));
        }

        var players = initParams.Players;
        var loadouts = players == null || players.Count == 0
            ? Array.Empty<MobaPlayerLoadout>()
            : new MobaPlayerLoadout[players.Count];

        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                loadouts[i] = new MobaPlayerLoadout(
                    playerId: new PlayerId(player.PlayerId.ToString()),
                    teamId: player.TeamId,
                    heroId: player.HeroId,
                    attributeTemplateId: 0,
                    level: 1,
                    basicAttackSkillId: 0,
                    skillIds: Array.Empty<int>(),
                    spawnIndex: i,
                    unitSubType: 1,
                    mainType: 1,
                    hasSpawnPosition: 1,
                    spawnX: player.PosX,
                    spawnY: player.PosY,
                    spawnZ: player.PosZ);
            }
        }

        var localPlayerId = loadouts.Length > 0 ? loadouts[0].PlayerId : new PlayerId(initParams.WorldId.ToString());
        var worldType = string.IsNullOrWhiteSpace(initParams.WorldType) ? "battle" : initParams.WorldType;
        var clientId = string.IsNullOrWhiteSpace(initParams.ClientId) ? "orleans_logic_host" : initParams.ClientId;
        var profile = MobaBattleLaunchProfile.Create(
            clientId: clientId,
            launchMode: MobaBattleLaunchMode.RoomFlow,
            syncMode: MobaBattleLaunchSyncMode.StateSync,
            authorityMode: MobaBattleLaunchAuthorityMode.ServerAuthority,
            worldType: worldType,
            tickRate: tickRate,
            inputDelayFrames: initParams.InputDelayFrames,
            ruleSetId: initParams.RuleSetId,
            configVersion: initParams.ConfigVersion,
            protocolVersion: initParams.ProtocolVersion);

        var launchSpec = MobaBattleLaunchSpecBuilder.FromLoadouts(
            battleId: battleId,
            localPlayerId: localPlayerId,
            mapId: initParams.MapId > 0 ? initParams.MapId : 1,
            players: loadouts,
            profile: in profile,
            matchId: battleId,
            worldId: battleId,
            gameplayId: initParams.GameplayId,
            randomSeed: initParams.RandomSeed);

        return launchSpec.ToGameStartSpec();
    }

    public IReadOnlyList<PlayerInputCommand> CreatePlayerInputCommands(int frame, IReadOnlyList<BattleInputItem>? inputs)
    {
        if (inputs == null || inputs.Count == 0)
        {
            return Array.Empty<PlayerInputCommand>();
        }

        var frameIndex = new FrameIndex(frame);
        var commands = new List<PlayerInputCommand>(inputs.Count);
        for (int i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (input == null)
            {
                continue;
            }

            commands.Add(new PlayerInputCommand(
                frameIndex,
                new PlayerId(input.PlayerId.ToString()),
                input.OpCode,
                input.Payload ?? Array.Empty<byte>()));
        }

        return commands;
    }

    public StateSyncPush CreateStateSyncPush(
        ulong worldId,
        int frame,
        WorldStateSnapshot? snapshot,
        IReadOnlyList<LogicWorldEntityState>? readModelStates,
        bool isFullSnapshot)
    {
        return new StateSyncPush
        {
            WorldId = worldId,
            Frame = frame,
            Timestamp = DateTime.UtcNow.Ticks,
            Actors = CreateActorSnapshots(frame, snapshot, readModelStates),
            IsFullSnapshot = isFullSnapshot
        };
    }

    public BattleSnapshot CreateBattleSnapshot(int frame, WorldStateSnapshot snapshot, IReadOnlyList<LogicWorldEntityState>? readModelStates)
    {
        return new BattleSnapshot
        {
            Frame = frame,
            Actors = CreateActorSnapshots(frame, snapshot, readModelStates)
        };
    }

    private List<ActorSnapshot> CreateActorSnapshots(int frame, WorldStateSnapshot? snapshot, IReadOnlyList<LogicWorldEntityState>? readModelStates)
    {
        if (snapshot.HasValue)
        {
            var runtimeSnapshot = snapshot.Value;
            var context = new MobaRuntimeSnapshotContext(frame, DateTime.UtcNow.Ticks);
            if (_actorSnapshotMappers.TryMap(in runtimeSnapshot, in context, out var actors))
            {
                return actors;
            }
        }

        return ConvertReadModelActors(readModelStates);
    }

    private static List<ActorSnapshot> ConvertReadModelActors(IReadOnlyList<LogicWorldEntityState>? states)
    {
        var actors = new List<ActorSnapshot>(states?.Count ?? 0);
        if (states == null)
        {
            return actors;
        }

        for (int i = 0; i < states.Count; i++)
        {
            var state = states[i];
            actors.Add(new ActorSnapshot
            {
                ActorId = state.EntityId,
                X = state.X,
                Y = state.Y,
                Z = state.Z,
                Rotation = state.Rotation,
                VelocityX = state.VelocityX,
                VelocityZ = state.VelocityZ,
                Hp = state.Hp,
                HpMax = state.HpMax,
                TeamId = state.TeamId
            });
        }

        return actors;
    }

    private sealed class ActorTransformSnapshotMapper : IMobaRuntimeSnapshotMapper<List<ActorSnapshot>>
    {
        public int OpCode => MobaOpCodes.Snapshot.ActorTransform;

        public bool TryMap(in WorldStateSnapshot snapshot, in MobaRuntimeSnapshotContext context, out List<ActorSnapshot> output)
        {
            var actors = new List<ActorSnapshot>();
            var entries = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                actors.Add(new ActorSnapshot
                {
                    ActorId = entry.ActorId,
                    X = entry.X,
                    Y = entry.Y,
                    Z = entry.Z
                });
            }

            output = actors;
            return actors.Count > 0;
        }
    }
}
