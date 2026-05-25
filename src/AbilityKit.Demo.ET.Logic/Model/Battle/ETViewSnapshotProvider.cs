using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// View 快照提供器实现
    /// 提供统一的实体状态查询接口给 ET.View 层
    /// </summary>
    public sealed class ETViewSnapshotProvider : IETViewSnapshotProvider
    {
        private readonly ETBattleEntityCacheComponent _cacheComponent;
        private readonly List<TransformSnapshot> _transformSnapshots = new List<TransformSnapshot>();
        private readonly List<HpSnapshot> _hpSnapshots = new List<HpSnapshot>();

        public ETViewSnapshotProvider(ETBattleEntityCacheComponent cacheComponent)
        {
            _cacheComponent = cacheComponent;
        }

        public int CurrentFrame => _cacheComponent?.CachedFrame ?? 0;

        public ETBattleEntityCacheComponent GetCacheComponent() => _cacheComponent;

        public bool TryGetTransformSnapshot(int actorId, out TransformSnapshot snapshot)
        {
            snapshot = default;
            if (_cacheComponent == null)
                return false;

            var unit = _cacheComponent.GetEntity(actorId);
            if (unit != null)
            {
                snapshot = new TransformSnapshot
                {
                    ActorId = (int)unit.ActorId,
                    X = unit.X,
                    Y = unit.Y,
                    Rotation = unit.Rotation,
                    RenderX = unit.RenderX,
                    RenderY = unit.RenderY,
                    IsDead = unit.IsDead
                };
                return true;
            }
            return false;
        }

        public bool TryGetHpSnapshot(int actorId, out HpSnapshot snapshot)
        {
            snapshot = default;
            if (_cacheComponent == null)
                return false;

            var unit = _cacheComponent.GetEntity(actorId);
            if (unit != null)
            {
                snapshot = new HpSnapshot
                {
                    ActorId = actorId,
                    Hp = unit.Hp,
                    MaxHp = unit.MaxHp
                };
                return true;
            }
            return false;
        }

        public IReadOnlyList<TransformSnapshot> GetAllTransformSnapshots()
        {
            _transformSnapshots.Clear();
            if (_cacheComponent == null)
                return _transformSnapshots;

            foreach (var unit in _cacheComponent.GetAllEntities())
            {
                _transformSnapshots.Add(new TransformSnapshot
                {
                    ActorId = (int)unit.ActorId,
                    X = unit.X,
                    Y = unit.Y,
                    Rotation = unit.Rotation,
                    RenderX = unit.RenderX,
                    RenderY = unit.RenderY,
                    IsDead = unit.IsDead
                });
            }

            return _transformSnapshots;
        }

        public IReadOnlyList<HpSnapshot> GetAllHpSnapshots()
        {
            _hpSnapshots.Clear();
            if (_cacheComponent == null)
                return _hpSnapshots;

            foreach (var unit in _cacheComponent.GetAllEntities())
            {
                _hpSnapshots.Add(new HpSnapshot
                {
                    ActorId = (int)unit.ActorId,
                    Hp = unit.Hp,
                    MaxHp = unit.MaxHp
                });
            }

            return _hpSnapshots;
        }

        public bool HasEntity(int actorId)
        {
            return _cacheComponent?.HasEntity(actorId) ?? false;
        }

        public bool IsEntityDead(int actorId)
        {
            var unit = _cacheComponent?.GetEntity(actorId);
            return unit?.IsDead ?? false;
        }
    }
}
