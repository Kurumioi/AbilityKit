using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    public sealed class MobaProjectileEventSnapshotService : IService
    {
        private readonly AbilityKit.Demo.Moba.Services.MobaGamePhaseService _phase;
        private readonly IProjectileService _projectiles;
        private readonly MobaProjectileLinkService _links;

        private FrameIndex _lastFrame;

        private readonly List<ProjectileSpawnEvent> _spawns = new List<ProjectileSpawnEvent>(32);
        private readonly List<ProjectileHitEvent> _hits = new List<ProjectileHitEvent>(32);
        private readonly List<ProjectileExitEvent> _exits = new List<ProjectileExitEvent>(32);

        public MobaProjectileEventSnapshotService(AbilityKit.Demo.Moba.Services.MobaGamePhaseService phase, IProjectileService projectiles, MobaProjectileLinkService links)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _links = links;
            _lastFrame = new FrameIndex(-999999);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (!_phase.InGame)
            {
                snapshot = default;
                return false;
            }

            if (frame.Value == _lastFrame.Value)
            {
                snapshot = default;
                return false;
            }
            _lastFrame = frame;

            _spawns.Clear();
            _hits.Clear();
            _exits.Clear();

            _projectiles.DrainSpawnEvents(_spawns);
            _projectiles.DrainHitEvents(_hits);
            _projectiles.DrainExitEvents(_exits);

            if (_spawns.Count == 0 && _hits.Count == 0 && _exits.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var entries = new List<MobaProjectileEventSnapshotEntry>(_spawns.Count + _hits.Count + _exits.Count);

            for (int i = 0; i < _spawns.Count; i++)
            {
                var e = _spawns[i];
                var it = FromSpawn(in e);
                if (_links != null && _links.TryGetActorId(e.Projectile, out var projectileActorId) && projectileActorId > 0)
                {
                    it.ProjectileActorId = projectileActorId;
                }
                entries.Add(it);
            }

            for (int i = 0; i < _hits.Count; i++)
            {
                var e = _hits[i];
                var it = FromHit(in e);
                if (_links != null && _links.TryGetActorId(e.Projectile, out var projectileActorId) && projectileActorId > 0)
                {
                    it.ProjectileActorId = projectileActorId;
                }
                entries.Add(it);
            }

            for (int i = 0; i < _exits.Count; i++)
            {
                var e = _exits[i];
                var it = FromExit(in e);
                if (_links != null && _links.TryGetActorId(e.Projectile, out var projectileActorId) && projectileActorId > 0)
                {
                    it.ProjectileActorId = projectileActorId;
                }
                entries.Add(it);
            }

            var payload = MobaProjectileEventSnapshotCodec.Serialize(entries.ToArray());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.ProjectileEventSnapshot, payload);
            return true;
        }

        private static MobaProjectileEventSnapshotEntry FromSpawn(in ProjectileSpawnEvent e)
        {
            return new MobaProjectileEventSnapshotEntry
            {
                Kind = (int)ProjectileEventKind.Spawn,
                ProjectileActorId = 0,
                OwnerActorId = e.OwnerId,
                TemplateId = e.TemplateId,
                LauncherActorId = e.LauncherActorId,
                RootActorId = e.RootActorId,
                X = e.Position.X,
                Y = e.Position.Y,
                Z = e.Position.Z,
                HitCollider = 0,
                ExitReason = 0
            };
        }

        private static MobaProjectileEventSnapshotEntry FromHit(in ProjectileHitEvent e)
        {
            return new MobaProjectileEventSnapshotEntry
            {
                Kind = (int)ProjectileEventKind.Hit,
                ProjectileActorId = 0,
                OwnerActorId = e.OwnerId,
                TemplateId = e.TemplateId,
                LauncherActorId = e.LauncherActorId,
                RootActorId = e.RootActorId,
                X = e.Point.X,
                Y = e.Point.Y,
                Z = e.Point.Z,
                HitCollider = e.HitCollider.Value,
                ExitReason = 0
            };
        }

        private static MobaProjectileEventSnapshotEntry FromExit(in ProjectileExitEvent e)
        {
            return new MobaProjectileEventSnapshotEntry
            {
                Kind = (int)ProjectileEventKind.Exit,
                ProjectileActorId = 0,
                OwnerActorId = e.OwnerId,
                TemplateId = e.TemplateId,
                LauncherActorId = e.LauncherActorId,
                RootActorId = e.RootActorId,
                X = e.Position.X,
                Y = e.Position.Y,
                Z = e.Position.Z,
                HitCollider = 0,
                ExitReason = (int)e.Reason
            };
        }

        public void Dispose()
        {
        }
    }
}
