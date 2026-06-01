using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Util.Generator;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Ability.Share.Impl.Moba.CreateWorld;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaEnterGameFlowService))]
    public sealed class MobaEnterGameFlowService : IService
    {
        [WorldInject] private MobaEnterGameSnapshotService _snapshot;
        [WorldInject] private IWorldContext _worldContext;
        [WorldInject] private ActorIdAllocator _actorIds;
        [WorldInject] private MobaActorRegistry _registry;
        [WorldInject] private MobaEntityManager _entities;
        [WorldInject] private MobaPlayerActorMapService _playerActorMap;
        [WorldInject] private MobaSkillLoadoutService _skills;
        [WorldInject(required: false)] private MobaConfigDatabase _config;
        [WorldInject(required: false)] private ActorEntityInitPipeline _generator;
        [WorldInject] private MobaActorSpawnSnapshotService _spawn;

        public bool ApplyGameStartSpec(ActorContext actorContext, in MobaGameStartSpec spec)
        {
            if (actorContext == null) throw new ArgumentNullException(nameof(actorContext));

            var req = spec.EnterReq;

            var effectiveReq = MobaGameStartSpecNormalizer.Normalize(_config, in req);

            Log.Info($"[MobaEnterGameFlowService] TryStartGame: begin (players={(effectiveReq.Players != null ? effectiveReq.Players.Length : 0)}, playerId={effectiveReq.PlayerId.Value})");

            var spawnEntries = new List<MobaActorSpawnSnapshotEntry>(effectiveReq.Players != null ? effectiveReq.Players.Length : 4);

            var built = ActorSpawnPipeline.BuildActorsFromEnterGameReqAndInitialize(
                actorContext,
                _actorIds,
                _registry,
                _entities,
                effectiveReq,
                initializer: (entity, loadout) =>
                {
                    if (_generator == null) return;
                    _generator.InitializeFromLoadout(entity, loadout);
                },
                onActorBuilt: (entity, loadout) =>
                {
                    try
                    {
                        var actorId = entity != null && entity.hasActorId ? entity.actorId.Value : 0;
                        if (actorId > 0)
                        {
                            spawnEntries.Add(new MobaActorSpawnSnapshotEntry
                            {
                                NetId = actorId,
                                Kind = (int)SpawnEntityKind.Character,
                                Code = loadout.HeroId,
                                OwnerNetId = 0,
                                X = loadout.SpawnX,
                                Y = loadout.SpawnY,
                                Z = loadout.SpawnZ
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "[MobaEnterGameFlowService] build spawn entry failed");
                    }
                });

            Log.Info($"[MobaEnterGameFlowService] TryStartGame: BuildEnterGameActors done (localActorId={built.LocalActorId})");

            BindPlayerActors(built.PlayerActors);

            var p = built.LocalActorTransform.Position;
            var payload = MobaEnterGamePayloadCodec.Serialize(in p);

            var res = new EnterMobaGameRes(
                worldId: _worldContext.Id,
                playerId: effectiveReq.PlayerId,
                localActorId: built.LocalActorId,
                randomSeed: effectiveReq.RandomSeed,
                tickRate: effectiveReq.TickRate,
                inputDelayFrames: effectiveReq.InputDelayFrames,
                players: built.Players,
                opCode: MobaEnterGamePayloadCodec.PayloadOpCode,
                payload: payload,
                playersLoadout: effectiveReq.Players
            );

            _snapshot.PublishEnterGameResPayload(EnterMobaGameCodec.SerializeRes(res));

            try
            {
                var payload2 = MobaActorSpawnSnapshotCodec.Serialize(spawnEntries.ToArray());
                _spawn.PublishSpawnPayload(payload2);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEnterGameFlowService] publish spawn payload failed");
            }
            return true;
        }

        private void BindPlayerActors(MobaPlayerActorEntry[] playerActors)
        {
            if (playerActors == null)
            {
                return;
            }

            for (int i = 0; i < playerActors.Length; i++)
            {
                var entry = playerActors[i];
                if (entry.ActorId <= 0)
                {
                    continue;
                }

                _playerActorMap.Bind(entry.PlayerId, entry.ActorId);
            }
        }

        public void Dispose()
        {
        }
    }
}
