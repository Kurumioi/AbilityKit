using System;
using AbilityKit.Triggering.Runtime.RuleScheduler;

namespace AbilityKit.Triggering.Samples.Scheduler
{
    /// <summary>
    /// 规则调度使用示例。
    ///
    /// 本示例演示正式 RuleSchedulerRegistry 的周期、查询、控制与批量操作。
    /// 旧 Runtime/Scheduler/SchedulerRegistry 仅保留兼容用途，新代码应优先使用 RuleScheduler、ActionScheduler 或 Schedule。
    /// </summary>
    public static class SchedulerExample
    {
        /// <summary>
        /// 运行基础示例。
        /// </summary>
        public static void RunBasic()
        {
            Console.WriteLine("========== RuleScheduler 基础示例 ==========");
            Console.WriteLine();

            var registry = new RuleSchedulerRegistry();
            Console.WriteLine("[1] 创建 RuleSchedulerRegistry");
            Console.WriteLine($"    - DriverCount: {registry.DriverCount}");
            Console.WriteLine();

            int tickCount = 0;
            var plan = RuleSchedulePlan.Every(
                intervalMs: 500f,
                maxOccurrences: 3,
                groupId: "sample:basic",
                subjectId: "buff:100",
                label: "basic periodic demo");

            var handle = registry.Schedule(
                in plan,
                new DelegateRuleScheduleEffect(
                    ctx =>
                    {
                        tickCount++;
                        Console.WriteLine($"    ★ [Tick #{tickCount}] 执行周期性行为");
                    },
                    onCompleted: ctx => Console.WriteLine($"    ✓ 调度完成! 共执行 {tickCount} 次")));

            Console.WriteLine("[2] 创建周期性规则调度");
            Console.WriteLine($"    - Handle: {handle}");
            Console.WriteLine();

            Console.WriteLine("[3] 模拟游戏循环 (2秒):");
            float totalTime = 0f;
            const float deltaTime = 500f;

            while (totalTime < 2f)
            {
                totalTime += deltaTime / 1000f;
                registry.Update(deltaTime);
                Console.WriteLine($"    [帧 {totalTime:F1}s] TickCount: {tickCount}");
            }

            Console.WriteLine();
            Console.WriteLine("========== 基础示例结束 ==========");
        }

        /// <summary>
        /// 演示查询功能。
        /// </summary>
        public static void RunQuery()
        {
            Console.WriteLine();
            Console.WriteLine("========== 规则调度查询示例 ==========");
            Console.WriteLine();

            var driver = new DefaultRuleSchedulerDriver();
            var registry = new RuleSchedulerRegistry(driver);

            registry.Schedule(in RuleSchedulePlan.Every(1000f, 10, groupId: "business:100", subjectId: "A"), new DelegateRuleScheduleEffect(_ => { }));
            registry.Schedule(in RuleSchedulePlan.Every(1000f, 10, groupId: "business:100", subjectId: "B"), new DelegateRuleScheduleEffect(_ => { }));
            registry.Schedule(in RuleSchedulePlan.Every(1000f, 10, groupId: "business:200", subjectId: "C"), new DelegateRuleScheduleEffect(_ => { }));

            Console.WriteLine("[1] 创建 3 个规则调度 (2个属于 business:100, 1个属于 business:200)");
            Console.WriteLine($"    - DriverCount: {registry.DriverCount}");
            Console.WriteLine();

            Console.WriteLine("[2] 查询 business:100 的规则调度:");
            var snapshots = driver.FindByGroup("business:100");
            foreach (var snapshot in snapshots)
            {
                Console.WriteLine($"    - {snapshot.Handle}: Subject={snapshot.Plan.SubjectId}, State={snapshot.State}");
            }
            Console.WriteLine($"    共 {snapshots.Count} 个");
            Console.WriteLine();

            Console.WriteLine("[3] 获取调度数据:");
            if (snapshots.Count > 0 && driver.TryGet(snapshots[0].Handle, out var data))
            {
                Console.WriteLine($"    - Handle: {data.Handle}");
                Console.WriteLine($"    - State: {data.State}");
                Console.WriteLine($"    - Mode: {data.Plan.Mode}");
                Console.WriteLine($"    - IntervalMs: {data.Plan.IntervalMs}");
            }
            Console.WriteLine();

            Console.WriteLine("========== 查询示例结束 ==========");
        }

        /// <summary>
        /// 演示控制功能。
        /// </summary>
        public static void RunControl()
        {
            Console.WriteLine();
            Console.WriteLine("========== 规则调度控制示例 ==========");
            Console.WriteLine();

            var registry = new RuleSchedulerRegistry();
            int tickCount = 0;
            var plan = RuleSchedulePlan.Every(200f, maxOccurrences: 100, groupId: "sample:control");
            var handle = registry.Schedule(in plan, new DelegateRuleScheduleEffect(_ => tickCount++));

            Console.WriteLine("[1] 创建调度 (每 200ms 执行)");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine();

            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[2] 执行 1 秒 (5帧)");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine();

            registry.Pause(handle);
            Console.WriteLine("[3] 暂停调度");

            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[4] 再执行 1 秒（暂停中）");
            Console.WriteLine($"    - tickCount: {tickCount} (不应该变化)");
            Console.WriteLine();

            registry.Resume(handle);
            Console.WriteLine("[5] 恢复调度");

            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[6] 再执行 1 秒（已恢复）");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine();

            registry.Interrupt(handle, "测试中断");
            Console.WriteLine("[7] 中断调度");
            Console.WriteLine();

            Console.WriteLine("========== 控制示例结束 ==========");
        }

        /// <summary>
        /// 演示批量操作。
        /// </summary>
        public static void RunBatch()
        {
            Console.WriteLine();
            Console.WriteLine("========== 规则调度批量操作示例 ==========");
            Console.WriteLine();

            var driver = new DefaultRuleSchedulerDriver();
            var registry = new RuleSchedulerRegistry(driver);

            registry.Schedule(in RuleSchedulePlan.Every(1000f, 100, groupId: "business:100", subjectId: "trigger:1"), new DelegateRuleScheduleEffect(_ => { }));
            registry.Schedule(in RuleSchedulePlan.Every(1000f, 100, groupId: "business:100", subjectId: "trigger:1"), new DelegateRuleScheduleEffect(_ => { }));
            registry.Schedule(in RuleSchedulePlan.Every(1000f, 100, groupId: "business:200", subjectId: "trigger:2"), new DelegateRuleScheduleEffect(_ => { }));
            registry.Schedule(in RuleSchedulePlan.Every(1000f, 100, groupId: "business:200", subjectId: "trigger:2"), new DelegateRuleScheduleEffect(_ => { }));

            Console.WriteLine("[1] 创建 4 个规则调度");
            Console.WriteLine($"    - business:100 Count: {driver.FindByGroup("business:100").Count}");
            Console.WriteLine($"    - business:200 Count: {driver.FindByGroup("business:200").Count}");
            Console.WriteLine();

            int interrupted = driver.InterruptGroup("business:100", "批量中断 business:100");
            Console.WriteLine("[2] 中断 business:100 的规则调度");
            Console.WriteLine($"    - Interrupted: {interrupted}");
            Console.WriteLine();

            int cancelled = driver.CancelGroup("business:200");
            Console.WriteLine("[3] 取消 business:200 的规则调度");
            Console.WriteLine($"    - Cancelled: {cancelled}");
            Console.WriteLine();

            registry.Clear();
            Console.WriteLine("[4] 清除所有");
            Console.WriteLine();

            Console.WriteLine("========== 批量操作示例结束 ==========");
        }
    }
}
