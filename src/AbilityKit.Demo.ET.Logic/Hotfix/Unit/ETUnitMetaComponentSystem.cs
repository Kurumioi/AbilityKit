using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETUnitMetaComponent System
    /// 管理单位元数据逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETUnitMetaComponent))]
    [FriendOf(typeof(ETUnitMetaComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitMetaComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitMetaComponent self)
        {
        }

        /// <summary>
        /// 初始化元数据
        /// </summary>
        public static void Initialize(
            this ETUnitMetaComponent self,
            int entityCode,
            ActorKind kind,
            string name,
            bool isLocalPlayer = false,
            long abilityEntityId = 0)
        {
            self.EntityCode = entityCode;
            self.Kind = kind;
            self.Name = name;
            self.IsLocalPlayer = isLocalPlayer;
            self.AbilityEntityId = abilityEntityId;
        }

        /// <summary>
        /// 设置 AbilityEntityId
        /// </summary>
        public static void SetAbilityEntityId(this ETUnitMetaComponent self, long abilityEntityId)
        {
            self.AbilityEntityId = abilityEntityId;
        }

        /// <summary>
        /// 设置逻辑层 EntityCode
        /// </summary>
        public static void SetEntityCode(this ETUnitMetaComponent self, int entityCode)
        {
            self.EntityCode = entityCode;
        }

        /// <summary>
        /// 获取拥有者单位
        /// </summary>
        public static ETUnit? GetOwner(this ETUnitMetaComponent self)
        {
            return self.Parent as ETUnit;
        }
    }
}
