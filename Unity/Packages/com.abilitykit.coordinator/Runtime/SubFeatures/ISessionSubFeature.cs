using System;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// 会话子功能接口。
    ///
    /// 设计：
    /// - SubFeature 是扩展会话功能的模块化组件。
    /// - 每个 SubFeature 处理会话生命周期中的特定方面。
    /// - SubFeature 挂接到会话后接收生命周期回调。
    /// </summary>
    public interface ISessionSubFeature
    {
        /// <summary>
        /// SubFeature 名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 优先级（数值越高越先调用）。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// SubFeature 挂接到会话时调用。
        /// </summary>
        void OnAttach(ISessionHost host);

        /// <summary>
        /// SubFeature 从会话分离时调用。
        /// </summary>
        void OnDetach();

        /// <summary>
        /// 会话运行时每帧调用。
        /// </summary>
        void OnTick(float deltaTime);
    }

    /// <summary>
    /// 支持 PreTick 的会话子功能。
    /// </summary>
    public interface ISessionPreTickSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 主会话 Tick 之前调用。
        /// </summary>
        void OnPreTick(float deltaTime);
    }

    /// <summary>
    /// 支持 PostTick 的会话子功能。
    /// </summary>
    public interface ISessionPostTickSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 主会话 Tick 之后调用。
        /// </summary>
        void OnPostTick(float deltaTime);
    }

    /// <summary>
    /// 支持生命周期回调的会话子功能。
    /// </summary>
    public interface ISessionLifecycleSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 会话即将启动时调用。
        /// </summary>
        void OnSessionStarting();

        /// <summary>
        /// 会话即将停止时调用。
        /// </summary>
        void OnSessionStopping();
    }

    /// <summary>
    /// 会话事件子功能接口。
    /// 用于需要抛出会话生命周期事件的 SubFeature。
    /// </summary>
    public interface ISessionEventsSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 请求启动会话时调用。
        /// </summary>
        void OnStartSessionRequested();

        /// <summary>
        /// 抛出会话已启动事件。
        /// </summary>
        void RaiseSessionStarted();

        /// <summary>
        /// 抛出会话失败事件。
        /// </summary>
        void RaiseSessionFailed(Exception ex);
    }

    /// <summary>
    /// 会话 TickLoop 子功能接口。
    /// 用于需要驱动主 Tick 循环的 SubFeature。
    /// </summary>
    public interface ISessionTickLoopSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 主 Tick 阶段调用。
        /// </summary>
        void OnMainTick(float deltaTime);

        /// <summary>
        /// 启动 Tick 循环。
        /// </summary>
        void Start();

        /// <summary>
        /// 停止 Tick 循环。
        /// </summary>
        void Stop();

        /// <summary>
        /// 检查是否正在运行。
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// 会话快照路由子功能接口。
    /// 用于需要处理帧快照的 SubFeature。
    /// </summary>
    public interface ISessionSnapshotRoutingSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// 初始化路由。
        /// </summary>
        void InitializeRouting();

        /// <summary>
        /// 收到帧时调用。
        /// </summary>
        void OnFrameReceived(int frameIndex);
    }
}
