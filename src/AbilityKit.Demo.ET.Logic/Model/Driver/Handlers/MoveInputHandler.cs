using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// 移动输入处理器
    /// 将移动命令（方向向量）提交到战斗输入端口
    /// </summary>
    [InputHandler(3003)] // Move = 3003
    public sealed class MoveInputHandler : ISubmittableInputHandler
    {
        public const int MoveOpCode = 3003;

        public int OpCode => MoveOpCode;

        public bool CanHandle(int opCode) => opCode == MoveOpCode;

        public void Handle(ETMobaBattleDriver driver, int frame, PlayerInputCommand input)
        {
            // 解析移动方向向量
            MobaMoveCodec.Deserialize(input.Payload, out var dx, out var dz);
            int actorId = PlayerIdUtils.ToActorId(input.Player);

            Log.Debug($"[MoveInputHandler] Handle: ActorId={actorId}, Dir=({dx:F2}, {dz:F2})");

            // 提交移动输入
            Submit(driver, actorId, dx, dz);
        }

        /// <summary>
        /// 提交移动输入到战斗输入端口。
        /// dx/dz 是移动方向向量（-1 到 1）。
        /// </summary>
        public void Submit(ETMobaBattleDriver driver, int actorId, float dx, float dz)
        {
            var inputPort = driver.InputPort;
            if (inputPort == null)
            {
                Log.Error($"[MoveInputHandler] InputPort is null! Driver not started properly.");
                return;
            }

            // 序列化方向向量
            var payload = MobaMoveCodec.Serialize(dx, dz);
            var playerId = PlayerIdUtils.ToPlayerId(actorId);
            var frameIndex = new FrameIndex(driver.CurrentFrame);
            var command = new PlayerInputCommand(frameIndex, playerId, MoveOpCode, payload);

            inputPort.Submit(frameIndex, new List<PlayerInputCommand> { command });
            Log.Debug($"[MoveInputHandler] Submit: ActorId={actorId}, Dir=({dx:F2}, {dz:F2})");
        }
    }
}
