using System;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterSveltoWorld : IShooterSveltoWorld
    {
        private readonly IShooterEcsEntityStoreSynchronization _synchronization;

        public ShooterSveltoWorld(ISveltoWorldContext context, IShooterEcsEntityStore entityStore)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            EntityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
            _synchronization = entityStore as IShooterEcsEntityStoreSynchronization
                ?? throw new ArgumentException("Shooter Svelto entity store must support synchronization.", nameof(entityStore));
        }

        public ISveltoWorldContext Context { get; }

        public IShooterEcsEntityStore EntityStore { get; }

        public void SyncEntities()
        {
            _synchronization.SyncToEcs();
        }
    }
}
