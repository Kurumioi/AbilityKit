using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// 单位技能列表组件
    /// 存储实体的技能冷却和表现层技能状态
    ///
    /// 注意：业务逻辑在 ETUnitSkillListComponentSystem 中实现
    /// </summary>
    public class ETUnitSkillListComponent : Entity, IAwake
    {
        /// <summary>
        /// 技能冷却时间数组 (每个槽位对应一个冷却时间)
        /// </summary>
        public float[] SkillCooldowns { get; set; } = new float[4];

        /// <summary>
        /// 表现层技能状态列表
        /// </summary>
        public List<SkillViewData> SkillViews { get; set; } = new List<SkillViewData>();

        public void Awake()
        {
        }

        /// <summary>
        /// 技能视图数据
        /// </summary>
        public class SkillViewData
        {
            public int SkillId;
            public int Level;
            public bool IsReady;
        }
    }
}
