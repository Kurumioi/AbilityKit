using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.View
{
    /// <summary>
    /// 视图层事件监听器
    /// 监听实体创建销毁等事件
    ///
    /// 设计：
    /// - 纯数据 Component
    /// - Handler 更新数据
    /// - 双字典设计：
    ///   - _unitViews: ActorId -> ETUnitViewComponent (用于逻辑层事件)
    ///   - _entityIdToActorId: EntityId -> ActorId (用于 ET 内部操作)
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETViewEventListener: Entity, IAwake
    {
        // 单位视图数据字典：ActorId -> ETUnitViewComponent。
        private readonly Dictionary<int, ETUnitViewComponent> _unitViews = new();

        // 用于 ET 内部操作的 EntityId 到 ActorId 映射。
        private readonly Dictionary<long, int> _entityIdToActorId = new();

        public IReadOnlyDictionary<int, ETUnitViewComponent> UnitViews => _unitViews;

        public void Awake()
        {
        }

        /// <summary>
        /// 添加带 EntityId 映射的单位视图。
        /// </summary>
        public void AddUnitView(int actorId, ETUnitViewComponent view, long entityId = 0)
        {
            _unitViews[actorId] = view;
            if (entityId > 0)
            {
                _entityIdToActorId[entityId] = actorId;
            }
            Log.Info($"[ETViewEventListener] Unit view added: ActorId={actorId}, EntityId={entityId}, Name={view.Name}");
        }

        /// <summary>
        /// 移除单位视图。
        /// </summary>
        public void RemoveUnitView(int actorId)
        {
            _unitViews.Remove(actorId);
            // 同时从 EntityId 映射中移除。
            long entityIdToRemove = 0;
            foreach (var kv in _entityIdToActorId)
            {
                if (kv.Value == actorId)
                {
                    entityIdToRemove = kv.Key;
                    break;
                }
            }
            if (entityIdToRemove > 0)
            {
                _entityIdToActorId.Remove(entityIdToRemove);
            }
        }

        /// <summary>
        /// 通过 ActorId 获取单位视图（moba.core 逻辑层 ID）。
        /// </summary>
        public ETUnitViewComponent GetUnitView(int actorId)
        {
            return _unitViews.TryGetValue(actorId, out var view) ? view : null;
        }

        /// <summary>
        /// 通过 EntityId 获取单位视图（ET 框架内部 ID）。
        /// </summary>
        public ETUnitViewComponent GetUnitViewByEntityId(long entityId)
        {
            if (_entityIdToActorId.TryGetValue(entityId, out var actorId))
            {
                return _unitViews.TryGetValue(actorId, out var view) ? view : null;
            }
            return null;
        }

        /// <summary>
        /// 通过 EntityId 获取 ActorId。
        /// </summary>
        public int GetActorId(long entityId)
        {
            return _entityIdToActorId.TryGetValue(entityId, out var actorId) ? actorId : 0;
        }
    }
}
