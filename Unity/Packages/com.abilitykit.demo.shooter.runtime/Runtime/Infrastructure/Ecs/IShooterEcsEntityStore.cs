using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterEcsEntityStore
    {
        IDictionary<int, ShooterEcsPlayerEntity> Players { get; }

        IList<ShooterEcsProjectileEntity> Projectiles { get; }

        void Clear();
    }

    public interface IShooterEcsEntityStoreSynchronization
    {
        void SyncToEcs();
    }

    public sealed class ShooterEcsPlayerEntity
    {
        public int PlayerId;
        public float X;
        public float Y;
        public float AimX;
        public float AimY;
        public int Hp;
        public int Score;
        public bool Alive;
    }

    public struct ShooterEcsProjectileEntity
    {
        public int BulletId;
        public int OwnerPlayerId;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public int RemainingFrames;
    }
}
