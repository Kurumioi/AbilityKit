using System;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// 调度器句柄
    /// 用于引用和操作已创建的调度器
    /// </summary>
    public readonly struct SchedulerHandle : IEquatable<SchedulerHandle>
    {
        /// <summary>调度器ID</summary>
        public readonly int SchedulerId;

        /// <summary>句柄版本（用于检测过期）</summary>
        public readonly int Version;

        public bool IsValid => SchedulerId > 0;

        public static SchedulerHandle Invalid => default;

        public SchedulerHandle(int schedulerId, int version = 1)
        {
            SchedulerId = schedulerId;
            Version = version;
        }

        public bool Equals(SchedulerHandle other) => SchedulerId == other.SchedulerId && Version == other.Version;
        public override bool Equals(object obj) => obj is SchedulerHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SchedulerId, Version);
        public static bool operator ==(SchedulerHandle left, SchedulerHandle right) => left.Equals(right);
        public static bool operator !=(SchedulerHandle left, SchedulerHandle right) => !left.Equals(right);
        public override string ToString() => IsValid ? $"Scheduler[{SchedulerId}]" : "Scheduler[Invalid]";
    }

    /// <summary>
    /// 调度器接口
    /// 定义调度器的核心功能
    ///
    /// 【框架层】提供通用调度抽象，项目可实现 IScheduler 接口扩展自定义调度器
    /// </summary>
    public interface IScheduler
    {
        #region 属性

        /// <summary>
        /// 调度器句柄
        /// </summary>
        SchedulerHandle Handle { get; }

        /// <summary>
        /// 调度器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 关联的业务对象ID
        /// </summary>
        int BusinessId { get; }

        /// <summary>
        /// 关联的触发器ID
        /// </summary>
        int TriggerId { get; }

        /// <summary>
        /// 调度配置
        /// </summary>
        SchedulerConfig Config { get; }

        /// <summary>
        /// 上下文数据
        /// </summary>
        object Context { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        ESchedulerState State { get; }

        /// <summary>
        /// 是否激活
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 是否可以中断
        /// </summary>
        bool CanBeInterrupted { get; }

        /// <summary>
        /// 当前执行次数
        /// </summary>
        int ExecutionCount { get; }

        /// <summary>
        /// 已消耗时间（毫秒）
        /// </summary>
        float ElapsedMs { get; }

        #endregion

        #region 控制

        /// <summary>
        /// 开始调度
        /// </summary>
        void Start();

        /// <summary>
        /// 暂停调度
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复调度
        /// </summary>
        void Resume();

        /// <summary>
        /// 停止调度
        /// </summary>
        void Stop();

        /// <summary>
        /// 重置调度器
        /// </summary>
        void Reset();

        #endregion

        #region 执行

        /// <summary>
        /// 每帧更新
        /// </summary>
        /// <param name="deltaTimeMs">帧间隔（毫秒）</param>
        /// <param name="triggerContext">触发上下文</param>
        /// <returns>是否继续执行，返回 false 表示调度器已结束</returns>
        bool Update(float deltaTimeMs, object triggerContext = null);

        #endregion
    }
}
