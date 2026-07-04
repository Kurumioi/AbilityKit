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
            var scenario = ShooterSveltoGameplayScenarioCatalog.WaveSurvival;
            var battleFlow = CreateBattleFlow(scenario.BattleFlow, options);
            var enemyWaveOptions = new ShooterEnemyWaveOptions(true, battleFlow);
            var arenaOptions = ShooterArenaGameplayOptions.CreateCircular(scenario.ArenaRadius);
            options.ServiceBuilder.Register<ShooterEnemyWaveOptions>(WorldLifetime.Singleton, _ => enemyWaveOptions);
            options.ServiceBuilder.Register<ShooterArenaGameplayOptions>(WorldLifetime.Singleton, _ => arenaOptions);
            options.Modules.Add(new ShooterWorldModule());
        }

        private static ShooterSveltoGameplayBattleFlowConfig CreateBattleFlow(
            ShooterSveltoGameplayBattleFlowConfig battleFlow,
            WorldCreateOptions options)
        {
            if (!options.Extensions.TryGetValue(typeof(ShooterGameplay), out var value) ||
                value is not int durationFrames ||
                durationFrames <= 0 ||
                durationFrames == battleFlow.DurationFrames)
            {
                return battleFlow;
            }

            return new ShooterSveltoGameplayBattleFlowConfig(
                durationFrames,
                battleFlow.VictoryTargetDefeats,
                battleFlow.MaxActiveEnemies,
                battleFlow.Waves,
                battleFlow.EnemyLoadoutId,
                battleFlow.EnemyAttackIntervalFrames,
                battleFlow.EnemyAttackDamage,
                battleFlow.EnemyProjectileSpeedScale,
                battleFlow.EnemyProjectilesPerShot,
                battleFlow.EnemySpreadDegrees);
        }
    }
}
