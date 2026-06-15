namespace ET.Logic
{
    /// <summary>
    /// Installs opt-in demo battle automation components outside the formal battle startup path.
    /// </summary>
    public static class ETBattleAutomationInstaller
    {
        public static void Install(Scene scene, ETBattleAutomationOptions options)
        {
            if (scene == null || options == null || !options.HasAnyAutomationEnabled)
            {
                return;
            }

            if (options.EnableAutoMoveTest)
            {
                var autoTest = scene.GetComponent<ETBattleAutoTestComponent>() ?? scene.AddComponent<ETBattleAutoTestComponent>();
                autoTest.MoveIntervalFrames = options.MoveIntervalFrames;
                autoTest.MoveSpeed = options.MoveSpeed;
                autoTest.Enable();
                Log.Info($"[ETBattleSmokeAutomation] Auto move installed: IntervalFrames={autoTest.MoveIntervalFrames}, MoveSpeed={autoTest.MoveSpeed:F2}");
            }

            if (options.EnableSkillTest)
            {
                var skillTest = scene.GetComponent<ETBattleSkillTestComponent>() ?? scene.AddComponent<ETBattleSkillTestComponent>();
                skillTest.SkillIntervalFrames = options.SkillIntervalFrames;
                skillTest.SkillSlot = options.SkillSlot;
                skillTest.Enable();
                Log.Info($"[ETBattleSmokeAutomation] Skill test installed: IntervalFrames={skillTest.SkillIntervalFrames}, Slot={skillTest.SkillSlot}");
            }
        }
    }
}
