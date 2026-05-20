namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 结束阶段
    /// </summary>
    public sealed class EndPhase : IPhase
    {
        public string Name => "End";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[End] Entered End phase");
            Platform.Log.Title("BATTLE RESULTS");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[End] Exiting to {nextPhase}");
        }
    }
}
