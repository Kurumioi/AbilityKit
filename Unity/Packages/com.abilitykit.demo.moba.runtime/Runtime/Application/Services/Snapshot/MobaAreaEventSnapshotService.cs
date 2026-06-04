using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(50)]
    [WorldService(typeof(MobaAreaEventSnapshotService))]
    public sealed class MobaAreaEventSnapshotService : IService, IMobaSnapshotEmitter
    {
        private readonly MobaGamePhaseService _phase;
        private readonly IProjectileService _projectiles;
        private readonly MobaAreaTriggerRegistry _areaTriggers;

        private FrameIndex _lastFrame;

        private readonly List<AreaSpawnEvent> _spawns = new List<AreaSpawnEvent>(32);
        private readonly List<AreaExpireEvent> _expires = new List<AreaExpireEvent>(32);
        private readonly MobaSnapshotBuffer<MobaAreaEventSnapshotEntry> _areaEntries = new MobaSnapshotBuffer<MobaAreaEventSnapshotEntry>(32, 512);

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

            _areaEntries.Clear();

            for (int i = 0; i < _spawns.Count; i++)
            {
                var e = _spawns[i];
                var templateId = 0;
                if (_areaTriggers != null && _areaTriggers.TryGet(e.Area, out var entry))
                {
                    templateId = entry.TemplateId;
                }
                _areaEntries.Add(new MobaAreaEventSnapshotEntry((int)AreaEventKind.Spawn, e.Area.Value, e.OwnerId, templateId, e.Center.X, e.Center.Y, e.Center.Z, e.Radius));
            }

            for (int i = 0; i < _expires.Count; i++)
            {
                var e = _expires[i];
                _areaEntries.Add(new MobaAreaEventSnapshotEntry((int)AreaEventKind.Expire, e.Area.Value, e.OwnerId, 0, 0f, 0f, 0f, 0f));
            }

            var payload = MobaAreaEventSnapshotCodec.Serialize(_areaEntries.ToArrayClearAndTrim());
            snapshot = new WorldStateSnapshot(AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.AreaEvent, payload);
            return true;
        }

        public void Dispose()
        {
            _spawns.Clear();
            _expires.Clear();
            _areaEntries.ClearAndTrim();
            _lastFrame = new FrameIndex(-999999);
        }
    }
}
