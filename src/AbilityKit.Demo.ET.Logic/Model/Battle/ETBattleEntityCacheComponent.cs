using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 实体状态缓存组件
    ///
    /// 职责：
    /// - 缓存 moba.core 快照数据
    /// - 提供帧间差分
    /// - 支持插值计算
    /// - 供 ET.View 渲染使用
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleEntityCacheComponent : Entity, IAwake, IDestroy
    {
        // 实体缓存：ActorId -> ETUnit
        private readonly Dictionary<int, ETUnit> _entityCache = new();

        // 缓存的帧号
        public int CachedFrame { get; private set; }

        // 缓存时间戳
        public long CacheTimestamp { get; private set; }

        // 实体数量
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
        /// 更新缓存
        /// </summary>
        public void UpdateCache(int frame, in FrameSnapshotData snapshot)
        {
            CachedFrame = frame;
            CacheTimestamp = Environment.TickCount64;

            // 更新变换数据
            if (snapshot.ActorTransforms != null)
            {
                foreach (var transform in snapshot.ActorTransforms)
                {
                    if (_entityCache.TryGetValue(transform.ActorId, out var unit))
                    {
                        unit.UpdateFromSnapshot(transform.PositionX, transform.PositionY, transform.RotationY);
                    }
                }
            }

            // 处理伤害事件
            if (snapshot.DamageEvents != null)
            {
                foreach (var damage in snapshot.DamageEvents)
                {
                    if (_entityCache.TryGetValue(damage.TargetId, out var unit))
                    {
                        unit.Hp = damage.TargetHpAfter;
                        if (damage.IsKill)
                        {
                            unit.Hp = 0;
                        }
                    }
                }
            }
        }

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

        #region Interpolation

        /// <summary>
        /// 更新渲染位置（插值）
        /// </summary>
        public void UpdateRenderPositions(float interpolationSpeed, float deltaTime)
        {
            foreach (var unit in _entityCache.Values)
            {
                unit.UpdateRenderPosition(interpolationSpeed, deltaTime);
            }
        }

        #endregion
    }
}
