namespace AbilityKit.Game.Flow
{
    public sealed class BattleContextFeature : IGamePhaseFeature
    {
        public void OnAttach(in GamePhaseContext ctx)
        {
            if (ctx.Features.TryGet(out BattleContext existing) && existing != null) return;
            ctx.Features.Set(BattleContext.Rent());
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            if (ctx.Features.TryGet(out BattleContext existing) && existing != null)
            {
                ctx.Features.Remove<BattleContext>();
                BattleContext.Return(existing);
            }
            else
            {
                ctx.Features.Remove<BattleContext>();
            }
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }
    }
}
