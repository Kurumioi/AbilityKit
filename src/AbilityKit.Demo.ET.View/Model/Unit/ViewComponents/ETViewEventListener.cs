using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 视图层事件监听器
    /// 监听实体创建销毁等事件
    ///
    /// Design:
    /// - 纯数据 Component
    /// - Handler 更新数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETViewEventListener: Entity, IAwake
    {
        // Unit view data dictionary: MobaActorId -> ETUnitViewComponent
        private readonly Dictionary<int, ETUnitViewComponent> _unitViews = new();

        public IReadOnlyDictionary<int, ETUnitViewComponent> UnitViews => _unitViews;

        public void Awake()
        {
        }

        /// <summary>
        /// Add unit view
        /// </summary>
        public void AddUnitView(int mobaActorId, ETUnitViewComponent view)
        {
            _unitViews[mobaActorId] = view;
        }

        /// <summary>
        /// Remove unit view
        /// </summary>
        public void RemoveUnitView(int mobaActorId)
        {
            _unitViews.Remove(mobaActorId);
        }

        /// <summary>
        /// Get unit view
        /// </summary>
        public ETUnitViewComponent GetUnitView(int mobaActorId)
        {
            return _unitViews.TryGetValue(mobaActorId, out var view) ? view : null;
        }
    }
}
