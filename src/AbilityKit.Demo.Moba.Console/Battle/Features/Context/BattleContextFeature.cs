using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Game;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// 战斗上下文 Feature
    /// 对齐 Unity BattleContextFeature，管理 Context 的生命周期
    /// </summary>
    public sealed class BattleContextFeature : IGameModule<ConsoleBattleContext>, IGamePhaseFeature
    {
        /// <summary>
        /// IGamePhaseFeature.OnAttach
        /// </summary>
        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            OnAttach(ctx.BattleContext!);
        }

        /// <summary>
        /// IGamePhaseFeature.OnDetach
        /// </summary>
        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            OnDetach(ctx.BattleContext!);
        }

        /// <summary>
        /// IGamePhaseFeature.Tick
        /// </summary>
        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
        }

        /// <summary>
        /// IGameModule.OnAttach: 从对象池获取 Context 并 Attach
        /// </summary>
        public void OnAttach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                Platform.Log.Error("[BattleContextFeature] OnAttach failed: context is null");
                return;
            }

            // 确保 Context 已重置
            if (context.IsInitialized)
            {
                Platform.Log.Warn("[BattleContextFeature] Context already initialized, resetting...");
                context.Reset();
            }

            Platform.Log.Battle("[BattleContextFeature] Context attached");
        }

        /// <summary>
        /// IGameModule.OnDetach: 归还 Context 到对象池
        /// </summary>
        public void OnDetach(ConsoleBattleContext context)
        {
            if (context == null)
            {
                return;
            }

            // 重置并归还到对象池
            context.Reset();
            ConsoleBattleContext.Return(context);

            Platform.Log.Battle("[BattleContextFeature] Context returned to pool");
        }
    }
}
