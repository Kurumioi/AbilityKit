namespace ET.Logic
{
    /// <summary>
    /// ETUnit System
    /// 管理单位实体的生命周期
    ///
    /// 设计说明：
    /// - ETUnit 作为快照缓存实体
    /// - 生命周期由 ETUnitComponentSystem 统一处理
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
    }
}
