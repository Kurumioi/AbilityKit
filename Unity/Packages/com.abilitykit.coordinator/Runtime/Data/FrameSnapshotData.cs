using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Frame snapshot data
    /// 
    /// Design:
    /// - Generic container for frame data
    /// - Contains both framework-defined data (EntityStates) and custom data
    /// - Applications can extend with custom payloads
    /// 
    /// This is the minimal data structure that all games need.
    /// For MOBA-specific data (damage events, projectile events), use the MOBA snapshot.
    /// </summary>
    public readonly struct FrameSnapshotData
    {
        /// <summary>
        /// Frame index
        /// </summary>
        public int FrameIndex { get; }

        /// <summary>
        /// Timestamp in seconds
        /// </summary>
        public double Timestamp { get; }

        /// <summary>
        /// Snapshot type
        /// </summary>
        public SnapshotType Type { get; }

        /// <summary>
        /// All entity states in this frame
        /// </summary>
        public EntityState[] Entities { get; }

        /// <summary>
        /// Custom data payload (application-specific format)
        /// Can be used for game-specific events like damage, projectiles, etc.
        /// Format is determined by the application.
        /// </summary>
        public byte[] CustomPayload { get; }

        public FrameSnapshotData(
            int frameIndex,
            double timestamp,
            SnapshotType type,
            EntityState[] entities = null,
            byte[] customPayload = null)
        {
            FrameIndex = frameIndex;
            Timestamp = timestamp;
            Type = type;
            Entities = entities ?? Array.Empty<EntityState>();
            CustomPayload = customPayload;
        }

        /// <summary>
        /// Create an empty snapshot
        /// </summary>
        public static FrameSnapshotData Empty => default;
    }

    /// <summary>
    /// Snapshot type
    /// </summary>
    public enum SnapshotType
    {
        /// <summary>
        /// Full snapshot with all entities
        /// </summary>
        Full = 0,

        /// <summary>
        /// Delta snapshot with only changed entities
        /// </summary>
        Delta = 1,

        /// <summary>
        /// Key frame snapshot (periodic full snapshot)
        /// </summary>
        KeyFrame = 2,
    }
}
