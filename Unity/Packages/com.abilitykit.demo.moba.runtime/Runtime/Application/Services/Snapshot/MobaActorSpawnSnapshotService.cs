using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(20)]
    [WorldService(typeof(MobaActorSpawnSnapshotService))]
    public sealed class MobaActorSpawnSnapshotService : IWorldStateSnapshotProvider, IMobaSnapshotEmitter
    {
        private bool _hasSnapshot;
        private bool _sent;
        private byte[] _snapshotPayload;

        private FrameIndex _lastFrame;
        private readonly MobaSnapshotBuffer<MobaActorSpawnSnapshotEntry> _pending = new MobaSnapshotBuffer<MobaActorSpawnSnapshotEntry>(64, 512);

        public MobaActorSpawnSnapshotService()
        {
            _lastFrame = new FrameIndex(-999999);
        }

        public void PublishSpawnPayload(byte[] payload)
        {
            _snapshotPayload = payload;
            _hasSnapshot = payload != null && payload.Length > 0;
            _sent = false;
        }

        public void Enqueue(in MobaActorSpawnSnapshotEntry entry)
        {
            if (entry.NetId <= 0) return;
            _pending.Add(entry);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (frame.Value == _lastFrame.Value)
            {
                snapshot = default;
                return false;
            }
            _lastFrame = frame;

            if (_hasSnapshot && !_sent)
            {
                snapshot = new WorldStateSnapshot(AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.ActorSpawn, _snapshotPayload);
                _sent = true;
                return true;
            }

            if (_pending.Count > 0)
            {
                try
                {
                    var payload = MobaActorSpawnSnapshotCodec.Serialize(_pending.ToArrayClearAndTrim());
                    snapshot = new WorldStateSnapshot(AbilityKit.Protocol.Moba.MobaOpCodes.Snapshot.ActorSpawn, payload);
                    return true;
                }
                catch
                {
                }
            }

            snapshot = default;
            return false;
        }

        public void Dispose()
        {
            _hasSnapshot = false;
            _sent = false;
            _snapshotPayload = null;
            _pending.Clear();
            _lastFrame = new FrameIndex(-999999);
        }
    }
}
