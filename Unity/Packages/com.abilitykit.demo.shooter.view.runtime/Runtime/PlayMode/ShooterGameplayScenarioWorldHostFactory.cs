#nullable enable

using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Shooter.Runtime;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public static class ShooterGameplayScenarioWorldHostFactory
    {
        public static ShooterWorldHost Create(ShooterSveltoGameplayScenarioConfig? scenario)
        {
            return scenario.HasValue
                ? new ShooterWorldHost(options => ConfigureWorldOptions(options, scenario.Value))
                : new ShooterWorldHost();
        }

        public static void ConfigureWorldOptions(WorldCreateOptions options, in ShooterSveltoGameplayScenarioConfig scenario)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.ServiceBuilder ??= WorldServiceContainerFactory.CreateDefaultOnly();
            var enemyWaveOptions = new ShooterEnemyWaveOptions(true, scenario.BattleFlow);
            var arenaOptions = ShooterArenaGameplayOptions.CreateCircular(scenario.ArenaRadius);
            options.ServiceBuilder.Register<ShooterEnemyWaveOptions>(WorldLifetime.Singleton, _ => enemyWaveOptions);
            options.ServiceBuilder.Register<ShooterArenaGameplayOptions>(WorldLifetime.Singleton, _ => arenaOptions);
        }
    }
}
