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
using AbilityKit.Demo.Moba.Config.Core;
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

        public void ExecuteAreaStage(string eventId, int areaId, int templateId, object raw, in MobaAreaRuntimeInfo info, int ownerActorId, int targetActorId, int frame, in Vec3 center, float radius, ColliderId collider, int collisionLayerMask, int maxTargets)
        {
            // omitted
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
            // omitted
        }

        public void Dispose()
        {
        }
    }
}
