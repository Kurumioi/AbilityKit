using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// Console 游戏主入口
    /// 对齐 Unity GameEntry，管理流程域
    /// </summary>
    public sealed class ConsoleGameEntry : IDisposable
    {
        private static ConsoleGameEntry? _instance;
        public static ConsoleGameEntry Instance => _instance ?? throw new InvalidOperationException("ConsoleGameEntry not initialized");

        /// <summary>
        /// 游戏流程域
        /// </summary>
        public ConsoleGameFlowDomain Flow { get; }

        /// <summary>
        /// 当前战斗上下文
        /// </summary>
        public ConsoleBattleContext? CurrentBattleContext { get; internal set; }

        private ConsoleGameEntry()
        {
            Flow = new ConsoleGameFlowDomain(this);
        }

        /// <summary>
        /// 初始化单例
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("ConsoleGameEntry already initialized");
            }
            _instance = new ConsoleGameEntry();
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Tick(float deltaTime)
        {
            Flow.Step(deltaTime);
        }

        /// <summary>
        /// 销毁单例
        /// </summary>
        public void Dispose()
        {
            Flow?.Dispose();
            _instance = null;
        }
    }
}
