using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterWorldModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.TryRegister<ShooterEnemyWaveOptions>(WorldLifetime.Singleton, _ => ShooterEnemyWaveOptions.DefaultEnabled);
            builder.TryRegister<ShooterArenaGameplayOptions>(WorldLifetime.Singleton, _ => ShooterArenaGameplayOptions.Disabled);
            builder.AddModule(new SveltoWorldModule());
            builder.AddModule(new ShooterServicesAutoModule());
        }
    }
}
