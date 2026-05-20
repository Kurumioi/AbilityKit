using System;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// Lobby 阶段 Feature
    /// 显示大厅界面，等待玩家开始游戏
    /// </summary>
    public sealed class ConsoleLobbyFeature : IGamePhaseFeature
    {
        private readonly ConsoleGameFlowDomain _domain;

        public ConsoleLobbyFeature(ConsoleGameFlowDomain domain)
        {
            _domain = domain ?? throw new ArgumentNullException(nameof(domain));
        }

        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            Platform.Log.System("[LobbyFeature] Attached - Welcome to AbilityKit MOBA!");
            Platform.Log.System("========================================");
            Platform.Log.System("  [1] Start Local Battle");
            Platform.Log.System("  [2] Start Auto Test");
            Platform.Log.System("  [Q] Quit");
            Platform.Log.System("========================================");
        }

        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            Platform.Log.System("[LobbyFeature] Detached");
        }

        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
            // Lobby 阶段等待玩家输入
            // 输入处理由 Platform 层处理
        }
    }
}
