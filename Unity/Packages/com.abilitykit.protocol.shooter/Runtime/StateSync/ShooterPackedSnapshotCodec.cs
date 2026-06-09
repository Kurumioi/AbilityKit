using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Shooter
{
    public static class ShooterPackedSnapshotFlags
    {
        public const uint Full = 1u << 0;
        public const uint Delta = 1u << 1;
        public const uint KeyFrame = 1u << 2;
        public const uint AuthorityOverride = 1u << 3;
    }

    public static class ShooterPackedEntityFlags
    {
        public const byte Alive = 1 << 0;
        public const byte Player = 1 << 1;
        public const byte Projectile = 1 << 2;
        public const byte Enemy = 1 << 3;
        public const byte Spawned = 1 << 4;
        public const byte Despawned = 1 << 5;
        public const byte DirtyTransform = 1 << 6;
        public const byte DirtyStats = 1 << 7;
    }

    public static class ShooterPackedEntityKinds
    {
        public const int Player = 1;
        public const int Projectile = 2;
        public const int Enemy = 3;
    }

    [MemoryPackable]
    public partial struct ShooterPackedEntityChunk
    {
        [MemoryPackOrder(0)] public int EntityKind;
        [MemoryPackOrder(1)] public int Count;
        [MemoryPackOrder(2)] public int[] EntityIds;
        [MemoryPackOrder(3)] public float[] PosX;
        [MemoryPackOrder(4)] public float[] PosY;
        [MemoryPackOrder(5)] public float[] VelX;
        [MemoryPackOrder(6)] public float[] VelY;
        [MemoryPackOrder(7)] public float[] FacingX;
        [MemoryPackOrder(8)] public float[] FacingY;
        [MemoryPackOrder(9)] public short[] Hp;
        [MemoryPackOrder(10)] public byte[] Flags;
        [MemoryPackOrder(11)] public int[] OwnerIds;
        [MemoryPackOrder(12)] public int[] Aux;

        [MemoryPackConstructor]
        public ShooterPackedEntityChunk(
            int entityKind,
            int count,
            int[] entityIds,
            float[] posX,
            float[] posY,
            float[] velX,
            float[] velY,
            float[] facingX,
            float[] facingY,
            short[] hp,
            byte[] flags,
            int[] ownerIds,
            int[] aux)
        {
            EntityKind = entityKind;
            Count = count;
            EntityIds = entityIds;
            PosX = posX;
            PosY = posY;
            VelX = velX;
            VelY = velY;
            FacingX = facingX;
            FacingY = facingY;
            Hp = hp;
            Flags = flags;
            OwnerIds = ownerIds;
            Aux = aux;
        }

        public static ShooterPackedEntityChunk Empty(int entityKind)
        {
            return new ShooterPackedEntityChunk(
                entityKind,
                0,
                Array.Empty<int>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<short>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }
    }

    [MemoryPackable]
    public partial struct ShooterPackedSnapshotPayload
    {
        [MemoryPackOrder(0)] public int Version;
        [MemoryPackOrder(1)] public ulong WorldId;
        [MemoryPackOrder(2)] public int Frame;
        [MemoryPackOrder(3)] public long ServerTick;
        [MemoryPackOrder(4)] public uint SnapshotFlags;
        [MemoryPackOrder(5)] public uint StateHash;
        [MemoryPackOrder(6)] public int EntityCount;
        [MemoryPackOrder(7)] public ShooterPackedEntityChunk[] Chunks;
        [MemoryPackOrder(8)] public byte[] ExtensionPayload;

        [MemoryPackConstructor]
        public ShooterPackedSnapshotPayload(
            int version,
            ulong worldId,
            int frame,
            long serverTick,
            uint snapshotFlags,
            uint stateHash,
            int entityCount,
            ShooterPackedEntityChunk[] chunks,
            byte[] extensionPayload)
        {
            Version = version;
            WorldId = worldId;
            Frame = frame;
            ServerTick = serverTick;
            SnapshotFlags = snapshotFlags;
            StateHash = stateHash;
            EntityCount = entityCount;
            Chunks = chunks;
            ExtensionPayload = extensionPayload;
        }

        public static ShooterPackedSnapshotPayload Empty(int frame = 0)
        {
            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                0,
                frame,
                0,
                ShooterPackedSnapshotFlags.Full,
                0,
                0,
                Array.Empty<ShooterPackedEntityChunk>(),
                Array.Empty<byte>());
        }
    }

    public static class ShooterPackedSnapshotCodec
    {
        public const int CurrentVersion = 1;

        public static byte[] Serialize(in ShooterPackedSnapshotPayload snapshot)
        {
            return WireSerializer.Serialize(in snapshot);
        }

        public static ShooterPackedSnapshotPayload Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return ShooterPackedSnapshotPayload.Empty();
            }

            var value = WireSerializer.Deserialize<ShooterPackedSnapshotPayload>(payload);
            return new ShooterPackedSnapshotPayload(
                value.Version <= 0 ? CurrentVersion : value.Version,
                value.WorldId,
                value.Frame,
                value.ServerTick,
                value.SnapshotFlags,
                value.StateHash,
                value.EntityCount,
                value.Chunks ?? Array.Empty<ShooterPackedEntityChunk>(),
                value.ExtensionPayload ?? Array.Empty<byte>());
        }
    }
}
