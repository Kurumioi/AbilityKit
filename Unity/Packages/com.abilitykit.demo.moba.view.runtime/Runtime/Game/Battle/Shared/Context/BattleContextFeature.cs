using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleContextFeature : IGamePhaseFeature
    {
        public void OnAttach(in GamePhaseContext ctx)
        {
            if (ctx.Root.TryGetRef(out BattleContext existing) && existing != null) return;
            ctx.Root.WithRef(BattleContext.Rent());
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            if (ctx.Root.IsValid)
            {
                if (ctx.Root.TryGetRef(out BattleContext existing) && existing != null)
                {
                    ctx.Root.RemoveComponent(typeof(BattleContext));
                    BattleContext.Return(existing);
                }
                else
                {
                    ctx.Root.RemoveComponent(typeof(BattleContext));
                }
            }
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
        }
    }
}
