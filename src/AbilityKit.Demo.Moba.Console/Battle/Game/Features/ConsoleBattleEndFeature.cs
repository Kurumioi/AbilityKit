using AbilityKit.Demo.Moba.Console.Battle.Context;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// Battle End 阶段 Feature
    /// 显示战斗结束界面
    /// </summary>
    public sealed class ConsoleBattleEndFeature : IGamePhaseFeature
    {
        private ConsoleBattleContext? _context;

        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            _context = ctx.BattleContext;
            Platform.Log.System("");
            Platform.Log.System("========================================");
            Platform.Log.System("           BATTLE ENDED");
            Platform.Log.System("========================================");
            Platform.Log.System($"  Final Frame: {_context?.LastFrame ?? 0}");
            Platform.Log.System("----------------------------------------");
            Platform.Log.System("  Press [R] to Return to Lobby");
            Platform.Log.System("  Press [Q] to Quit");
            Platform.Log.System("========================================");
            Platform.Log.System("");
        }

        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            _context = null;
        }

        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
            // 等待玩家输入返回大厅
        }
    }
}
