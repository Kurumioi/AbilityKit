using System;

namespace AbilityKit.Coordinator.Core
{
    /// <summary>
    /// 会话状态枚举。
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// 会话已创建但尚未启动。
        /// </summary>
        Idle,

        /// <summary>
        /// 会话正在初始化。
        /// </summary>
        Initializing,

        /// <summary>
        /// 会话正在运行。
        /// </summary>
        Running,

        /// <summary>
        /// 会话已暂停。
        /// </summary>
        Paused,

        /// <summary>
        /// 会话正在停止。
        /// </summary>
        Stopping,

        /// <summary>
        /// 会话已停止。
        /// </summary>
        Stopped,

        /// <summary>
        /// 会话遇到错误。
        /// </summary>
        Error
    }

    /// <summary>
    /// 同步模式枚举。
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 本地锁步模拟（单机或局域网）。
        /// </summary>
        Lockstep = 0,

        /// <summary>
        /// 服务端权威快照模式。
        /// </summary>
        SnapshotAuthority = 1,

        /// <summary>
        /// 从服务端同步状态。
        /// </summary>
        StateSync = 2,

        /// <summary>
        /// 客户端预测并由服务端校正。
        /// </summary>
        Hybrid = 3
    }

    /// <summary>
    /// 宿主模式枚举。
    /// </summary>
    public enum HostMode
    {
        /// <summary>
        /// 单人或离线模式。
        /// </summary>
        Local = 0,

        /// <summary>
        /// 多人模式中的主机玩家。
        /// </summary>
        Host = 1,

        /// <summary>
        /// 多人模式中的客户端玩家。
        /// </summary>
        Client = 2
    }
}
