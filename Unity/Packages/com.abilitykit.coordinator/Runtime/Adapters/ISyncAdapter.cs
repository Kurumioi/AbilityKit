using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 同步适配器接口。
    ///
    /// 设计：
    /// - 作为全部同步适配器的基础接口。
    /// - 提供通用属性和方法。
    /// - 通过标记接口承载模式专属行为。
    /// </summary>
    public interface ISyncAdapter : IDisposable
    {
        // ============== 核心属性 ==============

        /// <summary>
        /// 当前同步模式。
        /// </summary>
        Core.SyncMode Mode { get; }

        /// <summary>
        /// 当前逻辑帧号。
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 逻辑时间（秒）。
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// 渲染时间（秒，用于视图插值）。
        /// </summary>
        double RenderTimeSeconds { get; }

        /// <summary>
        /// 本地玩家标识。
        /// </summary>
        int LocalPlayerId { get; }

        // ============== 核心事件 ==============

        /// <summary>
        /// 帧同步事件（每帧触发）。
        /// </summary>
        event Action<int, double> OnFrameSync;

        // ============== 核心方法 ==============

        /// <summary>
        /// 附加到会话协调器。
        /// </summary>
        void Attach(ISessionCoordinator coordinator);

        /// <summary>
        /// 携带驱动宿主附加到会话协调器。
        /// </summary>
        void Attach(ISessionCoordinator coordinator, ILogicWorldDriverBridge driverHost);

        /// <summary>
        /// 初次附加后设置逻辑世界驱动。
        /// </summary>
        void SetLogicWorldDriver(ILogicWorldDriverBridge driverHost);

        /// <summary>
        /// 帧更新（由 tick 循环调用）。
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 提交本地玩家输入。
        /// </summary>
        void SubmitInput(PlayerInput input);

        /// <summary>
        /// 获取用于渲染的全部实体状态。
        /// </summary>
        SnapshotEntityState[] GetAllEntityStates();
    }

    // ============== 模式专属接口 ==============

    /// <summary>
    /// 本地同步适配器（Lockstep 模式）。
    /// 仅本地同步适配器使用的标记接口。
    /// </summary>
    public interface ILocalSyncAdapter : ISyncAdapter
    {
        /// <summary>
        /// 本地模式始终返回 true。
        /// </summary>
        bool IsConnected { get; }
    }

    /// <summary>
    /// 远程同步适配器（StateSync/Hybrid 模式）。
    /// 需要网络连接的适配器使用的标记接口。
    /// </summary>
    public interface IRemoteSyncAdapter : ISyncAdapter
    {
        /// <summary>
        /// 是否已连接到远程服务器。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接状态变化事件。
        /// </summary>
        event Action<bool> OnConnectionChanged;

        /// <summary>
        /// 收到服务器快照时触发的实体状态快照事件。
        /// </summary>
        event Action<SnapshotEntityState[]> OnServerSnapshot;

        /// <summary>
        /// 连接到远程服务器。
        /// </summary>
        void Connect(NetworkEndpoint endpoint, long roomId, long playerId);

        /// <summary>
        /// 断开远程服务器连接。
        /// </summary>
        void Disconnect();
    }

    /// <summary>
    /// 预测支持接口。
    /// 用于支持客户端预测的适配器。
    /// </summary>
    public interface IPredictionSyncAdapter : IRemoteSyncAdapter
    {
        /// <summary>
        /// 是否启用预测。
        /// </summary>
        bool IsPredictionEnabled { get; }

        /// <summary>
        /// 启用或禁用预测。
        /// </summary>
        void SetPredictionEnabled(bool enabled);

        /// <summary>
        /// 获取预测超前帧数。
        /// </summary>
        int PredictionAheadFrames { get; }

        /// <summary>
        /// 触发校正（回滚）。
        /// </summary>
        void TriggerReconciliation(int confirmedFrame, SnapshotEntityState[] serverState);
    }
}
