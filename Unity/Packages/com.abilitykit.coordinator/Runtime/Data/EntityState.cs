using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Entity state snapshot
    /// </summary>
    public struct EntityState
    {
        /// <summary>
        /// Entity identifier
        /// </summary>
        public int EntityId;

        /// <summary>
        /// Position X
        /// </summary>
        public float X;

        /// <summary>
        /// Position Y
        /// </summary>
        public float Y;

        /// <summary>
        /// Position Z
        /// </summary>
        public float Z;

        /// <summary>
        /// Rotation (Y-axis)
        /// </summary>
        public float Rotation;

        /// <summary>
        /// Velocity X
        /// </summary>
        public float VelocityX;

        /// <summary>
        /// Velocity Z
        /// </summary>
        public float VelocityZ;

        /// <summary>
        /// Current HP
        /// </summary>
        public float Hp;

        /// <summary>
        /// Maximum HP
        /// </summary>
        public float HpMax;

        /// <summary>
        /// Team identifier
        /// </summary>
        public int TeamId;

        /// <summary>
        /// Is entity dead
        /// </summary>
        public bool IsDead;

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

        public static EntityState Empty(int entityId) => new EntityState(entityId);
    }
}
