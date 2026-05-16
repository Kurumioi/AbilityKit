using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    public enum SpawnEntityKind : byte
    {
        Character = 1,
        Projectile = 2,
    }

    [MemoryPackable]
    public partial struct MobaActorSpawnSnapshotEntry
    {
        [MemoryPackOrder(0)] public int NetId;
        [MemoryPackOrder(1)] public int Kind;
        [MemoryPackOrder(2)] public int Code;
        [MemoryPackOrder(3)] public int OwnerNetId;
        [MemoryPackOrder(4)] public float X;
        [MemoryPackOrder(5)] public float Y;
        [MemoryPackOrder(6)] public float Z;
    }

    [MemoryPackable]
    public partial struct MobaActorSpawnSnapshotPayload
    {
        [MemoryPackOrder(0)] public MobaActorSpawnSnapshotEntry[] Entries;

        [MemoryPackConstructor]
        public MobaActorSpawnSnapshotPayload(MobaActorSpawnSnapshotEntry[] entries)
        {
            Entries = entries;
        }
    }

    public static class MobaActorSpawnSnapshotCodec
    {
        public static byte[] Serialize(MobaActorSpawnSnapshotEntry[] entries)
        {
            entries ??= Array.Empty<MobaActorSpawnSnapshotEntry>();
            var payload = new MobaActorSpawnSnapshotPayload { Entries = entries };
            return WireSerializer.Serialize(in payload);
        }

        public static MobaActorSpawnSnapshotEntry[] Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return Array.Empty<MobaActorSpawnSnapshotEntry>();

            var p = WireSerializer.Deserialize<MobaActorSpawnSnapshotPayload>(payload);
            return p.Entries ?? Array.Empty<MobaActorSpawnSnapshotEntry>();
        }
    }
}
