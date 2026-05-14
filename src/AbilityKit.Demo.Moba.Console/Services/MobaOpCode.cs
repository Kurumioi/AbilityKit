using AbilityKit.Protocol.Moba.FrameSync;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// Moba 操作码定义
    /// 已迁移到 AbilityKit.Protocol.Moba.FrameSync.InputOpCodes
    /// 此处保留向后兼容别名
    /// </summary>
    [System.Obsolete("Use AbilityKit.Protocol.Moba.FrameSync.InputOpCodes instead")]
    public enum MobaOpCode
    {
        Ready = InputOpCodes.Ready,
        Unready = InputOpCodes.Unready,
        Move = InputOpCodes.Move,
        Stop = InputOpCodes.Stop,
        Attack = InputOpCodes.Attack,
        Skill1 = InputOpCodes.Skill1,
        Skill2 = InputOpCodes.Skill2,
        Skill3 = InputOpCodes.Skill3,
        SkillInput = InputOpCodes.SkillInput,

        // 快照操作码
        LobbySnapshot = InputOpCodes.LobbySnapshot,
        EnterGameSnapshot = InputOpCodes.EnterGameSnapshot,
        ActorTransformSnapshot = InputOpCodes.ActorTransformSnapshot,
        StateHashSnapshot = InputOpCodes.StateHashSnapshot,
        ActorSpawnSnapshot = InputOpCodes.ActorSpawnSnapshot,
        ProjectileEventSnapshot = InputOpCodes.ProjectileEventSnapshot,
        DamageEventSnapshot = InputOpCodes.DamageEventSnapshot,
        ActorDespawnSnapshot = InputOpCodes.ActorDespawnSnapshot,
        AreaEventSnapshot = InputOpCodes.AreaEventSnapshot,
    }

    /// <summary>
    /// 技能输入阶段
    /// </summary>
    public enum SkillInputPhase
    {
        Press = 1,
        Hold = 2,
        Release = 3,
        Cancel = 4,
    }

    /// <summary>
    /// 技能施放阶段
    /// </summary>
    public enum SkillCastStage
    {
        PreCast = 0,
        Cast = 1,
        Timeline = 2,
        Completed = 3,
        Interrupted = 4,
    }
}
