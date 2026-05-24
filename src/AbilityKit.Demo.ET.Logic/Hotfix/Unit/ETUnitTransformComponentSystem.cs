using System;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitTransformComponent System
    /// 管理单位变换逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETUnitTransformComponent))]
    [FriendOf(typeof(ETUnitTransformComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitTransformComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitTransformComponent self)
        {
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        public static void SetPosition(this ETUnitTransformComponent self, float x, float y)
        {
            self.X = x;
            self.Y = y;
        }

        /// <summary>
        /// 设置目标位置
        /// </summary>
        public static void SetTarget(this ETUnitTransformComponent self, float targetX, float targetY)
        {
            self.TargetX = targetX;
            self.TargetY = targetY;
        }

        /// <summary>
        /// 设置朝向
        /// </summary>
        public static void SetRotation(this ETUnitTransformComponent self, float rotation)
        {
            self.Rotation = rotation;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public static void StopMove(this ETUnitTransformComponent self)
        {
            self.TargetX = 0;
            self.TargetY = 0;
        }

        /// <summary>
        /// 添加目标偏移量
        /// </summary>
        public static void AddTargetOffset(this ETUnitTransformComponent self, float dx, float dy)
        {
            self.TargetX += dx;
            self.TargetY += dy;
        }

        /// <summary>
        /// 计算距离平方
        /// </summary>
        public static float DistanceSquared(this ETUnitTransformComponent self, float x, float y)
        {
            float dx = self.X - x;
            float dy = self.Y - y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 计算到目标点的距离
        /// </summary>
        public static float DistanceTo(this ETUnitTransformComponent self, float x, float y)
        {
            float dx = self.X - x;
            float dy = self.Y - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 获取拥有者单位
        /// </summary>
        public static ETUnit? GetOwner(this ETUnitTransformComponent self)
        {
            return self.Parent as ETUnit;
        }
    }
}
