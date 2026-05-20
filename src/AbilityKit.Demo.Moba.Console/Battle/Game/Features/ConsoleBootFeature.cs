namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// Boot 阶段 Feature
    /// 显示启动画面
    /// </summary>
    public sealed class ConsoleBootFeature : IGamePhaseFeature
    {
        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            Platform.Log.System("[BootFeature] Attached - showing boot screen...");
            Platform.Log.System("  _____          __  __ ______ _______ _    _ _____ ");
            Platform.Log.System(" |_   _|   /\\   |  \\/  |  ____|__   __| |  | |  __ \\ ");
            Platform.Log.System("   | |    /  \\  | \\  / | |__     | |  | |  | | |  | |");
            Platform.Log.System("   | |   / /\\ \\ | |\\/| |  __|    | |  | |  | | |  | |");
            Platform.Log.System("  _| |_ / ____ \\| |  | | |____   | |  | |__| | |__| |");
            Platform.Log.System(" |_____/_/    \\_\\_|  |_|______|  |_|   \\____/|_____/ ");
            Platform.Log.System("");
        }

        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            Platform.Log.System("[BootFeature] Detached");
        }

        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
            // Boot 阶段不需要每帧处理
        }
    }
}
