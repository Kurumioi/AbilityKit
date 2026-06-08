namespace AbilityKit.Game.Flow
{
    public sealed class BootPhase : IGamePhase
    {
        public void Enter(in GamePhaseContext ctx)
        {
            var flow = ctx.Entry.Get<GameFlowDomain>();
            flow.Attach(new BootMenuOnGUIFeature());
        }

        public void Exit(in GamePhaseContext ctx)
        {
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }
    }
}
