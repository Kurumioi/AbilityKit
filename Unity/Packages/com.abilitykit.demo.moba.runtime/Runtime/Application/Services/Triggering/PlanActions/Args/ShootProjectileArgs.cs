namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// shoot_projectile Action 鐨勫己绫诲瀷鍙傛暟
    /// </summary>
    public readonly struct ShootProjectileArgs
    {
        /// <summary>
        /// 鍙戝皠鍣↖D
        /// </summary>
        public readonly int LauncherId;

        /// <summary>
        /// 寮逛綋ID
        /// </summary>
        public readonly int ProjectileId;

        public ShootProjectileArgs(int launcherId, int projectileId)
        {
            LauncherId = launcherId;
            ProjectileId = projectileId;
        }

        public static ShootProjectileArgs Default => new ShootProjectileArgs(0, 0);
    }
}
