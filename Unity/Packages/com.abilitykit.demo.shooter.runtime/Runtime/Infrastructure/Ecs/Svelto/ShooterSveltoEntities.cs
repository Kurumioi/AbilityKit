using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public static class ShooterSveltoGroups
    {
        public static readonly ExclusiveGroup Players = new ExclusiveGroup("shooter.players");

        public static readonly ExclusiveGroup Projectiles = new ExclusiveGroup("shooter.projectiles");
    }

    public struct ShooterSveltoPlayerComponent : IEntityComponent
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

    public sealed class ShooterSveltoPlayerDescriptor : GenericEntityDescriptor<ShooterSveltoPlayerComponent>
    {
    }

    public sealed class ShooterSveltoProjectileDescriptor : GenericEntityDescriptor<ShooterSveltoProjectileComponent>
    {
    }
}
