using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// 会话主机接口。
    ///
    /// 设计：
    /// - SubFeature 通过该接口访问会话功能。
    /// - 为 SubFeature 提供依赖注入入口。
    /// </summary>
    public interface ISessionHost
    {
        // ============== 会话属性 ==============

        /// <summary>
        /// 会话状态。
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// 当前帧号。
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 当前逻辑时间，单位为秒。
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// 会话配置。
        /// </summary>
        SessionConfig Config { get; }

        /// <summary>
        /// 会话钩子。
        /// </summary>
        SessionHooks Hooks { get; }

        // ============== SubFeature 管理 ==============

        /// <summary>
        /// 从会话中获取服务。
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// 尝试从会话中获取服务。
        /// </summary>
        bool TryGetService<T>(out T service) where T : class;

        /// <summary>
        /// 注册服务。
        /// </summary>
        void RegisterService<T>(T service) where T : class;
    }
}
