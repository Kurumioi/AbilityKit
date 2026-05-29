namespace ET.Logic
{
    /// <summary>
    /// Initializes the demo auto-test component after ET units have been created.
    /// </summary>
    public static class ETBattleAutoTestInitializer
    {
        public static void Initialize(ETMobaBattleDriver driver)
        {
            var scene = driver.Scene();
            if (scene == null)
            {
                return;
            }

            var autoTest = scene.GetComponent<ETBattleAutoTestComponent>();
            if (autoTest == null)
            {
                return;
            }

            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            var unit = unitComponent.GetLocalPlayerUnit() ?? unitComponent.GetFirstUnit();
            if (unit == null)
            {
                return;
            }

            var playerId = driver.PlayerSpawnData.Count > 0 ? driver.PlayerSpawnData[0].PlayerId : string.Empty;
            autoTest.Initialize(unit.LogicActorId, playerId, unit.X, unit.Y);
        }
    }
}
