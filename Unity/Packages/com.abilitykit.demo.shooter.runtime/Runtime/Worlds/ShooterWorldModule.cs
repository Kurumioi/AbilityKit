using System;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterWorldModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.TryRegister<IShooterBattleRuntimePort>(WorldLifetime.Singleton, _ => new ShooterBattleRuntimePort());
        }
    }
}
