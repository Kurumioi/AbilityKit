using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.RuleScheduler;

namespace AbilityKit.Triggering.Samples
{
    /// <summary>
    /// ID-based 触发系统示例。
    ///
    /// 本示例保留 TriggerAtomRegistry 的 ID 化配置思路，但调度部分统一演示正式 RuleSchedulerRegistry。
    /// 旧 SchedulerRegistry 仅作为 Runtime/Scheduler 兼容层保留，不再作为新样例入口。
    /// </summary>
    public static class SchedulerRegistryExample
    {
        /// <summary>
        /// 运行示例。
        /// </summary>
        public static void RunOnce()
        {
            Console.WriteLine("========== ID-based RuleScheduler 示例开始 ==========");
            Console.WriteLine();

            var atomRegistry = new TriggerAtomRegistry();
            var schedulerRegistry = new RuleSchedulerRegistry();
            Console.WriteLine("[1] 创建 TriggerAtomRegistry 和 RuleSchedulerRegistry");

            var actions = new Dictionary<string, Action<object>>
            {
                ["action.damage"] = ctx => Console.WriteLine("    ★ [执行行为] 造成伤害"),
                ["action.heal"] = ctx => Console.WriteLine("    ★ [执行行为] 治疗"),
                ["action.buff"] = ctx => Console.WriteLine("    ★ [执行行为] 添加 buff"),
            };
            Console.WriteLine("[2] 注册行为: damage, heal, buff");
            Console.WriteLine();

            var eventOnHit = EventId.Get("event.on_hit");
            var eventOnHeal = EventId.Get("event.on_heal");

            atomRegistry.Register(TriggerAtom.CreateUnconditional(
                TriggerAtomId.Get("atom.counter_attack"),
                eventOnHit,
                ActionId.Get("action.damage"),
                priority: 100));

            atomRegistry.Register(TriggerAtom.CreateUnconditional(
                TriggerAtomId.Get("atom.heal_buff"),
                eventOnHeal,
                ActionId.Get("action.buff"),
                priority: 50));

            atomRegistry.Register(TriggerAtom.CreateActionOnly(
                TriggerAtomId.Get("atom.periodic_log"),
                ActionId.Get("action.damage"),
                priority: 0));

            Console.WriteLine("[3] 注册触发原子:");
            Console.WriteLine("    - atom.counter_attack: event.on_hit -> action.damage");
            Console.WriteLine("    - atom.heal_buff: event.on_heal -> action.buff");
            Console.WriteLine("    - atom.periodic_log: 无事件 -> action.damage (规则调度)");
            Console.WriteLine();

            Console.WriteLine("[4] 查询触发原子:");
            Console.WriteLine($"    - 总数: {atomRegistry.Count}");
            Console.WriteLine($"    - 事件 'on_hit' 相关的原子: {atomRegistry.GetByEvent(eventOnHit).Count} 个");
            Console.WriteLine($"    - 获取 atom.counter_attack: {atomRegistry.Get(TriggerAtomId.Get("atom.counter_attack"))?.Id}");
            Console.WriteLine();

            Console.WriteLine("[5] 创建并启动规则调度:");
            int executionCount = 0;
            var plan = RuleSchedulePlan.Every(
                intervalMs: 1000f,
                maxOccurrences: 3,
                groupId: "sample:id-based",
                subjectId: "atom.periodic_log",
                label: "periodic damage demo");

            var handle = schedulerRegistry.Schedule(
                in plan,
                new DelegateRuleScheduleEffect(
                    ctx =>
                    {
                        executionCount++;
                        Console.WriteLine($"    ★ [调度执行] #{executionCount}");
                        actions["action.damage"](ctx.UserContext);
                    },
                    onCompleted: ctx => Console.WriteLine($"    ✓ [调度完成] 总执行: {executionCount} 次")));

            Console.WriteLine($"    - Handle: {handle}");
            Console.WriteLine();

            Console.WriteLine("[6] 模拟游戏循环（3.5秒）:");
            float totalTime = 0f;
            const float deltaTime = 500f;

            while (totalTime < 3.5f)
            {
                totalTime += deltaTime / 1000f;
                schedulerRegistry.Update(deltaTime);
                Console.WriteLine($"    [帧 {totalTime:F1}s] 规则调度更新");
            }

            Console.WriteLine();
            Console.WriteLine("========== ID-based RuleScheduler 示例结束 ==========");
            Console.WriteLine();
            Console.WriteLine("总结:");
            Console.WriteLine("  - Registry 模式: 可通过 ID 间接引用行为，但要避免全局状态扩散");
            Console.WriteLine("  - 推荐用法: 事件触发走 TriggerRunner，规则级时间语义走 RuleSchedulerRegistry");
            Console.WriteLine("  - 旧 Runtime/Scheduler: 仅保留兼容和迁移用途");
        }

        /// <summary>
        /// 演示规则调度的暂停/恢复功能。
        /// </summary>
        public static void DemonstratePauseResume()
        {
            Console.WriteLine();
            Console.WriteLine("========== 演示 RuleScheduler 暂停/恢复 ==========");

            var schedulerRegistry = new RuleSchedulerRegistry();
            int count = 0;
            var plan = RuleSchedulePlan.Every(500f, maxOccurrences: 20, groupId: "sample:pause-resume");
            var handle = schedulerRegistry.Schedule(in plan, new DelegateRuleScheduleEffect(_ => count++));

            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.Update(500f);
            }
            Console.WriteLine($"执行1秒后: count={count}");

            schedulerRegistry.Pause(handle);
            Console.WriteLine("暂停...");

            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.Update(500f);
            }
            Console.WriteLine($"暂停1秒后: count={count}");

            schedulerRegistry.Resume(handle);
            Console.WriteLine("恢复...");

            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.Update(500f);
            }
            Console.WriteLine($"恢复1秒后: count={count}");
        }
    }
}
