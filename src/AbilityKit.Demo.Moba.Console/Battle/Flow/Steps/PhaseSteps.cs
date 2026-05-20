using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 步骤执行模式
    /// </summary>
    public enum StepMode
    {
        /// <summary>
        /// 顺序执行，等待每步完成
        /// </summary>
        Sequential,

        /// <summary>
        /// 并行执行，等待所有步骤完成
        /// </summary>
        Parallel,

        /// <summary>
        /// 第一个完成后即完成
        /// </summary>
        FirstComplete
    }

    /// <summary>
    /// 步骤状态
    /// </summary>
    public enum StepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// 阶段步骤接口
    /// 表示一个可执行的阶段步骤
    /// </summary>
    public interface IPhaseStep
    {
        /// <summary>
        /// 步骤名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 执行步骤
        /// </summary>
        /// <returns>true 表示完成，false 表示需要继续等待</returns>
        bool Execute();

        /// <summary>
        /// 步骤是否完成
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 步骤是否失败
        /// </summary>
        bool IsFailed { get; }

        /// <summary>
        /// 失败原因
        /// </summary>
        string? FailureReason { get; }

        /// <summary>
        /// 重置步骤状态
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 异步阶段步骤接口
    /// </summary>
    public interface IAsyncPhaseStep : IPhaseStep
    {
        /// <summary>
        /// 异步执行步骤
        /// </summary>
        Task<bool> ExecuteAsync();

        /// <summary>
        /// 异步取消
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// 步骤组
    /// 用于组织和管理多个步骤
    /// </summary>
    public sealed class StepGroup : IPhaseStep
    {
        private readonly string _name;
        private readonly StepMode _mode;
        private readonly List<IPhaseStep> _steps = new();
        private int _currentIndex;
        private StepStatus _status = StepStatus.Pending;
        private string? _failureDetail;

        public string Name => _name;
        public bool IsCompleted => _status == StepStatus.Completed;
        public bool IsFailed => _status == StepStatus.Failed;
        public string? FailureReason => _failureDetail;

        public StepGroup(string name, StepMode mode = StepMode.Sequential)
        {
            _name = name;
            _mode = mode;
        }

        public StepGroup AddStep(IPhaseStep step)
        {
            _steps.Add(step);
            return this;
        }

        public void Reset()
        {
            _currentIndex = 0;
            _status = StepStatus.Pending;
            _failureDetail = null;
            foreach (var step in _steps)
            {
                step.Reset();
            }
        }

        public bool Execute()
        {
            switch (_mode)
            {
                case StepMode.Sequential:
                    return ExecuteSequential();
                case StepMode.Parallel:
                    return ExecuteParallel();
                case StepMode.FirstComplete:
                    return ExecuteFirstComplete();
                default:
                    return true;
            }
        }

        private bool ExecuteSequential()
        {
            if (_status == StepStatus.Completed || _status == StepStatus.Failed)
            {
                return _status == StepStatus.Completed;
            }

            _status = StepStatus.Running;

            while (_currentIndex < _steps.Count)
            {
                var step = _steps[_currentIndex];
                if (!step.IsCompleted)
                {
                    if (step.IsFailed)
                    {
                        _status = StepStatus.Failed;
                        _failureDetail = $"[{_name}] Step '{step.Name}' failed: {step.FailureReason}";
                        return false;
                    }

                    if (step.Execute())
                    {
                        _currentIndex++;
                    }
                    else
                    {
                        return false; // 需要继续等待
                    }
                }
                else
                {
                    _currentIndex++;
                }
            }

            _status = StepStatus.Completed;
            return true;
        }

        private bool ExecuteParallel()
        {
            if (_status == StepStatus.Completed || _status == StepStatus.Failed)
            {
                return _status == StepStatus.Completed;
            }

            _status = StepStatus.Running;
            bool allCompleted = true;
            bool anyFailed = false;

            foreach (var step in _steps)
            {
                if (step.IsFailed)
                {
                    anyFailed = true;
                    break;
                }

                if (!step.IsCompleted)
                {
                    step.Execute();
                    if (!step.IsCompleted)
                    {
                        allCompleted = false;
                    }
                    if (step.IsFailed)
                    {
                        anyFailed = true;
                        break;
                    }
                }
            }

            if (anyFailed)
            {
                _status = StepStatus.Failed;
                foreach (var step in _steps)
                {
                    if (step.IsFailed)
                    {
                        _failureDetail = $"[{_name}] Step '{step.Name}' failed: {step.FailureReason}";
                        break;
                    }
                }
                return false;
            }

            if (allCompleted)
            {
                _status = StepStatus.Completed;
                return true;
            }

            return false;
        }

        private bool ExecuteFirstComplete()
        {
            if (_status == StepStatus.Completed || _status == StepStatus.Failed)
            {
                return _status == StepStatus.Completed;
            }

            _status = StepStatus.Running;

            foreach (var step in _steps)
            {
                if (step.IsFailed)
                {
                    _status = StepStatus.Failed;
                    _failureDetail = $"[{_name}] Step '{step.Name}' failed: {step.FailureReason}";
                    return false;
                }

                if (step.IsCompleted)
                {
                    _status = StepStatus.Completed;
                    return true;
                }

                step.Execute();
                if (step.IsCompleted)
                {
                    _status = StepStatus.Completed;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 简单步骤基类
    /// </summary>
    public abstract class PhaseStepBase : IPhaseStep
    {
        public string Name { get; }
        protected StepStatus Status { get; set; } = StepStatus.Pending;
        protected string? FailureDetail { get; set; }

        public bool IsCompleted => Status == StepStatus.Completed;
        public bool IsFailed => Status == StepStatus.Failed;
        public string? FailureReason => FailureDetail;

        protected PhaseStepBase(string name)
        {
            Name = name;
        }

        public abstract bool Execute();

        public virtual void Reset()
        {
            Status = StepStatus.Pending;
            FailureDetail = null;
        }

        protected void Complete()
        {
            Status = StepStatus.Completed;
        }

        protected void Fail(string reason)
        {
            Status = StepStatus.Failed;
            FailureDetail = reason;
        }
    }

    /// <summary>
    /// 同步步骤 - 单次执行即完成
    /// </summary>
    public sealed class SyncStep : PhaseStepBase
    {
        private readonly Action _action;

        public SyncStep(string name, Action action) : base(name)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override bool Execute()
        {
            if (Status != StepStatus.Pending)
            {
                return IsCompleted;
            }

            Status = StepStatus.Running;
            try
            {
                _action();
                Complete();
                return true;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// 条件步骤 - 根据条件决定是否执行
    /// </summary>
    public sealed class ConditionalStep : PhaseStepBase
    {
        private readonly Func<bool> _condition;
        private readonly Action? _thenAction;
        private readonly Action? _elseAction;

        public ConditionalStep(string name, Func<bool> condition, Action? thenAction = null, Action? elseAction = null) : base(name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _thenAction = thenAction;
            _elseAction = elseAction;
        }

        public override bool Execute()
        {
            if (Status != StepStatus.Pending)
            {
                return IsCompleted;
            }

            Status = StepStatus.Running;

            try
            {
                if (_condition())
                {
                    _thenAction?.Invoke();
                }
                else
                {
                    _elseAction?.Invoke();
                }
                Complete();
                return true;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// 延迟步骤 - 等待指定时间后完成
    /// </summary>
    public sealed class DelayStep : PhaseStepBase
    {
        private readonly float _delaySeconds;
        private double _elapsed;

        public DelayStep(string name, float delaySeconds) : base(name)
        {
            _delaySeconds = delaySeconds;
        }

        public override bool Execute()
        {
            if (IsCompleted)
            {
                return true;
            }

            Status = StepStatus.Running;
            _elapsed += 0.033f; // 假设每帧约 33ms

            if (_elapsed >= _delaySeconds)
            {
                Complete();
                return true;
            }

            return false;
        }

        public override void Reset()
        {
            base.Reset();
            _elapsed = 0;
        }
    }

    /// <summary>
    /// 空步骤 - 立即完成
    /// </summary>
    public sealed class EmptyStep : PhaseStepBase
    {
        public EmptyStep(string name = "Empty") : base(name)
        {
        }

        public override bool Execute()
        {
            Complete();
            return true;
        }
    }
}
