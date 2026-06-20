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
                if (!_sys.Links.TryGetActorId(evt.Projectile, out var actorId) || actorId <= 0) continue;
            }

            exits.Clear();
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
