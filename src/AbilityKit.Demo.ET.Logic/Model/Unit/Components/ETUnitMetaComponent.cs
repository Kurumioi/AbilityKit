using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位元数据组件
    /// 存储实体的身份信息
    /// </summary>
    public class ETUnitMetaComponent : Entity, IAwake
    {
        /// <summary>
        /// 逻辑层 ID，用于和 moba.core 交互
        /// </summary>
        public int EntityCode { get; set; }

        public ActorKind Kind { get; set; } = ActorKind.None;

        public string Name { get; set; }

        /// <summary>
        /// 是否是本地玩家
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>
        /// AbilityKit 关联的实体 ID
        /// </summary>
        public long AbilityEntityId { get; set; }

        public void Awake()
        {
        }
    }
}
