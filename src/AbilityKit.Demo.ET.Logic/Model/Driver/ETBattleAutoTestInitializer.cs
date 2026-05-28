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

            if (driver.PlayerSpawnData.Count == 0)
            {
                return;
            }

            var localPlayer = driver.PlayerSpawnData[0];
            var unit = ETUnitComponentSystem.GetUnit(unitComponent, localPlayer.ActorId);
            if (unit == null)
            {
                return;
            }

            autoTest.Initialize(localPlayer.ActorId, localPlayer.PlayerId, unit.X, unit.Y);
        }
    }
}
