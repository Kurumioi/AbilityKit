using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal interface IShooterBattlePipelineFactory
    {
        ShooterBattleSveltoStepEngine Create(IShooterBattleServiceResolver services);
    }

    internal sealed class ShooterBattlePipelineFactory : IShooterBattlePipelineFactory
    {
        public ShooterBattleSveltoStepEngine Create(IShooterBattleServiceResolver services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return new ShooterBattleSveltoStepEngine(CreateSystems(services));
        }

        private static IEnumerable<IShooterBattleSystem> CreateSystems(IShooterBattleServiceResolver services)
        {
            yield return new ShooterFrameBeginBattleSystem(services);
            yield return new ShooterBotAiServiceBattleSystem(services);
            yield return new ShooterEnemyWaveBattleSystem(services, ShooterEnemyWavePhase.Spawn);
            yield return new ShooterSimulationBattleSystem(services);
            yield return new ShooterEnemyWaveBattleSystem(services, ShooterEnemyWavePhase.Attack);
            yield return new ShooterMatchStateBattleSystem(services);
        }
    }
}
