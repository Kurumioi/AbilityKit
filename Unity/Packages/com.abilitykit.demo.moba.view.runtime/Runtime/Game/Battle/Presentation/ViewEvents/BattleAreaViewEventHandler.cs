using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Flow.Battle.View;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleAreaViewEventHandler
    {
        private readonly BattleContext _ctx;
        private readonly IBattleEntityQuery _query;
        private readonly BattleViewBinder _binder;
        private readonly BattleAreaViewSystem _areaViews;

        public BattleAreaViewEventHandler(
            BattleContext ctx,
            IBattleEntityQuery query,
            BattleViewBinder binder,
            BattleAreaViewSystem areaViews)
        {
            _ctx = ctx;
            _query = query;
            _binder = binder;
            _areaViews = areaViews;
        }

        public void HandleSnapshot(MobaAreaEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (_ctx?.EntityWorld == null) return;
            if (_query == null) return;

            _areaViews?.HandleSnapshot(_binder, _query, entries);
        }
    }
}
