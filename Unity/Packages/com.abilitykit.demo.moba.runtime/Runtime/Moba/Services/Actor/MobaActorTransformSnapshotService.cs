using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaActorTransformSnapshotService : IService
    {
        private readonly MobaGamePhaseService _phase;
        private readonly MobaActorRegistry _registry;
        private FrameIndex _lastFrame;

        public MobaActorTransformSnapshotService(MobaGamePhaseService phase, MobaActorRegistry registry)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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

            var entries = BuildEntries();
            if (entries.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var payload = MobaActorTransformSnapshotCodec.Serialize(entries.ToArray());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorTransformSnapshot, payload);
            return true;
        }

        private List<MobaActorTransformSnapshotEntry> BuildEntries()
        {
            var tmp = new List<MobaActorTransformSnapshotEntry>(8);

            foreach (var kv in _registry.Entries)
            {
                var id = kv.Key;
                var e = kv.Value;
                if (e == null) continue;
                if (!e.hasTransform) continue;
                var p = e.transform.Value.Position;
                tmp.Add(new MobaActorTransformSnapshotEntry
                {
                    ActorId = id,
                    X = p.X,
                    Y = p.Y,
                    Z = p.Z
                });
            }

            return tmp;
        }

        public void Dispose()
        {
        }
    }
}
