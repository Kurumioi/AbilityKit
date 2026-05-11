using UnityEngine;
using AbilityKit.Triggering.Samples.Scheduler;

namespace AbilityKit.Samples
{
    /// <summary>
    /// 调度器示例 - 使用新的 ECA 架构
    ///
    /// 本示例演示：
    /// 1. 创建触发原子注册中心（非单例，支持 DI）
    /// 2. 创建调度器注册中心（非单例，支持 DI）
    /// 3. 注册 ECA 触发原子
    /// 4. 创建并启动调度器
    /// 5. 每帧更新调度器
    /// </summary>
    public class ScheduleManagerSample : MonoBehaviour
    {
        [Header("调度设置")]
        [Tooltip("执行间隔（秒）")]
        [SerializeField] private float _interval = 1f;

        [Tooltip("最大执行次数")]
        [SerializeField] private int _maxExecutions = 5;

        // 调度器注册中心（非单例，由调用方管理）
        private SchedulerRegistry _schedulerRegistry;
        private int _executionCount;

        // 简单的行为存储（实际项目中可使用 ActionRegistry）
        private System.Action<object> _periodicAction;

        private void Start()
        {
            Debug.Log("========== ScheduleManager ECA 架构示例开始 ==========");
            Debug.Log($"配置: Interval={_interval}s, MaxExecutions={_maxExecutions}");

            InitializeSchedulerSystem();
            RegisterAction();
            RegisterTriggerAtoms();
            CreateAndStartScheduler();
        }

        private void Update()
        {
            // 每帧更新所有活跃调度器
            if (_schedulerRegistry != null)
            {
                foreach (var scheduler in _schedulerRegistry.GetActiveSchedulers())
                {
                    scheduler.Update(Time.deltaTime * 1000f, null);
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("========== ScheduleManager ECA 架构示例结束 ==========");
            Debug.Log($"最终执行次数: {_executionCount}");
        }

        /// <summary>
        /// 初始化调度系统（非单例，支持 DI）
        /// </summary>
        private void InitializeSchedulerSystem()
        {
            Debug.Log("[1] 初始化调度系统");

            // 创建触发原子注册中心（可注入）
            var triggerAtomRegistry = new TriggerAtomRegistry();

            // 创建调度器注册中心（可注入）
            _schedulerRegistry = new SchedulerRegistry(triggerAtomRegistry);

            Debug.Log("    - TriggerAtomRegistry 创建完成");
            Debug.Log("    - SchedulerRegistry 创建完成");
            Debug.Log("");
        }

        /// <summary>
        /// 注册行为（示例：周期性日志）
        /// </summary>
        private void RegisterAction()
        {
            Debug.Log("[2] 注册行为 (Action)");

            // 注册周期性日志行为
            _periodicAction = (ctx) =>
            {
                _executionCount++;
                Debug.Log($"    ★ [执行 #{_executionCount}]");
            };

            Debug.Log("    - action:log_periodic 回调注册完成");
            Debug.Log("");
        }

        /// <summary>
        /// 注册触发原子
        /// </summary>
        private void RegisterTriggerAtoms()
        {
            Debug.Log("[3] 注册触发原子 (ECA)");

            var registry = _schedulerRegistry.TriggerAtoms;

            // 创建无条件触发原子（主动调度用，无事件，无条件）
            // ECA: Event=空, Condition=空, Action=内部回调
            var unconditionalAtom = TriggerAtom.CreateActionOnly(
                TriggerAtomId.Get("atom:periodic_log"),
                priority: 0
            );
            registry.Register(unconditionalAtom);

            Debug.Log("    - atom:periodic_log 注册完成 (无事件，无条件)");
            Debug.Log("");
        }

        /// <summary>
        /// 创建并启动调度器
        /// </summary>
        private void CreateAndStartScheduler()
        {
            Debug.Log("[4] 创建并启动调度器");

            var schedulerId = SchedulerId.Get("scheduler:periodic_test");
            var atomId = TriggerAtomId.Get("atom:periodic_log");
            var config = ScheduleConfig.Periodic(_interval * 1000f, _maxExecutions);

            // 创建调度器（主动调度模式）
            var scheduler = _schedulerRegistry.CreateScheduler(
                id: schedulerId,
                context: null,
                triggerAtomId: atomId,
                config: config,
                actionCallback: _periodicAction, // 直接传入回调
                onComplete: (ctx, triggerCtx) =>
                {
                    Debug.Log("    - 调度完成回调!");
                },
                onInterrupt: (ctx, triggerCtx) =>
                {
                    Debug.Log("    - 调度中断回调!");
                }
            );

            if (scheduler != null)
            {
                // 启动调度器
                scheduler.Start();

                Debug.Log($"    - 调度器已启动: {schedulerId}");
                Debug.Log($"    - 触发原子: {atomId}");
                Debug.Log($"    - 调度配置: interval={_interval * 1000f}ms, maxExecutions={_maxExecutions}");
            }

            Debug.Log("");
        }
    }
}
