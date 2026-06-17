using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterEntityManager
    {
        int MaxEntityCount { get; }

        int PlayerCount { get; }

        int ProjectileCount { get; }

        IReadOnlyCollection<int> PlayerIds { get; }

        IReadOnlyCollection<int> ProjectileIds { get; }

        void Clear();

        bool HasPlayer(int playerId);

        bool TryGetPlayer(int playerId, out ShooterSveltoPlayerComponent player);

        void AddPlayer(in ShooterSveltoPlayerComponent player);

        void SetPlayer(in ShooterSveltoPlayerComponent player);

        void RemovePlayer(int playerId);

        bool HasProjectile(int bulletId);

        bool TryGetProjectile(int bulletId, out ShooterSveltoProjectileComponent projectile);

        void AddProjectile(in ShooterSveltoProjectileComponent projectile);

        void SetProjectile(in ShooterSveltoProjectileComponent projectile);

        void RemoveProjectile(int bulletId);
    }
}
