using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// 战斗调试 Feature
    /// 对齐 Unity BattleDebugOnGUIFeature，提供调试信息输出
    /// </summary>
    public sealed class ConsoleDebugFeature : IGameModule<ConsoleBattleContext>
    {
        private ConsoleBattleContext _context;

        /// <summary>
        /// 是否启用调试信息
        /// </summary>
        public bool EnableDebugInfo { get; set; } = true;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLog { get; set; } = false;

        /// <summary>
        /// OnAttach: 初始化调试功能
        /// </summary>
        public void OnAttach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[ConsoleDebugFeature] OnAttach failed: context is null");
                return;
            }

            _context = context;

            // 启用详细日志（如果开启）
            if (EnableVerboseLog)
            {
                Platform.Log.EnableTrace();
            }
            else
            {
                Platform.Log.DisableTrace();
            }

            Platform.Log.Debug("[ConsoleDebugFeature] Debug initialized");
        }

        /// <summary>
        /// OnDetach: 清理调试功能
        /// </summary>
        public void OnDetach(ConsoleBattleContext context)
        {
            _context = null;
            Platform.Log.Debug("[ConsoleDebugFeature] Debug disposed");
        }

        /// <summary>
        /// 打印战斗统计信息
        /// </summary>
        public void PrintBattleStats()
        {
            if (_context == null || !EnableDebugInfo) return;

            Platform.Log.Separator();
            Platform.Log.System("=== Battle Statistics ===");
            Platform.Log.System($"Frame: {_context.LastFrame}");
            Platform.Log.System($"Players: {_context.PlayerCount}");
            Platform.Log.System($"State: {_context.State}");
            Platform.Log.System($"Local ActorId: {_context.LocalActorId}");
            Platform.Log.Separator();
        }

        /// <summary>
        /// 打印实体统计信息
        /// </summary>
        public void PrintEntityStats()
        {
            if (_context == null || !EnableDebugInfo) return;

            var lookup = _context.EntityLookup;
            if (lookup == null) return;

            Platform.Log.Separator();
            Platform.Log.System("=== Entity Statistics ===");
            Platform.Log.System($"Entity Count: {lookup.Count}");
            Platform.Log.Separator();
        }

        /// <summary>
        /// 打印帧同步状态
        /// </summary>
        public void PrintFrameSyncStats()
        {
            if (_context == null || !EnableDebugInfo) return;

            Platform.Log.Separator();
            Platform.Log.System("=== Frame Sync Statistics ===");
            Platform.Log.System($"Frame: {_context.LastFrame}");
            Platform.Log.System($"Logic Time: {_context.LogicTimeSeconds:F2}s");
            Platform.Log.System($"State: {_context.State}");
            Platform.Log.Separator();
        }
    }
}
