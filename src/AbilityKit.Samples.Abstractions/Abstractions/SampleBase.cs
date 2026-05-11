using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// 示例基类 - 提供便捷的环境访问
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
        /// 日志输出器（运行时注入）
        /// </summary>
        protected ILogger Output => _logger ?? throw new InvalidOperationException("Logger not initialized");

        /// <summary>
        /// 运行环境（运行时注入）
        /// </summary>
        protected ISampleEnvironment Environment => _environment ?? throw new InvalidOperationException("Environment not initialized");

        /// <summary>
        /// 配置提供器
        /// </summary>
        protected IConfigProvider? Config { get; private set; }

        /// <summary>
        /// 当前时间
        /// </summary>
        protected float Time => Environment.Time;

        private ILogger? _logger;
        private ISampleEnvironment? _environment;

        /// <summary>
        /// 初始化（由运行器调用）
        /// </summary>
        internal void Initialize(ILogger logger, ISampleEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <inheritdoc />
        public virtual void Run()
        {
            Section(Title);
            if (!string.IsNullOrEmpty(Description))
            {
                Info(Description);
            }
            Line();

            try
            {
                OnRun();
            }
            catch (Exception ex)
            {
                Error($"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// 子类实现的运行逻辑
        /// </summary>
        protected abstract void OnRun();

        /// <summary>
        /// 记录日志（Info级别）
        /// </summary>
        protected void Log(string message) => Output.Info(message);

        /// <summary>
        /// 信息日志
        /// </summary>
        protected void Info(string message) => Output.Info(message);

        /// <summary>
        /// 显示分节标题
        /// </summary>
        protected void Section(string title) => Output.Section(title);

        /// <summary>
        /// 显示分隔线
        /// </summary>
        protected void Divider() => Output.Divider();

        /// <summary>
        /// 显示空行
        /// </summary>
        protected void Line() => Output.Line();

        /// <summary>
        /// 记录警告
        /// </summary>
        protected void Warn(string message) => Output.Warn(message);

        /// <summary>
        /// 记录错误
        /// </summary>
        protected void Error(string message) => Output.Error(message);

        /// <summary>
        /// 显示项目符号
        /// </summary>
        protected void Bullet(string text) => Output.Bullet(text);

        /// <summary>
        /// 显示编号项
        /// </summary>
        protected void Numbered(int num, string text) => Output.Numbered(num, text);

        /// <summary>
        /// 显示键值对
        /// </summary>
        protected void KeyValue(string key, string value) => Output.KeyValue(key, value);

        /// <summary>
        /// 刷新输出
        /// </summary>
        protected void Flush() => Output.Flush();

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
        /// 执行到完成
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
