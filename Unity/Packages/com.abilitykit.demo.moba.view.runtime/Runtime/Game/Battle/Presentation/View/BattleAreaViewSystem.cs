using System.Collections.Generic;
using AbilityKit.Protocol.Moba;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleAreaViewSystem
    {
        private readonly Dictionary<int, BattleAreaViewHandle> _areaViews = new Dictionary<int, BattleAreaViewHandle>(128);

        public void HandleSnapshot(
            BattleViewBinder binder,
            IBattleEntityQuery query,
            MobaAreaEventSnapshotEntry[] entries)
        {
            if (entries == null || entries.Length == 0) return;
            if (query == null) return;
            if (BattleViewFactory.GetOrLoadConfigs() == null) return;

            for (var i = 0; i < entries.Length; i++)
            {
                var evt = entries[i];
                if (evt.AreaId <= 0) continue;

                if (evt.Kind == (int)AreaEventKind.Spawn)
                {
                    Spawn(binder, in evt);
                }
                else if (evt.Kind == (int)AreaEventKind.Expire)
                {
                    Expire(evt.AreaId);
                }
            }
        }

        public void Clear()
        {
            foreach (var kv in _areaViews)
            {
                kv.Value?.Destroy();
            }

            _areaViews.Clear();
        }

        private void Spawn(BattleViewBinder binder, in MobaAreaEventSnapshotEntry evt)
        {
            if (_areaViews.ContainsKey(evt.AreaId)) return;

            var handle = BattleAreaViewHandleFactory.TryCreate(binder, in evt);
            if (handle == null) return;

            _areaViews[evt.AreaId] = handle;
        }

        private void Expire(int areaId)
        {
            if (!_areaViews.TryGetValue(areaId, out var handle)) return;

            handle?.Destroy();
            _areaViews.Remove(areaId);
        }
    }
}
