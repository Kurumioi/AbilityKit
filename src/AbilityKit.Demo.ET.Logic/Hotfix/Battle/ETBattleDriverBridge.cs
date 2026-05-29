using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle driver bridge.
    /// </summary>
    public static class ETBattleDriverBridge
    {
        public static void Start(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.StartBattle();
            }
        }

        public static void Stop(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                driver.StopBattle();
            }
        }

        public static int GetCurrentFrame(ETBattleComponent self)
        {
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                return driver.CurrentFrame;
            }

            return 0;
        }

        public static void ProcessAutoTest(ETBattleComponent self, int currentFrame)
        {
            var scene = self.Scene();
            var autoTest = scene?.GetComponent<ETBattleAutoTestComponent>();
            if (autoTest == null || !autoTest.IsEnabled)
            {
                return;
            }

            if (!EnsureAutoTestPlayerInitialized(self, autoTest))
            {
                return;
            }

            autoTest.OnUpdate(currentFrame);
        }

        public static void ProcessSkillTest(ETBattleComponent self, int currentFrame)
        {
            var scene = self.Scene();
            var skillTest = scene?.GetComponent<ETBattleSkillTestComponent>();
            if (skillTest == null || !skillTest.IsEnabled)
            {
                return;
            }

            if (!EnsureSkillTestPlayerInitialized(self, skillTest))
            {
                return;
            }

            skillTest.OnUpdate(currentFrame);
        }

        private static bool EnsureAutoTestPlayerInitialized(ETBattleComponent self, ETBattleAutoTestComponent autoTest)
        {
            if (!(self.BattleDriver is ETMobaBattleDriver driver) || driver.PlayerSpawnData.Count == 0)
            {
                return !string.IsNullOrEmpty(autoTest.TestPlayerId);
            }

            var localPlayerId = driver.PlayerSpawnData[0].PlayerId;
            if (string.IsNullOrEmpty(localPlayerId))
            {
                return !string.IsNullOrEmpty(autoTest.TestPlayerId);
            }

            if (autoTest.TestPlayerId == localPlayerId)
            {
                return true;
            }

            ETBattleAutoTestInitializer.Initialize(driver);
            return autoTest.TestPlayerId == localPlayerId;
        }

        private static bool EnsureSkillTestPlayerInitialized(ETBattleComponent self, ETBattleSkillTestComponent skillTest)
        {
            if (!(self.BattleDriver is ETMobaBattleDriver driver) || driver.PlayerSpawnData.Count == 0)
            {
                return !string.IsNullOrEmpty(skillTest.TestPlayerId);
            }

            var localPlayerId = driver.PlayerSpawnData[0].PlayerId;
            if (string.IsNullOrEmpty(localPlayerId))
            {
                return !string.IsNullOrEmpty(skillTest.TestPlayerId);
            }

            if (skillTest.TestPlayerId == localPlayerId)
            {
                return true;
            }

            var scene = self.Scene();
            var unitComponent = scene?.GetComponent<ETUnitComponent>();
            var unit = unitComponent?.GetLocalPlayerUnit() ?? unitComponent?.GetFirstUnit();
            if (unit == null)
            {
                return false;
            }

            skillTest.Initialize(unit.LogicActorId, localPlayerId, skillTest.SkillSlot);
            Log.Info("[ETBattleSkillTest] Reinitialized with Runtime PlayerId=" + localPlayerId + ", ActorId=" + unit.LogicActorId);
            return true;
        }
    }
}
