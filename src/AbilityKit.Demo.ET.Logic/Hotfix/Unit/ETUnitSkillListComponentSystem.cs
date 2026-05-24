using System;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitSkillListComponent System
    /// 管理单位技能列表
    ///
    /// Design:
    /// - 冷却数据由 moba.core 快照同步而来
    /// - 只提供只读访问和快照同步设置
    /// - 不实现客户端冷却计算逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETUnitSkillListComponent))]
    [FriendOf(typeof(ETUnitSkillListComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitSkillListComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitSkillListComponent self)
        {
            if (self.SkillCooldowns == null || self.SkillCooldowns.Length == 0)
            {
                self.SkillCooldowns = new float[4];
            }
        }

        /// <summary>
        /// 查找指定技能 ID 的视图数据
        /// </summary>
        public static ET.Logic.ETUnitSkillListComponent.SkillViewData? FindSkillView(
            this ETUnitSkillListComponent self, int skillId)
        {
            for (int i = 0; i < self.SkillViews.Count; i++)
            {
                var s = self.SkillViews[i];
                if (s != null && s.SkillId == skillId)
                    return s;
            }
            return null;
        }

        /// <summary>
        /// 获取或添加技能视图数据
        /// </summary>
        public static ET.Logic.ETUnitSkillListComponent.SkillViewData GetOrAddSkillView(
            this ETUnitSkillListComponent self, int skillId)
        {
            var s = FindSkillView(self, skillId);
            if (s != null)
                return s;

            s = new ET.Logic.ETUnitSkillListComponent.SkillViewData { SkillId = skillId };
            self.SkillViews.Add(s);
            return s;
        }

        /// <summary>
        /// 移除技能视图数据
        /// </summary>
        public static bool RemoveSkillView(this ETUnitSkillListComponent self, int skillId)
        {
            for (int i = 0; i < self.SkillViews.Count; i++)
            {
                var s = self.SkillViews[i];
                if (s != null && s.SkillId == skillId)
                {
                    self.SkillViews.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取技能冷却时间（只读，用于 UI 显示）
        /// </summary>
        public static float GetCooldown(this ETUnitSkillListComponent self, int slot)
        {
            if (slot < 0 || slot >= self.SkillCooldowns.Length)
                return 0;
            return self.SkillCooldowns[slot];
        }

        /// <summary>
        /// 设置技能冷却时间（从快照同步）
        /// </summary>
        public static void SetCooldown(this ETUnitSkillListComponent self, int slot, float cooldown)
        {
            if (slot < 0 || slot >= self.SkillCooldowns.Length)
                return;
            self.SkillCooldowns[slot] = cooldown;
        }

        /// <summary>
        /// 检查技能是否就绪（只读，用于 UI 显示）
        /// </summary>
        public static bool IsSkillReady(this ETUnitSkillListComponent self, int slot)
        {
            if (slot < 0 || slot >= self.SkillCooldowns.Length)
                return false;
            return self.SkillCooldowns[slot] <= 0;
        }

        /// <summary>
        /// 获取拥有者单位
        /// </summary>
        public static ETUnit? GetOwner(this ETUnitSkillListComponent self)
        {
            return self.Parent as ETUnit;
        }
    }
}
