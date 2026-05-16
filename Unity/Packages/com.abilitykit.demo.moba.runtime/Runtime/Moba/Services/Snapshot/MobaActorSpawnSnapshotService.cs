using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaActorSpawnSnapshotService : IWorldStateSnapshotProvider
    {
        private bool _hasSnapshot;
        private bool _sent;
        private byte[] _snapshotPayload;

        private FrameIndex _lastFrame;
        private readonly List<MobaActorSpawnSnapshotEntry> _pending = new List<MobaActorSpawnSnapshotEntry>(64);

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
                snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorSpawnSnapshot, _snapshotPayload);
                _sent = true;
                return true;
            }

            if (_pending.Count > 0)
            {
                try
                {
                    var payload = MobaActorSpawnSnapshotCodec.Serialize(_pending.ToArray());
                    _pending.Clear();
                    snapshot = new WorldStateSnapshot((int)MobaOpCode.ActorSpawnSnapshot, payload);
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
