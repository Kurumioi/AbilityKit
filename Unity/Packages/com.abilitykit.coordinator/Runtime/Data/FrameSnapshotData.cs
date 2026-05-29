using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Frame snapshot data.
    /// </summary>
    public readonly struct FrameSnapshotData
    {
        /// <summary>
        /// Frame index.
        /// </summary>
        public int FrameIndex { get; }

        /// <summary>
        /// Timestamp in seconds.
        /// </summary>
        public double Timestamp { get; }

        /// <summary>
        /// Snapshot type.
        /// </summary>
        public SnapshotType Type { get; }

        /// <summary>
        /// Application-defined entity states in this frame.
        /// </summary>
        public SnapshotEntityState[] Entities { get; }

        /// <summary>
        /// Custom data payload in an application-specific format.
        /// </summary>
        public byte[] CustomPayload { get; }

        public FrameSnapshotData(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            SnapshotEntityState[] entities = null,
            byte[] customPayload = null)
        {
            FrameIndex = frameIndex;
            Timestamp = timestamp;
            Type = type;
            Entities = entities ?? Array.Empty<SnapshotEntityState>();
            CustomPayload = customPayload ?? Array.Empty<byte>();
        }

        public FrameSnapshotData(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            EntityState[] entities,
            byte[] customPayload = null)
            : this(frameIndex, timestamp, type, ToSnapshotEntityStates(entities), customPayload)
        {
        }

        public FrameSnapshotData WithCustomPayload<TPayload>(in TPayload payload)
        {
            return new FrameSnapshotData(
                FrameIndex,
                Timestamp,
                Type,
                Entities,
                CoordinatorPayloadCodec.Encode(in payload));
        }

        public bool TryGetCustomPayload<TPayload>(out TPayload payload)
        {
            return CoordinatorPayloadCodec.TryDecode(CustomPayload, out payload);
        }

        public static FrameSnapshotData Create<TPayload>(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            in TPayload customPayload,
            SnapshotEntityState[] entities = null)
        {
            return new FrameSnapshotData(
                frameIndex,
                timestamp,
                type,
                entities,
                CoordinatorPayloadCodec.Encode(in customPayload));
        }

        public bool TryGetEntities<TPayload>(out TPayload[] payloads)
        {
            payloads = Array.Empty<TPayload>();
            if (Entities == null || Entities.Length == 0)
            {
                return false;
            }

            var result = new TPayload[Entities.Length];
            for (int i = 0; i < Entities.Length; i++)
            {
                if (!Entities[i].TryGetPayload(out result[i]))
                {
                    payloads = Array.Empty<TPayload>();
                    return false;
                }
            }

            payloads = result;
            return true;
        }

        public EntityState[] ToDefaultEntityStates()
        {
            return TryGetEntities<EntityState>(out var states) ? states : Array.Empty<EntityState>();
        }

        private static SnapshotEntityState[] ToSnapshotEntityStates(EntityState[] entities)
        {
            if (entities == null || entities.Length == 0)
            {
                return Array.Empty<SnapshotEntityState>();
            }

            var result = new SnapshotEntityState[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                result[i] = entities[i].ToSnapshotEntityState();
            }

            return result;
        }

        /// <summary>
        /// Create an empty snapshot.
        /// </summary>
        public static FrameSnapshotData Empty => default;
    }

    /// <summary>
    /// Snapshot type.
    /// </summary>
    public enum SnapshotType
    {
        /// <summary>
        /// Full snapshot with all entities.
        /// </summary>
        Full = 0,

        /// <summary>
        /// Delta snapshot with only changed entities.
        /// </summary>
        Delta = 1,

        /// <summary>
        /// Key frame snapshot.
        /// </summary>
        KeyFrame = 2,
    }
}
