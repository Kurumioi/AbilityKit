using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Numbers;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.Triggering;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaSnapshotRouter : IWorldStateSnapshotProvider
    {
        private readonly MobaEnterGameSnapshotService _enter;
        private readonly MobaActorSpawnSnapshotService _spawn;
        private readonly MobaActorDespawnSnapshotService _despawn;
        private readonly MobaProjectileEventSnapshotService _projectileEvents;
        private readonly MobaAreaEventSnapshotService _areaEvents;
        private readonly MobaDamageEventSnapshotService _damageEvents;
        private readonly MobaActorTransformSnapshotService _transform;
        private readonly MobaStateHashSnapshotService _hash;

        public MobaSnapshotRouter(MobaEnterGameSnapshotService enter, MobaActorSpawnSnapshotService spawn, MobaActorDespawnSnapshotService despawn, MobaProjectileEventSnapshotService projectileEvents, MobaAreaEventSnapshotService areaEvents, MobaDamageEventSnapshotService damageEvents, MobaActorTransformSnapshotService transform, MobaStateHashSnapshotService hash)
        {
            _enter = enter ?? throw new ArgumentNullException(nameof(enter));
            _spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            _despawn = despawn ?? throw new ArgumentNullException(nameof(despawn));
            _projectileEvents = projectileEvents ?? throw new ArgumentNullException(nameof(projectileEvents));
            _areaEvents = areaEvents ?? throw new ArgumentNullException(nameof(areaEvents));
            _damageEvents = damageEvents ?? throw new ArgumentNullException(nameof(damageEvents));
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (_enter.TryGetSnapshot(frame, out snapshot)) return true;
            if (_spawn.TryGetSnapshot(frame, out snapshot)) return true;
            if (_despawn.TryGetSnapshot(frame, out snapshot)) return true;
            if (_projectileEvents.TryGetSnapshot(frame, out snapshot)) return true;
            if (_areaEvents.TryGetSnapshot(frame, out snapshot)) return true;
            if (_damageEvents.TryGetSnapshot(frame, out snapshot)) return true;
            if (_hash.TryGetSnapshot(frame, out snapshot)) return true;
            if (_transform.TryGetSnapshot(frame, out snapshot)) return true;
            snapshot = default;
            return false;
        }

        public void Dispose()
        {
        }
    }

    public sealed class MobaProjectileEventSnapshotService : IService
    {
        private readonly MobaGamePhaseService _phase;
        private readonly IProjectileService _projectiles;
        private readonly MobaProjectileLinkService _links;

        private FrameIndex _lastFrame;

        private readonly List<ProjectileSpawnEvent> _spawns = new List<ProjectileSpawnEvent>(32);
        private readonly List<ProjectileHitEvent> _hits = new List<ProjectileHitEvent>(32);
        private readonly List<ProjectileExitEvent> _exits = new List<ProjectileExitEvent>(32);

        public MobaProjectileEventSnapshotService(MobaGamePhaseService phase, IProjectileService projectiles, MobaProjectileLinkService links)
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

            if (_projectiles is AbilityKit.Core.Common.Projectile.ProjectileService ps)
            {
                ps.PeekSpawnEvents(_spawns);
                ps.PeekHitEvents(_hits);
                ps.PeekExitEvents(_exits);
            }
            else
            {
                _projectiles.DrainSpawnEvents(_spawns);
                _projectiles.DrainHitEvents(_hits);
                _projectiles.DrainExitEvents(_exits);
            }

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

    public sealed class MobaAreaEventSnapshotService : IService
    {
        private readonly MobaGamePhaseService _phase;
        private readonly IProjectileService _projectiles;
        private readonly MobaAreaTriggerRegistry _areaTriggers;

        private FrameIndex _lastFrame;

        private readonly List<AreaSpawnEvent> _spawns = new List<AreaSpawnEvent>(32);
        private readonly List<AreaExpireEvent> _expires = new List<AreaExpireEvent>(32);

        public MobaAreaEventSnapshotService(MobaGamePhaseService phase, IProjectileService projectiles, MobaAreaTriggerRegistry areaTriggers)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _areaTriggers = areaTriggers;
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
            _expires.Clear();

            if (_projectiles is AbilityKit.Core.Common.Projectile.ProjectileService ps)
            {
                ps.PeekAreaSpawnEvents(_spawns);
                ps.PeekAreaExpireEvents(_expires);
            }
            else
            {
                _projectiles.DrainAreaSpawnEvents(_spawns);
                _projectiles.DrainAreaExpireEvents(_expires);
            }

            if (_spawns.Count == 0 && _expires.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var entries = new List<MobaAreaEventSnapshotEntry>(_spawns.Count + _expires.Count);

            for (int i = 0; i < _spawns.Count; i++)
            {
                var e = _spawns[i];
                var templateId = 0;
                if (_areaTriggers != null && _areaTriggers.TryGet(e.Area, out var entry))
                {
                    templateId = entry.TemplateId;
                }
                entries.Add(new MobaAreaEventSnapshotEntry
                {
                    Kind = (int)AreaEventKind.Spawn,
                    AreaId = e.Area.Value,
                    OwnerActorId = e.OwnerId,
                    TemplateId = templateId,
                    X = e.Center.X,
                    Y = e.Center.Y,
                    Z = e.Center.Z,
                    Radius = e.Radius
                });
            }

            for (int i = 0; i < _expires.Count; i++)
            {
                var e = _expires[i];
                entries.Add(new MobaAreaEventSnapshotEntry
                {
                    Kind = (int)AreaEventKind.Expire,
                    AreaId = e.Area.Value,
                    OwnerActorId = e.OwnerId,
                    TemplateId = 0,
                    X = 0f,
                    Y = 0f,
                    Z = 0f,
                    Radius = 0f
                });
            }

            var payload = MobaAreaEventSnapshotCodec.Serialize(entries.ToArray());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.AreaEventSnapshot, payload);
            return true;
        }

        public void Dispose()
        {
        }
    }

    public sealed class MobaActorDespawnSnapshotService : IService
    {
        private readonly MobaGamePhaseService _phase;
        private FrameIndex _lastFrame;
        private readonly List<MobaActorDespawnSnapshotEntry> _pending = new List<MobaActorDespawnSnapshotEntry>(64);

        public MobaActorDespawnSnapshotService(MobaGamePhaseService phase)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _lastFrame = new FrameIndex(-999999);
        }

        public void Enqueue(int actorId, byte reason = 0)
        {
            if (actorId <= 0) return;
            _pending.Add(new MobaActorDespawnSnapshotEntry { ActorId = actorId, Reason = reason });
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

            if (_pending.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var payload = MobaActorDespawnSnapshotCodec.Serialize(_pending.ToArray());
            _pending.Clear();
            snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorDespawnSnapshot, payload);
            return true;
        }

        public void Dispose()
        {
            _pending.Clear();
        }
    }
}
