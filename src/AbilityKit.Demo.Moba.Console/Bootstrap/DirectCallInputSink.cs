using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// 直接调用模式的输入转发表层
    ///
    /// 实现 IWorldInputSink（框架接口），处理输入命令
    /// 适用于本地开发/测试，零网络开销
    ///
    /// 架构说明：
    /// - InputFeature 调用此 Sink
    /// - 此 Sink 直接调用逻辑层服务处理输入
    /// - 无任何网络传输开销
    /// </summary>
    public sealed class DirectCallInputSink : IWorldInputSink
    {
        private bool _disposed;

        public DirectCallInputSink()
        {
        }

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (_disposed || inputs == null || inputs.Count == 0)
            {
                return;
            }

            // 输入命令由逻辑层服务处理
            // 这里只是转发，具体处理逻辑由 MobaLobbyInputSink 或其他逻辑层服务执行
            Platform.Log.Input($"[DirectCallInputSink] Submit {inputs.Count} commands at frame {frame}");
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// 输入传输模式枚举
    /// </summary>
    public enum InputTransportMode
    {
        /// <summary>
        /// 直接调用模式（本地开发/测试）
        /// </summary>
        DirectCall = 0,

        /// <summary>
        /// 帧同步模式（模拟网络传输）
        /// </summary>
        FrameSync = 1,
    }
}
