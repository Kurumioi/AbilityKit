using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Logic-world entity state exposed by the runtime output port.
    /// Host adapters can convert this model to coordinator, network, view, or server DTOs.
    /// </summary>
    public struct LogicWorldEntityState
    {
        public int EntityId;
        public float X;
        public float Y;
        public float Z;
        public float Rotation;
        public float VelocityX;
        public float VelocityZ;
        public float Hp;
        public float HpMax;
        public int TeamId;
        public bool IsDead;

        public LogicWorldEntityState(int entityId)
        {
            EntityId = entityId;
            X = Y = Z = 0f;
            Rotation = 0f;
            VelocityX = VelocityZ = 0f;
            Hp = HpMax = 0f;
            TeamId = 0;
            IsDead = true;
        }

        public static LogicWorldEntityState Empty(int entityId)
        {
            return new LogicWorldEntityState(entityId);
        }
    }

    public interface IMobaBattleOutputPort
    {
        bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot);

        int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32);
    }

    public interface IMobaLogicWorldStateReadModel
    {
        LogicWorldEntityState[] GetAllEntityStates();
    }
}
