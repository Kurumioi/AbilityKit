using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Effect;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Core.Common.Event;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Projectile
{
    internal sealed class MobaProjectileHitSyncHandler : IProjectileSyncHandler
    {
        private readonly MobaProjectileSyncSystem _sys;

        public MobaProjectileHitSyncHandler(MobaProjectileSyncSystem sys)
        {
            _sys = sys;
        }

        public void HandleHits(List<ProjectileHitEvent> hits)
        {
            if (hits == null || hits.Count == 0) return;

            HashSet<(int Frame, int ProjectileId, int HitActorId)> hitActorOnce = null;
            if (hits.Count > 1)
            {
                hitActorOnce = new HashSet<(int, int, int)>();
            }
            for (int i = 0; i < hits.Count; i++)
            {
                var evt = hits[i];
                var hitActorId = _sys.ResolveActorIdByCollider(evt.HitCollider);

                if (hitActorOnce != null && hitActorId > 0 && !hitActorOnce.Add((evt.Frame, evt.Projectile.Value, hitActorId)))
                {
                    continue;
                }

                var eventBus = _sys.EventBus;
                if (eventBus != null)
                {
                    var eventId = ProjectileTriggering.Events.Hit;
                    var eid = AbilityKit.Demo.Moba.Services.TriggeringIdUtil.GetEventEid(eventId);

                    eventBus.Publish(new EventKey<ProjectileHitEvent>(eid), in evt);
                }

                var effects = _sys.Effects;
                var cfgs = _sys.Configs;
                try
                {
                    if (cfgs == null)
                    {
                        throw new System.InvalidOperationException($"Projectile hit requires MobaConfigDatabase. templateId={evt.TemplateId} projectileId={evt.Projectile.Value}");
                    }

                    var proj = cfgs.GetProjectile(evt.TemplateId);
                    if (proj == null)
                    {
                        throw new System.InvalidOperationException($"Projectile hit requires a valid projectile config. templateId={evt.TemplateId} projectileId={evt.Projectile.Value}");
                    }

                    var onHitTriggerId = proj.OnHitEffectId;
                    if (onHitTriggerId <= 0) continue;

                    if (effects == null)
                    {
                        throw new System.InvalidOperationException($"Projectile hit effect requires MobaEffectExecutionService. templateId={evt.TemplateId} projectileId={evt.Projectile.Value} triggerId={onHitTriggerId}");
                    }

                    if (_sys.Links == null || !_sys.Links.TryGetSource(evt.Projectile, out var sourceContext))
                    {
                        throw new System.InvalidOperationException($"Projectile hit effect requires a bound source context. templateId={evt.TemplateId} projectileId={evt.Projectile.Value} triggerId={onHitTriggerId}");
                    }

                    if (!sourceContext.IsValid || sourceContext.SourceActorId <= 0)
                    {
                        throw new System.InvalidOperationException($"Projectile hit effect source context is invalid. templateId={evt.TemplateId} projectileId={evt.Projectile.Value} triggerId={onHitTriggerId}");
                    }

                    var payload = new ProjectileHitArgs
                    {
                        TriggerId = onHitTriggerId,
                        SourceActorId = sourceContext.SourceActorId,
                        TargetActorId = hitActorId,
                        SourceContextId = sourceContext.SourceContextId,
                        SourceConfigId = evt.TemplateId,
                        SourceContext = sourceContext,
                        Frame = evt.Frame,
                        CasterActorId = evt.OwnerId,
                        ProjectileTemplateId = evt.TemplateId,
                        ProjectileId = evt.Projectile,
                        Point = evt.Point,
                        Normal = evt.Normal,
                        HitCollider = evt.HitCollider,
                        Raw = evt,
                    };

                    effects.ExecuteTriggerId(onHitTriggerId, payload);
                }
                catch (System.Exception ex)
                {
                    Log.Exception(ex, "[MobaProjectileHitSyncHandler] Execute projectile OnHitEffectId failed");
                    throw;
                }
            }
        }

        public void HandleSpawns(List<ProjectileSpawnEvent> spawns) { }
        public void HandleTicks(List<ProjectileTickEvent> ticks) { }
        public void HandleExits(List<ProjectileExitEvent> exits) { }
    }
}
