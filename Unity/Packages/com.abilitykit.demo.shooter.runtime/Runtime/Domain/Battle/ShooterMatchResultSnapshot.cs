namespace AbilityKit.Demo.Shooter.Runtime
{
    public readonly struct ShooterMatchResultSnapshot
    {
        public ShooterMatchResultSnapshot(
            ShooterBattleMatchState matchState,
            int completedFrame,
            bool isFinal,
            bool isVictory,
            int defeatedEnemies,
            int victoryTargetDefeats)
        {
            MatchState = matchState;
            CompletedFrame = completedFrame;
            IsFinal = isFinal;
            IsVictory = isVictory;
            DefeatedEnemies = defeatedEnemies;
            VictoryTargetDefeats = victoryTargetDefeats;
        }

        public ShooterBattleMatchState MatchState { get; }

        public int CompletedFrame { get; }

        public bool IsFinal { get; }

        public bool IsVictory { get; }

        public int DefeatedEnemies { get; }

        public int VictoryTargetDefeats { get; }

        public static ShooterMatchResultSnapshot NotCompleted(int currentFrame, int defeatedEnemies, int victoryTargetDefeats)
        {
            return new ShooterMatchResultSnapshot(
                ShooterBattleMatchState.Running,
                currentFrame,
                isFinal: false,
                isVictory: false,
                defeatedEnemies,
                victoryTargetDefeats);
        }
    }
}
