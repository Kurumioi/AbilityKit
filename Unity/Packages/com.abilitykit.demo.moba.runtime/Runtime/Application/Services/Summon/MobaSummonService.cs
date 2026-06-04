using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Events.Summon;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Util.Converter;
using AbilityKit.Demo.Moba.Util.Generator;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Effect;
using AbilityKit.Core.Common.Event;
using AbilityKit.Trace;
using StableStringId = AbilityKit.Triggering.Eventing.StableStringId;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaSummonService))]
    public sealed class MobaSummonService : IService
    {
        [WorldInject] private ActorIdAllocator _actorIds;
        [WorldInject] private MobaActorRegistry _registry;
        [WorldInject] private MobaEntityManager _entities;
        [WorldInject] private MobaActorLookupService _actors;
        [WorldInject] private AbilityKit.Demo.Moba.Util.Generator.ActorEntityInitPipeline _generator;
        [WorldInject] private MobaConfigDatabase _config;
        [WorldInject] private MobaComponentTemplateService _componentTemplates;
        [WorldInject] private AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        [WorldInject(required: false)] private IFrameTime _frameTime;
        [WorldInject(required: false)] private IWorldClock _clock;
        [WorldInject(required: false)] private MobaTraceRegistry _trace;
        [WorldInject(required: false)] private IMobaActorSpawnService _actorSpawn;

        private readonly Dictionary<int, List<int>> _summonsByRootOwner = new Dictionary<int, List<int>>();

        public bool TrySummon(int casterActorId, int summonId, in Vec3 pos)
        {
            return TrySummonInternal(casterActorId, summonId, in pos, hasForward: false, forward: default, sourceContext: default);
        }

        public bool TrySummon(int casterActorId, int summonId, in Vec3 pos, in Vec3 forward)
        {
            return TrySummonInternal(casterActorId, summonId, in pos, hasForward: true, forward: in forward, sourceContext: default);
        }

        public bool TrySummon(int casterActorId, int summonId, in Vec3 pos, in SummonSourceContext sourceContext)
        {
            return TrySummonInternal(casterActorId, summonId, in pos, hasForward: false, forward: default, sourceContext: in sourceContext);
        }

        public bool TrySummon(int casterActorId, int summonId, in Vec3 pos, in Vec3 forward, in SummonSourceContext sourceContext)
        {
            return TrySummonInternal(casterActorId, summonId, in pos, hasForward: true, forward: in forward, sourceContext: in sourceContext);
        }

        private bool TrySummonInternal(int casterActorId, int summonId, in Vec3 pos, bool hasForward, in Vec3 forward, in SummonSourceContext sourceContext)
        {
            if (casterActorId <= 0) return false;
            if (summonId <= 0) return false;
            if (_actorIds == null || _registry == null || _entities == null) return false;
            if (_config == null) return false;

            if (!_config.TryGetSummon(summonId, out var summon) || summon == null) return false;

            if (!_entities.TryGetActorEntity(casterActorId, out var caster) || caster == null || !caster.hasTransform)
            {
                return false;
            }

            if (_actorSpawn == null) return false;

            var actorId = _actorIds.Next();
            var rootOwner = OwnerLinkUtil.ResolveRootOwner(caster);
            if (rootOwner <= 0) rootOwner = casterActorId;

            var spec = MobaConverter.ToSummonActorBuildSpec(actorId, summonId, summon, caster, in pos, hasForward, in forward);
            var request = MobaActorSpawnRequest.FromSpec(in spec);
            request.PostSetup = new MobaActorSpawnPostSetup
            {
                SetOwnerLink = true,
                OwnerActorId = casterActorId,
                RootOwnerActorId = rootOwner,
                SetSummonMeta = true,
                SummonId = summonId,
                DespawnOnOwnerDie = summon.DespawnOnOwnerDie,
                SetLifetime = summon.LifetimeMs > 0,
                LifetimeEndTimeMs = summon.LifetimeMs > 0 ? NowMs() + summon.LifetimeMs : 0L,
                SetModelId = summon.ModelId > 0,
                ModelId = summon.ModelId,
            };

            if (!_actorSpawn.TrySpawn(in request, out var spawnResult) || !spawnResult.Success)
            {
                Log.Warning($"[MobaSummonService] spawn failed. summonId={summonId} actorId={actorId} casterActorId={casterActorId} error={spawnResult.Error}");
                return false;
            }

            var entity = spawnResult.Entity;
            if (entity == null) return false;
            var spawnSourceContext = CreateSpawnSourceContext(casterActorId, actorId, summonId, in sourceContext);

            if (_generator != null)
            {
                _generator.InitializeFromAttributeTemplate(entity, summon.AttributeTemplateId);
            }

            TryApplyDefaultComponentTemplates(entity, summon.DefaultComponentTemplateIds);

            TryInitSkillLoadout(entity, summon.SkillIds, summon.PassiveSkillIds);

            TrackSummon(rootOwner, actorId, summon.MaxAlivePerOwner, summon.OverflowPolicy);

            PublishSummonEvent(MobaSummonTriggering.Events.Spawned, rootOwner, casterActorId, actorId, summonId, (int)SummonDespawnReason.None, in spawnSourceContext);
            PublishSummonEvent(MobaSummonTriggering.Events.SpawnedByOwner(rootOwner), rootOwner, casterActorId, actorId, summonId, (int)SummonDespawnReason.None, in spawnSourceContext);

            return true;
        }

        private void TryApplyDefaultComponentTemplates(global::ActorEntity entity, IReadOnlyList<int> templateIds)
        {
            if (_componentTemplates == null) return;
            if (entity == null) return;
            if (templateIds == null || templateIds.Count == 0) return;

            for (int i = 0; i < templateIds.Count; i++)
            {
                var id = templateIds[i];
                if (id <= 0) continue;
                try { _componentTemplates.TryApply(entity, id); }
                catch (Exception ex) { Log.Exception(ex, $"[MobaSummonService] TryApply component template failed (templateId={id})"); }
            }
        }

        public bool TryDespawn(int summonActorId, SummonDespawnReason reason)
        {
            if (summonActorId <= 0) return false;
            if (_registry == null) return false;

            if (!_registry.TryGet(summonActorId, out var e) || e == null) return false;

            var rootOwner = 0;
            var owner = 0;
            var summonId = 0;

            if (e.hasOwnerLink && e.ownerLink != null)
            {
                owner = e.ownerLink.OwnerActorId;
                rootOwner = e.ownerLink.RootOwnerActorId;
            }
            if (rootOwner <= 0) rootOwner = owner;
            if (e.hasSummonMeta && e.summonMeta != null) summonId = e.summonMeta.SummonId;

            try { e.Destroy(); }
            catch (Exception ex) { Log.Exception(ex, $"[MobaSummonService] destroy summon entity failed (summonActorId={summonActorId}, summonId={summonId})"); }

            _registry.Unregister(summonActorId);
            try { _entities?.Unregister(summonActorId); }
            catch (Exception ex) { Log.Exception(ex, $"[MobaSummonService] unregister summon failed (summonActorId={summonActorId}, summonId={summonId})"); }

            UntrackSummon(rootOwner, summonActorId);

            if (reason == SummonDespawnReason.Killed)
            {
                PublishSummonEvent(MobaSummonTriggering.Events.Died, rootOwner, owner, summonActorId, summonId, (int)reason, sourceContext: default);
                if (rootOwner > 0)
                {
                    PublishSummonEvent(MobaSummonTriggering.Events.DiedByOwner(rootOwner), rootOwner, owner, summonActorId, summonId, (int)reason, sourceContext: default);
                }
            }

            PublishSummonEvent(MobaSummonTriggering.Events.Despawned, rootOwner, owner, summonActorId, summonId, (int)reason, sourceContext: default);
            if (rootOwner > 0)
            {
                PublishSummonEvent(MobaSummonTriggering.Events.DespawnedByOwner(rootOwner), rootOwner, owner, summonActorId, summonId, (int)reason, sourceContext: default);
            }

            return true;
        }

        private void TrackSummon(int rootOwnerActorId, int summonActorId, int maxAlivePerOwner, int overflowPolicy)
        {
            if (rootOwnerActorId <= 0) return;

            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null)
            {
                list = new List<int>(8);
                _summonsByRootOwner[rootOwnerActorId] = list;
            }

            list.Add(summonActorId);

            if (maxAlivePerOwner <= 0) return;
            if (list.Count <= maxAlivePerOwner) return;

            var removeCount = list.Count - maxAlivePerOwner;
            for (int i = 0; i < removeCount; i++)
            {
                if (list.Count == 0) break;
                var oldest = list[0];
                list.RemoveAt(0);
                TryDespawn(oldest, SummonDespawnReason.ReplacedByLimit);
            }
        }

        private void UntrackSummon(int rootOwnerActorId, int summonActorId)
        {
            if (rootOwnerActorId <= 0) return;
            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null) return;
            list.Remove(summonActorId);
        }

        private void PublishSummonEvent(string eventId, int rootOwnerActorId, int ownerActorId, int summonActorId, int summonId, int reason, in SummonSourceContext sourceContext)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(eventId)) return;

            var payload = new SummonEventPayload
            {
                SummonActorId = summonActorId,
                SummonId = summonId,
                OwnerActorId = ownerActorId,
                RootOwnerActorId = rootOwnerActorId,
                Reason = reason,
                SourceContext = sourceContext,
            };

            var eid = TriggeringIdUtil.GetEventEid(eventId);
            _eventBus.Publish(new EventKey<SummonEventPayload>(eid), in payload);
            object boxed = payload;
            _eventBus.Publish(new EventKey<object>(eid), in boxed);
        }

        private SummonSourceContext CreateSpawnSourceContext(int casterActorId, int summonActorId, int summonId, in SummonSourceContext sourceContext)
        {
            var origin = sourceContext.TryGetOrigin(out var sourceOrigin)
                ? sourceOrigin.WithActors(casterActorId, summonActorId)
                : MobaGameplayOrigin.FromLegacy(casterActorId, summonActorId, MobaTraceKind.SummonSpawn, summonId, 0);

            var parentContextId = origin.EffectiveParentContextId;
            var spawnContextId = 0L;
            if (_trace != null)
            {
                spawnContextId = parentContextId != 0L
                    ? _trace.CreateChildContext(parentContextId, MobaTraceKind.SummonSpawn, summonId, casterActorId, summonActorId, TraceEndpoint.Actor(casterActorId), TraceEndpoint.Actor(summonActorId))
                    : _trace.CreateRootContext(MobaTraceKind.SummonSpawn, summonId, casterActorId, summonActorId, TraceEndpoint.Actor(casterActorId), TraceEndpoint.Actor(summonActorId));
            }

            if (spawnContextId != 0L)
            {
                origin = MobaGameplayOriginBuilder.Create()
                    .FromOrigin(in origin)
                    .WithActors(casterActorId, summonActorId)
                    .WithImmediate(MobaTraceKind.SummonSpawn, summonId, spawnContextId)
                    .WithRootContext(origin.EffectiveRootContextId != 0L ? origin.EffectiveRootContextId : spawnContextId)
                    .WithOwnerContext(origin.OwnerContextId != 0L ? origin.OwnerContextId : spawnContextId)
                    .Build();
            }

            return SummonSourceContextBuilder.Create()
                .WithActors(casterActorId, summonActorId)
                .WithSummonConfig(summonId)
                .WithSourceContext(spawnContextId)
                .WithRootContext(origin.EffectiveRootContextId)
                .WithOwnerContext(origin.OwnerContextId)
                .WithOrigin(in origin)
                .Build();
        }

        private void TryInitSkillLoadout(global::ActorEntity entity, IReadOnlyList<int> skillIds, IReadOnlyList<int> passiveSkillIds)
        {
            if (entity == null) return;

            var active = CreateActiveSkillRuntimes(skillIds);
            var passive = CreatePassiveSkillRuntimes(passiveSkillIds);

            if (entity.hasSkillLoadout) entity.ReplaceSkillLoadout(active, passive);
            else entity.AddSkillLoadout(active, passive);
        }

        private static AbilityKit.Demo.Moba.Components.ActiveSkillRuntime[] CreateActiveSkillRuntimes(IReadOnlyList<int> skillIds)
        {
            if (skillIds == null || skillIds.Count == 0) return Array.Empty<AbilityKit.Demo.Moba.Components.ActiveSkillRuntime>();
            var list = new List<AbilityKit.Demo.Moba.Components.ActiveSkillRuntime>(skillIds.Count);
            for (int i = 0; i < skillIds.Count; i++)
            {
                var id = skillIds[i];
                if (id <= 0) continue;
                list.Add(new AbilityKit.Demo.Moba.Components.ActiveSkillRuntime { SkillId = id, Level = 1, CooldownEndTimeMs = 0L });
            }
            return list.Count == 0 ? Array.Empty<AbilityKit.Demo.Moba.Components.ActiveSkillRuntime>() : list.ToArray();
        }

        private static AbilityKit.Demo.Moba.Components.PassiveSkillRuntime[] CreatePassiveSkillRuntimes(IReadOnlyList<int> passiveSkillIds)
        {
            if (passiveSkillIds == null || passiveSkillIds.Count == 0) return Array.Empty<AbilityKit.Demo.Moba.Components.PassiveSkillRuntime>();
            var list = new List<AbilityKit.Demo.Moba.Components.PassiveSkillRuntime>(passiveSkillIds.Count);
            for (int i = 0; i < passiveSkillIds.Count; i++)
            {
                var id = passiveSkillIds[i];
                if (id <= 0) continue;
                list.Add(new AbilityKit.Demo.Moba.Components.PassiveSkillRuntime { PassiveSkillId = id, Level = 1, CooldownEndTimeMs = 0L });
            }
            return list.Count == 0 ? Array.Empty<AbilityKit.Demo.Moba.Components.PassiveSkillRuntime>() : list.ToArray();
        }

        private long NowMs()
        {
            if (_frameTime != null)
            {
                return (long)System.MathF.Round(_frameTime.Time * 1000f);
            }
            if (_clock != null)
            {
                return (long)System.MathF.Round(_clock.Time * 1000f);
            }
            return 0L;
        }

        public void Dispose()
        {
            _summonsByRootOwner.Clear();
        }
    }
}

