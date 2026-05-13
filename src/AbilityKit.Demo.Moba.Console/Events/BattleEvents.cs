using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Events
{
    /// <summary>
    /// 移动输入处理完成事件
    /// </summary>
    public readonly struct MoveInputProcessedEvent
    {
        public int ActorId { get; init; }
        public float Dx { get; init; }
        public float Dz { get; init; }
    }

    /// <summary>
    /// 技能执行事件
    /// </summary>
    public readonly struct SkillExecutedEvent
    {
        public int ActorId { get; init; }
        public int Slot { get; init; }
        public bool Success { get; init; }
        public string FailReason { get; init; }
    }

    /// <summary>
    /// 帧同步事件
    /// </summary>
    public readonly struct FrameSyncEvent
    {
        public int Frame { get; init; }
        public int ActorCount { get; init; }
        public double LogicTimeSeconds { get; init; }
    }

    /// <summary>
    /// 实体更新事件
    /// </summary>
    public readonly struct EntityUpdatedEvent
    {
        public int ActorId { get; init; }
        public float HP { get; init; }
        public float MaxHp { get; init; }
        public float X { get; init; }
        public float Z { get; init; }
    }

    /// <summary>
    /// 实体销毁事件
    /// </summary>
    public readonly struct EntityDestroyedEvent
    {
        public int ActorId { get; init; }
    }

    /// <summary>
    /// 实体创建事件
    /// </summary>
    public readonly struct EntityCreatedEvent
    {
        public int ActorId { get; init; }
        public string Name { get; init; }
        public float X { get; init; }
        public float Z { get; init; }
        public float HP { get; init; }
        public float MaxHp { get; init; }
    }

    /// <summary>
    /// 阶段切换事件
    /// </summary>
    public readonly struct PhaseChangedEvent
    {
        public string PhaseName { get; init; }
        public string PreviousPhase { get; init; }
    }

    /// <summary>
    /// 伤害事件
    /// </summary>
    public readonly struct DamageEvent
    {
        public int SourceId { get; init; }
        public int TargetId { get; init; }
        public float Damage { get; init; }
        public int SkillId { get; init; }
    }
}
