using System;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// 调度器实现
    /// 主动调度模式：通过回调执行行为，支持周期性、延迟、持续执行
    ///
    /// 【框架层】默认调度器实现，具体项目可实现 IScheduler 接口提供自定义实现
    /// </summary>
    public sealed class Scheduler : IScheduler
    {
        private readonly Action<object> _actionCallback;
        private readonly Action<object, object> _onComplete;
        private readonly Action<object, object> _onInterrupt;

        private ESchedulerState _state;
        private int _executionCount;
        private float _elapsedMs;
        private float _nextExecutionMs;
        private int _version;

        #region 属性

        public SchedulerHandle Handle { get; private set; }
        public string Name { get; }
        public int BusinessId { get; }
        public int TriggerId { get; }
        public SchedulerConfig Config { get; }
        public object Context { get; }

        public ESchedulerState State => _state;
        public bool IsActive => _state == ESchedulerState.Active;
        public bool CanBeInterrupted => Config.CanBeInterrupted;
        public int ExecutionCount => _executionCount;
        public float ElapsedMs => _elapsedMs;

        #endregion

        #region 构造

        public Scheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            in SchedulerConfig config,
            object context,
            Action<object> actionCallback,
            Action<object, object> onComplete = null,
            Action<object, object> onInterrupt = null)
        {
            Handle = new SchedulerHandle(schedulerId, 1);
            Name = $"Scheduler_{schedulerId}";
            BusinessId = businessId;
            TriggerId = triggerId;
            Config = config;
            Context = context;
            _actionCallback = actionCallback ?? throw new ArgumentNullException(nameof(actionCallback));
            _onComplete = onComplete;
            _onInterrupt = onInterrupt;
            _state = ESchedulerState.Idle;
            _executionCount = 0;
            _elapsedMs = 0;
            _version = 1;
        }

        #endregion

        #region 控制

        public void Start()
        {
            if (_state == ESchedulerState.Active)
                return;

            _state = ESchedulerState.Active;
            _executionCount = 0;
            _elapsedMs = 0;

            // 周期性调度：首次执行在 interval 后
            // 延迟调度/立即执行：首次执行在 delay 后
            _nextExecutionMs = Config.Mode == EScheduleMode.Periodic
                ? Config.IntervalMs
                : Config.DelayMs;
        }

        public void Stop()
        {
            if (_state != ESchedulerState.Active && _state != ESchedulerState.Paused)
                return;

            _state = ESchedulerState.Cancelled;
            _onInterrupt?.Invoke(Context, null);
        }

        public void Pause()
        {
            if (_state != ESchedulerState.Active)
                return;

            _state = ESchedulerState.Paused;
        }

        public void Resume()
        {
            if (_state != ESchedulerState.Paused)
                return;

            _state = ESchedulerState.Active;
        }

        public void Reset()
        {
            _state = ESchedulerState.Idle;
            _executionCount = 0;
            _elapsedMs = 0;
            _version++;
            Handle = new SchedulerHandle(Handle.SchedulerId, _version);
        }

        #endregion

        #region 执行

        public bool Update(float deltaTimeMs, object triggerContext = null)
        {
            if (_state != ESchedulerState.Active)
                return _state != ESchedulerState.Completed && _state != ESchedulerState.Cancelled;

            _elapsedMs += deltaTimeMs;

            // 检查最大持续时间
            if (Config.MaxDurationMs > 0 && _elapsedMs >= Config.MaxDurationMs)
            {
                Complete(triggerContext);
                return false;
            }

            // 检查是否到达执行时间
            if (_elapsedMs >= _nextExecutionMs)
            {
                TryExecute(triggerContext);
                // 使用 += 而不是 = elapsed +，避免累积误差
                _nextExecutionMs += Config.IntervalMs;
            }

            return true;
        }

        private void TryExecute(object triggerContext)
        {
            // 检查最大执行次数
            if (Config.MaxExecutions > 0 && _executionCount >= Config.MaxExecutions)
            {
                Complete(triggerContext);
                return;
            }

            // 执行行为回调
            _executionCount++;

            try
            {
                _actionCallback?.Invoke(triggerContext ?? Context);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"Scheduler[{Handle.SchedulerId}] execution error");
            }

            // 检查是否完成
            switch (Config.Mode)
            {
                case EScheduleMode.Immediate:
                case EScheduleMode.Delayed:
                    Complete(triggerContext);
                    break;

                case EScheduleMode.Periodic:
                    if (Config.MaxExecutions > 0 && _executionCount >= Config.MaxExecutions)
                        Complete(triggerContext);
                    break;

                case EScheduleMode.Continuous:
                    // Continuous 由外部控制终止
                    break;
            }
        }

        private void Complete(object triggerContext)
        {
            if (_state == ESchedulerState.Completed)
                return;

            _state = ESchedulerState.Completed;
            _onComplete?.Invoke(Context, triggerContext);
        }

        #endregion
    }
}
