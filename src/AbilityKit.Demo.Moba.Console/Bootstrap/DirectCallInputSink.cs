using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Simulation;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// 直接调用模式的输入转发表层
    ///
    /// 实现 IConsoleInputSink，直接调用 SimulatedBattleSession
    /// 适用于本地开发/测试，零网络开销
    ///
    /// 架构说明：
    /// - InputFeature 调用此 Sink
    /// - 此 Sink 直接调用 Session.SubmitInput()
    /// - 无任何网络传输开销
    /// </summary>
    public sealed class DirectCallInputSink : IConsoleInputSink
    {
        private readonly SimulatedBattleSession _session;

        public DirectCallInputSink(SimulatedBattleSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0)
            {
                return;
            }

            _session.SubmitInput(frame, inputs);
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
