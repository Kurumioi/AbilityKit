using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// 调度器状态
    /// </summary>
    public enum ESchedulerState
    {
        Idle,
        Active,
        Paused,
        Completed,
        Cancelled
    }

    /// <summary>
    /// 调度模式
    /// </summary>
    public enum EScheduleMode
    {
        Immediate,  // 立即执行一次
        Delayed,    // 延迟后执行一次
        Periodic,   // 周期性执行
        Continuous  // 持续执行（需要手动终止）
    }

    /// <summary>
    /// 调度配置数据（纯数据，不包含业务逻辑）
    /// 可序列化，用于 JSON 配置或数据驱动场景
    /// </summary>
    [Serializable]
    public struct SchedulerConfig
    {
        /// <summary>调度模式</summary>
        public EScheduleMode Mode;

        /// <summary>延迟时间（毫秒）</summary>
        public float DelayMs;

        /// <summary>执行间隔（毫秒）</summary>
        public float IntervalMs;

        /// <summary>最大执行次数，-1 表示无限</summary>
        public int MaxExecutions;

        /// <summary>最大持续时间（毫秒），0 表示无限制</summary>
        public float MaxDurationMs;

        /// <summary>是否可以中断</summary>
        public bool CanBeInterrupted;

        /// <summary>
        /// 创建周期性配置
        /// </summary>
        public static SchedulerConfig Periodic(float intervalMs, int maxExecutions = -1)
        {
            return new SchedulerConfig
            {
                Mode = EScheduleMode.Periodic,
                DelayMs = 0,
                IntervalMs = intervalMs,
                MaxExecutions = maxExecutions,
                MaxDurationMs = 0,
                CanBeInterrupted = true
            };
        }

        /// <summary>
        /// 创建延迟配置
        /// </summary>
        public static SchedulerConfig Delayed(float delayMs)
        {
            return new SchedulerConfig
            {
                Mode = EScheduleMode.Delayed,
                DelayMs = delayMs,
                IntervalMs = 0,
                MaxExecutions = 1,
                MaxDurationMs = 0,
                CanBeInterrupted = true
            };
        }

        /// <summary>
        /// 创建持续配置
        /// </summary>
        public static SchedulerConfig Continuous(float intervalMs, float maxDurationMs = 0)
        {
            return new SchedulerConfig
            {
                Mode = EScheduleMode.Continuous,
                DelayMs = 0,
                IntervalMs = intervalMs,
                MaxExecutions = -1,
                MaxDurationMs = maxDurationMs,
                CanBeInterrupted = true
            };
        }

        /// <summary>
        /// 创建立即执行配置
        /// </summary>
        public static SchedulerConfig Immediate()
        {
            return new SchedulerConfig
            {
                Mode = EScheduleMode.Immediate,
                DelayMs = 0,
                IntervalMs = 0,
                MaxExecutions = 1,
                MaxDurationMs = 0,
                CanBeInterrupted = false
            };
        }
    }

    /// <summary>
    /// 调度器运行时数据（纯数据，不包含业务逻辑）
    /// 用于查询调度器当前状态
    /// </summary>
    public struct SchedulerData
    {
        /// <summary>调度器唯一ID</summary>
        public int SchedulerId;

        /// <summary>调度器名称</summary>
        public string Name;

        /// <summary>关联的业务对象ID（如 BuffId、子弹Id）</summary>
        public int BusinessId;

        /// <summary>关联的触发器ID</summary>
        public int TriggerId;

        /// <summary>当前状态</summary>
        public ESchedulerState State;

        /// <summary>调度配置</summary>
        public SchedulerConfig Config;

        /// <summary>当前执行次数</summary>
        public int ExecutionCount;

        /// <summary>已消耗时间（毫秒）</summary>
        public float ElapsedMs;

        /// <summary>下次执行时间（毫秒）</summary>
        public float NextExecuteMs;
    }
}
