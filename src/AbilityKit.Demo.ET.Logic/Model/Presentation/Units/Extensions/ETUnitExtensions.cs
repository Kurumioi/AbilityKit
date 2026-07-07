namespace ET.Logic
{
    /// <summary>
    /// ETUnit 扩展方法。
    /// 提供访问 ETUnit 的便捷方法。
    /// </summary>
    public static class ETUnitExtensions
    {
        /// <summary>
        /// 从 ETUnit 获取实体编码。
        /// </summary>
        public static int GetEntityCode(this ETUnit self)
        {
            return self?.EntityCode ?? 0;
        }

        /// <summary>
        /// 从 ETUnit 获取名称。
        /// </summary>
        public static string GetName(this ETUnit self)
        {
            return self?.Name ?? string.Empty;
        }

        /// <summary>
        /// 获取当前 HP。
        /// </summary>
        public static float GetHp(this ETUnit self)
        {
            return self?.Hp ?? 0f;
        }

        /// <summary>
        /// 检查单位是否死亡。
        /// </summary>
        public static bool IsDead(this ETUnit self)
        {
            return self?.IsDead ?? true;
        }
    }
}
