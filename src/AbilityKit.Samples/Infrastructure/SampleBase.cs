using System;
using AbilityKit.Samples.Common;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 示例基类
    /// </summary>
    public abstract class SampleBase : ISample
    {
        /// <inheritdoc />
        public abstract string Title { get; }

        /// <inheritdoc />
        public virtual string Description => string.Empty;

        /// <inheritdoc />
        public abstract SampleCategory Category { get; }

        /// <summary>
        /// 日志输出器
        /// </summary>
        protected ILogger Output => AbilityKit.Samples.Common.Logger.Instance;

        /// <summary>
        /// 运行环境（由子类或运行器设置）
        /// </summary>
        protected ISampleEnvironment Environment { get; private set; } = new InstantEnvironment();

        /// <summary>
        /// 当前时间
        /// </summary>
        protected float Time => Environment.Time;

        /// <inheritdoc />
        public virtual void Run()
        {
            Output.Section(Title);
            if (!string.IsNullOrEmpty(Description))
            {
                Output.Info(Description);
            }
            Output.Line();

            try
            {
                // 每次运行创建新环境
                Environment = CreateEnvironment();
                Setup();
                OnRun();
            }
            catch (Exception ex)
            {
                Output.Error($"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建运行环境（可被子类重写）
        /// </summary>
        protected virtual ISampleEnvironment CreateEnvironment()
        {
            return SampleEnvironmentFactory.Create(ExecutionMode.Instant);
        }

        /// <summary>
        /// 设置阶段（运行前）
        /// </summary>
        protected virtual void Setup() { }

        /// <summary>
        /// 子类实现的运行逻辑
        /// </summary>
        protected abstract void OnRun();

        /// <summary>
        /// 记录日志
        /// </summary>
        protected void Log(string message) => Output.Info(message);

        /// <summary>
        /// 记录警告
        /// </summary>
        protected void Warn(string message) => Output.Warn(message);

        /// <summary>
        /// 记录错误
        /// </summary>
        protected void Error(string message) => Output.Error(message);

        /// <summary>
        /// 推进时间
        /// </summary>
        protected void AdvanceTime(float delta)
        {
            Environment.Advance(delta);
        }

        /// <summary>
        /// 模拟多帧
        /// </summary>
        protected void SimulateFrames(int frames, float deltaPerFrame = 0.016f)
        {
            for (int i = 0; i < frames; i++)
            {
                Environment.Advance(deltaPerFrame);
            }
        }

        /// <summary>
        /// 执行到完成（用于模拟模式）
        /// </summary>
        protected void ExecuteUntilComplete()
        {
            Environment.ExecuteUntilComplete();
        }

        /// <summary>
        /// 推进到指定时间
        /// </summary>
        protected void AdvanceTo(float targetTime)
        {
            Environment.AdvanceTo(targetTime);
        }

        /// <summary>
        /// 暂停
        /// </summary>
        protected void Pause() => Environment.Pause();

        /// <summary>
        /// 继续
        /// </summary>
        protected void Resume() => Environment.Resume();

        /// <summary>
        /// 重置时间
        /// </summary>
        protected void ResetTime() => Environment.Reset();
    }
}
