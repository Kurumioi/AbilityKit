using System;

namespace AbilityKit.Core.Continuous
{
    /// <summary>
    /// 持续体管理器接口
    /// 
    /// 业务层可以实现此接口来管理 IContinuous 的生命周期。
    /// 核心包不提供默认实现，具体的互斥、标签、暂停/恢复逻辑由业务层决定。
    /// 
    /// 使用方式：
    /// 1. 业务层实现 IContinuousManager 接口
    /// 2. 或继承/组合 DefaultContinuousManager（如果提供了默认实现）
    /// </summary>
    public interface IContinuousManager
    {
        /// <summary>
        /// 注册持续体
        /// </summary>
        /// <returns>是否注册成功</returns>
        bool Register(IContinuous continuous);

        /// <summary>
        /// 注销持续体
        /// </summary>
        void Unregister(IContinuous continuous, ContinuousEndReason reason = ContinuousEndReason.CleanedUp);

        /// <summary>
        /// 尝试激活持续体
        /// </summary>
        bool TryActivate(IContinuous continuous);

        /// <summary>
        /// 获取实体的所有持续体
        /// </summary>
        IReadOnlyList<IContinuous> GetOwnerContinuous(long ownerId);

        /// <summary>
        /// 获取实体的活跃持续体
        /// </summary>
        IReadOnlyList<IContinuous> GetOwnerActiveContinuous(long ownerId);

        /// <summary>
        /// 中断实体的所有持续体
        /// </summary>
        void InterruptAll(long ownerId, string reason);

        /// <summary>
        /// 暂停实体的所有持续体
        /// </summary>
        void PauseAll(long ownerId);

        /// <summary>
        /// 恢复实体的所有持续体
        /// </summary>
        void ResumeAll(long ownerId);

        /// <summary>
        /// 活跃持续体数量
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// 总持续体数量
        /// </summary>
        int TotalCount { get; }
    }
}
