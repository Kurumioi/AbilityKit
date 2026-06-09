using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattlePresentationSessionResolver
    {
        private readonly IBattlePresentationSessionFactory _factory;

        public BattlePresentationSessionResolver(IBattlePresentationSessionFactory factory = null)
        {
            _factory = factory ?? new BattlePresentationSessionFactory();
        }

        public BattlePresentationSessionContext Resolve(in GamePhaseContext ctx)
        {
            if (ctx.Root.IsValid &&
                ctx.Root.TryGetRef(out BattlePresentationSessionContext existing) &&
                existing != null)
            {
                existing.Retain();
                return existing;
            }

            var created = _factory.Create() ?? new BattlePresentationSessionFactory().Create();
            created.Retain();
            if (ctx.Root.IsValid)
            {
                ctx.Root.WithRef(created);
            }

            return created;
        }

        public void Release(in GamePhaseContext ctx, BattlePresentationSessionContext session)
        {
            if (session == null) return;
            if (!session.Release()) return;

            if (!ctx.Root.IsValid) return;
            if (!ctx.Root.TryGetRef(out BattlePresentationSessionContext existing)) return;
            if (!ReferenceEquals(existing, session)) return;

            ctx.Root.RemoveComponent(typeof(BattlePresentationSessionContext));
        }
    }
}
