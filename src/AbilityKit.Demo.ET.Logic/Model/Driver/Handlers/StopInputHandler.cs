using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// 停止输入处理器
    /// </summary>
    [InputHandler(3002)] // Stop = 3002
    public sealed class StopInputHandler : IStopInputHandler
    {
        public const int StopOpCode = 3002;

        public int OpCode => StopOpCode;

        public bool CanHandle(int opCode) => opCode == StopOpCode;

        public void Handle(ETMobaBattleDriver driver, int frame, PlayerInputCommand input)
        {
            int actorId = PlayerIdUtils.ToActorId(input.Player);
            Log.Debug($"[StopInputHandler] Handle: ActorId={actorId}");
            Submit(driver, actorId);
        }

        public void Submit(ETMobaBattleDriver driver, int actorId)
        {
            var inputPort = driver.InputPort;
            if (inputPort == null)
            {
                Log.Error($"[StopInputHandler] InputPort is null!");
                return;
            }

            var playerId = PlayerIdUtils.ToPlayerId(actorId);
            var frameIndex = new FrameIndex(driver.CurrentFrame);
            var command = new PlayerInputCommand(frameIndex, playerId, StopOpCode, null);

            inputPort.Submit(frameIndex, new List<PlayerInputCommand> { command });
            Log.Debug($"[StopInputHandler] Submit: ActorId={actorId}");
        }
    }
}
