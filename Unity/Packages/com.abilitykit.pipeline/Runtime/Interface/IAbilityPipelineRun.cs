namespace AbilityKit.Pipeline
{
    /// <summary>
    /// 一次管线运行的控制句柄。运行对象是一次性的，进入已完成或失败终态后调用控制方法应保持幂等。
    /// </summary>
    /// <typeparam name="TCtx">管线上文类型。</typeparam>
    public interface IAbilityPipelineRun<TCtx>
        where TCtx : IAbilityPipelineContext
    {
        /// <summary>
        /// 当前运行状态。
        /// </summary>
        EAbilityPipelineState State { get; }

        /// <summary>
        /// 本次运行绑定的上下文。
        /// </summary>
        TCtx Context { get; }

        /// <summary>
        /// 当前正在执行或最近执行的阶段 ID。
        /// </summary>
        AbilityPipelinePhaseId CurrentPhaseId { get; }

        /// <summary>
        /// 当前运行是否处于暂停状态。
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 推进一次运行。非执行中状态或暂停状态下调用不会产生副作用。
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 暂停运行。非执行中状态或已暂停时调用保持幂等。
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复运行。非执行中状态或未暂停时调用保持幂等。
        /// </summary>
        void Resume();

        /// <summary>
        /// 中断运行并进入失败终态。非执行中状态下调用保持幂等。
        /// </summary>
        void Interrupt();

        /// <summary>
        /// 请求取消运行。取消会在下一次推进时转为终态；重复调用保持幂等。
        /// </summary>
        void Cancel();
    }
}
