namespace ET.Logic
{
    /// <summary>
    /// ETUnit System
    /// 管理单位实体的生命周期和业务逻辑
    ///
    /// 设计说明：
    /// - ETUnit 作为快照缓存实体（纯数据）
    /// - 所有业务逻辑在此 System 中处理
    /// - 生命周期由 ETUnitComponentSystem 统一管理
    /// </summary>
    [EntitySystemOf(typeof(ETUnit))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnit self)
        {
            Log.Debug($"[ETUnit] Unit awake: EntityCode={self.EntityCode}, Name={self.Name}");
        }

        /// <summary>
        /// 从快照更新变换数据
        /// </summary>
        public static void UpdateFromSnapshot(this ETUnit self, float x, float y, float rotation = 0, long updateFrame = 0)
        {
            self.PrevX = self.X;
            self.PrevY = self.Y;
            self.X = x;
            self.Y = y;
            self.Rotation = rotation;
            self.LastUpdateFrame = updateFrame;
        }

        /// <summary>
        /// 从快照更新 HP 数据
        /// </summary>
        public static void UpdateHpFromSnapshot(this ETUnit self, float hp, float maxHp)
        {
            self.Hp = hp;
            self.MaxHp = maxHp;
        }

        /// <summary>
        /// 更新渲染位置（插值）
        /// </summary>
        public static void UpdateRenderPosition(this ETUnit self, float interpolationSpeed, float deltaTime)
        {
            self.RenderX += (self.X - self.RenderX) * interpolationSpeed * deltaTime;
            self.RenderY += (self.Y - self.RenderY) * interpolationSpeed * deltaTime;
        }
    }
}
