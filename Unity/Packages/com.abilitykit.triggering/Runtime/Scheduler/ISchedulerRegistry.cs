using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// 调度器注册中心接口
    /// 提供统一的调度器生命周期管理和查询能力
    ///
    /// 使用场景：
    /// - Buff 系统：管理所有 Buff 相关的调度器
    /// - 子弹系统：管理所有子弹的飞行调度器
    /// - 技能系统：管理所有技能效果的调度器
    /// - 项目特定调度：任何需要统一管理调度器的场景
    ///
    /// 【框架层】此接口定义在 Runtime 层，具体项目可自行实现或扩展
    /// </summary>
    public interface ISchedulerRegistry
    {
        #region 属性

        /// <summary>
        /// 总调度器数量
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// 活跃的调度器数量
        /// </summary>
        int ActiveCount { get; }

        #endregion

        #region 创建

        /// <summary>
        /// 创建调度器
        /// </summary>
        /// <param name="schedulerId">调度器唯一ID</param>
        /// <param name="businessId">业务对象ID（如 BuffId、子弹Id）</param>
        /// <param name="triggerId">关联的触发器ID</param>
        /// <param name="config">调度配置</param>
        /// <param name="actionCallback">执行回调</param>
        /// <param name="context">上下文数据</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="onInterrupt">中断回调</param>
        /// <returns>调度器句柄，无效返回 default</returns>
        SchedulerHandle CreateScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            in SchedulerConfig config,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null,
            Action<object, object> onInterrupt = null);

        /// <summary>
        /// 创建周期性调度器
        /// </summary>
        SchedulerHandle CreatePeriodicScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float intervalMs,
            int maxExecutions,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null);

        /// <summary>
        /// 创建延迟调度器
        /// </summary>
        SchedulerHandle CreateDelayedScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float delayMs,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null);

        /// <summary>
        /// 创建持续调度器
        /// </summary>
        SchedulerHandle CreateContinuousScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float intervalMs,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onInterrupt = null);

        #endregion

        #region 查询

        /// <summary>
        /// 获取调度器
        /// </summary>
        IScheduler GetScheduler(SchedulerHandle handle);

        /// <summary>
        /// 获取调度器数据
        /// </summary>
        bool TryGetSchedulerData(SchedulerHandle handle, out SchedulerData data);

        /// <summary>
        /// 根据业务ID查找所有调度器
        /// </summary>
        IEnumerable<IScheduler> FindByBusinessId(int businessId);

        /// <summary>
        /// 根据触发器ID查找所有调度器
        /// </summary>
        IEnumerable<IScheduler> FindByTriggerId(int triggerId);

        /// <summary>
        /// 获取所有活跃的调度器
        /// </summary>
        IEnumerable<IScheduler> GetActiveSchedulers();

        #endregion

        #region 控制

        /// <summary>
        /// 暂停调度器
        /// </summary>
        bool Pause(SchedulerHandle handle);

        /// <summary>
        /// 恢复调度器
        /// </summary>
        bool Resume(SchedulerHandle handle);

        /// <summary>
        /// 中断调度器
        /// </summary>
        bool Interrupt(SchedulerHandle handle, string reason = null);

        /// <summary>
        /// 移除调度器
        /// </summary>
        bool Remove(SchedulerHandle handle);

        /// <summary>
        /// 暂停所有调度器
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复所有调度器
        /// </summary>
        void ResumeAll();

        /// <summary>
        /// 中断所有可中断的调度器
        /// </summary>
        int InterruptAll(string reason = null);

        /// <summary>
        /// 根据业务ID中断所有相关调度器
        /// </summary>
        int InterruptByBusinessId(int businessId, string reason = null);

        /// <summary>
        /// 根据触发器ID中断所有相关调度器
        /// </summary>
        int InterruptByTriggerId(int triggerId, string reason = null);

        #endregion

        #region 更新

        /// <summary>
        /// 更新所有调度器
        /// </summary>
        void Update(float deltaTimeMs);

        #endregion

        #region 清理

        /// <summary>
        /// 清空所有调度器
        /// </summary>
        void Clear();

        #endregion
    }
}
