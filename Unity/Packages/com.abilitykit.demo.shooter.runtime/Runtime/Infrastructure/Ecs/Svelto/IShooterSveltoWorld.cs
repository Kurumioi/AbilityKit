using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterSveltoWorld
    {
        ISveltoWorldContext Context { get; }

        IShooterEcsEntityStore EntityStore { get; }

        void SyncEntities();
    }
}
