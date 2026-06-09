namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleViewDirtyEntityRefresher
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly BattleViewBinder _binder;
        private readonly ViewDirtyEntityRefreshOperation _operation;

        public BattleViewDirtyEntityRefresher(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleViewBinder binder,
            ViewDirtyEntityRefreshOperation operation = null)
        {
            _ctx = ctx;
            _query = query;
            _binder = binder;
            _operation = operation ?? new ViewDirtyEntityRefreshOperation();
        }

        public void Refresh()
        {
            _operation.Refresh(_ctx, _query, _binder);
        }
    }
}
