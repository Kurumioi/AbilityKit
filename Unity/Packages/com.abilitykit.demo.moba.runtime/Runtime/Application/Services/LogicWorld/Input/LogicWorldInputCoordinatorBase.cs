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
            if (inputs == null || inputs.Count == 0) return;
            if (!CanSubmit(frame, inputs)) return;

            TContext context = CreateContext(frame, inputs);
            if (context == null) return;

            for (int i = 0; i < inputs.Count; i++)
            {
                PlayerInputCommand command = inputs[i];
                if (TryHandleBeforeDispatch(context, frame, command)) continue;

                Dispatch(context, frame, command);
            }
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

        protected abstract void Dispatch(TContext context, FrameIndex frame, PlayerInputCommand command);

        public virtual void Dispose()
        {
        }
    }
}
