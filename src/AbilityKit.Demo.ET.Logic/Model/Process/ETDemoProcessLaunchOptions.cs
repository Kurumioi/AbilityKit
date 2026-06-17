namespace ET.Logic
{
    /// <summary>
    /// Explicit launch options for the ET MOBA demo process.
    /// Keeps battle automation opt-in at entry-point level.
    /// </summary>
    public sealed class ETDemoProcessLaunchOptions
    {
        public string PlayerName { get; set; } = "TestPlayer";
        public bool AutoLogin { get; set; }
        public bool AutoEnterBattle { get; set; }
        public ETLocalMobaScenarioConfig ScenarioConfig { get; set; } = ETLocalMobaScenarioConfig.CreateLocalScenarioDefaults();
 
        public static ETDemoProcessLaunchOptions CreateLocalScenarioDefaults()
        {
            return new ETDemoProcessLaunchOptions
            {
                AutoLogin = false,
                AutoEnterBattle = false,
                ScenarioConfig = ETLocalMobaScenarioConfig.CreateLocalScenarioDefaults()
            };
        }

        public static ETDemoProcessLaunchOptions CreateLocalSmokeDefaults()
        {
            return new ETDemoProcessLaunchOptions
            {
                AutoLogin = true,
                AutoEnterBattle = true,
                ScenarioConfig = ETLocalMobaScenarioConfig.CreateLocalSmokeDefaults()
            };
        }
    }
}
