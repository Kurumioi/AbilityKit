using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Flow;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// 战斗上下文 Feature
    /// 对齐 Unity BattleContextFeature，管理 Context 的生命周期
    /// </summary>
    public sealed class BattleContextFeature : IGameModule<ConsoleBattleContext>
    {
        /// <summary>
        /// OnAttach: 从对象池获取 Context 并 Attach
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
        /// OnDetach: 归还 Context 到对象池
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
