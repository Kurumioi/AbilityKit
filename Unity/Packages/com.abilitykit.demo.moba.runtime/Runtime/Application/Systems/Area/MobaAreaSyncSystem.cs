using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Combat.Projectile;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Core.Mathematics;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Eventing;

namespace AbilityKit.Demo.Moba.Systems.Area
{
    [WorldSystem(order: MobaSystemOrder.ProjectileSync + 1, Phase = WorldSystemPhase.PostExecute)]
    public sealed class MobaAreaSyncSystem : WorldSystemBase
    {
        private IProjectileService _projectiles;
        private MobaActorRegistry _registry;
        private AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private MobaAreaRuntimeService _areaRuntime;
        private IMobaTemporaryEntityLifecycleService _lifecycle;

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
            Services.TryResolve(out _areaRuntime);
            Services.TryResolve(out _lifecycle);
        }

        protected override void OnExecute()
        {
            if (_projectiles == null || _registry == null) return;

            _spawns.Clear();
            _projectiles.DrainAreaSpawnEvents(_spawns);
            for (int i = 0; i < _spawns.Count; i++)
            {
                var evt = _spawns[i];
                var info = RequireAreaInfo(evt.Area);
                PublishAreaEvent("area.spawn", evt.Area.Value, info.TemplateId, MobaTraceKind.AreaSpawn, evt, in info, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: evt.Center, radius: evt.Radius, collider: default, info.CollisionLayerMask, info.MaxTargets);
            }

            _enters.Clear();
            _projectiles.DrainAreaEnterEvents(_enters);
            if (_enters.Count > 0) _lifecycle?.RecordEnterEvents(MobaTemporaryEntityKind.Area, _enters.Count);
            for (int i = 0; i < _enters.Count; i++)
            {
                var evt = _enters[i];
                var hitActorId = ResolveActorIdByCollider(evt.Collider);

                var info = RequireAreaInfo(evt.Area);
                PublishAreaEvent("area.enter", evt.Area.Value, info.TemplateId, MobaTraceKind.AreaEnter, evt, in info, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: info.Center, radius: info.Radius, collider: evt.Collider, info.CollisionLayerMask, info.MaxTargets);
            }

            _exits.Clear();
            _projectiles.DrainAreaExitEvents(_exits);
            if (_exits.Count > 0) _lifecycle?.RecordExitEvents(MobaTemporaryEntityKind.Area, _exits.Count);
            for (int i = 0; i < _exits.Count; i++)
            {
                var evt = _exits[i];
                var hitActorId = ResolveActorIdByCollider(evt.Collider);

                var info = RequireAreaInfo(evt.Area);
                PublishAreaEvent("area.exit", evt.Area.Value, info.TemplateId, MobaTraceKind.AreaExit, evt, in info, ownerActorId: evt.OwnerId, targetActorId: hitActorId, frame: evt.Frame, center: info.Center, radius: info.Radius, collider: evt.Collider, info.CollisionLayerMask, info.MaxTargets);
            }

            _expires.Clear();
            _projectiles.DrainAreaExpireEvents(_expires);
            if (_expires.Count > 0) _lifecycle?.RecordExpireEvents(MobaTemporaryEntityKind.Area, _expires.Count);
            for (int i = 0; i < _expires.Count; i++)
            {
                var evt = _expires[i];

                var info = RequireAreaInfo(evt.Area);
                PublishAreaEvent("area.expire", evt.Area.Value, info.TemplateId, MobaTraceKind.AreaExpire, evt, in info, ownerActorId: evt.OwnerId, targetActorId: 0, frame: evt.Frame, center: info.Center, radius: info.Radius, collider: default, info.CollisionLayerMask, info.MaxTargets);

                _areaRuntime.Unregister(evt.Area);
            }
        }

        private void PublishAreaEvent(string eventId, int areaId, int templateId, MobaTraceKind traceKind, object raw, in MobaAreaRuntimeInfo info, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider, int collisionLayerMask, int maxTargets)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(eventId)) return;

            var eid = TriggeringIdUtil.GetEventEid(eventId);
            var payload = new AreaEventArgs
            {
                EventId = eventId,
                AreaId = areaId,
                TemplateId = templateId,
                OwnerActorId = ownerActorId,
                TargetActorId = targetActorId,
                Frame = frame,
                TraceKind = traceKind != MobaTraceKind.None ? traceKind : MobaTraceKind.AreaSpawn,
                Center = center,
                Radius = radius,
                Collider = collider,
                CollisionLayerMask = collisionLayerMask,
                MaxTargets = maxTargets,
                SourceContextId = info.SourceContextId,
                RootContextId = info.RootContextId,
                OwnerContextId = info.OwnerContextId,
                Raw = raw,
            };

            _eventBus.Publish(new EventKey<AreaEventArgs>(eid), in payload);
            object boxed = payload;
            _eventBus.Publish(new EventKey<object>(eid), in boxed);
        }

        private MobaAreaRuntimeInfo RequireAreaInfo(AreaId areaId)
        {
            if (_areaRuntime == null)
            {
                throw new InvalidOperationException("MobaAreaSyncSystem requires MobaAreaRuntimeService for area event metadata.");
            }

            if (!_areaRuntime.TryGetArea(areaId.Value, out var info))
            {
                throw new InvalidOperationException($"Area event missing runtime metadata. areaId={areaId.Value}");
            }

            if (info.TemplateId <= 0)
            {
                throw new InvalidOperationException($"Area event missing template id. areaId={areaId.Value}");
            }

            return info;
        }

        private int ResolveActorIdByCollider(ColliderId id)
        {
            if (_registry == null) return 0;
            if (id.Value <= 0) return 0;

            foreach (var kv in _registry.Entries)
            {
                var e = kv.Value;
                if (e == null || !e.hasActorId || !e.hasCollisionId) continue;
                if (e.collisionId.Value.Equals(id))
                {
                    return e.actorId.Value;
                }
            }

            return 0;
        }
    }
}

