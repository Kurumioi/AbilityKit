using System;
using AbilityKit.Triggering.Runtime.Executable;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Dispatcher;

namespace AbilityKit.Triggering.Runtime.ActionScheduler
{
    /// <summary>
    /// Action 实例状态
    /// </summary>
    public enum EActionInstanceState : byte
    {
        /// <summary>已注册，等待调度</summary>
        Registered = 0,
        /// <summary>等待延迟（Delayed 模式）</summary>
        WaitingDelay = 1,
        /// <summary>等待条件满足</summary>
        WaitingCondition = 2,
        /// <summary>等待同步信号</summary>
        WaitingSignal = 3,
        /// <summary>等待队列</summary>
        WaitingQueue = 4,
        /// <summary>正在执行</summary>
        Executing = 5,
        /// <summary>执行完成</summary>
        Completed = 6,
        /// <summary>已中断</summary>
        Interrupted = 7,
        /// <summary>执行失败</summary>
        Failed = 8,
    }

    /// <summary>
    /// Action 运行时实例
    /// 由 Trigger 激活时创建，由 ActionScheduler 管理生命周期
    /// </summary>
    public sealed class ActionInstance
    {
        /// <summary>实例唯一ID</summary>
        public int InstanceId { get; }

        /// <summary>关联的触发器ID</summary>
        public int TriggerId { get; }

        /// <summary>Action 调用计划</summary>
        public ActionCallPlan Plan { get; }

        /// <summary>当前状态</summary>
        public EActionInstanceState State { get; internal set; }

        /// <summary>是否活跃</summary>
        public bool IsActive => State is EActionInstanceState.Registered
                                or EActionInstanceState.WaitingDelay
                                or EActionInstanceState.WaitingCondition
                                or EActionInstanceState.WaitingSignal
                                or EActionInstanceState.WaitingQueue
                                or EActionInstanceState.Executing;

        /// <summary>已执行次数</summary>
        public int ExecutionCount { get; internal set; }

        /// <summary>总消耗时间（毫秒）</summary>
        public float ElapsedMs { get; internal set; }

        /// <summary>上次执行时间（从启动算起）</summary>
        public float LastExecuteMs { get; internal set; }

        /// <summary>是否可以中断</summary>
        public bool CanBeInterrupted => Plan.Schedule.CanBeInterrupted;

        /// <summary>中断原因</summary>
        public string InterruptReason { get; internal set; }

        /// <summary>执行器</summary>
        public IActionExecutor Executor { get; }

        /// <summary>Action 委托（延迟解析）</summary>
        internal Action<object, ITriggerDispatcherContext> ActionDelegate { get; set; }

        /// <summary>条件委托（可选）</summary>
        internal TriggerPredicate<object> ConditionDelegate { get; set; }

        /// <summary>参数对象（延迟绑定）</summary>
        internal object BoundArgs { get; set; }

        /// <summary>全局上下文（从 Trigger 传递）</summary>
        public object GlobalContext { get; }

        /// <summary>创建时间戳</summary>
        public float CreatedAtMs { get; }

        private float _delayStartMs;
        private bool _hasDelayStart;

        internal ActionInstance(
            int instanceId,
            int triggerId,
            ActionCallPlan plan,
            IActionExecutor executor,
            object globalContext,
            float createdAtMs = 0f)
        {
            InstanceId = instanceId;
            TriggerId = triggerId;
            Plan = plan;
            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
            GlobalContext = globalContext;
            State = EActionInstanceState.Registered;
            ElapsedMs = 0;
            LastExecuteMs = 0;
            ExecutionCount = 0;
            CreatedAtMs = Math.Max(0f, createdAtMs);
            _hasDelayStart = false;
        }

        /// <summary>
        /// 每帧更新（由 ActionScheduler 调用）
        /// </summary>
        /// <param name="deltaTimeMs">帧间隔（毫秒）</param>
        /// <param name="ctx">执行上下文</param>
        /// <returns>执行结果，如果已完成/中断则返回非 Continue</returns>
        public ExecutionResult Update(float deltaTimeMs, ActionExecutionContext ctx)
        {
            if (State is EActionInstanceState.Completed or EActionInstanceState.Interrupted or EActionInstanceState.Failed)
                return ExecutionResult.Success(0);

            ElapsedMs += deltaTimeMs;

            // 阶段1: 调度时间窗检查
            if (!CanEnterExecutionWindow())
            {
                State = EActionInstanceState.WaitingDelay;
                return ExecutionResult.None;
            }

            // 阶段2: 条件检查（如果需要）
            if (ConditionDelegate != null && State != EActionInstanceState.Executing)
            {
                bool conditionMet = ConditionDelegate(BoundArgs, ctx.DispatcherContext);
                if (!conditionMet)
                {
                    State = EActionInstanceState.WaitingCondition;
                    return ExecutionResult.None;
                }

                State = EActionInstanceState.Executing;
            }

            // 阶段3: 根据调度模式执行
            var schedule = Plan.Schedule;
            if (schedule.Mode == Config.EActionScheduleMode.Timeline)
            {
                State = EActionInstanceState.Failed;
                return ExecutionResult.Failed("ActionScheduler 尚未定义 Timeline 子 Action 序列，不能按单一 ActionCallPlan 执行 Timeline。请改用 Plan/Executables 主线承载时间线行为。");
            }

            var execution = ExecuteInternal(ctx);

            // 检查是否完成。等待队列/同步/延迟重试等未实际执行的帧不能终结实例。
            if (execution.Result.IsFailed)
            {
                State = EActionInstanceState.Failed;
            }
            else if (execution.Executed && ShouldTerminate())
            {
                State = EActionInstanceState.Completed;
            }

            return execution.Result;
        }

