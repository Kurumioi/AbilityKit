using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Effect;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Core.Math;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Core.Common.Event;
using AbilityKit.Triggering.Eventing;

namespace AbilityKit.Demo.Moba.Systems.Area
{
    [WorldSystem(order: MobaSystemOrder.ProjectileSync + 1, Phase = WorldSystemPhase.PostExecute)]
    public sealed class MobaAreaSyncSystem : WorldSystemBase
    {
        private sealed class AreaTriggerPayload : IMobaTriggerInvocationContext, IMobaActorContextProvider, IMobaOriginContextProvider, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaContextSourceProvider, IMobaTriggerDataContext
        {
            private readonly MobaTriggerDataBag _data = new MobaTriggerDataBag();

            public int TriggerId { get; set; }
            public EffectContextKind Kind => EffectContextKind.Area;
            public int SourceActorId { get; set; }
            public int TargetActorId { get; set; }
            public long SourceContextId { get; set; }
            public long RootContextId { get; set; }
            public long OwnerContextId { get; set; }
            public MobaTraceKind OriginKind { get; set; }
            public MobaTraceKind TraceKind
            {
                get => OriginKind;
                set => OriginKind = value;
            }
            public int SourceConfigId { get; set; }
            public int AreaId { get; set; }
            public int Frame { get; set; }
            public object Raw { get; set; }
            public AreaSpawnEvent Spawn;
            public AreaEnterEvent Enter;
            public AreaExitEvent Exit;
            public AreaExpireEvent Expire;
            public int OwnerId;
            public Vec3 Center;
            public float Radius;
            public ColliderId Collider;
            public int CollisionLayerMask;
            public int MaxTargets;
            public MobaTriggerDataBag Data => _data;
            public Dictionary<string, object> SharedData => _data.SharedData;

            public bool TryGetSourceActorId(out int actorId)
            {
                actorId = SourceActorId;
                return actorId > 0;
            }

            public bool TryGetTargetActorId(out int actorId)
            {
                actorId = TargetActorId;
                return actorId > 0;
            }

            public bool TryGetOrigin(out MobaGameplayOrigin origin)
            {
                var traceKind = OriginKind != MobaTraceKind.None ? OriginKind : MobaTraceKind.AreaSpawn;
                var configId = SourceConfigId != 0 ? SourceConfigId : AreaId;
                origin = MobaGameplayOrigin.FromLegacy(SourceActorId, TargetActorId, traceKind, configId, SourceContextId);
                return origin.IsValid;
            }

            public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
            {
                if (TryGetOrigin(out var origin) && origin.IsValid)
                {
                    lineageContext = new MobaTriggerLineageContext(Kind, origin.ImmediateKind, origin.SourceActorId, origin.TargetActorId, origin.EffectiveParentContextId, RootContextId != 0 ? RootContextId : origin.EffectiveRootContextId, OwnerContextId, origin.ImmediateConfigId);
                    return true;
                }

                var traceKind = OriginKind != MobaTraceKind.None ? OriginKind : MobaTraceKind.AreaSpawn;
                var configId = SourceConfigId != 0 ? SourceConfigId : AreaId;
                lineageContext = new MobaTriggerLineageContext(Kind, traceKind, SourceActorId, TargetActorId, SourceContextId, RootContextId, OwnerContextId, configId);
                return SourceActorId > 0 || TargetActorId > 0 || AreaId > 0 || SourceConfigId > 0 || SourceContextId != 0;
            }

            public bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
            {
                if (TryGetLineageContext(out var lineageContext))
                {
                    traceContext = lineageContext.ToTraceContext();
                    return true;
                }

                traceContext = default;
                return false;
            }

            public bool TryGetContextSource(out MobaContextSourceView source)
            {
                if (TryGetLineageContext(out var lineageContext))
                {
                    source = MobaContextSourceView.FromLineage(
                        in lineageContext,
                        MobaContextSourceResolveKind.DirectProvider,
                        MobaContextSourceBoundary.Snapshot,
                        runtimeKind: "AreaTrigger",
                        runtimeConfigId: SourceConfigId != 0 ? SourceConfigId : AreaId);
                    return source.IsValid;
                }

                source = default;
                return false;
            }

            public T GetData<T>(string key, T defaultValue = default) => _data.GetData(key, defaultValue);
            public void SetData<T>(string key, T value) => _data.SetData(key, value);
            public bool TryGetData<T>(string key, out T value) => _data.TryGetData(key, out value);
            public bool RemoveData(string key) => _data.RemoveData(key);
            public void ClearData() => _data.ClearData();
        }

