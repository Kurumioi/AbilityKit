namespace ET.Logic
{
    /// <summary>
    /// ETUnit System
    /// 管理单位实体的生命周期
    ///
    /// 设计说明：
    /// - ETUnit 作为根实体，由 ETUnitSystem 管理生命周期
    /// - 子组件的生命周期由各自的 System 管理
    /// - ETUnit 的销毁由 ETUnitComponentSystem 统一处理
    /// </summary>
    [EntitySystemOf(typeof(ETUnit))]
    [FriendOf(typeof(ETUnit))]
    [FriendOf(typeof(ETUnitMetaComponent))]
    [FriendOf(typeof(ETUnitTransformComponent))]
    [FriendOf(typeof(ETUnitCharacterComponent))]
    [FriendOf(typeof(ETUnitSkillListComponent))]
    [FriendOf(typeof(ETUnitBuffListComponent))]
    public static partial class ETUnitSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnit self)
        {
            Log.Debug($"[ETUnit] Unit awake: EntityCode={self.GetEntityCode()}, Name={self.GetName()}");
        }
    }
}
