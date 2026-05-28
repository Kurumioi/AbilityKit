using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位实体（纯数据）
    ///
    /// 职责：
    /// - 存储单位的所有属性数据
    /// - 不包含任何业务逻辑
    /// - 业务逻辑由 ETUnitSystem 处理
    ///
    /// ID 设计说明：
    /// - Id（继承自 Entity）: 使用 moba.core 的 ActorId 作为 ET 实体 ID
    ///   创建时: AddChild&lt;ETUnit&gt;((long)logicActorId)
    ///   查询时: GetChild&lt;ETUnit&gt;(logicActorId)
    /// - LogicActorId: 冗余存储，方便访问（与 Id 相同）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETUnit : Entity, IAwake
    {
        // ========== 常量 ==========
        public const int MaxSkillSlots = 4;

        // ========== 标识 ==========

        /// <summary>
        /// 逻辑层 ActorId（与 Entity.Id 相同，用于与战斗逻辑层交互）
        /// 创建时 AddChild&lt;ETUnit&gt;((long)logicActorId)
        /// </summary>
        public int LogicActorId { get; set; }

        public int EntityCode { get; set; }
        public ActorKind Kind { get; set; } = ActorKind.None;
        public string Name { get; set; }

        // ========== 位置信息 ==========
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }

        // ========== 渲染插值信息 ==========
        public float RenderX { get; set; }
        public float RenderY { get; set; }
        public float PrevX { get; set; }
        public float PrevY { get; set; }
        public long LastUpdateTime { get; set; }

        // ========== 属性信息 ==========
        public float Hp { get; set; } = 100f;
        public float MaxHp { get; set; } = 100f;
        public float Attack { get; set; } = 10f;
        public float Defense { get; set; } = 5f;
        public float MoveSpeed { get; set; } = 5f;

        // ========== 状态 ==========
        public bool IsDead => Hp <= 0;
        public bool IsLocalPlayer { get; set; }

        // ========== 移动目标 ==========
        public float TargetX { get; set; }
        public float TargetY { get; set; }

        // ========== 技能冷却 ==========
        public float[] SkillCooldowns { get; set; } = new float[MaxSkillSlots];

        public void Awake()
        {
            if (SkillCooldowns == null || SkillCooldowns.Length != MaxSkillSlots)
                SkillCooldowns = new float[MaxSkillSlots];
            LastUpdateTime = Environment.TickCount64;
        }
    }
}
