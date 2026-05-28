using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 兼容旧 IWorldInputSink 接入点的适配器，实际输入协调由 IMobaInputCoordinator 负责。
    /// </summary>
    [WorldService(typeof(IWorldInputSink))]
    [WorldService(typeof(MobaLobbyInputSink))]
    public sealed class MobaLobbyInputSink : IWorldInputSink
    {
        private readonly IMobaInputCoordinator _coordinator;

        public MobaLobbyInputSink(IMobaInputCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        public void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            _coordinator.Submit(frame, inputs);
        }

        public void Dispose()
        {
        }
    }
}

