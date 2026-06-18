#pragma warning disable CS0618
using System;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Continuous;
using AbilityKit.Triggering.Runtime.Dispatcher;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Triggering.Runtime.Dispatcher
{
    /// <summary>
    /// Dispatcher 兼容聚合入口。
    /// 封装 TriggerDispatcherRegistry，统一事件、定时和持续执行等外部驱动方式。
    /// 新的事件订阅、条件评估和执行控制主线优先使用 Runtime/TriggerRunner；此类型保留用于旧 Dispatcher API 与外部驱动适配。
    /// </summary>
    [Obsolete("TriggerDispatcherHub is a legacy Dispatcher aggregate. Use TriggerRunner/EventBus for new event dispatch and ActionScheduler for plan action scheduling.")]
    public class TriggerDispatcherHub
    {
        private readonly TriggerDispatcherRegistry _registry;
        private ITriggerDispatcherContext _currentContext;

        /// <summary>
        /// Action 调度器管理器，用于维护由计划 Action 派生出的延迟、周期和持续型 Action 实例。
        /// </summary>
        private readonly ActionSchedulerManager _actionSchedulerManager;

        /// <summary>
        /// 创建调度中心
        /// </summary>
        public TriggerDispatcherHub()
        {
            _registry = new TriggerDispatcherRegistry();
            _actionSchedulerManager = new ActionSchedulerManager();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            _registry.Initialize();
        }

        /// <summary>
        /// 设置当前上下文
        /// </summary>
        public void SetContext(ITriggerDispatcherContext context)
        {
            _currentContext = context;
        }

        /// <summary>
        /// 获取事件总线调度器
        /// </summary>
        public EventBusDispatcher Event => _registry.Event;

        /// <summary>
        /// 获取定时调度器
        /// </summary>
        public TimedDispatcher Timed => _registry.Timed;

        /// <summary>
        /// 获取 Action 调度器管理器
        /// </summary>
        public ActionSchedulerManager ActionSchedulerManager => _actionSchedulerManager;

        /// <summary>
        /// 注册持续行为触发器
        /// 复用一个 Predicate 和 Action 的原子操作
        /// </summary>
        public void RegisterContinuous<TArgs>(
            int triggerId,
            in TriggerPlan<TArgs> plan,
            TriggerPredicate<TArgs> predicate,
            TriggerExecutor<TArgs> executor)
            where TArgs : class
        {
            // 持续行为使用 TimedDispatcher
            _registry.Timed.Register(in plan, predicate, executor);
        }

        /// <summary>
        /// 注册事件触发器
        /// </summary>
        public void RegisterEvent<TArgs>(
            int triggerId,
            in TriggerPlan<TArgs> plan,
            TriggerPredicate<TArgs> predicate,
            TriggerExecutor<TArgs> executor)
            where TArgs : class
        {
            _registry.Event.Register(in plan, predicate, executor);
        }

        /// <summary>
        /// 注册定时触发器
        /// </summary>
        public void RegisterTimed<TArgs>(
            int triggerId,
            in TriggerPlan<TArgs> plan,
            TriggerPredicate<TArgs> predicate,
            TriggerExecutor<TArgs> executor)
            where TArgs : class
        {
            _registry.Timed.Register(in plan, predicate, executor);
        }

        /// <summary>
        /// 注册到自动选择的调度器
        /// </summary>
        public void RegisterAuto<TArgs>(
            in TriggerPlan<TArgs> plan,
            TriggerPredicate<TArgs> predicate,
            TriggerExecutor<TArgs> executor)
            where TArgs : class
        {
            _registry.RegisterToAll(in plan, predicate, executor);
        }

        /// <summary>
        /// 注册外部生命周期控制的持续 tick 执行器。
        /// 返回值是持续执行器的 triggerId，可用于后续中断或注销。
        /// </summary>
        public int RegisterContinuousExecutor<TCtx>(
            ContinuousExecutorBase<TCtx> executor,
            float intervalMs = 0,
            int maxExecutions = -1,
            TCtx context = null,
            bool canBeInterrupted = true)
            where TCtx : class
        {
            var triggerId = ContinuousExecutorRegistry.Register(executor, intervalMs);
            _registry.Timed.RegisterContinuousExecutor(triggerId, intervalMs, maxExecutions, context, canBeInterrupted);
            return triggerId;
        }

        /// <summary>
        /// 每帧更新。
        /// 先推进触发器级 Dispatcher，再推进由触发器计划派生出的 Action 级调度实例。
        /// </summary>
        public void Update(float deltaTimeMs)
        {
            _registry.Update(deltaTimeMs, _currentContext);
            _actionSchedulerManager.Update(deltaTimeMs, _currentContext);
        }

        /// <summary>
        /// 中断所有持续行为
        /// </summary>
        public void InterruptAllContinuous(string reason = null)
        {
            _registry.Timed.InterruptAll(reason ?? "User interrupt");
        }

        /// <summary>
        /// 获取活跃的持续行为数量
        /// </summary>
        public int GetActiveContinuousCount()
        {
            return _registry.Timed.RegisteredCount;
        }

        /// <summary>
        /// 获取总注册数量
        /// </summary>
        public int GetTotalRegisteredCount()
        {
            return _registry.TotalRegisteredCount;
        }

        /// <summary>
        /// 销毁调度中心并清理触发器级与 Action 级调度资源。
        /// </summary>
        public void Dispose()
        {
            _registry.Dispose();
            _actionSchedulerManager.Clear();
        }
    }
}