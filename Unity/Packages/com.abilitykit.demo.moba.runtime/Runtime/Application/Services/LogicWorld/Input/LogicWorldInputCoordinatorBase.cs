using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Services.LogicWorld
{
    /// <summary>
    /// 逻辑世界输入协调器基类。
    /// 框架负责输入批次遍历、生命周期接入、上下文创建时机和命令路由顺序；具体逻辑层只实现上下文和命令处理。
    /// </summary>
    public abstract class LogicWorldInputCoordinatorBase<TContext> : IService, IWorldInitializable, ILogicWorldInputCoordinator where TContext : class
    {
        private IFrameTime _frameTime;
        private bool _missingFrameTimeLogged;
        private bool _futureFrameLogged;

        protected IWorldResolver Services { get; private set; }

        public void OnInit(IWorldResolver services)
        {
            Services = services;
            services?.TryResolve(out _frameTime);
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
                return LogicWorldInputSubmitResult.Rejected("input command batch is null or empty");
            }

            if (!CanSubmit(frame, inputs))
            {
                return LogicWorldInputSubmitResult.Rejected($"input batch rejected by frame validation: targetFrame={frame.Value}, count={inputs.Count}");
            }

            TContext context = CreateContext(frame, inputs);
            if (context == null)
            {
                return LogicWorldInputSubmitResult.Rejected("input context creation returned null");
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
                    handledByDispatch = Dispatch(context, frame, command, out var failureReason);
                    if (handledByDispatch)
                    {
                        handledCount++;
                    }
                    else if (!string.IsNullOrEmpty(failureReason))
                    {
                        if (i > 0) dispatchTrace += ";";
                        dispatchTrace += $"#{i}:Before={handledBeforeDispatch},Dispatch={handledByDispatch},Reason={failureReason}";
                        continue;
                    }

                    if (i > 0) dispatchTrace += ";";
                    dispatchTrace += $"#{i}:Before={handledBeforeDispatch},Dispatch={handledByDispatch}";
                    if (!string.IsNullOrEmpty(failureReason))
                    {
                        dispatchTrace += $",Reason={failureReason}";
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
                    Log.Warning($"[{GetType().Name}] Input batch accepted but no command was handled. {message}");
                }
                else
                {
                    Log.Warning($"[{GetType().Name}] Input batch partially handled. {message}");
                }

                return LogicWorldInputSubmitResult.Accepted(inputs.Count, handledCount, message);
            }

            string successMessage = $"Coordinator={GetType().Name}, Frame={frame.Value}, Count={inputs.Count}, Handled={handledCount}, Commands={FormatCommands(inputs)}, DispatchTrace={dispatchTrace}";
            return LogicWorldInputSubmitResult.Accepted(inputs.Count, handledCount, successMessage);
        }

        protected virtual bool CanSubmit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (frame.Value < 0)
            {
                Log.Warning($"[{GetType().Name}] Input batch rejected: targetFrame={frame.Value} is negative, count={inputs.Count}.");
                return false;
            }

            if (_frameTime == null)
            {
                if (!_missingFrameTimeLogged)
                {
                    _missingFrameTimeLogged = true;
                    Log.Warning($"[{GetType().Name}] Input frame validation degraded: IFrameTime not resolved.");
                }

                return true;
            }

            int currentFrame = _frameTime.Frame.Value;
            if (frame.Value < currentFrame)
            {
                Log.Warning($"[{GetType().Name}] Input batch rejected: targetFrame={frame.Value}, currentFrame={currentFrame}, count={inputs.Count}.");
                return false;
            }

            if (frame.Value > currentFrame + 1 && !_futureFrameLogged)
            {
                _futureFrameLogged = true;
                Log.Warning($"[{GetType().Name}] Input batch accepted with future target frame: targetFrame={frame.Value}, currentFrame={currentFrame}, count={inputs.Count}.");
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

        protected abstract bool Dispatch(TContext context, FrameIndex frame, PlayerInputCommand command, out string failureReason);

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
