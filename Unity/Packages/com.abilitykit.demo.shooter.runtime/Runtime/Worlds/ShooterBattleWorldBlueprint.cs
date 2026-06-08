using System;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterBattleWorldBlueprint : IWorldBlueprint
    {
        public string WorldType => ShooterGameplay.WorldType;

        public void Configure(WorldCreateOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.WorldType = ShooterGameplay.WorldType;
            options.ServiceBuilder ??= WorldServiceContainerFactory.CreateDefaultOnly();
            options.Modules.Add(new ShooterWorldModule());
        }
    }
}
