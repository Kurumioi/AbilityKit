using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(30)]
    [WorldService(typeof(MobaActorDespawnSnapshotService))]
    public sealed class MobaActorDespawnSnapshotService : IService, IMobaSnapshotEmitter
    {
        private readonly MobaGamePhaseService _phase;
        private FrameIndex _lastFrame;
        private readonly MobaSnapshotBuffer<MobaActorDespawnSnapshotEntry> _pending = new MobaSnapshotBuffer<MobaActorDespawnSnapshotEntry>(64, 512);

        public MobaActorDespawnSnapshotService(MobaGamePhaseService phase)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _lastFrame = new FrameIndex(-999999);
        }

        public void Enqueue(int actorId, byte reason = 0)
        {
            if (actorId <= 0) return;
            _pending.Add(new MobaActorDespawnSnapshotEntry(actorId, reason));
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

            var payload = MobaActorDespawnSnapshotCodec.Serialize(_pending.ToArrayClearAndTrim());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorDespawnSnapshot, payload);
            return true;
        }

        public void Dispose()
        {
            _pending.ClearAndTrim();
            _lastFrame = new FrameIndex(-999999);
        }
    }
}
