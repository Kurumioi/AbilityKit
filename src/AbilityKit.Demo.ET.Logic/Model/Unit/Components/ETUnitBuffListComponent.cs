using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// 单位 Buff 列表组件
    /// 存储实体的表现层 Buff 状态
    ///
    /// 注意：业务逻辑在 ETUnitBuffListComponentSystem 中实现
    /// </summary>
    public class ETUnitBuffListComponent : Entity, IAwake
    {
        /// <summary>
        /// 表现层 Buff 列表
        /// </summary>
        public List<BuffViewData> BuffViews { get; set; } = new List<BuffViewData>();

        public void Awake()
        {
        }

        /// <summary>
        /// Buff 视图数据
        /// </summary>
        public class BuffViewData
        {
            public int BuffId;
            public int Stacks;
            public float RemainingDuration;
            public bool IsExpired;
        }
    }
}
