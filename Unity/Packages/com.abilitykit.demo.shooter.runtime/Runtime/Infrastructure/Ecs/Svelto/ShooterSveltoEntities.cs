using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public static class ShooterSveltoGroups
    {
        public static readonly ExclusiveGroup Players = new ExclusiveGroup("shooter.players");

        public static readonly ExclusiveGroup Projectiles = new ExclusiveGroup("shooter.projectiles");

        public static readonly ExclusiveGroup GameplayShooters = new ExclusiveGroup("shooter.gameplay.shooters");

        public static readonly ExclusiveGroup GameplayTargets = new ExclusiveGroup("shooter.gameplay.targets");

        public static readonly ExclusiveGroup GameplayProjectiles = new ExclusiveGroup("shooter.gameplay.projectiles");
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

    public struct ShooterSveltoTransformComponent : IEntityComponent
    {
        public float X;
        public float Y;
        public float DirectionX;
        public float DirectionY;
    }

    public struct ShooterSveltoHealthComponent : IEntityComponent
    {
        public int Current;
        public int Max;
        public int Alive;
    }

    public struct ShooterSveltoWeaponComponent : IEntityComponent
    {
        public int LoadoutId;
        public float ProjectileSpeed;
        public int ProjectileLifeFrames;
        public int Damage;
        public int CooldownFrames;
        public int ProjectilesPerShot;
        public float SpreadRadians;
    }

    public struct ShooterSveltoCooldownComponent : IEntityComponent
    {
        public int RemainingFrames;
    }

    public struct ShooterSveltoTargetComponent : IEntityComponent
    {
        public uint TargetEntityId;
    }

    public struct ShooterSveltoProjectileDamageComponent : IEntityComponent
    {
        public int Damage;
        public uint OwnerEntityId;
        public uint TargetEntityId;
    }

    public sealed class ShooterSveltoPlayerDescriptor : GenericEntityDescriptor<ShooterSveltoPlayerComponent>
    {
    }

    public sealed class ShooterSveltoProjectileDescriptor : GenericEntityDescriptor<ShooterSveltoProjectileComponent>
    {
    }

    public sealed class ShooterSveltoGameplayShooterDescriptor : GenericEntityDescriptor<ShooterSveltoTransformComponent, ShooterSveltoWeaponComponent, ShooterSveltoCooldownComponent, ShooterSveltoTargetComponent>
    {
    }

    public sealed class ShooterSveltoGameplayTargetDescriptor : GenericEntityDescriptor<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>
    {
    }

    public sealed class ShooterSveltoGameplayProjectileDescriptor : GenericEntityDescriptor<ShooterSveltoTransformComponent, ShooterSveltoProjectileComponent, ShooterSveltoProjectileDamageComponent>
    {
    }
}
