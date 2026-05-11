using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Samples.Scheduler;

namespace AbilityKit.Triggering.Samples
{
    /// <summary>
    /// ID-based 触发系统示例
    ///
    /// 本示例演示如何使用 TriggerAtomRegistry + SchedulerRegistry 构建 ID-based 触发系统。
    ///
    /// 【重要】这是一种可选的架构模式：
    /// - 优点：可以通过 JSON 配置驱动，支持运行时修改
    /// - 缺点：引入全局状态，增加耦合
    ///
    /// 建议：大多数项目直接使用 TriggerRunner.Register() API 即可，
    /// 只有确实需要 JSON 配置驱动的场景才考虑此模式。
    /// </summary>
    public static class SchedulerRegistryExample
    {
        /// <summary>
        /// 运行示例
        /// </summary>
        public static void RunOnce()
        {
            Console.WriteLine("========== ID-based 触发系统示例开始 ==========");
            Console.WriteLine();

            // 1. 创建注册中心（非单例，支持 DI）
            var atomRegistry = new TriggerAtomRegistry();
            var schedulerRegistry = new SchedulerRegistry(atomRegistry);
            Console.WriteLine("[1] 创建 TriggerAtomRegistry 和 SchedulerRegistry");

            // 2. 定义行为（模拟项目中的行为系统）
            var actions = new Dictionary<string, Action<object>>
            {
                ["action.damage"] = ctx => Console.WriteLine($"    ★ [执行行为] 造成伤害"),
                ["action.heal"] = ctx => Console.WriteLine($"    ★ [执行行为] 治疗"),
                ["action.buff"] = ctx => Console.WriteLine($"    ★ [执行行为] 添加buff"),
            };
            Console.WriteLine("[2] 注册行为: damage, heal, buff");
            Console.WriteLine();

            // 3. 注册触发原子（EDA 模式）
            // ECA: Event-Condition-Action
            var eventOnHit = EventId.Get("event.on_hit");
            var eventOnHeal = EventId.Get("event.on_heal");

            // 触发原子A：受到伤害时，有50%概率造成反击
            atomRegistry.Register(TriggerAtom.CreateUnconditional(
                TriggerAtomId.Get("atom.counter_attack"),
                eventOnHit,
                ActionId.Get("action.damage"),
                priority: 100
            ));

            // 触发原子B：治疗时添加buff
            atomRegistry.Register(TriggerAtom.CreateUnconditional(
                TriggerAtomId.Get("atom.heal_buff"),
                eventOnHeal,
                ActionId.Get("action.buff"),
                priority: 50
            ));

            // 触发原子C：无条件主动调度（无事件）
            atomRegistry.Register(TriggerAtom.CreateActionOnly(
                TriggerAtomId.Get("atom.periodic_log"),
                ActionId.Get("action.damage"),
                priority: 0
            ));

            Console.WriteLine("[3] 注册触发原子:");
            Console.WriteLine("    - atom.counter_attack: event.on_hit → action.damage");
            Console.WriteLine("    - atom.heal_buff: event.on_heal → action.buff");
            Console.WriteLine("    - atom.periodic_log: 无事件 → action.damage (主动调度)");
            Console.WriteLine();

            // 4. 演示查询功能
            Console.WriteLine("[4] 查询触发原子:");
            Console.WriteLine($"    - 总数: {atomRegistry.Count}");

            var atomsOnHit = atomRegistry.GetByEvent(eventOnHit);
            Console.WriteLine($"    - 事件 'on_hit' 相关的原子: {atomsOnHit.Count} 个");

            var counterAtom = atomRegistry.Get(TriggerAtomId.Get("atom.counter_attack"));
            Console.WriteLine($"    - 获取 atom.counter_attack: {counterAtom?.Id}");
            Console.WriteLine();

            // 5. 演示调度器
            Console.WriteLine("[5] 创建并启动调度器:");
            int executionCount = 0;

            var scheduler = schedulerRegistry.CreateScheduler(
                id: SchedulerId.Get("scheduler.periodic"),
                context: null,
                triggerAtomId: TriggerAtomId.Get("atom.periodic_log"),
                config: ScheduleConfig.Periodic(1000f, 3), // 每秒执行，执行3次
                actionCallback: ctx =>
                {
                    executionCount++;
                    Console.WriteLine($"    ★ [调度执行] #{executionCount}");

                    // 模拟从 actions 字典执行
                    var action = actions["action.damage"];
                    action(ctx);
                },
                onComplete: (sctx, tctx) =>
                {
                    Console.WriteLine($"    ✓ [调度完成] 总执行: {executionCount} 次");
                }
            );

            if (scheduler != null)
            {
                scheduler.Start();
            }
            Console.WriteLine();

            // 6. 模拟游戏循环
            Console.WriteLine("[6] 模拟游戏循环（3.5秒）:");
            float totalTime = 0f;
            float deltaTime = 500f; // 每帧 500ms

            while (totalTime < 3.5f)
            {
                totalTime += deltaTime / 1000f;

                // 更新调度器
                foreach (var sched in schedulerRegistry.GetActiveSchedulers())
                {
                    sched.Update(deltaTime, null);
                }

                Console.WriteLine($"    [帧 {totalTime:F1}s] 活跃调度器: {schedulerRegistry.ActiveCount}");
            }

            Console.WriteLine();
            Console.WriteLine("========== ID-based 触发系统示例结束 ==========");
            Console.WriteLine();
            Console.WriteLine("总结:");
            Console.WriteLine("  - Registry 模式: 通过 ID 间接引用行为，增加灵活性但增加耦合");
            Console.WriteLine("  - 推荐用法: 大多数场景直接用 TriggerRunner.Register() 即可");
            Console.WriteLine("  - Registry 适用: 需要 JSON 配置驱动、运行时修改触发规则的场景");
        }

        /// <summary>
        /// 演示调度器的暂停/恢复功能
        /// </summary>
        public static void DemonstratePauseResume()
        {
            Console.WriteLine();
            Console.WriteLine("========== 演示暂停/恢复功能 ==========");

            var schedulerRegistry = new SchedulerRegistry();
            int count = 0;

            var scheduler = schedulerRegistry.CreateScheduler(
                id: SchedulerId.Get("scheduler.test"),
                context: null,
                triggerAtomId: TriggerAtomId.Get("atom.any"),
                config: ScheduleConfig.Periodic(500f, 20),
                actionCallback: ctx => count++
            );

            scheduler?.Start();

            // 执行1秒
            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.GetScheduler(SchedulerId.Get("scheduler.test"))?.Update(500f, null);
            }
            Console.WriteLine($"执行1秒后: count={count}");

            // 暂停
            scheduler?.Pause();
            Console.WriteLine("暂停...");

            // 再执行1秒（应该不增加）
            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.GetScheduler(SchedulerId.Get("scheduler.test"))?.Update(500f, null);
            }
            Console.WriteLine($"暂停1秒后: count={count}");

            // 恢复
            scheduler?.Resume();
            Console.WriteLine("恢复...");

            // 再执行1秒
            for (int i = 0; i < 2; i++)
            {
                schedulerRegistry.GetScheduler(SchedulerId.Get("scheduler.test"))?.Update(500f, null);
            }
            Console.WriteLine($"恢复1秒后: count={count}");
        }
    }
}
