using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 会话协调器接口。
    ///
    /// 设计：
    /// - 作为会话协调的主接口。
    /// - 管理世界生命周期、同步适配器和子功能。
    /// - 提供会话资源的统一访问入口。
    /// </summary>
    public interface ISessionCoordinator : IDisposable
    {
        // ============== 标识 ==============

        /// <summary>
        /// 会话标识。
        /// </summary>
        SessionId SessionId { get; }

        /// <summary>
        /// 会话配置。
        /// </summary>
        SessionConfig Config { get; }

        /// <summary>
        /// 当前会话状态。
        /// </summary>
        SessionState State { get; }

        // ============== 世界访问 ==============

        /// <summary>
        /// 世界宿主实例。
        /// </summary>
        IWorldHost WorldHost { get; }

        /// <summary>
        /// 当前世界实例。
        /// </summary>
        IWorld World { get; }

        /// <summary>
        /// 用于服务访问的世界解析器。
        /// </summary>
        IWorldResolver WorldResolver { get; }

        // ============== 同步 ==============

        /// <summary>
        /// 同步适配器实例。
        /// </summary>
        ISyncAdapter SyncAdapter { get; }

        /// <summary>
        /// 用于插值的视图时间线。
        /// </summary>
        Timeline.IViewTimeline ViewTimeline { get; }

        // ============== 驱动与视图 ==============

        /// <summary>
        /// 设置逻辑世界驱动桥接器。
        /// </summary>
        void SetLogicWorldDriver(ILogicWorldDriverBridge driverHost);

        /// <summary>
        /// 获取逻辑世界驱动桥接器。
        /// </summary>
        ILogicWorldDriverBridge? LogicWorldDriver { get; }

        /// <summary>
        /// 设置视图事件接收器。
        /// </summary>
        void SetViewEventSink(IViewEventSink sink);

        /// <summary>
        /// 获取视图事件接收器。
        /// </summary>
        IViewEventSink? ViewEventSink { get; }

        // ============== 生命周期 ==============

        /// <summary>
        /// 初始化会话协调器。
        /// </summary>
        void Initialize(SessionConfig config, ISessionCoordinatorHost host);

        /// <summary>
        /// 启动会话。
        /// </summary>
        void Start();

        /// <summary>
        /// 停止会话。
        /// </summary>
        void Stop();

        /// <summary>
        /// 销毁会话并释放资源。
        /// </summary>
        void Destroy();

        // ============== 输入 ==============

        /// <summary>
        /// 提交本地玩家输入。
        /// </summary>
        void SubmitLocalInput(PlayerInput input);

        // ============== 子功能访问 ==============

        /// <summary>
        /// 获取用于事件订阅的会话钩子。
        /// </summary>
        SessionHooks Hooks { get; }

        /// <summary>
        /// 从世界中解析服务。
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        /// 尝试从世界中解析服务。
        /// </summary>
        bool TryResolve<T>(out T service) where T : class;
    }
}
