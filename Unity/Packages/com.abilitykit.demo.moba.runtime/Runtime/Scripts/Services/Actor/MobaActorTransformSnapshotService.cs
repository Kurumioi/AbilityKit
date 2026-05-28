using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(80)]
    [WorldService(typeof(MobaActorTransformSnapshotService))]
    public sealed class MobaActorTransformSnapshotService : IService, IMobaSnapshotEmitter
    {
        private readonly MobaGamePhaseService _phase;
        private readonly MobaActorRegistry _registry;
        private readonly MobaSnapshotBuffer<MobaActorTransformSnapshotEntry> _entries = new MobaSnapshotBuffer<MobaActorTransformSnapshotEntry>(8, 256);
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

            BuildEntries();
            if (_entries.Count == 0)
            {
                snapshot = default;
                return false;
            }

            var payload = MobaActorTransformSnapshotCodec.Serialize(_entries.ToArrayClearAndTrim());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorTransformSnapshot, payload);
            return true;
        }

        private void BuildEntries()
        {
            _entries.Clear();

            foreach (var kv in _registry.Entries)
            {
                var id = kv.Key;
                var e = kv.Value;
                if (e == null) continue;
                if (!e.hasTransform) continue;
                var p = e.transform.Value.Position;
                _entries.Add(new MobaActorTransformSnapshotEntry(id, p.X, p.Y, p.Z));
            }
        }

        public void Dispose()
        {
            _entries.Clear();
            _lastFrame = new FrameIndex(-999999);
        }
    }
}
