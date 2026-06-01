using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Coordinator;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Generic;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Ability.Share.Impl.Moba.CreateWorld;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services
{
    public interface ILogicWorldSpawnService : IService
    {
        bool CreateLogicWorldSpawns(LogicWorldSpawnData[] spawns);
    }

    /// <summary>
    /// Logic-world spawn service implementation.
    /// Coordinator hosts can still reach it through ISpawnService, while server/headless hosts
    /// can use ILogicWorldSpawnService without depending on coordinator DTOs.
    /// </summary>
    [WorldService(typeof(MobaSpawnService))]
    [WorldService(typeof(ILogicWorldSpawnService))]
    [WorldService(typeof(ISpawnService))]
    public sealed class MobaSpawnService : ILogicWorldSpawnService, ISpawnService
    {
        [WorldInject] private MobaEnterGameFlowService _enterGameFlow;
        [WorldInject] private MobaActorRegistry _registry;
        [WorldInject] private MobaEntityManager _entities;
        [WorldInject] private MobaActorSpawnSnapshotService _spawnSnapshot;
        [WorldInject] private global::Entitas.IContexts _contexts;
        private readonly PlayerId _defaultPlayerId = new PlayerId("default");

        public bool CreateSpawns(PlayerSpawnData[] spawns)
        {
            return CreateLogicWorldSpawns(ToLogicWorldSpawns(spawns));
        }

        public bool CreateLogicWorldSpawns(LogicWorldSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                Log.Warning("[MobaSpawnService] No spawns to create");
                return false;
            }

            Log.Info($"[MobaSpawnService] Creating {spawns.Length} player spawns");

            try
            {
                // 使用 SpawnDataConverter 转换数据
                var spec = SpawnDataConverter.ConvertToGameStartSpec(
                    spawns,
                    _defaultPlayerId,
                    "session_spawn",
                    mapId: 1,
                    tickRate: 30,
                    inputDelayFrames: 0,
                    randomSeed: Environment.TickCount
                );

                // Get ActorContext from contexts
                var actorContext = (_contexts as global::Contexts)?.actor;
                if (actorContext == null)
                {
                    Log.Error("[MobaSpawnService] ActorContext is null, cannot create spawns");
                    return false;
                }

                // Apply game start spec (creates entities)
                var result = _enterGameFlow.ApplyGameStartSpec(actorContext, in spec);

                if (result)
                {
                    Log.Info($"[MobaSpawnService] Successfully created {spawns.Length} player spawns");
                }
                else
                {
                    Log.Warning($"[MobaSpawnService] Failed to create player spawns");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaSpawnService] CreateLogicWorldSpawns failed");
                return false;
            }
        }

        private static LogicWorldSpawnData[] ToLogicWorldSpawns(PlayerSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return Array.Empty<LogicWorldSpawnData>();
            }

            var logicWorldSpawns = new LogicWorldSpawnData[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                var spawn = spawns[i];
                logicWorldSpawns[i] = new LogicWorldSpawnData(
                    spawn.PlayerId,
                    spawn.CharacterId,
                    spawn.TeamId,
                    spawn.X,
                    spawn.Y,
                    spawn.Z,
                    spawn.Name);
            }

            return logicWorldSpawns;
        }

        public void Dispose()
        {
        }
    }
}
