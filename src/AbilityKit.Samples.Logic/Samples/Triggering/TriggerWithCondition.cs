using System;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Triggering
{
    /// <summary>
    /// TriggerWithCondition - 带条件的触发器
    /// 演示如何使用 Predicate 条件控制触发器执行
    /// </summary>
    [Sample]
    public sealed class TriggerWithCondition : SampleBase
    {
        public override string Title => "Trigger with Condition";
        public override string Description => "使用条件控制触发器执行";
        public override SampleCategory Category => SampleCategory.Triggering;

        protected override void OnRun()
        {
            Log("=== 触发器与条件 ===");
            Output.Divider();

            Log("条件用于决定是否执行触发器");
            Log("");

            Log("Evaluate 方法:");
            Log("  bool Evaluate(in TArgs args, in ExecCtx<TCtx> ctx)");
            Log("  - 返回 true: 继续执行 Execute");
            Log("  - 返回 false: 跳过当前触发器");

            Output.Divider();

            Log("典型条件示例:");
            Output.Bullet("生命检查: Health > 50%");
            Output.Bullet("魔法检查: Mana >= 30");
            Output.Bullet("状态检查: HasTag(\"Stunned\") == false");
            Output.Bullet("冷却检查: CooldownReady(skillId)");
            Output.Bullet("距离检查: Distance < 10m");

            Output.Divider();

            Log("条件组合:");
            Output.Bullet("AllCondition: 所有条件都必须满足");
            Output.Bullet("AnyCondition: 任意条件满足即可");
            Output.Bullet("NotCondition: 取反条件");

            Output.Divider();

            Log("执行流程:");
            Log("  [开始]");
            Log("      |");
            Log("  [Trigger1.Evaluate] -> false (跳过)");
            Log("      |");
            Log("  [Trigger2.Evaluate] -> true");
            Log("      |");
            Log("  [Trigger2.Execute] -> 执行行为");
        }
    }
}
