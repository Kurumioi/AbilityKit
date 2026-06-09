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

            builder.AddModule(new SveltoWorldModule());
            builder.TryRegister<IShooterEcsEntityStore>(WorldLifetime.Singleton, r => new ShooterSveltoEcsEntityStore(r.Resolve<ISveltoWorldContext>()));
            builder.TryRegister<ShooterBattleState>(WorldLifetime.Singleton, r => new ShooterBattleState(r.Resolve<IShooterEcsEntityStore>()));
            builder.TryRegister<IShooterBattleSimulation>(WorldLifetime.Singleton, r => new ShooterBattleSimulation(r.Resolve<ShooterBattleState>()));
            builder.TryRegister<IShooterSveltoWorld>(WorldLifetime.Singleton, r => new ShooterSveltoWorld(r.Resolve<ISveltoWorldContext>(), r.Resolve<IShooterEcsEntityStore>()));
            builder.TryRegister<IShooterBattleRuntimePort>(WorldLifetime.Singleton, r => new ShooterBattleRuntimePort(
                r.Resolve<ShooterBattleState>(),
                r.Resolve<IShooterBattleSimulation>(),
                r.Resolve<IShooterSveltoWorld>()));
        }
    }
}
