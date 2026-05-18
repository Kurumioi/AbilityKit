using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Console.Core.Input
{
    /// <summary>
    /// Console 平台输入转发表层接口
    ///
    /// 架构说明：
    /// - 表现层（InputFeature）通过此接口与逻辑层（Session）解耦
    /// - 不同的 Sink 实现代表不同的输入传输方式
    /// - Console 提供 DirectCall Sink（直接调用）
    /// - Unity 可提供 FrameSync Sink（帧同步网络传输）
    ///
    /// 设计模式：Strategy 模式
    /// </summary>
    public interface IConsoleInputSink
    {
        /// <summary>
        /// 提交输入命令
        /// </summary>
        /// <param name="frame">当前帧索引</param>
        /// <param name="inputs">输入命令列表</param>
        void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
