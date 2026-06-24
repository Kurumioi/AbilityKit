using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 本地输入占位 Sink。
    ///
    /// 当前 Console 启动链路没有创建正式 runtime world，因此这里仅记录输入。
    /// 需要接入真实战斗逻辑时，应由 bootstrapper 注入 IMobaBattleInputPort backed sink。
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

            Platform.Log.Input($"[DirectCallInputSink] Runtime input port is not wired; recorded {inputs.Count} local commands at frame {frame}");
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Console 输入到正式运行时输入端口的转发 Sink。
    /// </summary>
    public sealed class RuntimePortInputSink : IWorldInputSink
    {
        private readonly IMobaBattleInputPort _inputPort;
        private bool _disposed;
        private RuntimeInputDiagnostics _diagnostics;

        public RuntimeInputDiagnostics Diagnostics => _diagnostics;

        public RuntimePortInputSink(IMobaBattleInputPort inputPort)
        {
            _inputPort = inputPort ?? throw new ArgumentNullException(nameof(inputPort));
        }

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (_disposed || inputs == null || inputs.Count == 0)
            {
                return;
            }

            var result = _inputPort.Submit(frame, inputs);
            _diagnostics = _diagnostics.Record(frame.Value, inputs.Count, result.Succeeded, result.CommandCount, result.ToString());
            if (!result.Succeeded)
            {
                Platform.Log.Input($"[RuntimePortInputSink] Submit rejected. {result}");
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    public readonly struct RuntimeInputDiagnostics
    {
        public static readonly RuntimeInputDiagnostics Empty = new RuntimeInputDiagnostics(0, 0, 0, 0, 0, false, null);

        public RuntimeInputDiagnostics(int submitCount, int acceptedCount, int submittedCommandCount, int acceptedCommandCount, int lastFrame, bool lastSucceeded, string lastResult)
        {
            SubmitCount = submitCount;
            AcceptedCount = acceptedCount;
            SubmittedCommandCount = submittedCommandCount;
            AcceptedCommandCount = acceptedCommandCount;
            LastFrame = lastFrame;
            LastSucceeded = lastSucceeded;
            LastResult = lastResult;
        }

        public int SubmitCount { get; }
        public int AcceptedCount { get; }
        public int SubmittedCommandCount { get; }
        public int AcceptedCommandCount { get; }
        public int LastFrame { get; }
        public bool LastSucceeded { get; }
        public string LastResult { get; }

        public RuntimeInputDiagnostics Record(int frame, int submittedCommands, bool succeeded, int acceptedCommands, string result)
        {
            return new RuntimeInputDiagnostics(
                SubmitCount + 1,
                AcceptedCount + (succeeded ? 1 : 0),
                SubmittedCommandCount + submittedCommands,
                AcceptedCommandCount + acceptedCommands,
                frame,
                succeeded,
                result);
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
