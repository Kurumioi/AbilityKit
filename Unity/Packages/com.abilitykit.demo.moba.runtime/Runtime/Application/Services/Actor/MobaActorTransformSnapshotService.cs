using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Services.Snapshot;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(80)]
    [WorldService(typeof(MobaActorTransformSnapshotService))]
    public sealed class MobaActorTransformSnapshotService : LogicWorldSnapshotBufferEmitterBase<MobaActorTransformSnapshotService, MobaActorTransformSnapshotEntry>
    {
        private readonly MobaLogicWorldRunGateService _phase;
        private readonly MobaActorRegistry _registry;
        public MobaActorTransformSnapshotService(MobaLogicWorldRunGateService phase, MobaActorRegistry registry) : base(8, 256)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        protected override bool CanEmit(FrameIndex frame)
        {
            return _phase.InGame;
        }

        protected override bool TryBuildSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            BuildEntries();
            if (Count == 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = CreateSnapshot(ToArrayClearAndTrim());
            return true;
        }

        protected override WorldStateSnapshot CreateSnapshot(MobaActorTransformSnapshotEntry[] entries)
        {
            var payload = MobaActorTransformSnapshotCodec.Serialize(entries);
            return new WorldStateSnapshot(AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.ActorTransform, payload);
        }

        private void BuildEntries()
        {
            Clear();

            foreach (var kv in _registry.Entries)
            {
                var id = kv.Key;
                var e = kv.Value;
                if (e == null) continue;
                if (!e.hasTransform) continue;
                var p = e.transform.Value.Position;
                Add(new MobaActorTransformSnapshotEntry(id, p.X, p.Y, p.Z));
            }
        }

    }
}
