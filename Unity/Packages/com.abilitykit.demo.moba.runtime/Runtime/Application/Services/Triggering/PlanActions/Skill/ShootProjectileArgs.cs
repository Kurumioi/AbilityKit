namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// shoot_projectile Action 的强类型参数。
    /// </summary>
    public readonly struct ShootProjectileArgs
    {
        /// <summary>
        /// 发射器 ID。
        /// </summary>
        public readonly int LauncherId;

        /// <summary>
        /// 弹体 ID。
        /// </summary>
        public readonly int ProjectileId;

        public readonly int ContinuousProcessId;

        public ShootProjectileArgs(int launcherId, int projectileId, int continuousProcessId)
        {
            LauncherId = launcherId;
            ProjectileId = projectileId;
            ContinuousProcessId = continuousProcessId;
        }

        public static ShootProjectileArgs Default => new ShootProjectileArgs(0, 0, 0);
    }
}
