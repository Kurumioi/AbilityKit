namespace ET.Logic
{
    /// <summary>
    /// 技能测试组件（纯数据）
    ///
    /// 职责：
    /// - 仅存储测试参数和状态
    /// - 业务逻辑由 ETBattleSkillTestComponentSystem 处理
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleSkillTestComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ========== 测试参数 ==========

        /// <summary>
        /// 是否启用技能测试
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// 测试的 ActorId（运行时自增 ID）
        /// </summary>
        public int TestActorId { get; set; }

        /// <summary>
        /// 测试的 PlayerId（与 moba.core 中 MobaPlayerActorMapService 注册时一致）
        /// </summary>
        public string TestPlayerId { get; set; }

        /// <summary>
        /// 技能槽位
        /// </summary>
        public int SkillSlot { get; set; } = BattleTestConfig.DefaultSkillSlot;

        /// <summary>
        /// 技能释放间隔帧数
        /// </summary>
        public int SkillIntervalFrames { get; set; } = BattleTestConfig.DefaultSkillIntervalFrames;

        // ========== 统计信息 ==========

        /// <summary>
        /// 技能释放计数
        /// </summary>
        public int SkillCastCount { get; set; }

        /// <summary>
        /// 上次释放帧号
        /// </summary>
        public int LastCastFrame { get; set; }

        /// <summary>
        /// 初始化技能测试
        /// </summary>
        public void Initialize(int actorId, string playerId, int skillSlot = 0)
        {
            TestActorId = actorId;
            TestPlayerId = playerId;
            SkillSlot = skillSlot;
            SkillCastCount = 0;
            LastCastFrame = 0;
        }

        public void Awake()
        {
        }

        public void Destroy()
        {
        }
    }
}
