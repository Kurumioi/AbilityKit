using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;
using ET.AbilityKit.Demo.ET.Share;
using ActorKind = ET.AbilityKit.Demo.ET.Share.ActorKind;

namespace ET.Logic
{
    /// <summary>
    /// Coordinates moba.core game start, ET presentation unit creation, and enter-game snapshot dispatch.
    /// </summary>
    public static class ETBattleEnterGameCoordinator
    {
        public static void Trigger(ETMobaBattleDriver driver)
        {
            try
            {
                var scene = driver.Scene();
                if (scene == null)
                {
                    Log.Error("[ETMobaBattleDriver] Scene is null, cannot spawn entities");
                    return;
                }

                var unitComponent = scene.GetComponent<ETUnitComponent>();
                if (unitComponent == null)
                {
                    Log.Error("[ETMobaBattleDriver] ETUnitComponent not found");
                    return;
                }

                if (!driver.TryResolve<MobaEnterGameFlowService>(out var enterGameService) || enterGameService == null)
                {
                    Log.Error("[ETMobaBattleDriver] MobaEnterGameFlowService not found");
                    return;
                }

                if (!driver.TryResolve<global::Entitas.IContexts>(out var contexts) || contexts == null)
                {
                    Log.Error("[ETMobaBattleDriver] Entitas IContexts not found");
                    return;
                }

                var actorContext = ((global::Contexts)contexts).actor;
                var spec = CreateGameStartSpec(driver.PlayerSpawnData);
                var spawns = new List<ActorSpawnData>();
                var localPlayerId = driver.PlayerSpawnData.Count > 0 ? new PlayerId(driver.PlayerSpawnData[0].PlayerId) : default;
                var localActorId = 0;

                enterGameService.ApplyGameStartSpec(actorContext, in spec);
                SetGamePhaseInGame(driver);

                if (driver.TryResolve<MobaActorRegistry>(out var registry) && registry != null &&
                    driver.TryResolve<MobaPlayerActorMapService>(out var playerActorMap) && playerActorMap != null &&
                    playerActorMap.TryGetActorId(localPlayerId, out var actorId))
                {
                    localActorId = actorId;
                }

                CreatePresentationUnits(driver, unitComponent, spawns);
                DispatchEnterGameSnapshot(driver, spawns, localActorId);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] TriggerEnterGameSnapshot failed: {ex}");
            }
        }

        private static MobaGameStartSpec CreateGameStartSpec(IReadOnlyList<ETPlayerSpawnData> playerSpawnData)
        {
            var loadouts = new MobaPlayerLoadout[playerSpawnData.Count];
            for (int i = 0; i < playerSpawnData.Count; i++)
            {
                var spawnData = playerSpawnData[i];
                var playerId = new PlayerId(spawnData.PlayerId);

                loadouts[i] = new MobaPlayerLoadout(
                    playerId: playerId,
                    teamId: spawnData.TeamId,
                    heroId: spawnData.CharacterId,
                    attributeTemplateId: 0,
                    level: 1,
                    basicAttackSkillId: 0,
                    skillIds: null,
                    spawnIndex: i,
                    unitSubType: (int)UnitSubType.Hero,
                    mainType: (int)EntityMainType.Unit,
                    hasSpawnPosition: 1,
                    spawnX: spawnData.PositionX,
                    spawnY: 0f,
                    spawnZ: spawnData.PositionZ);
            }

            var localPlayerId = playerSpawnData.Count > 0 ? new PlayerId(playerSpawnData[0].PlayerId) : default;
            var enterReq = new EnterMobaGameReq(
                playerId: localPlayerId,
                matchId: $"et_demo_{Environment.TickCount}",
                mapId: 1,
                randomSeed: Environment.TickCount,
                tickRate: 30,
                inputDelayFrames: 0,
                players: loadouts);

            return new MobaGameStartSpec(in enterReq);
        }

        private static void SetGamePhaseInGame(ETMobaBattleDriver driver)
        {
            if (driver.TryResolve<MobaGamePhaseService>(out var phaseService) && phaseService != null)
            {
                phaseService.SetInGame();
            }
        }

        private static void CreatePresentationUnits(
            ETMobaBattleDriver driver,
            ETUnitComponent unitComponent,
            List<ActorSpawnData> spawns)
        {
            foreach (var spawnData in driver.PlayerSpawnData)
            {
                var playerId = new PlayerId(spawnData.PlayerId);
                int actorId = 0;

                if (driver.TryResolve<MobaPlayerActorMapService>(out var map) && map != null)
                {
                    map.TryGetActorId(playerId, out actorId);
                }

                if (actorId == 0)
                {
                    Log.Warning($"[ETMobaBattleDriver] No ActorId for PlayerId={spawnData.PlayerId}");
                    continue;
                }

                var unit = unitComponent.CreateUnit(
                    actorId: actorId,
                    entityCode: spawnData.CharacterId,
                    kind: spawnData.CharacterId == 1 ? ActorKind.Hero : ActorKind.Monster,
                    name: spawnData.CharacterName,
                    x: spawnData.PositionX,
                    y: spawnData.PositionZ,
                    maxHp: spawnData.MaxHp);

                if (unit != null)
                {
                    spawns.Add(new ActorSpawnData(
                        actorId: actorId,
                        entityCode: spawnData.CharacterId,
                        characterId: spawnData.CharacterId,
                        name: spawnData.CharacterName,
                        x: spawnData.PositionX,
                        y: spawnData.PositionZ,
                        z: 0f,
                        rotationY: 0f,
                        scale: 1f,
                        teamId: spawnData.TeamId,
                        maxHp: spawnData.MaxHp,
                        hp: spawnData.Hp,
                        playerId: spawnData.PlayerId));
                }
            }
        }

        private static void DispatchEnterGameSnapshot(
            ETMobaBattleDriver driver,
            IReadOnlyList<ActorSpawnData> spawns,
            int localActorId)
        {
            if (spawns.Count == 0 || driver.ViewSink == null)
            {
                return;
            }

            var playerIds = new List<int>(driver.PlayerSpawnData.Count);
            var teamDict = new Dictionary<int, List<int>>();

            foreach (var spawn in spawns)
            {
                playerIds.Add(spawn.ActorId);

                if (!teamDict.TryGetValue(spawn.TeamId, out var list))
                {
                    list = new List<int>();
                    teamDict[spawn.TeamId] = list;
                }

                list.Add(spawn.ActorId);
            }

            var teams = new List<TeamData>();
            foreach (var kv in teamDict)
            {
                teams.Add(new TeamData(kv.Key, kv.Value));
            }

            var enterGameData = new EnterGameData(
                mapId: 1,
                localPlayerId: localActorId,
                playerIds: playerIds,
                teams: teams);

            var enterGameSnapshot = new FrameSnapshotData(
                frameIndex: 0,
                timestamp: 0,
                type: SnapshotType.Full,
                enterGame: enterGameData,
                actorSpawns: spawns);

            driver.ViewSink.OnEnterGameSnapshot(in enterGameSnapshot);
        }
    }
}
