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
using AbilityKit.Demo.Moba.Components;
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
        [WorldInject(required: false)] private IMobaTemporaryEntityLifecycleService _lifecycle;

        private enum SummonOverflowPolicy
        {
            ReplaceOldest = 0,
            RejectNew = 1,
            ReplaceNewest = 2,
            AllowOverflow = 3,
        }

        private readonly Dictionary<int, List<int>> _summonsByRootOwner = new Dictionary<int, List<int>>();
        private readonly List<int> _queryBuffer = new List<int>(16);

        public int ActiveCount => CountAllTrackedSummons();

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

            var rootOwner = OwnerLinkUtil.ResolveRootOwner(caster);
            if (rootOwner <= 0) rootOwner = casterActorId;
            if (!PrepareCapacityForSummon(rootOwner, summonId, summon.MaxAlivePerOwner, summon.OverflowPolicy))
            {
                _lifecycle?.RecordRejected(MobaTemporaryEntityKind.Summon, ActiveCount, CurrentFrame);
                Log.Warning($"[MobaSummonService] summon rejected by overflow policy. summonId={summonId} casterActorId={casterActorId} rootOwner={rootOwner} maxAlive={summon.MaxAlivePerOwner} policy={summon.OverflowPolicy}");
                return false;
            }

            var actorId = _actorIds.Next();

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
                _lifecycle?.RecordRejected(MobaTemporaryEntityKind.Summon, ActiveCount, CurrentFrame);
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

            TrackSummon(rootOwner, actorId);
            _lifecycle?.RecordSpawn(MobaTemporaryEntityKind.Summon, ActiveCount, CurrentFrame);

            PublishSummonEvent(MobaSummonTriggering.Events.Spawned, rootOwner, casterActorId, actorId, summonId, (int)SummonDespawnReason.None, in spawnSourceContext);
            PublishSummonEvent(MobaSummonTriggering.Events.SpawnedByOwner(rootOwner), rootOwner, casterActorId, actorId, summonId, (int)SummonDespawnReason.None, in spawnSourceContext);

            return true;
        }

        private void TryApplyDefaultComponentTemplates(global::ActorEntity entity, IReadOnlyList<int> templateIds)
        {
            if (templateIds == null || templateIds.Count == 0) return;

            if (_componentTemplates == null)
            {
                throw new InvalidOperationException("MobaSummonService requires MobaComponentTemplateService when summon default component templates are configured.");
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            for (int i = 0; i < templateIds.Count; i++)
            {
                var id = templateIds[i];
                if (id <= 0)
                {
                    throw new InvalidOperationException($"Summon default component template id must be positive. index={i}, templateId={id}");
                }

                _componentTemplates.Apply(entity, id);
            }
        }

        public bool TryDespawn(int summonActorId, SummonDespawnReason reason)
        {
            if (summonActorId <= 0) return false;
            if (_registry == null) return false;
            if (!_registry.TryGet(summonActorId, out var e) || e == null) return false;

            var frame = CurrentFrame;
            var actorReason = ToActorDespawnReason(reason);
            if (e.hasActorDespawnRequest)
            {
                e.ReplaceActorDespawnRequest(frame, frame, actorReason, 0, 0L);
            }
            else
            {
                e.AddActorDespawnRequest(frame, frame, actorReason, 0, 0L);
            }

            return true;
        }

        public bool ExecuteRequestedDespawn(int summonActorId, SummonDespawnReason reason)
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

            _registry.Unregister(summonActorId);
            try { _entities?.Unregister(summonActorId); }
            catch (Exception ex) { Log.Exception(ex, $"[MobaSummonService] unregister summon failed (summonActorId={summonActorId}, summonId={summonId})"); }

            UntrackSummon(rootOwner, summonActorId);
            _lifecycle?.RecordDespawn(MobaTemporaryEntityKind.Summon, ActiveCount, CurrentFrame);
            if (reason == SummonDespawnReason.ReplacedByLimit) _lifecycle?.RecordReplaced(MobaTemporaryEntityKind.Summon, ActiveCount, CurrentFrame);

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

            try { e.Destroy(); }
            catch (Exception ex) { Log.Exception(ex, $"[MobaSummonService] destroy summon entity failed (summonActorId={summonActorId}, summonId={summonId})"); }

            return true;
        }

        public int GetAliveCount(int rootOwnerActorId, int summonId = 0)
        {
            _queryBuffer.Clear();
            TryGetSummons(rootOwnerActorId, _queryBuffer, summonId);
            return _queryBuffer.Count;
        }

        public bool TryGetSummons(int rootOwnerActorId, List<int> results, int summonId = 0)
        {
            if (results == null) return false;
            results.Clear();
            if (rootOwnerActorId <= 0) return false;
            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null) return false;

            CompactTrackedSummons(rootOwnerActorId, list);
            for (var i = 0; i < list.Count; i++)
            {
                var actorId = list[i];
                if (!IsTrackedSummon(actorId, summonId)) continue;
                results.Add(actorId);
            }

            return results.Count > 0;
        }

        public int RemoveSummons(int rootOwnerActorId, int summonId, bool removeAll, SummonDespawnReason reason)
        {
            if (rootOwnerActorId <= 0) return 0;
            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null) return 0;

            CompactTrackedSummons(rootOwnerActorId, list);
            _queryBuffer.Clear();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var actorId = list[i];
                if (!IsTrackedSummon(actorId, summonId)) continue;
                _queryBuffer.Add(actorId);
                if (!removeAll) break;
            }

            var removed = 0;
            for (var i = 0; i < _queryBuffer.Count; i++)
            {
                if (TryDespawn(_queryBuffer[i], reason)) removed++;
            }

            _queryBuffer.Clear();
            return removed;
        }

        private int CountAllTrackedSummons()
        {
            var count = 0;
            foreach (var kv in _summonsByRootOwner)
            {
                var list = kv.Value;
                if (list == null) continue;
                for (var i = 0; i < list.Count; i++)
                {
                    if (IsTrackedSummon(list[i], 0)) count++;
                }
            }

            return count;
        }

        private void TrackSummon(int rootOwnerActorId, int summonActorId)
        {
            if (rootOwnerActorId <= 0) return;

            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null)
            {
                list = new List<int>(8);
                _summonsByRootOwner[rootOwnerActorId] = list;
            }

            if (!list.Contains(summonActorId)) list.Add(summonActorId);
        }

        private bool PrepareCapacityForSummon(int rootOwnerActorId, int summonId, int maxAlivePerOwner, int overflowPolicy)
        {
            if (rootOwnerActorId <= 0) return false;
            if (maxAlivePerOwner <= 0) return true;

            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null)
            {
                return true;
            }

            CompactTrackedSummons(rootOwnerActorId, list);
            var count = CountTrackedSummons(list, summonId);
            if (count < maxAlivePerOwner) return true;

            var policy = (SummonOverflowPolicy)overflowPolicy;
            switch (policy)
            {
                case SummonOverflowPolicy.RejectNew:
                    return false;
                case SummonOverflowPolicy.AllowOverflow:
                    return true;
                case SummonOverflowPolicy.ReplaceNewest:
                    return TryDespawn(FindNewestTrackedSummon(list, summonId), SummonDespawnReason.ReplacedByLimit);
                case SummonOverflowPolicy.ReplaceOldest:
                default:
                    return TryDespawn(FindOldestTrackedSummon(list, summonId), SummonDespawnReason.ReplacedByLimit);
            }
        }

        private void UntrackSummon(int rootOwnerActorId, int summonActorId)
        {
            if (rootOwnerActorId <= 0) return;
            if (!_summonsByRootOwner.TryGetValue(rootOwnerActorId, out var list) || list == null) return;
            list.Remove(summonActorId);
            if (list.Count == 0) _summonsByRootOwner.Remove(rootOwnerActorId);
        }

        private void CompactTrackedSummons(int rootOwnerActorId, List<int> list)
        {
            if (list == null) return;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (IsTrackedSummon(list[i], 0)) continue;
                list.RemoveAt(i);
            }

            if (list.Count == 0 && rootOwnerActorId > 0)
            {
                _summonsByRootOwner.Remove(rootOwnerActorId);
            }
        }

        private int CountTrackedSummons(List<int> list, int summonId)
        {
            if (list == null || list.Count == 0) return 0;
            var count = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (IsTrackedSummon(list[i], summonId)) count++;
            }

            return count;
        }

        private int FindOldestTrackedSummon(List<int> list, int summonId)
        {
            if (list == null) return 0;
            for (var i = 0; i < list.Count; i++)
            {
                var actorId = list[i];
                if (IsTrackedSummon(actorId, summonId)) return actorId;
            }

            return 0;
        }

        private int FindNewestTrackedSummon(List<int> list, int summonId)
        {
            if (list == null) return 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var actorId = list[i];
                if (IsTrackedSummon(actorId, summonId)) return actorId;
            }

            return 0;
        }

        private bool IsTrackedSummon(int summonActorId, int summonId)
        {
            if (summonActorId <= 0 || _registry == null) return false;
            if (!_registry.TryGet(summonActorId, out var e) || e == null) return false;
            if (!e.hasSummonMeta || e.summonMeta == null) return false;
            return summonId <= 0 || e.summonMeta.SummonId == summonId;
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

        private static ActorDespawnReason ToActorDespawnReason(SummonDespawnReason reason)
        {
            switch (reason)
            {
                case SummonDespawnReason.Timeout:
                    return ActorDespawnReason.SummonTimeout;
                case SummonDespawnReason.OwnerDead:
                    return ActorDespawnReason.SummonOwnerDead;
                case SummonDespawnReason.ReplacedByLimit:
                    return ActorDespawnReason.SummonReplacedByLimit;
                case SummonDespawnReason.ManualRemove:
                    return ActorDespawnReason.SummonManualRemove;
                case SummonDespawnReason.Killed:
                    return ActorDespawnReason.SummonKilled;
                case SummonDespawnReason.SceneCleanup:
                    return ActorDespawnReason.SceneCleanup;
                case SummonDespawnReason.None:
                default:
                    return ActorDespawnReason.Unknown;
            }
        }

        private int CurrentFrame
        {
            get
            {
                if (_frameTime != null) return _frameTime.Frame.Value;
                throw new InvalidOperationException("MobaSummonService requires IFrameTime for current frame.");
            }
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
            throw new InvalidOperationException("MobaSummonService requires IFrameTime or IWorldClock for current time.");
        }

        public void Dispose()
        {
            _summonsByRootOwner.Clear();
            _lifecycle?.SetActive(MobaTemporaryEntityKind.Summon, 0, CurrentFrame);
        }
    }
}

