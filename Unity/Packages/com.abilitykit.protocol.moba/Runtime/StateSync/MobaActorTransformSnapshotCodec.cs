using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    [MemoryPackable]
    public partial struct MobaActorTransformSnapshotEntry
    {
        [MemoryPackOrder(0)] public int ActorId;
        [MemoryPackOrder(1)] public float X;
        [MemoryPackOrder(2)] public float Y;
        [MemoryPackOrder(3)] public float Z;
    }

    [MemoryPackable]
    public partial struct MobaActorTransformSnapshotPayload
    {
        [MemoryPackOrder(0)] public MobaActorTransformSnapshotEntry[] Entries;

        [MemoryPackConstructor]
        public MobaActorTransformSnapshotPayload(MobaActorTransformSnapshotEntry[] entries)
        {
            Entries = entries;
        }
    }

    public static class MobaActorTransformSnapshotCodec
    {
        public static byte[] Serialize(MobaActorTransformSnapshotEntry[] entries)
        {
            entries ??= Array.Empty<MobaActorTransformSnapshotEntry>();
            var payload = new MobaActorTransformSnapshotPayload { Entries = entries };
            return WireSerializer.Serialize(in payload);
        }

        public static MobaActorTransformSnapshotEntry[] Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return Array.Empty<MobaActorTransformSnapshotEntry>();

            var p = WireSerializer.Deserialize<MobaActorTransformSnapshotPayload>(payload);
            return p.Entries ?? Array.Empty<MobaActorTransformSnapshotEntry>();
        }
    }
}
