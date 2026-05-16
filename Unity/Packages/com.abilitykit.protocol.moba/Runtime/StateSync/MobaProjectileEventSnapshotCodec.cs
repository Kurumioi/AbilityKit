using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    public enum ProjectileEventKind : byte
    {
        Spawn = 1,
        Hit = 2,
        Exit = 3,
    }

    [MemoryPackable]
    public partial struct MobaProjectileEventSnapshotEntry
    {
        [MemoryPackOrder(0)] public int Kind;
        [MemoryPackOrder(1)] public int ProjectileActorId;
        [MemoryPackOrder(2)] public int OwnerActorId;
        [MemoryPackOrder(3)] public int TemplateId;
        [MemoryPackOrder(4)] public int LauncherActorId;
        [MemoryPackOrder(5)] public int RootActorId;
        [MemoryPackOrder(6)] public float X;
        [MemoryPackOrder(7)] public float Y;
        [MemoryPackOrder(8)] public float Z;
        [MemoryPackOrder(9)] public int HitCollider;
        [MemoryPackOrder(10)] public int ExitReason;
    }

    [MemoryPackable]
    public partial struct MobaProjectileEventSnapshotPayload
    {
        [MemoryPackOrder(0)] public MobaProjectileEventSnapshotEntry[] Entries;

        [MemoryPackConstructor]
        public MobaProjectileEventSnapshotPayload(MobaProjectileEventSnapshotEntry[] entries)
        {
            Entries = entries;
        }
    }

    public static class MobaProjectileEventSnapshotCodec
    {
        public static byte[] Serialize(MobaProjectileEventSnapshotEntry[] entries)
        {
            entries ??= Array.Empty<MobaProjectileEventSnapshotEntry>();
            var payload = new MobaProjectileEventSnapshotPayload { Entries = entries };
            return WireSerializer.Serialize(in payload);
        }

        public static MobaProjectileEventSnapshotEntry[] Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return Array.Empty<MobaProjectileEventSnapshotEntry>();

            var p = WireSerializer.Deserialize<MobaProjectileEventSnapshotPayload>(payload);
            return p.Entries ?? Array.Empty<MobaProjectileEventSnapshotEntry>();
        }
    }
}