        private IProjectileService _projectiles;
        private MobaActorRegistry _registry;
        private AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private MobaEffectExecutionService _effects;
        private MobaAreaTriggerRegistry _areaTriggers;

        private readonly List<AreaSpawnEvent> _spawns = new List<AreaSpawnEvent>(32);
        private readonly List<AreaEnterEvent> _enters = new List<AreaEnterEvent>(64);
        private readonly List<AreaExitEvent> _exits = new List<AreaExitEvent>(64);
        private readonly List<AreaExpireEvent> _expires = new List<AreaExpireEvent>(32);

        public MobaAreaSyncSystem(global::Entitas.IContexts contexts, IWorldResolver services) : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _projectiles);
            Services.TryResolve(out _registry);
            Services.TryResolve(out _eventBus);
            Services.TryResolve(out _effects);
            Services.TryResolve(out _areaTriggers);
        }

        protected override void OnExecute()
        {
            if (_projectiles == null || _registry == null) return;

            _spawns.Clear();
            _projectiles.DrainAreaSpawnEvents(_spawns);
            for (int i = 0; i < _spawns.Count; i++)
            {
                var evt = _spawns[i];
                var templateId = 0;
                var collisionLayerMask = 0;
                var maxTargets = 0;
                if (_areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry))
                {
                    templateId = entry.TemplateId;
                    collisionLayerMask = entry.CollisionLayerMask;
                    maxTargets = entry.MaxTargets;
                }

                PublishAreaEvent(AreaTriggering.Events.Spawn, evt.Area.Value, templateId, MobaTraceKind.AreaSpawn, evt, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: evt.Center, radius: evt.Radius, collider: default, collisionLayerMask, maxTargets);
            }

            _enters.Clear();
            _projectiles.DrainAreaEnterEvents(_enters);
            for (int i = 0; i < _enters.Count; i++)
            {
                var evt = _enters[i];
                var hitActorId = ResolveActorIdByCollider(evt.Collider);

                PublishAreaEvent(AreaTriggering.Events.Enter, evt.Area.Value, 0, MobaTraceKind.AreaEnter, evt, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: default, radius: 0f, collider: evt.Collider, 0, 0);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnEnterTriggerId > 0)
                {
                    var payload = new AreaTriggerPayload
                    {
                        OriginKind = MobaTraceKind.AreaEnter,
                        TriggerId = entry.OnEnterTriggerId,
                        SourceActorId = evt.OwnerId,
                        TargetActorId = hitActorId,
                        SourceConfigId = entry.TemplateId != 0 ? entry.TemplateId : evt.Area.Value,
                        AreaId = evt.Area.Value,
                        Frame = evt.Frame,
                        Enter = evt,
                        OwnerId = evt.OwnerId,
                        Collider = evt.Collider,
                        Center = entry.Center,
                        Radius = entry.Radius,
                        CollisionLayerMask = entry.CollisionLayerMask,
                        MaxTargets = entry.MaxTargets,
                        Raw = evt,
                    };
                    SyncAreaPayload(payload);
                    _effects.ExecuteTriggerId(entry.OnEnterTriggerId, payload);
                }
            }

            _exits.Clear();
            _projectiles.DrainAreaExitEvents(_exits);
            for (int i = 0; i < _exits.Count; i++)
            {
                var evt = _exits[i];
                var hitActorId = ResolveActorIdByCollider(evt.Collider);

                PublishAreaEvent(AreaTriggering.Events.Exit, evt.Area.Value, 0, MobaTraceKind.AreaExit, evt, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: default, radius: 0f, collider: evt.Collider, 0, 0);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnExitTriggerId > 0)
                {
                    var payload = new AreaTriggerPayload
                    {
                        OriginKind = MobaTraceKind.AreaExit,
                        TriggerId = entry.OnExitTriggerId,
                        SourceActorId = evt.OwnerId,
                        TargetActorId = hitActorId,
                        SourceConfigId = entry.TemplateId != 0 ? entry.TemplateId : evt.Area.Value,
                        AreaId = evt.Area.Value,
                        Frame = evt.Frame,
                        Exit = evt,
                        OwnerId = evt.OwnerId,
                        Collider = evt.Collider,
                        Center = entry.Center,
                        Radius = entry.Radius,
                        CollisionLayerMask = entry.CollisionLayerMask,
                        MaxTargets = entry.MaxTargets,
                        Raw = evt,
                    };
                    SyncAreaPayload(payload);
                    _effects.ExecuteTriggerId(entry.OnExitTriggerId, payload);
                }
            }

            _expires.Clear();
            _projectiles.DrainAreaExpireEvents(_expires);
            for (int i = 0; i < _expires.Count; i++)
            {
                var evt = _expires[i];

                PublishAreaEvent(AreaTriggering.Events.Expire, evt.Area.Value, 0, MobaTraceKind.AreaExpire, evt, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: default, radius: 0f, collider: default, 0, 0);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnExpireTriggerIds != null && entry.OnExpireTriggerIds.Length > 0)
                {
                    for (int ti = 0; ti < entry.OnExpireTriggerIds.Length; ti++)
                    {
                        var triggerId = entry.OnExpireTriggerIds[ti];
                        if (triggerId <= 0) continue;

                        var payload = new AreaTriggerPayload
                        {
                            OriginKind = MobaTraceKind.AreaExpire,
                            TriggerId = triggerId,
                            SourceActorId = evt.OwnerId,
                            TargetActorId = 0,
                            SourceConfigId = entry.TemplateId != 0 ? entry.TemplateId : evt.Area.Value,
                            AreaId = evt.Area.Value,
                            Frame = evt.Frame,
                            Expire = evt,
                            OwnerId = evt.OwnerId,
                            Center = entry.Center,
                            Radius = entry.Radius,
                            CollisionLayerMask = entry.CollisionLayerMask,
                            MaxTargets = entry.MaxTargets,
                            Raw = evt,
                        };
                        SyncAreaPayload(payload);
                        _effects.ExecuteTriggerId(triggerId, payload);
                    }
                }

                _areaTriggers?.Unregister(evt.Area);
            }
        }

        private static void SyncAreaPayload(AreaTriggerPayload payload)
        {
            if (payload == null) return;
            payload.Data.SyncInvocationData(payload);
            if (payload.TryGetLineageContext(out var lineageContext)) payload.Data.SyncTraceData(lineageContext.ToTraceContext());
            payload.Data.SetData(AbilityContextKeys.AreaId.ToKeyString(), payload.SourceConfigId);
            payload.Data.SetData(AbilityContextKeys.AreaCenter.ToKeyString(), payload.Center);
            payload.Data.SetData(AbilityContextKeys.AreaRadius.ToKeyString(), payload.Radius);
            payload.Data.SetData(AbilityContextKeys.Frame.ToKeyString(), payload.Frame);
            payload.Data.SetData("area.ownerId", payload.OwnerId);
            payload.Data.SetData("area.collider", payload.Collider);
            payload.Data.SetData("area.collisionLayerMask", payload.CollisionLayerMask);
            payload.Data.SetData("area.maxTargets", payload.MaxTargets);
        }

        private void PublishAreaEvent(string eventId, int areaId, int templateId, MobaTraceKind traceKind, object raw, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider, int collisionLayerMask, int maxTargets)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(eventId)) return;

            if (templateId == 0 && _areaTriggers != null && areaId > 0 && _areaTriggers.TryGet(new AreaId(areaId), out var entry))
            {
                templateId = entry.TemplateId;
                if (collisionLayerMask == 0) collisionLayerMask = entry.CollisionLayerMask;
                if (maxTargets == 0) maxTargets = entry.MaxTargets;
            }

            var eid = TriggeringIdUtil.GetEventEid(eventId);
            var payload = new AreaEventArgs
            {
                EventId = eventId,
                AreaId = areaId,
                TemplateId = templateId != 0 ? templateId : areaId,
                OwnerActorId = ownerActorId,
                TargetActorId = targetActorId,
                Frame = frame,
                TraceKind = traceKind != MobaTraceKind.None ? traceKind : MobaTraceKind.AreaSpawn,
                Center = center,
                Radius = radius,
                Collider = collider,
                CollisionLayerMask = collisionLayerMask,
                MaxTargets = maxTargets,
                Raw = raw,
            };

            _eventBus.Publish(new EventKey<AreaEventArgs>(eid), in payload);
            object boxed = payload;
            _eventBus.Publish(new EventKey<object>(eid), in boxed);
        }

        private int ResolveActorIdByCollider(ColliderId id)
        {
            if (_registry == null) return 0;
            if (id.Value <= 0) return 0;

            try
            {
                foreach (var kv in _registry.Entries)
                {
                    var e = kv.Value;
                    if (e == null || !e.hasActorId || !e.hasCollisionId) continue;
                    if (e.collisionId.Value.Equals(id))
                    {
                        return e.actorId.Value;
                    }
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }
    }
}

