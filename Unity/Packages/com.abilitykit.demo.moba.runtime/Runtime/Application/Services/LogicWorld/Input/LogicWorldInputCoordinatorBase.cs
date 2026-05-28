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
        protected IWorldResolver Services { get; private set; }

        public void OnInit(IWorldResolver services)
        {
            Services = services;
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
