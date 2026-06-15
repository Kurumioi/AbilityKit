using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.Projectile
{
    public interface IProjectileHitFilter
    {
        bool ShouldHit(int ownerId, ColliderId collider, int frame);
    }
}
