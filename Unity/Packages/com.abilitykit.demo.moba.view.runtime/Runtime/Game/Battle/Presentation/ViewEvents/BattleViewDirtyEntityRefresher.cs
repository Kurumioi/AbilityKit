using AbilityKit.Game.Battle.Entity;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleViewDirtyEntityRefresher
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly BattleViewBinder _binder;

        public BattleViewDirtyEntityRefresher(BattleContext ctx, IBattleEntityQuery query, BattleViewBinder binder)
        {
            _ctx = ctx;
            _query = query;
            _binder = binder;
        }

        public void Refresh()
        {
            if (_query?.World == null) return;

            var dirty = _ctx != null ? _ctx.DirtyEntities : null;
            if (dirty == null || dirty.Count == 0) return;

            for (int i = 0; i < dirty.Count; i++)
            {
                var id = dirty[i];
                if (!_query.World.IsAlive(id)) continue;
                _binder?.Sync(_query.World.Wrap(id));
            }

            dirty.Clear();
        }
    }
}
