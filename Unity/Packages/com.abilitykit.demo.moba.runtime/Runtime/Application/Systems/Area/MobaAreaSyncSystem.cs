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
        private sealed class AreaTriggerPayload : IMobaTriggerInvocationContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaTriggerDataContext
        {
            private readonly MobaTriggerDataBag _data = new MobaTriggerDataBag();

            public int TriggerId { get; set; }
            public EffectContextKind Kind => EffectContextKind.Area;
            public int SourceActorId { get; set; }
            public int TargetActorId { get; set; }
            public long SourceContextId { get; set; }
            public MobaTraceKind OriginKind { get; set; }
            public MobaTraceKind TraceKind
            {
                get => OriginKind;
                set => OriginKind = value;
            }
            public int SourceConfigId { get; set; }
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

            public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
            {
                lineageContext = new MobaTriggerLineageContext(Kind, OriginKind, SourceActorId, TargetActorId, SourceContextId, SourceContextId, SourceConfigId, SourceConfigId);
                return true;
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
                PublishAreaEvent(AreaTriggering.Events.Spawn, evt, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: evt.Center, radius: evt.Radius, collider: default);
            }

            _enters.Clear();
            _projectiles.DrainAreaEnterEvents(_enters);
            for (int i = 0; i < _enters.Count; i++)
            {
                var evt = _enters[i];
                var hitActorId = ResolveActorIdByCollider(evt.Collider);

                PublishAreaEvent(AreaTriggering.Events.Enter, evt, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: default, radius: 0f, collider: evt.Collider);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnEnterTriggerId > 0)
                {
                    var payload = new AreaTriggerPayload
                    {
                        OriginKind = MobaTraceKind.AreaEnter,
                        TriggerId = entry.OnEnterTriggerId,
                        SourceActorId = evt.OwnerId,
                        TargetActorId = hitActorId,
                        SourceConfigId = evt.Area.Value,
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

                PublishAreaEvent(AreaTriggering.Events.Exit, evt, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: default, radius: 0f, collider: evt.Collider);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnExitTriggerId > 0)
                {
                    var payload = new AreaTriggerPayload
                    {
                        OriginKind = MobaTraceKind.AreaExit,
                        TriggerId = entry.OnExitTriggerId,
                        SourceActorId = evt.OwnerId,
                        TargetActorId = hitActorId,
                        SourceConfigId = evt.Area.Value,
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

                PublishAreaEvent(AreaTriggering.Events.Expire, evt, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: default, radius: 0f, collider: default);

                if (_effects != null && _areaTriggers != null && _areaTriggers.TryGet(evt.Area, out var entry) && entry.OnExpireTriggerIds != null && entry.OnExpireTriggerIds.Length > 0)
                {
                    for (int ti = 0; ti < entry.OnExpireTriggerIds.Length; ti++)
                    {
                        var triggerId = entry.OnExpireTriggerIds[ti];
                        if (triggerId <= 0) continue;

                        var payload = new AreaTriggerPayload
                        {
                            OriginKind = MobaTraceKind.AreaExit,
                            TriggerId = triggerId,
                            SourceActorId = evt.OwnerId,
                            TargetActorId = 0,
                            SourceConfigId = evt.Area.Value,
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

        private void PublishAreaEvent(string eventId, object raw, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(eventId)) return;

            var eid = TriggeringIdUtil.GetEventEid(eventId);
            var payload = new AreaEventArgs
            {
                EventId = eventId,
                AreaId = 0,
                OwnerActorId = ownerActorId,
                TargetActorId = targetActorId,
                Frame = frame,
                Center = center,
                Radius = radius,
                Collider = collider,
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

