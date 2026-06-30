using System;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal interface IShooterBattlePipelineFactory
    {
        ShooterBattleSveltoStepEngine Create(ShooterBattleServiceContext services);
    }

    internal sealed class ShooterBattlePipelineFactory : IShooterBattlePipelineFactory
    {
        public ShooterBattleSveltoStepEngine Create(ShooterBattleServiceContext services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return new ShooterBattleSveltoStepEngine(new IShooterBattleSystem[]
            {
                new ShooterFrameBeginBattleSystem(services),
                new ShooterBotAiServiceBattleSystem(services),
                new ShooterEnemyWaveBattleSystem(services, ShooterEnemyWavePhase.Spawn),
                new ShooterSimulationBattleSystem(services),
                new ShooterEnemyLifecycleCleanupBattleSystem(services),
                new ShooterEnemyWaveBattleSystem(services, ShooterEnemyWavePhase.Attack),
                new ShooterMatchStateBattleSystem(services)
            });
        }
    }
}
