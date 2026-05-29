using System;
using MemoryPack;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Transport-friendly snapshot entity envelope.
    /// </summary>
    public readonly struct SnapshotEntityState
    {
        /// <summary>
        /// Entity identifier in the logic world.
        /// </summary>
        public readonly int EntityId;

        /// <summary>
        /// Application-defined state type.
        /// </summary>
        public readonly int StateType;

        /// <summary>
        /// Serialized application-defined state payload.
        /// </summary>
        public readonly byte[] Payload;

        public SnapshotEntityState(int entityId, int stateType, byte[] payload)
        {
            EntityId = entityId;
            StateType = stateType;
            Payload = payload ?? Array.Empty<byte>();
        }

        public static SnapshotEntityState Create<TPayload>(int entityId, in TPayload payload)
        {
            var stateType = CoordinatorPayloadCodec.TryGetOpCode<TPayload>(out var opCode) ? opCode : 0;
            return Create(entityId, stateType, in payload);
        }

        public static SnapshotEntityState Create<TPayload>(int entityId, int stateType, in TPayload payload)
        {
            return new SnapshotEntityState(entityId, stateType, CoordinatorPayloadCodec.Encode(in payload));
        }

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            if (CoordinatorPayloadCodec.TryGetOpCode<TPayload>(out var opCode) && StateType != opCode)
            {
                payload = default;
                return false;
            }

            return CoordinatorPayloadCodec.TryDecode(Payload, out payload);
        }
    }

    /// <summary>
    /// Default entity-state payload retained as a convenience model.
    /// </summary>
    [MemoryPackable]
    public partial struct EntityState
    {
        /// <summary>
        /// Entity identifier.
        /// </summary>
        [MemoryPackOrder(0)] public int EntityId;

        /// <summary>
        /// Position X.
        /// </summary>
        [MemoryPackOrder(1)] public float X;

        /// <summary>
        /// Position Y.
        /// </summary>
        [MemoryPackOrder(2)] public float Y;

        /// <summary>
        /// Position Z.
        /// </summary>
        [MemoryPackOrder(3)] public float Z;

        /// <summary>
        /// Rotation around Y-axis.
        /// </summary>
        [MemoryPackOrder(4)] public float Rotation;

        /// <summary>
        /// Velocity X.
        /// </summary>
        [MemoryPackOrder(5)] public float VelocityX;

        /// <summary>
        /// Velocity Z.
        /// </summary>
        [MemoryPackOrder(6)] public float VelocityZ;

        /// <summary>
        /// Current HP.
        /// </summary>
        [MemoryPackOrder(7)] public float Hp;

        /// <summary>
        /// Maximum HP.
        /// </summary>
        [MemoryPackOrder(8)] public float HpMax;

        /// <summary>
        /// Team identifier.
        /// </summary>
        [MemoryPackOrder(9)] public int TeamId;

        /// <summary>
        /// Is entity dead.
        /// </summary>
        [MemoryPackOrder(10)] public bool IsDead;

        public EntityState(int entityId)
        {
            EntityId = entityId;
            X = Y = Z = 0;
            Rotation = 0;
            VelocityX = VelocityZ = 0;
            Hp = HpMax = 0;
            TeamId = 0;
            IsDead = true;
        }

        public SnapshotEntityState ToSnapshotEntityState()
        {
            return SnapshotEntityState.Create(EntityId, in this);
        }

        public static EntityState Empty(int entityId) => new EntityState(entityId);
    }
}
