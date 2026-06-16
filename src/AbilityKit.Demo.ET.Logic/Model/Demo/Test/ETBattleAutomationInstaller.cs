using AbilityKit.Demo.Moba.Share;

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
                Log.Info($"[ETBattleAutomation] Auto move installed: IntervalFrames={autoTest.MoveIntervalFrames}, MoveSpeed={autoTest.MoveSpeed:F2}");
            }

            if (options.EnableSkillTest)
            {
                var skillTest = scene.GetComponent<ETBattleSkillTestComponent>() ?? scene.AddComponent<ETBattleSkillTestComponent>();
                skillTest.SkillIntervalFrames = options.SkillIntervalFrames;
                skillTest.SkillSlot = options.SkillSlot;
                skillTest.Enable();
                Log.Info($"[ETBattleAutomation] Skill test installed: IntervalFrames={skillTest.SkillIntervalFrames}, Slot={skillTest.SkillSlot}");
            }
        }

        public static void InitializeFromEnterGameSnapshot(Scene scene, ETBattleComponent battleComponent, in FrameSnapshotData snapshot)
        {
            if (scene == null || battleComponent == null)
            {
                return;
            }

            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            var skillTest = scene.GetComponent<ETBattleSkillTestComponent>();
            if (autoTest == null && skillTest == null)
            {
                return;
            }

            int actorIdToUse = 0;
            string playerIdToUse = null;
            float startX = 0f;
            float startY = 0f;

            if (snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0)
            {
                var firstSpawn = snapshot.ActorSpawns[0];
                actorIdToUse = firstSpawn.ActorId;
                playerIdToUse = firstSpawn.PlayerId;
                startX = firstSpawn.PositionX;
                startY = firstSpawn.PositionY;
            }
            else if (battleComponent.PlayerActorId > 0)
            {
                actorIdToUse = (int)battleComponent.PlayerActorId;
                playerIdToUse = battleComponent.PlayerId.ToString();
            }

            if (autoTest != null && playerIdToUse != null)
            {
                autoTest.Initialize(actorIdToUse, playerIdToUse, startX, startY);
            }

            if (skillTest != null && playerIdToUse != null)
            {
                skillTest.Initialize(actorIdToUse, playerIdToUse, 0);
            }
        }
    }
}
