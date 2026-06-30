using System;
using System.Collections.Generic;
using AbilityKit.Combat.Projectile;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;

namespace AbilityKit.Demo.Moba.Runtime.Application.Systems.Projectile
{
    internal sealed class MobaProjectileExitSyncHandler : IProjectileSyncHandler
    {
        private readonly MobaProjectileSyncSystem _sys;

        public MobaProjectileExitSyncHandler(MobaProjectileSyncSystem sys)
        {
            _sys = sys;
        }

        public void HandleExits(List<ProjectileExitEvent> exits)
        {
            var count = exits.Count;
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var evt = exits[i];
                _sys.StageTriggers?.ExecuteProjectileExit(evt);
                DecrementLauncherActiveBullets(evt.LauncherActorId);
                if (!_sys.Links.TryGetActorId(evt.Projectile, out var actorId) || actorId <= 0) continue;
            }

            exits.Clear();
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

        public void HandleHits(List<ProjectileHitEvent> hits)
        {
        }
    }
}
