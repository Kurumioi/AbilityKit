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
    /// <summary>
    /// MOBA spawn service implementation
    ///
    /// Implements ISpawnService from Coordinator package
    /// Bridges SessionCoordinator player spawn requests to moba.core entity creation
    ///
    /// Design:
    /// - Uses SpawnDataConverter for data transformation
    /// - Uses MobaEnterGameFlowService for actual entity creation
    /// - Publishes spawn events for view synchronization
    /// </summary>
    [WorldService(typeof(MobaSpawnService))]
    [WorldService(typeof(ISpawnService))]
    public sealed class MobaSpawnService : ISpawnService
    {
        private readonly MobaEnterGameFlowService _enterGameFlow;
        private readonly MobaActorRegistry _registry;
        private readonly MobaEntityManager _entities;
        private readonly MobaActorSpawnSnapshotService _spawnSnapshot;
        private readonly PlayerId _defaultPlayerId;
        private readonly global::Entitas.IContexts _contexts;

        public MobaSpawnService(
            MobaEnterGameFlowService enterGameFlow,
            MobaActorRegistry registry,
            MobaEntityManager entities,
            MobaActorSpawnSnapshotService spawnSnapshot,
            global::Entitas.IContexts contexts)
        {
            _enterGameFlow = enterGameFlow ?? throw new ArgumentNullException(nameof(enterGameFlow));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _spawnSnapshot = spawnSnapshot ?? throw new ArgumentNullException(nameof(spawnSnapshot));
            _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
            _defaultPlayerId = new PlayerId("default");
        }

        public bool CreateSpawns(PlayerSpawnData[] spawns)
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
                Log.Exception(ex, "[MobaSpawnService] CreateSpawns failed");
                return false;
            }
        }

        public void Dispose()
        {
        }
    }
}
