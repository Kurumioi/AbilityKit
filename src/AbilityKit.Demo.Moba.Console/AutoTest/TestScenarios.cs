using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// 自动测试场景定义
    /// 提供预设的战斗测试场景
    /// </summary>
    public static class TestScenarios
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// 简单移动测试
        /// </summary>
        public static void AddSimpleMovement(AutoTestInputFeature feature, int cycles = 3)
        {
            for (int i = 0; i < cycles; i++)
            {
                feature.RegisterStep(InputStep.Move(1f, 0f, 10));  // 向右移动
                feature.RegisterStep(InputStep.Wait(5));
                feature.RegisterStep(InputStep.Move(0f, 1f, 10));  // 向上移动
                feature.RegisterStep(InputStep.Wait(5));
                feature.RegisterStep(InputStep.Move(-1f, 0f, 10)); // 向左移动
                feature.RegisterStep(InputStep.Wait(5));
                feature.RegisterStep(InputStep.Move(0f, -1f, 10)); // 向下移动
                feature.RegisterStep(InputStep.Idle(10));          // 停止
            }
            Log.Trace($"[Scenario] Added SimpleMovement: {cycles} cycles");
        }

        /// <summary>
        /// 随机移动测试
        /// </summary>
        public static void AddRandomMovement(AutoTestInputFeature feature, int steps = 20)
        {
            for (int i = 0; i < steps; i++)
            {
                float dx = (float)(_random.NextDouble() * 2 - 1);
                float dz = (float)(_random.NextDouble() * 2 - 1);
                int duration = _random.Next(5, 15);
                feature.RegisterStep(InputStep.Move(dx, dz, duration));
            }
            feature.RegisterStep(InputStep.Idle(10));
            Log.Trace($"[Scenario] Added RandomMovement: {steps} steps");
        }

        /// <summary>
        /// 技能释放测试
        /// </summary>
        public static void AddSkillCast(AutoTestInputFeature feature, int skillSlot = 1, int repeats = 3)
        {
            for (int i = 0; i < repeats; i++)
            {
                feature.RegisterStep(InputStep.Skill(skillSlot, 1));  // 释放技能
                feature.RegisterStep(InputStep.Wait(30));             // 等待冷却
            }
            Log.Trace($"[Scenario] Added SkillCast: Slot {skillSlot}, {repeats} times");
        }

        /// <summary>
        /// 移动+技能测试
        /// </summary>
        public static void AddMoveAndCast(AutoTestInputFeature feature, int cycles = 5)
        {
            for (int i = 0; i < cycles; i++)
            {
                // 移动
                feature.RegisterStep(InputStep.Move(1f, 0f, 10));
                feature.RegisterStep(InputStep.Wait(5));
                // 释放技能
                feature.RegisterStep(InputStep.Skill(1, 1));
                feature.RegisterStep(InputStep.Wait(30));
                // 移动
                feature.RegisterStep(InputStep.Move(0f, 1f, 10));
                feature.RegisterStep(InputStep.Wait(5));
                // 释放技能
                feature.RegisterStep(InputStep.Skill(2, 1));
                feature.RegisterStep(InputStep.Wait(30));
            }
            feature.RegisterStep(InputStep.Idle(10));
            Log.Trace($"[Scenario] Added MoveAndCast: {cycles} cycles");
        }

        /// <summary>
        /// 完整战斗测试
        /// </summary>
        public static void AddFullBattleTest(AutoTestInputFeature feature)
        {
            // 移动并释放技能
            AddMoveAndCast(feature, 3);
            
            // 随机移动
            AddRandomMovement(feature, 10);
            
            // 连续释放所有技能
            for (int slot = 1; slot <= 3; slot++)
            {
                AddSkillCast(feature, slot, 2);
            }
            
            Log.Trace("[Scenario] Added FullBattleTest");
        }

        /// <summary>
        /// 压力测试（高频输入）
        /// </summary>
        public static void AddStressTest(AutoTestInputFeature feature, int durationTicks = 300)
        {
            int step = 0;
            while (step < durationTicks)
            {
                int action = _random.Next(4);
                switch (action)
                {
                    case 0:
                        feature.RegisterStep(InputStep.Move(
                            (float)(_random.NextDouble() * 2 - 1),
                            (float)(_random.NextDouble() * 2 - 1),
                            3));
                        break;
                    case 1:
                        feature.RegisterStep(InputStep.Skill(_random.Next(1, 4), 1));
                        break;
                    default:
                        feature.RegisterStep(InputStep.Idle(1));
                        break;
                }
                step += 3;
            }
            feature.RegisterStep(InputStep.Idle(10));
            Log.Trace($"[Scenario] Added StressTest: {durationTicks} ticks");
        }
    }

    /// <summary>
    /// 测试场景接口
    /// </summary>
    public interface ITestScenario
    {
        string Name { get; }
        void Apply(AutoTestInputFeature feature);
    }

    /// <summary>
    /// 简单移动场景
    /// </summary>
    public sealed class SimpleMovementScenario : ITestScenario
    {
        public string Name => "SimpleMovement";

        public void Apply(AutoTestInputFeature feature)
        {
            TestScenarios.AddSimpleMovement(feature);
        }
    }

    /// <summary>
    /// 技能释放场景
    /// </summary>
    public sealed class SkillCastScenario : ITestScenario
    {
        public string Name => "SkillCast";
        public int SkillSlot { get; set; } = 1;
        public int Repeats { get; set; } = 3;

        public void Apply(AutoTestInputFeature feature)
        {
            TestScenarios.AddSkillCast(feature, SkillSlot, Repeats);
        }
    }

    /// <summary>
    /// 移动并释放技能场景
    /// </summary>
    public sealed class MoveAndCastScenario : ITestScenario
    {
        public string Name => "MoveAndCast";
        public int Cycles { get; set; } = 5;

        public void Apply(AutoTestInputFeature feature)
        {
            TestScenarios.AddMoveAndCast(feature, Cycles);
        }
    }

    /// <summary>
    /// 完整战斗场景
    /// </summary>
    public sealed class FullBattleScenario : ITestScenario
    {
        public string Name => "FullBattle";

        public void Apply(AutoTestInputFeature feature)
        {
            TestScenarios.AddFullBattleTest(feature);
        }
    }

    /// <summary>
    /// 压力测试场景
    /// </summary>
    public sealed class StressTestScenario : ITestScenario
    {
        public string Name => "StressTest";
        public int DurationTicks { get; set; } = 300;

        public void Apply(AutoTestInputFeature feature)
        {
            TestScenarios.AddStressTest(feature, DurationTicks);
        }
    }
}
