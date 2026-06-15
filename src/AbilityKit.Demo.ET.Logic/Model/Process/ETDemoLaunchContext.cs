namespace ET.Logic
{
    /// <summary>
    /// Demo process launch context shared between App and Logic layers.
    /// </summary>
    public static class ETDemoLaunchContext
    {
        private static ETDemoProcessLaunchOptions _launchOptions = ETDemoProcessLaunchOptions.CreateLocalDemoDefaults();

        public static ETDemoProcessLaunchOptions LaunchOptions => _launchOptions;

        public static void SetLaunchOptions(ETDemoProcessLaunchOptions launchOptions)
        {
            _launchOptions = launchOptions ?? ETDemoProcessLaunchOptions.CreateLocalDemoDefaults();
        }
    }
}
