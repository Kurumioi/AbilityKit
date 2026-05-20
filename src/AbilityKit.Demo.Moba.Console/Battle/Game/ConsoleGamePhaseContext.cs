using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// 战斗阶段上下文，传递给所有 Feature
    /// </summary>
    public readonly struct ConsoleGamePhaseContext
    {
        public ConsoleGameEntry Entry { get; }
        public ConsoleBattleContext? BattleContext { get; }

        public ConsoleGamePhaseContext(ConsoleGameEntry entry, ConsoleBattleContext? battleContext = null)
        {
            Entry = entry;
            BattleContext = battleContext;
        }
    }
}
