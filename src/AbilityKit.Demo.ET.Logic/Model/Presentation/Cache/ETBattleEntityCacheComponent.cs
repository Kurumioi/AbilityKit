using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// 实体状态缓存组件（纯数据）
    ///
    /// 职责：
    /// - 缓存 moba.core 快照数据
    /// - 业务逻辑由 ETBattleEntityCacheComponentSystem 处理
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleEntityCacheComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 实体缓存字典（internal 供 System 访问）
        /// </summary>
        internal readonly Dictionary<int, ETUnit> _entityCache = new();

        /// <summary>
        /// 缓存的帧号
        /// </summary>
        public int CachedFrame { get; internal set; }

        /// <summary>
        /// 缓存时间戳
        /// </summary>
        public long CacheTimestamp { get; internal set; }

        /// <summary>
        /// 实体数量
        /// </summary>
        public int EntityCount => _entityCache.Count;

        public void Awake()
        {
            CachedFrame = 0;
            CacheTimestamp = 0;
        }

        public void Destroy()
        {
            _entityCache.Clear();
        }

        #region Cache Operations

        /// <summary>
        /// 添加实体到缓存
        /// </summary>
        public void AddEntity(int actorId, ETUnit unit)
        {
            _entityCache[actorId] = unit;
        }

        /// <summary>
        /// 移除实体
        /// </summary>
        public void RemoveEntity(int actorId)
        {
            _entityCache.Remove(actorId);
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public ETUnit? GetEntity(int actorId)
        {
            return _entityCache.TryGetValue(actorId, out var unit) ? unit : null;
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        public bool HasEntity(int actorId)
        {
            return _entityCache.ContainsKey(actorId);
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public IEnumerable<ETUnit> GetAllEntities()
        {
            return _entityCache.Values;
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// 尝试获取位置
        /// </summary>
        public bool TryGetPosition(int actorId, out float x, out float y)
        {
            x = 0;
            y = 0;
            if (_entityCache.TryGetValue(actorId, out var unit))
            {
                x = unit.X;
                y = unit.Y;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试获取 HP
        /// </summary>
        public bool TryGetHp(int actorId, out float hp, out float maxHp)
        {
            hp = 0;
            maxHp = 0;
            if (_entityCache.TryGetValue(actorId, out var unit))
            {
                hp = unit.Hp;
                maxHp = unit.MaxHp;
                return true;
            }
            return false;
        }

        #endregion
    }
}
