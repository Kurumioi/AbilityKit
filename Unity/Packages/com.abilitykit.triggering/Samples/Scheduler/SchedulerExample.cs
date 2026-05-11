using System;
using AbilityKit.Triggering.Runtime.Scheduler;

namespace AbilityKit.Triggering.Samples.Scheduler
{
    /// <summary>
    /// 调度器使用示例
    ///
    /// 本示例演示：
    /// 1. 使用 SchedulerRegistry 管理调度器
    /// 2. 创建周期性、延迟、持续调度器
    /// 3. 查询和控制调度器
    ///
    /// 【Samples】此类型仅用于演示如何使用框架层的 SchedulerRegistry
    /// </summary>
    public static class SchedulerExample
    {
        /// <summary>
        /// 运行基础示例
        /// </summary>
        public static void RunBasic()
        {
            Console.WriteLine("========== SchedulerRegistry 基础示例 ==========");
            Console.WriteLine();

            // 1. 创建调度器注册中心（非单例，支持 DI）
            var registry = new SchedulerRegistry();
            Console.WriteLine("[1] 创建 SchedulerRegistry");
            Console.WriteLine($"    - TotalCount: {registry.TotalCount}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 2. 定义行为回调
            int tickCount = 0;
            Action<object> tickAction = ctx =>
            {
                tickCount++;
                Console.WriteLine($"    ★ [Tick #{tickCount}] 执行周期性行为");
            };
            Console.WriteLine("[2] 定义行为回调 (每 500ms 执行一次，共 3 次)");
            Console.WriteLine();

            // 3. 创建周期性调度器
            var handle = registry.CreatePeriodicScheduler(
                schedulerId: 1,
                businessId: 100, // 业务对象ID，如 BuffId
                triggerId: 0,
                intervalMs: 500f,
                maxExecutions: 3,
                actionCallback: tickAction,
                context: "MyContext",
                onComplete: (sctx, tctx) =>
                {
                    Console.WriteLine($"    ✓ 调度完成! 共执行 {tickCount} 次");
                });

            Console.WriteLine($"[3] 创建周期性调度器");
            Console.WriteLine($"    - Handle: {handle}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 4. 模拟游戏循环
            Console.WriteLine("[4] 模拟游戏循环 (2秒):");
            float totalTime = 0f;
            float deltaTime = 500f; // 每帧 500ms

            while (totalTime < 2f)
            {
                totalTime += deltaTime / 1000f;
                registry.Update(deltaTime);
                Console.WriteLine($"    [帧 {totalTime:F1}s] ActiveCount: {registry.ActiveCount}");
            }

            Console.WriteLine();
            Console.WriteLine("========== 基础示例结束 ==========");
        }

        /// <summary>
        /// 演示查询功能
        /// </summary>
        public static void RunQuery()
        {
            Console.WriteLine();
            Console.WriteLine("========== 调度器查询示例 ==========");
            Console.WriteLine();

            var registry = new SchedulerRegistry();

            // 创建多个调度器
            registry.CreatePeriodicScheduler(1, businessId: 100, triggerId: 0, 1000f, 10, _ => { }, "A");
            registry.CreatePeriodicScheduler(2, businessId: 100, triggerId: 0, 1000f, 10, _ => { }, "B");
            registry.CreatePeriodicScheduler(3, businessId: 200, triggerId: 0, 1000f, 10, _ => { }, "C");

            Console.WriteLine($"[1] 创建 3 个调度器 (2个属于 businessId=100, 1个属于 businessId=200)");
            Console.WriteLine($"    - TotalCount: {registry.TotalCount}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 查询 businessId=100 的调度器
            Console.WriteLine("[2] 查询 businessId=100 的调度器:");
            int count = 0;
            foreach (var scheduler in registry.FindByBusinessId(100))
            {
                count++;
                Console.WriteLine($"    - {scheduler.Handle}: Context={scheduler.Context}");
            }
            Console.WriteLine($"    共 {count} 个");
            Console.WriteLine();

            // 获取调度器数据
            Console.WriteLine("[3] 获取调度器数据:");
            if (registry.TryGetSchedulerData(new SchedulerHandle(1), out var data))
            {
                Console.WriteLine($"    - SchedulerId: {data.SchedulerId}");
                Console.WriteLine($"    - Name: {data.Name}");
                Console.WriteLine($"    - BusinessId: {data.BusinessId}");
                Console.WriteLine($"    - State: {data.State}");
                Console.WriteLine($"    - Config.Mode: {data.Config.Mode}");
                Console.WriteLine($"    - Config.IntervalMs: {data.Config.IntervalMs}");
            }
            Console.WriteLine();

            Console.WriteLine("========== 查询示例结束 ==========");
        }

        /// <summary>
        /// 演示控制功能
        /// </summary>
        public static void RunControl()
        {
            Console.WriteLine();
            Console.WriteLine("========== 调度器控制示例 ==========");
            Console.WriteLine();

            var registry = new SchedulerRegistry();
            int tickCount = 0;

            var handle = registry.CreatePeriodicScheduler(
                schedulerId: 1,
                businessId: 0,
                triggerId: 0,
                intervalMs: 200f,
                maxExecutions: 100,
                actionCallback: _ => tickCount++);

            Console.WriteLine("[1] 创建调度器 (每 200ms 执行)");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine();

            // 执行 1 秒
            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[2] 执行 1 秒 (5帧)");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 暂停
            registry.Pause(handle);
            Console.WriteLine("[3] 暂停调度器");
            Console.WriteLine($"    - State: {registry.GetScheduler(handle)?.State}");

            // 再执行 1 秒（应该不增加）
            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[4] 再执行 1 秒（暂停中）");
            Console.WriteLine($"    - tickCount: {tickCount} (不应该变化)");
            Console.WriteLine();

            // 恢复
            registry.Resume(handle);
            Console.WriteLine("[5] 恢复调度器");

            // 再执行 1 秒
            for (int i = 0; i < 5; i++)
            {
                registry.Update(200f);
            }
            Console.WriteLine("[6] 再执行 1 秒（已恢复）");
            Console.WriteLine($"    - tickCount: {tickCount}");
            Console.WriteLine();

            // 中断
            registry.Interrupt(handle, "测试中断");
            Console.WriteLine("[7] 中断调度器");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            Console.WriteLine("========== 控制示例结束 ==========");
        }

        /// <summary>
        /// 演示批量操作
        /// </summary>
        public static void RunBatch()
        {
            Console.WriteLine();
            Console.WriteLine("========== 批量操作示例 ==========");
            Console.WriteLine();

            var registry = new SchedulerRegistry();

            // 创建多个调度器
            registry.CreatePeriodicScheduler(1, businessId: 100, triggerId: 1, 1000f, 100, _ => { });
            registry.CreatePeriodicScheduler(2, businessId: 100, triggerId: 1, 1000f, 100, _ => { });
            registry.CreatePeriodicScheduler(3, businessId: 200, triggerId: 2, 1000f, 100, _ => { });
            registry.CreatePeriodicScheduler(4, businessId: 200, triggerId: 2, 1000f, 100, _ => { });

            Console.WriteLine($"[1] 创建 4 个调度器");
            Console.WriteLine($"    - TotalCount: {registry.TotalCount}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 暂停所有
            registry.PauseAll();
            Console.WriteLine("[2] 暂停所有");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");

            // 恢复所有
            registry.ResumeAll();
            Console.WriteLine("[3] 恢复所有");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 按 TriggerId 中断
            int interrupted = registry.InterruptByTriggerId(1);
            Console.WriteLine($"[4] 中断 TriggerId=1 的调度器");
            Console.WriteLine($"    - Interrupted: {interrupted}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 按 BusinessId 中断
            interrupted = registry.InterruptByBusinessId(200);
            Console.WriteLine($"[5] 中断 BusinessId=200 的调度器");
            Console.WriteLine($"    - Interrupted: {interrupted}");
            Console.WriteLine($"    - ActiveCount: {registry.ActiveCount}");
            Console.WriteLine();

            // 清除所有
            registry.Clear();
            Console.WriteLine("[6] 清除所有");
            Console.WriteLine($"    - TotalCount: {registry.TotalCount}");
            Console.WriteLine();

            Console.WriteLine("========== 批量操作示例结束 ==========");
        }
    }
}
