using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Shooter
{
    public static class ShooterPureStateSnapshotKinds
    {
        public const int FullBaseline = 1;
        public const int Delta = 2;
        public const int LowFrequency = 3;
        public const int VisibilityHint = 4;
    }

    public static class ShooterPureStateEntityLayers
    {
        public const int KeyInteraction = 1;
        public const int Combat = 2;
        public const int Decorative = 3;
    }

    public static class ShooterPureStateDeltaKinds
    {
        public const int None = 0;
        public const int Spawn = 1;
        public const int Despawn = 2;
        public const int Update = 3;
        public const int OwnerChange = 4;
        public const int VisibilityChange = 5;
    }

    public static class ShooterPureStateEntityFlags
    {
        public const byte Alive = 1 << 0;
        public const byte Visible = 1 << 1;
        public const byte PredictedLocal = 1 << 2;
        public const byte LowFrequency = 1 << 3;
    }

    [MemoryPackable]
    public partial struct ShooterPureStateSyncSettings
    {
        [MemoryPackOrder(0)] public int MaxEntityCount;
        [MemoryPackOrder(1)] public int ActiveSyncBudget;
        [MemoryPackOrder(2)] public int BaselineIntervalFrames;
        [MemoryPackOrder(3)] public int DeltaIntervalFrames;
        [MemoryPackOrder(4)] public int LowFrequencyIntervalFrames;
        [MemoryPackOrder(5)] public int InterpolationDelayFrames;

        public ShooterPureStateSyncSettings(
            int maxEntityCount,
            int activeSyncBudget,
            int baselineIntervalFrames,
            int deltaIntervalFrames,
            int lowFrequencyIntervalFrames,
            int interpolationDelayFrames)
        {
            MaxEntityCount = maxEntityCount;
            ActiveSyncBudget = activeSyncBudget;
            BaselineIntervalFrames = baselineIntervalFrames;
            DeltaIntervalFrames = deltaIntervalFrames;
            LowFrequencyIntervalFrames = lowFrequencyIntervalFrames;
            InterpolationDelayFrames = interpolationDelayFrames;
        }

        public static ShooterPureStateSyncSettings Default => new ShooterPureStateSyncSettings(
            10000,
            512,
            60,
            2,
            15,
            3);
    }

    [MemoryPackable]
    public partial struct ShooterPureStateEntityDelta
    {
        [MemoryPackOrder(0)] public int EntityId;
        [MemoryPackOrder(1)] public int EntityKind;
        [MemoryPackOrder(2)] public int EntityLayer;
        [MemoryPackOrder(3)] public int DeltaKind;
        [MemoryPackOrder(4)] public int OwnerId;
        [MemoryPackOrder(5)] public int QuantizedX;
        [MemoryPackOrder(6)] public int QuantizedY;
        [MemoryPackOrder(7)] public int QuantizedVelocityX;
        [MemoryPackOrder(8)] public int QuantizedVelocityY;
        [MemoryPackOrder(9)] public int Hp;
        [MemoryPackOrder(10)] public int Score;
        [MemoryPackOrder(11)] public int RemainingFrames;
        [MemoryPackOrder(12)] public byte Flags;

        public ShooterPureStateEntityDelta(
            int entityId,
            int entityKind,
            int entityLayer,
            int deltaKind,
            int ownerId,
            int quantizedX,
            int quantizedY,
            int quantizedVelocityX,
            int quantizedVelocityY,
            int hp,
            int score,
            int remainingFrames,
            byte flags)
        {
            EntityId = entityId;
            EntityKind = entityKind;
            EntityLayer = entityLayer;
            DeltaKind = deltaKind;
            OwnerId = ownerId;
            QuantizedX = quantizedX;
            QuantizedY = quantizedY;
            QuantizedVelocityX = quantizedVelocityX;
            QuantizedVelocityY = quantizedVelocityY;
            Hp = hp;
            Score = score;
            RemainingFrames = remainingFrames;
            Flags = flags;
        }
    }

    [MemoryPackable]
    public partial struct ShooterPureStateVisibilityHint
    {
        [MemoryPackOrder(0)] public int EntityId;
        [MemoryPackOrder(1)] public int EntityKind;
        [MemoryPackOrder(2)] public int EntityLayer;
        [MemoryPackOrder(3)] public byte Flags;
        [MemoryPackOrder(4)] public int Priority;

        public ShooterPureStateVisibilityHint(int entityId, int entityKind, int entityLayer, byte flags, int priority)
        {
            EntityId = entityId;
            EntityKind = entityKind;
            EntityLayer = entityLayer;
            Flags = flags;
            Priority = priority;
        }
    }

    [MemoryPackable]
    public partial struct ShooterPureStateSnapshotPayload
    {
        [MemoryPackOrder(0)] public int Version;
        [MemoryPackOrder(1)] public ulong WorldId;
        [MemoryPackOrder(2)] public int Frame;
        [MemoryPackOrder(3)] public long ServerTick;
        [MemoryPackOrder(4)] public int SnapshotKind;
        [MemoryPackOrder(5)] public int BaselineFrame;
        [MemoryPackOrder(6)] public uint BaselineHash;
        [MemoryPackOrder(7)] public uint StateHash;
        [MemoryPackOrder(8)] public ShooterPureStateSyncSettings Settings;
        [MemoryPackOrder(9)] public ShooterPureStateEntityDelta[] Entities;
        [MemoryPackOrder(10)] public ShooterPureStateVisibilityHint[] VisibilityHints;

        [MemoryPackConstructor]
        public ShooterPureStateSnapshotPayload(
            int version,
            ulong worldId,
            int frame,
            long serverTick,
            int snapshotKind,
            int baselineFrame,
            uint baselineHash,
            uint stateHash,
            ShooterPureStateSyncSettings settings,
            ShooterPureStateEntityDelta[] entities,
            ShooterPureStateVisibilityHint[] visibilityHints)
        {
            Version = version;
            WorldId = worldId;
            Frame = frame;
            ServerTick = serverTick;
            SnapshotKind = snapshotKind;
            BaselineFrame = baselineFrame;
            BaselineHash = baselineHash;
            StateHash = stateHash;
            Settings = settings;
            Entities = entities;
            VisibilityHints = visibilityHints;
        }

        public static ShooterPureStateSnapshotPayload Empty(int frame = 0)
        {
            return new ShooterPureStateSnapshotPayload(
                ShooterPureStateSyncCodec.CurrentVersion,
                0,
                frame,
                0,
                ShooterPureStateSnapshotKinds.FullBaseline,
                0,
                0,
                0,
                ShooterPureStateSyncSettings.Default,
                Array.Empty<ShooterPureStateEntityDelta>(),
                Array.Empty<ShooterPureStateVisibilityHint>());
        }
    }

    public static class ShooterPureStateSyncCodec
    {
        public const int CurrentVersion = 1;

        public static byte[] Serialize(in ShooterPureStateSnapshotPayload snapshot)
        {
            return WireSerializer.Serialize(in snapshot);
        }

        public static ShooterPureStateSnapshotPayload Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return ShooterPureStateSnapshotPayload.Empty();
            }

            var value = WireSerializer.Deserialize<ShooterPureStateSnapshotPayload>(payload);
            return new ShooterPureStateSnapshotPayload(
                value.Version <= 0 ? CurrentVersion : value.Version,
                value.WorldId,
                value.Frame,
                value.ServerTick,
                value.SnapshotKind <= 0 ? ShooterPureStateSnapshotKinds.FullBaseline : value.SnapshotKind,
                value.BaselineFrame,
                value.BaselineHash,
                value.StateHash,
                value.Settings.MaxEntityCount <= 0 ? ShooterPureStateSyncSettings.Default : value.Settings,
                value.Entities ?? Array.Empty<ShooterPureStateEntityDelta>(),
                value.VisibilityHints ?? Array.Empty<ShooterPureStateVisibilityHint>());
        }
    }
}
