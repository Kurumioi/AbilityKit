using System;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterEnemyWaveOptions
    {
        public static ShooterEnemyWaveOptions Disabled { get; } = new ShooterEnemyWaveOptions(false);

        public static ShooterEnemyWaveOptions EnabledOption { get; } = new ShooterEnemyWaveOptions(true);

        public static ShooterEnemyWaveOptions DefaultEnabled { get; } = new ShooterEnemyWaveOptions(true, ShooterSveltoGameplayBattleFlowConfig.Default);

        public ShooterEnemyWaveOptions(bool enabled)
            : this(enabled, ShooterSveltoGameplayBattleFlowConfig.Default)
        {
        }

        public ShooterEnemyWaveOptions(bool enabled, ShooterSveltoGameplayBattleFlowConfig battleFlow)
        {
            Enabled = enabled;
            BattleFlow = battleFlow.DurationFrames <= 0
                ? ShooterSveltoGameplayBattleFlowConfig.Default
                : battleFlow;
            Waves = BattleFlow.Waves is { Length: > 0 }
                ? (ShooterSveltoGameplayWaveConfig[])BattleFlow.Waves.Clone()
                : Array.Empty<ShooterSveltoGameplayWaveConfig>();
        }

        public bool Enabled { get; }

        public ShooterSveltoGameplayBattleFlowConfig BattleFlow { get; }

        public int DurationFrames => Enabled ? BattleFlow.DurationFrames : 0;

        public int VictoryTargetDefeats => BattleFlow.VictoryTargetDefeats;

        public int MaxActiveEnemies => BattleFlow.MaxActiveEnemies;

        public ShooterSveltoGameplayWaveConfig[] Waves { get; }
    }
}
