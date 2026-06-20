namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 战斗溯源节点种类枚举
    /// </summary>
    public enum MobaTraceKind : byte
    {
        None = 0,

        // 技能相关
        SkillCast = 1,
        SkillEffect = 2,
        SkillPhase = 3,

        // 效果相关
        EffectExecution = 10,
        EffectAction = 11,

        // Buff 相关
        BuffApply = 20,
        BuffTick = 21,
        BuffRemove = 22,

        // 弹道相关
        ProjectileLaunch = 30,
        ProjectileHit = 31,

        // 区域相关
        AreaSpawn = 40,
        AreaEnter = 41,
        AreaExit = 42,
        AreaExpire = 43,
        AreaStay = 44,

        // 召唤物相关
        SummonSpawn = 50,
        SummonDeath = 51,

        // 单位生命周期相关
        UnitSpawn = 60,
        UnitDespawn = 61,
        UnitDeath = 62,

        // 伤害管线相关
        DamageAttack = 70,
        DamageCalc = 71,
        DamageApply = 72,

        // 表现事件相关
        PresentationPlay = 80,
        PresentationStop = 81,
    }

    /// <summary>
    /// MOBA 战斗溯源结束原因枚举
    /// </summary>
    public enum MobaTraceEndReason : byte
    {
        None = 0,

        // 通用
        Completed = 1,
        Interrupted = 2,
        Cancelled = 3,
        Failed = 4,

        // 效果相关
        EffectConditionNotMet = 10,
        EffectNoTarget = 11,

        // Buff 相关
        BuffExpired = 20,
        BuffDispelled = 21,
        BuffStacksExceeded = 22,

        // 弹道相关
        ProjectileExpired = 30,
        ProjectileObstructed = 31,

        // 区域相关
        AreaExpired = 40,
        AreaMaxEnterCount = 41,
    }
}
