using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleEntityCacheComponent System
    /// 处理实体缓存的业务逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETBattleEntityCacheComponent))]
    [FriendOf(typeof(ETBattleEntityCacheComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETBattleEntityCacheComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleEntityCacheComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETBattleEntityCacheComponent self)
        {
        }

        /// <summary>
        /// 更新缓存数据
        /// </summary>
        public static void UpdateCache(this ETBattleEntityCacheComponent self, int frame, in FrameSnapshotData snapshot)
        {
            self.CachedFrame = frame;
            self.CacheTimestamp = frame;

            // 更新变换数据
            if (snapshot.ActorTransforms != null)
            {
                foreach (var transform in snapshot.ActorTransforms)
                {
                    if (self._entityCache.TryGetValue(transform.ActorId, out var unit))
                    {
                        unit.UpdateFromSnapshot(transform.PositionX, transform.PositionY, transform.RotationY, frame);
                    }
                }
            }

            // 处理伤害事件
            if (snapshot.DamageEvents != null)
            {
                foreach (var damage in snapshot.DamageEvents)
                {
                    if (self._entityCache.TryGetValue(damage.TargetId, out var unit))
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
        /// 更新所有实体的渲染位置（插值）
        /// </summary>
        public static void UpdateRenderPositions(this ETBattleEntityCacheComponent self, float interpolationSpeed, float deltaTime)
        {
            foreach (var unit in self._entityCache.Values)
            {
                unit.UpdateRenderPosition(interpolationSpeed, deltaTime);
            }
        }
    }
}
