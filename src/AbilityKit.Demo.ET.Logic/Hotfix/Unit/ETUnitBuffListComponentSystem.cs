using System;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitBuffListComponent System
    /// 管理单位 Buff 列表逻辑
    ///
    /// 设计说明：
    /// - 所有业务逻辑在此 System 中实现
    /// - Component 只定义数据结构
    /// </summary>
    [EntitySystemOf(typeof(ETUnitBuffListComponent))]
    [FriendOf(typeof(ETUnitBuffListComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitBuffListComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitBuffListComponent self)
        {
        }

        /// <summary>
        /// 查找指定 Buff ID 的视图数据
        /// </summary>
        public static ET.Logic.ETUnitBuffListComponent.BuffViewData? FindBuffView(
            this ETUnitBuffListComponent self, int buffId)
        {
            for (int i = 0; i < self.BuffViews.Count; i++)
            {
                var b = self.BuffViews[i];
                if (b != null && b.BuffId == buffId)
                    return b;
            }
            return null;
        }

        /// <summary>
        /// 获取或添加 Buff 视图数据
        /// </summary>
        public static ET.Logic.ETUnitBuffListComponent.BuffViewData GetOrAddBuffView(
            this ETUnitBuffListComponent self, int buffId)
        {
            var b = FindBuffView(self, buffId);
            if (b != null)
                return b;

            b = new ET.Logic.ETUnitBuffListComponent.BuffViewData { BuffId = buffId };
            self.BuffViews.Add(b);
            return b;
        }

        /// <summary>
        /// 移除 Buff 视图数据
        /// </summary>
        public static bool RemoveBuffView(this ETUnitBuffListComponent self, int buffId)
        {
            for (int i = 0; i < self.BuffViews.Count; i++)
            {
                var b = self.BuffViews[i];
                if (b != null && b.BuffId == buffId)
                {
                    self.BuffViews.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 清除所有 Buff
        /// </summary>
        public static void ClearAll(this ETUnitBuffListComponent self)
        {
            self.BuffViews.Clear();
        }

        /// <summary>
        /// 添加 Buff 层数
        /// </summary>
        public static void AddStack(this ETUnitBuffListComponent self, int buffId)
        {
            var b = self.GetOrAddBuffView(buffId);
            b.Stacks++;
        }

        /// <summary>
        /// 移除 Buff 层数
        /// </summary>
        public static void RemoveStack(this ETUnitBuffListComponent self, int buffId)
        {
            var b = FindBuffView(self, buffId);
            if (b != null)
            {
                b.Stacks--;
                if (b.Stacks <= 0)
                {
                    RemoveBuffView(self, buffId);
                }
            }
        }

        /// <summary>
        /// 更新 Buff 持续时间
        /// </summary>
        public static void UpdateDuration(this ETUnitBuffListComponent self, float deltaTime)
        {
            for (int i = self.BuffViews.Count - 1; i >= 0; i--)
            {
                var b = self.BuffViews[i];
                if (b != null)
                {
                    b.RemainingDuration -= deltaTime;
                    if (b.RemainingDuration <= 0)
                    {
                        b.IsExpired = true;
                        self.BuffViews.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 获取拥有者单位
        /// </summary>
        public static ETUnit? GetOwner(this ETUnitBuffListComponent self)
        {
            return self.Parent as ETUnit;
        }
    }
}
