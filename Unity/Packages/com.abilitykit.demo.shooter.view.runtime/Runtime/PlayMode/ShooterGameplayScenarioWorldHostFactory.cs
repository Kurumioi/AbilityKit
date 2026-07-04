#nullable enable

using System;
using AbilityKit.Ability.World.Abstractions;
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

            options.Extensions[typeof(ShooterSveltoGameplayScenarioConfig)] = scenario;
        }
    }
}
