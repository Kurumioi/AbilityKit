using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services.Projectile;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaSnapshotRouter))]
    [WorldService(typeof(IWorldStateSnapshotProvider))]
    public sealed class MobaSnapshotRouter : IWorldStateSnapshotProvider, IMobaSnapshotBatchProvider, IWorldInitializable
    {
        private readonly MobaEnterGameSnapshotService _enter;
        private readonly MobaActorSpawnSnapshotService _spawn;
        private readonly MobaActorDespawnSnapshotService _despawn;
        private readonly MobaProjectileEventSnapshotService _projectileEvents;
        private readonly MobaAreaEventSnapshotService _areaEvents;
        private readonly MobaDamageEventSnapshotService _damageEvents;
        private readonly MobaActorTransformSnapshotService _transform;
        private readonly MobaStateHashSnapshotService _hash;
        private List<IMobaSnapshotEmitter> _emitters;

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
            _emitters = new List<IMobaSnapshotEmitter>(8);
            AddFallbackEmitters();
        }

        public void OnInit(IWorldResolver services)
        {
            var registry = MobaSnapshotEmitterRegistry.CreateDefault();
            var resolved = registry.ResolveEmitters(services);
            if (resolved.Count > 0)
            {
                _emitters = resolved;
            }
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            for (int i = 0; i < _emitters.Count; i++)
            {
                if (_emitters[i].TryGetSnapshot(frame, out snapshot)) return true;
            }

            snapshot = default;
            return false;
        }

        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            if (snapshots == null || maxSnapshots <= 0) return 0;

            int count = 0;
            for (int i = 0; i < _emitters.Count && count < maxSnapshots; i++)
            {
                if (!_emitters[i].TryGetSnapshot(frame, out WorldStateSnapshot snapshot)) continue;

                snapshots.Add(snapshot);
                count++;
            }

            return count;
        }

        private void AddFallbackEmitters()
        {
            _emitters.Add(_enter);
            _emitters.Add(_spawn);
            _emitters.Add(_despawn);
            _emitters.Add(_projectileEvents);
            _emitters.Add(_areaEvents);
            _emitters.Add(_damageEvents);
            _emitters.Add(_hash);
            _emitters.Add(_transform);
        }

        public void Dispose()
        {
        }
    }
}
