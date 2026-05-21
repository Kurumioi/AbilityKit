using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Session;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// 战斗阶段上下文，传递给所有 Feature
    /// 对齐 Unity GamePhaseContext
    /// </summary>
    public readonly struct ConsoleGamePhaseContext
    {
        public ConsoleGameEntry Entry { get; }
        public ConsoleBattleContext? BattleContext { get; }
        public ConsoleSessionHooks? SessionHooks { get; }

        public ConsoleGamePhaseContext(ConsoleGameEntry entry, ConsoleBattleContext? battleContext = null, ConsoleSessionHooks? sessionHooks = null)
        {
            Entry = entry;
            BattleContext = battleContext;
            SessionHooks = sessionHooks;
        }
    }
}
