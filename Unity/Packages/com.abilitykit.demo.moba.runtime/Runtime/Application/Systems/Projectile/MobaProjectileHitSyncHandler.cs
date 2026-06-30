using System;
using System.Collections.Generic;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;
using AbilityKit.Combat.Projectile;

namespace AbilityKit.Demo.Moba.Runtime.Application.Systems.Projectile
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
            var count = hits.Count;
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.Projectile.Value <= 0)
                {
                    Log.Warning("[MobaProjectileHitSyncHandler] Projectile id is invalid when processing hit event.");
                    continue;
                }

                DecrementLauncherActiveBullets(hit.LauncherActorId);
                var hitActorId = _sys.ResolveActorIdByCollider(hit.HitCollider);
                if (hitActorId <= 0) continue;

                _sys.StageTriggers?.ExecuteProjectileHit(hit, hitActorId);
            }

            hits.Clear();
        }

        private void DecrementLauncherActiveBullets(int launcherActorId)
        {
            if (launcherActorId <= 0) return;
            if (_sys.Registry == null || !_sys.Registry.TryGet(launcherActorId, out var launcherEntity) || launcherEntity == null) return;
            if (!launcherEntity.hasProjectileLauncher) return;

            var plc = launcherEntity.projectileLauncher;
            var nextActiveBullets = Math.Max(0, plc.ActiveBullets - 1);
            if (nextActiveBullets == plc.ActiveBullets) return;

            launcherEntity.ReplaceProjectileLauncher(
                newLauncherId: plc.LauncherId,
                newProjectileId: plc.ProjectileId,
                newRootActorId: plc.RootActorId,
                newEndTimeMs: plc.EndTimeMs,
                newActiveBullets: nextActiveBullets,
                newScheduleId: plc.ScheduleId,
                newIntervalFrames: plc.IntervalFrames,
                newTotalCount: plc.TotalCount);
        }

        public void HandleSpawns(List<ProjectileSpawnEvent> spawns)
        {
        }

        public void HandleTicks(List<ProjectileTickEvent> ticks)
        {
        }

        public void HandleExits(List<ProjectileExitEvent> exits)
        {
        }
    }
}
