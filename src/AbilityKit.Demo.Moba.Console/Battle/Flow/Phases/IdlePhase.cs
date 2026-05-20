namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 空闲阶段
    /// </summary>
    public sealed class IdlePhase : IPhase
    {
        public string Name => "Idle";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[Idle] Entered Idle phase");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Idle] Exiting to {nextPhase}");
        }
    }
}
