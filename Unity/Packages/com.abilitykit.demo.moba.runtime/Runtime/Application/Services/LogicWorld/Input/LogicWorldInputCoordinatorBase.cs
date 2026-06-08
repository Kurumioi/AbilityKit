using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Services.LogicWorld
{
    /// <summary>
    /// 逻辑世界输入协调器基类。
    /// 框架负责输入批次遍历、生命周期接入、上下文创建时机和命令路由顺序；具体逻辑层只实现上下文和命令处理。
    /// </summary>
    public abstract class LogicWorldInputCoordinatorBase<TContext> : IService, IWorldInitializable, ILogicWorldInputCoordinator where TContext : class
    {
        private IFrameTime _frameTime;
        private IMobaBattleDiagnosticsService _diagnostics;
        private bool _missingFrameTimeLogged;
        private bool _futureFrameLogged;

        protected IWorldResolver Services { get; private set; }

        public void OnInit(IWorldResolver services)
        {
            Services = services;
            services?.TryResolve(out _frameTime);
            services?.TryResolve(out _diagnostics);
            OnServicesReady(services);
        }

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            TrySubmit(frame, inputs);
        }

        public LogicWorldInputSubmitResult TrySubmit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0)
            {
                const string message = "input command batch is null or empty";
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.empty", message, GetType().Name);
                return LogicWorldInputSubmitResult.Rejected(message);
            }

            if (!CanSubmit(frame, inputs))
            {
                var message = $"input batch rejected by frame validation: targetFrame={frame.Value}, count={inputs.Count}";
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.frameRejected", message, GetType().Name);
                return LogicWorldInputSubmitResult.Rejected(message);
            }

            TContext context = CreateContext(frame, inputs);
            if (context == null)
            {
                const string message = "input context creation returned null";
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.contextMissing", message, GetType().Name);
                return LogicWorldInputSubmitResult.Rejected(message);
            }

            var handledCount = 0;
            var dispatchTrace = string.Empty;
            for (int i = 0; i < inputs.Count; i++)
            {
                PlayerInputCommand command = inputs[i];
                bool handledBeforeDispatch = TryHandleBeforeDispatch(context, frame, command);
                bool handledByDispatch = false;
                if (handledBeforeDispatch)
                {
                    handledCount++;
                }
                else
                {
                    handledByDispatch = Dispatch(context, frame, command, out var commandResult);
                    if (handledByDispatch)
                    {
                        handledCount++;
                    }
                    else
                    {
                        MobaInputDiagnostics.RecordCommandRejected(_diagnostics, frame, in commandResult, GetType().Name);
                    }

                    if (i > 0) dispatchTrace += ";";
                    dispatchTrace += $"#{i}:Before={handledBeforeDispatch},Dispatch={handledByDispatch}";
                    if (!string.IsNullOrEmpty(commandResult.Message))
                    {
                        dispatchTrace += $",Result={commandResult}";
                    }
                    continue;
                }

                if (i > 0) dispatchTrace += ";";
                dispatchTrace += $"#{i}:Before={handledBeforeDispatch},Dispatch={handledByDispatch}";
            }

            if (handledCount < inputs.Count)
            {
                string message = $"Coordinator={GetType().Name}, Frame={frame.Value}, Count={inputs.Count}, Handled={handledCount}, Commands={FormatCommands(inputs)}, DispatchTrace={dispatchTrace}";
                if (handledCount == 0)
                {
                    MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.noCommandHandled", $"Input batch accepted but no command was handled. {message}", GetType().Name);
                }
                else
                {
                    MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.partialCommandHandled", $"Input batch partially handled. {message}", GetType().Name);
                }

                return LogicWorldInputSubmitResult.Accepted(inputs.Count, handledCount, message);
            }

            string successMessage = $"Coordinator={GetType().Name}, Frame={frame.Value}, Count={inputs.Count}, Handled={handledCount}, Commands={FormatCommands(inputs)}, DispatchTrace={dispatchTrace}";
            MobaInputDiagnostics.RecordBatchAccepted(_diagnostics, inputs.Count, handledCount);
            return LogicWorldInputSubmitResult.Accepted(inputs.Count, handledCount, successMessage);
        }

        protected virtual bool CanSubmit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (frame.Value < 0)
            {
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.negativeFrame", $"Input batch rejected: targetFrame={frame.Value} is negative, count={inputs.Count}.", GetType().Name);
                return false;
            }

            if (_frameTime == null)
            {
                if (!_missingFrameTimeLogged)
                {
                    _missingFrameTimeLogged = true;
                    MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.frameValidationMissing", "Input batch rejected: IFrameTime is required for deterministic frame validation.", GetType().Name);
                }

                return false;
            }

            int currentFrame = _frameTime.Frame.Value;
            if (frame.Value < currentFrame)
            {
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.pastFrame", $"Input batch rejected: targetFrame={frame.Value}, currentFrame={currentFrame}, count={inputs.Count}.", GetType().Name);
                return false;
            }

            if (frame.Value > currentFrame + 1 && !_futureFrameLogged)
            {
                _futureFrameLogged = true;
                MobaInputDiagnostics.RecordBatchWarning(_diagnostics, "input.batch.futureFrame", $"Input batch accepted with future target frame: targetFrame={frame.Value}, currentFrame={currentFrame}, count={inputs.Count}.", GetType().Name);
            }

            return true;
        }

        protected virtual void OnServicesReady(IWorldResolver services)
        {
        }

        protected abstract TContext CreateContext(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);

        protected virtual bool TryHandleBeforeDispatch(TContext context, FrameIndex frame, PlayerInputCommand command)
        {
            return false;
        }

        protected abstract bool Dispatch(TContext context, FrameIndex frame, PlayerInputCommand command, out MobaInputCommandResult result);

        private static string FormatCommands(IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0) return "empty";

            var text = string.Empty;
            for (int i = 0; i < inputs.Count; i++)
            {
                PlayerInputCommand command = inputs[i];
                if (i > 0) text += ";";
                text += $"#{i}:Player={command.Player.Value},Op={command.OpCode},Payload={command.Payload?.Length ?? 0}";
            }

            return text;
        }

        public virtual void Dispose()
        {
        }
    }
}
