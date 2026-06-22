using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public struct ShooterSveltoProjectileComponent : IEntityComponent
    {
        public int BulletId;
        public int OwnerPlayerId;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public int RemainingFrames;
    }

    public sealed class ShooterSveltoProjectileDescriptor : GenericEntityDescriptor<ShooterSveltoProjectileComponent>
    {
    }
}
