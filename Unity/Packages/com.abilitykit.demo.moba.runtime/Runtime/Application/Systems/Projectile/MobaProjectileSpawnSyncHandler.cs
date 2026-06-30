using System;
using System.Collections.Generic;
using AbilityKit.Combat.Projectile;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;

namespace AbilityKit.Demo.Moba.Runtime.Application.Systems.Projectile
{
    internal sealed class MobaProjectileSpawnSyncHandler : IProjectileSyncHandler
    {
        private readonly MobaProjectileSyncSystem _sys;

        public MobaProjectileSpawnSyncHandler(MobaProjectileSyncSystem sys)
        {
            _sys = sys;
        }

        public void HandleSpawns(List<ProjectileSpawnEvent> spawns)
        {
            var count = spawns.Count;
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var evt = spawns[i];
                BindProjectileSource(evt);
                IncrementLauncherActiveBullets(evt.LauncherActorId);
                _sys.StageTriggers?.ExecuteProjectileSpawn(evt);
            }

            spawns.Clear();
        }

        public void HandleTicks(List<ProjectileTickEvent> ticks)
        {
        }

        public void HandleExits(List<ProjectileExitEvent> exits)
        {
        }

        public void HandleHits(List<ProjectileHitEvent> hits)
        {
        }

        private void BindProjectileSource(in ProjectileSpawnEvent evt)
        {
            var links = _sys.Links;
            if (links == null) return;

            if (links.TryGetSource(evt.Projectile, out _))
            {
                return;
            }

            if (evt.LauncherActorId <= 0)
            {
                return;
            }

            if (!links.TryGetLauncherSource(evt.LauncherActorId, out var launcherSource))
            {
                return;
            }

            links.BindSource(evt.Projectile, in launcherSource);
        }

        private void IncrementLauncherActiveBullets(int launcherActorId)
        {
            if (launcherActorId <= 0) return;
            if (_sys.Registry == null || !_sys.Registry.TryGet(launcherActorId, out var launcherEntity) || launcherEntity == null) return;
            if (!launcherEntity.hasProjectileLauncher) return;

            var plc = launcherEntity.projectileLauncher;
            var nextActiveBullets = Math.Max(0, plc.ActiveBullets) + 1;
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
    }
}
