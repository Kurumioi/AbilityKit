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
                    object boxed = evt;
                    eventBus.Publish(new EventKey<object>(eid), in boxed);
                }

                var effects = _sys.Effects;
                var cfgs = _sys.Configs;
                if (effects != null && cfgs != null)
                {
                    try
                    {
                        var proj = cfgs.GetProjectile(evt.TemplateId);
                        var onHitTriggerId = proj != null ? proj.OnHitEffectId : 0;
                        if (onHitTriggerId > 0)
                        {
                            _sys.Links.TryGetSource(evt.Projectile, out var sourceContext);
                            var sourceActorId = sourceContext.IsValid && sourceContext.SourceActorId > 0 ? sourceContext.SourceActorId : evt.OwnerId;
                            var sourceContextId = sourceContext.IsValid ? sourceContext.SourceContextId : 0L;
                            var payload = new ProjectileHitArgs
                            {
                                TriggerId = onHitTriggerId,
                                SourceActorId = sourceActorId,
                                TargetActorId = hitActorId,
                                SourceContextId = sourceContextId,
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

                            payload.Data.SyncInvocationData(payload);
                            if (payload.TryGetTraceContext(out var traceContext)) payload.Data.SyncTraceData(traceContext);
                            payload.Data.SetData(AbilityContextKeys.ProjectileId.ToKeyString(), evt.Projectile.Value);
                            payload.Data.SetData(AbilityContextKeys.HitTriggerPlanId.ToKeyString(), onHitTriggerId);
                            payload.Data.SetData(AbilityContextKeys.HitPosition.ToKeyString(), evt.Point);
                            payload.Data.SetData(AbilityContextKeys.HitNormal.ToKeyString(), evt.Normal);
                            payload.Data.SetData(AbilityContextKeys.Frame.ToKeyString(), evt.Frame);
                            payload.Data.SetData("projectile.templateId", evt.TemplateId);
                            payload.Data.SetData("projectile.hitCollider", evt.HitCollider);

                            effects.ExecuteTriggerId(onHitTriggerId, payload);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Exception(ex, "[MobaProjectileHitSyncHandler] Execute projectile OnHitEffectId failed");
                    }
                }
            }
        }

        public void HandleSpawns(List<ProjectileSpawnEvent> spawns) { }
        public void HandleTicks(List<ProjectileTickEvent> ticks) { }
        public void HandleExits(List<ProjectileExitEvent> exits) { }
    }
}
