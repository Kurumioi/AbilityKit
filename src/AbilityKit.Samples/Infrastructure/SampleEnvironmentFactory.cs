using System;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 执行模式
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// 即时模式 - 立即执行所有逻辑
        /// </summary>
        Instant,

        /// <summary>
        /// 模拟模式 - 按时间步进执行
        /// </summary>
        Simulated,
    }

    /// <summary>
    /// 运行环境工厂
    /// </summary>
    public static class SampleEnvironmentFactory
    {
        /// <summary>
        /// 创建运行环境
        /// </summary>
        public static ISampleEnvironment Create(ExecutionMode mode)
        {
            return mode switch
            {
                ExecutionMode.Instant => new InstantEnvironment(),
                ExecutionMode.Simulated => new SimulatedEnvironment(),
                _ => new InstantEnvironment()
            };
        }
    }
}
