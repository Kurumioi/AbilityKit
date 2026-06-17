using System;

namespace AbilityKit.Ability.Flow
{
    /// <summary>
    /// Flow 节点同步执行选项，用于统一正式 API 的执行边界、异常处理和死循环保护。
    /// </summary>
    public sealed class FlowExecutionOptions
    {
        /// <summary>
        /// 创建默认执行选项。异常会被捕获到 FlowExecutionResult，最大步数为 1024。
        /// </summary>
        public static FlowExecutionOptions Default => new FlowExecutionOptions();

        /// <summary>
        /// 每次 Step 传入的 deltaTime。
        /// </summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// 最大执行步数。小于等于 0 表示不限制。
        /// </summary>
        public int MaxSteps { get; set; } = 1024;

        /// <summary>
        /// 节点执行异常回调。默认情况下异常会被 FlowRunner 捕获并使结果失败。
        /// </summary>
        public Action<Exception> ExceptionHandler { get; set; }

        /// <summary>
        /// 为 true 时，执行中捕获到的异常会在返回结果前重新抛出。
        /// </summary>
        public bool RethrowExceptions { get; set; }
    }
}
