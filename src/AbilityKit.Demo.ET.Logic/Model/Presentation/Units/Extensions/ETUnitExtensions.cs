namespace ET.Logic
{
    /// <summary>
    /// ETUnit Extensions
    /// Provides convenience methods for accessing ETUnit
    /// </summary>
    public static class ETUnitExtensions
    {
        /// <summary>
        /// Get entity code from ETUnit
        /// </summary>
        public static int GetEntityCode(this ETUnit self)
        {
            return self?.EntityCode ?? 0;
        }

        /// <summary>
        /// Get name from ETUnit
        /// </summary>
        public static string GetName(this ETUnit self)
        {
            return self?.Name ?? string.Empty;
        }

        /// <summary>
        /// Get current HP
        /// </summary>
        public static float GetHp(this ETUnit self)
        {
            return self?.Hp ?? 0f;
        }

        /// <summary>
        /// Check if unit is dead
        /// </summary>
        public static bool IsDead(this ETUnit self)
        {
            return self?.IsDead ?? true;
        }
    }
}
