namespace ET.Logic
{
    /// <summary>
    /// Explicit battle automation options for demo and smoke scenarios.
    /// Formal battle startup keeps automation disabled unless an entry point opts in.
    /// </summary>
    public sealed class ETBattleAutomationOptions
    {
        public static ETBattleAutomationOptions CreateDisabled()
        {
            return new ETBattleAutomationOptions();
        }

        public bool EnableAutoMoveTest { get; set; }
        public bool EnableSkillTest { get; set; }
        public bool HasAnyAutomationEnabled => EnableAutoMoveTest || EnableSkillTest;
        public int MoveIntervalFrames { get; set; } = BattleTestConfig.DefaultMoveIntervalFrames;
        public float MoveSpeed { get; set; } = BattleTestConfig.DefaultMoveSpeed;
        public int SkillIntervalFrames { get; set; } = BattleTestConfig.DefaultSkillIntervalFrames * 2;
        public int SkillSlot { get; set; } = BattleTestConfig.DefaultSkillSlot;

        public static ETBattleAutomationOptions CreateLocalSmokeDefaults()
        {
            return new ETBattleAutomationOptions
            {
                EnableAutoMoveTest = true,
                EnableSkillTest = true
            };
        }
    }
}
