using System;
using System.Collections.Generic;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Eventing;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Mathematics;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Demo.Moba.Services.Triggering;
using AbilityKit.Triggering.Eventing;

namespace AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering
{
    public interface IMobaStageTriggerService
    {
        void ExecuteAreaStage(string eventId, int areaId, int templateId, object raw, in MobaAreaRuntimeInfo info, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider, int collisionLayerMask, int maxTargets);
        void ExecuteProjectileSpawn(in ProjectileSpawnEvent evt);
        void ExecuteProjectileTick(in ProjectileTickEvent evt);
        void ExecuteProjectileExit(in ProjectileExitEvent evt);
        void ExecuteProjectileHit(in ProjectileHitEvent evt, int hitActorId);
    }

    [WorldService(typeof(MobaStageTriggerService))]
    public sealed class MobaStageTriggerService : IService, IMobaStageTriggerService, IDisposable
    {
        [WorldInject(required: false)] private MobaConfigDatabase _configs = null;
        [WorldInject(required: false)] private MobaTriggerExecutionGateway _triggers = null;
        [WorldInject(required: false)] private MobaProjectileLinkService _projectileLinks = null;

        public void ExecuteAreaStage(string eventId, int areaId, int templateId, object raw, in MobaAreaRuntimeInfo info, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider, int collisionLayerMask, int maxTargets)
        {
            if (_triggers == null || _configs == null) return;
            if (templateId <= 0 || string.IsNullOrEmpty(eventId)) return;
            if (!_configs.TryGetAoe(templateId, out var aoe) || aoe == null) return;

            var triggerIds = ResolveAreaTriggerIds(eventId, aoe);
            if (triggerIds == null || triggerIds.Length == 0) return;

            var payload = new AreaEventArgs
            {
                EventId = eventId,
                AreaId = areaId,
                TemplateId = templateId,
                OwnerActorId = ownerActorId,
                TargetActorId = targetActorId,
                Frame = frame,
                SourceContextId = info.SourceContextId,
                RootContextId = info.RootContextId,
                OwnerContextId = info.OwnerContextId,
                TraceKind = ResolveAreaTraceKind(eventId),
                Center = center,
                Radius = radius,
                Collider = collider,
                CollisionLayerMask = collisionLayerMask,
                MaxTargets = maxTargets,
                Raw = raw,
            };

            for (var i = 0; i < triggerIds.Length; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;
                var request = MobaTriggerExecutionRequest<AreaEventArgs>.Create(triggerId, payload, eventId);
                _triggers.ExecuteDirectTrigger(in request);
            }
        }

        public void ExecuteProjectileSpawn(in ProjectileSpawnEvent evt)
        {
            // omitted
        }

        public void ExecuteProjectileTick(in ProjectileTickEvent evt)
        {
            // omitted
        }

        public void ExecuteProjectileExit(in ProjectileExitEvent evt)
        {
            // omitted
        }

        public void ExecuteProjectileHit(in ProjectileHitEvent evt, int hitActorId)
        {
            if (_triggers == null || _configs == null || _projectileLinks == null) return;
            if (evt.TemplateId <= 0 || hitActorId <= 0) return;
            if (!_configs.TryGetProjectile(evt.TemplateId, out var projectile) || projectile == null) return;
            if (projectile.OnHitEffectId <= 0) return;
            if (!_projectileLinks.TryGetSource(evt.Projectile, out var sourceContext)) return;

            var payload = new ProjectileHitArgs
            {
                SourceActorId = sourceContext.SourceActorId > 0 ? sourceContext.SourceActorId : evt.OwnerId,
                TargetActorId = hitActorId,
                SourceConfigId = projectile.Id,
                Frame = evt.Frame,
                Raw = evt,
                SourceContext = sourceContext,
                CasterActorId = sourceContext.SourceActorId > 0 ? sourceContext.SourceActorId : evt.OwnerId,
                ProjectileTemplateId = projectile.Id,
                ProjectileId = evt.Projectile,
                Point = evt.Point,
                Normal = evt.Normal,
                HitCollider = evt.HitCollider,
            };

            var request = MobaTriggerExecutionRequest<ProjectileHitArgs>.Create(projectile.OnHitEffectId, payload, "projectile.hit");
            _triggers.ExecuteDirectTrigger(in request);
        }

        public void Dispose()
        {
        }

        private static int[] ResolveAreaTriggerIds(string eventId, AoeMO aoe)
        {
            if (aoe == null || string.IsNullOrEmpty(eventId)) return Array.Empty<int>();

            switch (eventId)
            {
                case "area.spawn":
                    return aoe.OnDelayTriggerIds;
                case "area.enter":
                    return aoe.OnEnterTriggerIds;
                case "area.exit":
                    return aoe.OnExitTriggerIds;
                case "area.tick":
                case "area.stay":
                    return aoe.OnIntervalTriggerIds;
                default:
                    return Array.Empty<int>();
            }
        }

        private static MobaTraceKind ResolveAreaTraceKind(string eventId)
        {
            switch (eventId)
            {
                case "area.spawn":
                    return MobaTraceKind.AreaSpawn;
                case "area.enter":
                    return MobaTraceKind.AreaEnter;
                case "area.exit":
                    return MobaTraceKind.AreaExit;
                case "area.tick":
                case "area.stay":
                    return MobaTraceKind.AreaStay;
                case "area.expire":
                    return MobaTraceKind.AreaExpire;
                default:
                    return MobaTraceKind.None;
            }
        }
    }
}
