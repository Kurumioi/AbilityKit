using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.Host.Transport;

namespace AbilityKit.Ability.Host.Builder.Components
{
    /// <summary>
    /// 连接管理器接口
    /// 负责管理客户端连接
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// 附加到 Runtime
        /// </summary>
        void Attach(HostRuntime runtime);

        /// <summary>
        /// 从 Runtime 分离
        /// </summary>
        void Detach();

        /// <summary>
        /// 启动监听
        /// </summary>
        void StartListen(string address, int port);

        /// <summary>
        /// 停止监听
        /// </summary>
        void StopListen();

        /// <summary>
        /// 获取所有连接
        /// </summary>
        IReadOnlyCollection<IServerConnection> Connections { get; }

        /// <summary>
        /// 连接事件
        /// </summary>
        event Action<IServerConnection> OnClientConnected;
        event Action<ServerClientId> OnClientDisconnected;
    }
}
