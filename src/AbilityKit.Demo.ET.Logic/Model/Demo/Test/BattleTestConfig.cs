namespace ET.Logic
{
    /// <summary>
    /// 战斗测试配置常量
    ///
    /// Design:
    /// - 集中管理所有测试相关的配置参数
    /// - 使用 const 确保编译期常量，无运行时开销
    /// </summary>
    public static class BattleTestConfig
    {
        // ========== 移动测试配置 ==========
        public const int DefaultMoveIntervalFrames = 10;
        public const float DefaultMoveSpeed = 5f;
        public const float DefaultTargetX = 50f;
        public const float DefaultTargetY = 0f;

        // ========== 技能测试配置 ==========
        public const int DefaultSkillIntervalFrames = 60;
        public const int DefaultSkillSlot = 0;

        // ========== 测试移动范围 ==========
        public const float MovementAmplitude = 5f;
        public const float MovementMinBound = -100f;
        public const float MovementMaxBound = 100f;
    }
}
