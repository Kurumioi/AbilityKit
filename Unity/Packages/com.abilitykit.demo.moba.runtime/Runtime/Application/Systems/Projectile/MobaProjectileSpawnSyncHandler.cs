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
    }
}