        private bool CanEnterExecutionWindow()
        {
            var schedule = Plan.Schedule;
            switch (schedule.Mode)
            {
                case Config.EActionScheduleMode.Immediate:
                    return true;

                case Config.EActionScheduleMode.Delayed:
                    if (!_hasDelayStart)
                    {
                        _delayStartMs = ElapsedMs;
                        _hasDelayStart = true;
                    }
                    return ElapsedMs - _delayStartMs >= Math.Max(0f, schedule.Param);

                case Config.EActionScheduleMode.Periodic:
                case Config.EActionScheduleMode.Continuous:
                    if (schedule.Param <= 0f)
                    {
                        return true;
                    }

                    if (ExecutionCount <= 0)
                    {
                        return ElapsedMs >= schedule.Param;
                    }

                    return ElapsedMs - LastExecuteMs >= schedule.Param;

                case Config.EActionScheduleMode.Timeline:
                    return true;

                default:
                    return true;
            }
        }

        private ActionExecutionStep ExecuteInternal(ActionExecutionContext ctx)
        {
            // 检查执行条件（ExecutionPolicy）
            if (!CanExecuteByPolicy(ctx))
            {
                return new ActionExecutionStep(false, ExecutionResult.Skipped("Policy check failed"));
            }

            // 通过执行器执行
            if (Plan.Execution.Policy == Config.EActionExecutionPolicy.WithRollback)
            {
                return new ActionExecutionStep(false, ExecutionResult.Failed($"Action[{Plan.Id.Value}] 请求 WithRollback，但 ActionCallPlan 当前没有正式的补偿 Action 或回滚计划结构。"));
            }

            var executed = Executor.TryExecute(ctx, out var result);

            if (executed)
            {
                ExecutionCount++;
                LastExecuteMs = ElapsedMs;
            }

            return new ActionExecutionStep(executed, result);
        }

        private readonly struct ActionExecutionStep
        {
            public readonly bool Executed;
            public readonly ExecutionResult Result;

            public ActionExecutionStep(bool executed, ExecutionResult result)
            {
                Executed = executed;
                Result = result;
            }
        }

        private bool CanExecuteByPolicy(ActionExecutionContext ctx)
        {
            return Plan.Execution.Policy switch
            {
                Config.EActionExecutionPolicy.Conditional => ConditionDelegate?.Invoke(BoundArgs, ctx.DispatcherContext) ?? true,
                Config.EActionExecutionPolicy.Queued => !IsQueued(),
                Config.EActionExecutionPolicy.Parallel => true, // 并行总是允许
                Config.EActionExecutionPolicy.Deferred => ElapsedMs > 0, // 至少等待一帧
                _ => true
            };
        }

        private bool IsQueued()
        {
            return Executor is QueuedActionExecutor queued && queued.IsQueued;
        }

        private bool ShouldTerminate()
        {
            var schedule = Plan.Schedule;
            switch (schedule.Mode)
            {
                case Config.EActionScheduleMode.Immediate:
                    return true;

                case Config.EActionScheduleMode.Delayed:
                    return ExecutionCount >= 1;

                case Config.EActionScheduleMode.Periodic:
                    if (schedule.MaxExecutions > 0 && ExecutionCount >= schedule.MaxExecutions)
                        return true;
                    break;

                case Config.EActionScheduleMode.Continuous:
                    // Continuous 由外部控制终止
                    break;

                case Config.EActionScheduleMode.Timeline:
                    return ExecutionCount >= 1;
            }

            return false;
        }

        /// <summary>
        /// 请求中断
        /// </summary>
        public void RequestInterrupt(string reason)
        {
            if (!CanBeInterrupted) return;
            State = EActionInstanceState.Interrupted;
            InterruptReason = reason;
            Executor.Cancel(reason);
        }

        /// <summary>
        /// 重置（用于重用）
        /// </summary>
        public void Reset()
        {
            State = EActionInstanceState.Registered;
            ElapsedMs = 0;
            LastExecuteMs = 0;
            ExecutionCount = 0;
            InterruptReason = null;
            _hasDelayStart = false;
        }
    }
}
