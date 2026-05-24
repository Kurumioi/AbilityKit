using System;
using System.Collections.Generic;
using System.Linq;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Console Platform 层 - 渲染器
    /// 封装所有 Console 特定的渲染输出
    /// </summary>
    public static class ConsoleBattleRenderer
    {
        /// <summary>
        /// 渲染战斗开始
        /// </summary>
        public static void RenderBattleStart(long battleId)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[BATTLE] Battle {battleId} STARTED!");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 渲染战斗结束
        /// </summary>
        public static void RenderBattleEnd(bool isVictory)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"[RESULT] {(isVictory ? "VICTORY!" : "DEFEAT!")}");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
        }

        /// <summary>
        /// 渲染帮助信息
        /// </summary>
        public static void RenderHelp()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Controls:");
            Console.WriteLine("  W/Up - Move up");
            Console.WriteLine("  S/Down - Move down");
            Console.WriteLine("  A/Left - Move left");
            Console.WriteLine("  D/Right - Move right");
            Console.WriteLine("  1/2/3/4 - Cast skill");
            Console.WriteLine("  SPACE - Stop");
            Console.WriteLine("  Q - Quit");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        /// <summary>
        /// 渲染单位视图列表
        /// </summary>
        public static void RenderUnitViews(Dictionary<long, UnitViewRenderData> unitViews)
        {
            // 清屏
            Console.Clear();

            // 渲染边界
            Console.WriteLine("============================================================");
            Console.WriteLine($"Battle View - Units: {unitViews.Count}");
            Console.WriteLine("============================================================");

            // 按 Y 坐标排序（从上到下）
            var sortedUnits = unitViews.Values
                .OrderByDescending(v => v.Y)
                .ToList();

            foreach (var view in sortedUnits)
            {
                // 单位符号
                string symbol = view.Kind switch
                {
                    ActorKind.Character => view.IsLocalPlayer ? "@" : "A",
                    ActorKind.Monster => "M",
                    _ => "?"
                };

                // 颜色
                string color;
                if (view.IsDead)
                {
                    color = "\x1b[90m"; // 灰色
                }
                else if (view.Hp < view.MaxHp * 0.3f)
                {
                    color = "\x1b[31m"; // 红色（低血量）
                }
                else
                {
                    color = "\x1b[32m"; // 绿色
                }

                string reset = "\x1b[0m";

                // 血条
                float hpPercent = view.MaxHp > 0 ? view.Hp / view.MaxHp : 0;
                int hpBarWidth = 10;
                int filledBars = (int)(hpPercent * hpBarWidth);
                string hpBar = new string('|', filledBars) + new string('-', hpBarWidth - filledBars);

                Console.WriteLine($"{color}[{symbol}] {view.Name,-15} HP:[{hpBar}] {view.Hp:F0}/{view.MaxHp} @ ({view.RenderX:F1}, {view.RenderY:F1}){reset}");
            }

            Console.WriteLine("============================================================");
        }

        /// <summary>
        /// 单位视图渲染数据
        /// </summary>
        public class UnitViewRenderData
        {
            public long ActorId { get; set; }
            public string Name { get; set; }
            public ActorKind Kind { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float RenderX { get; set; }
            public float RenderY { get; set; }
            public float Hp { get; set; }
            public float MaxHp { get; set; }
            public bool IsDead { get; set; }
            public bool IsLocalPlayer { get; set; }
        }
    }
}
