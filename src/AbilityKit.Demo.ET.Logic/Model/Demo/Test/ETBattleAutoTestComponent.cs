using System;

namespace ET.Logic
{
    /// <summary>
    /// 战斗自动测试组件（纯数据）
    ///
    /// 职责：
    /// - 仅存储测试参数和状态
    /// - 业务逻辑由 ETBattleAutoTestComponentSystem 处理
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleAutoTestComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ========== 测试参数 ==========

        /// <summary>
        /// 是否启用自动测试
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 测试的 ActorId（moba.core 逻辑层 ID）
        /// </summary>
        public int TestActorId { get; set; }

        /// <summary>
        /// 测试的 PlayerId（与 moba.core 中 MobaPlayerActorMapService 注册时一致）
        /// </summary>
        public string TestPlayerId { get; set; }

        /// <summary>
        /// 移动命令间隔帧数
        /// </summary>
        public int MoveIntervalFrames { get; set; } = BattleTestConfig.DefaultMoveIntervalFrames;

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed { get; set; } = BattleTestConfig.DefaultMoveSpeed;

        // ========== 统计信息 ==========

        /// <summary>
        /// 移动命令计数
        /// </summary>
        public int MoveCommandCount { get; set; }

        /// <summary>
        /// 当前 X 位置
        /// </summary>
        public float CurrentX { get; set; }

        /// <summary>
        /// 当前 Y 位置（对应 Z 轴）
        /// </summary>
        public float CurrentY { get; set; }

        /// <summary>
        /// 移动总距离
        /// </summary>
        public float MoveDistance { get; set; }

        // ========== 移动方向控制 ==========

        /// <summary>
        /// 当前移动方向 X
        /// </summary>
        public float MoveDirX { get; set; } = 1f;

        /// <summary>
        /// 当前移动方向 Z
        /// </summary>
        public float MoveDirZ { get; set; } = 0f;

        /// <summary>
        /// 边界检测
        /// </summary>
        public float MinX { get; set; } = BattleTestConfig.MovementMinBound;
        public float MaxX { get; set; } = BattleTestConfig.MovementMaxBound;
        public float MinZ { get; set; } = BattleTestConfig.MovementMinBound;
        public float MaxZ { get; set; } = BattleTestConfig.MovementMaxBound;

        /// <summary>
        /// 初始化测试
        /// </summary>
        public void Initialize(int actorId, string playerId, float startX, float startZ)
        {
            TestActorId = actorId;
            TestPlayerId = playerId;
            CurrentX = startX;
            CurrentY = startZ;
            MoveCommandCount = 0;
            MoveDistance = 0f;

            // 初始方向：向 X 正方向移动
            MoveDirX = 1f;
            MoveDirZ = 0f;
        }

        public void Awake()
        {
        }

        public void Destroy()
        {
        }
    }
}
