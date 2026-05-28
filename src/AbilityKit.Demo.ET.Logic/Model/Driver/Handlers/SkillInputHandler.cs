using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// 技能输入处理器
    /// </summary>
    [InputHandler(3020)] // SkillInput = 3020
    public sealed class SkillInputHandler : ISkillInputHandler
    {
        public const int SkillInputOpCode = 3020;

        public int OpCode => SkillInputOpCode;

        public bool CanHandle(int opCode) => opCode == SkillInputOpCode;

        public void Handle(ETMobaBattleDriver driver, int frame, PlayerInputCommand input)
        {
            int skillSlot = 0;
            float targetX = 0f;
            float targetZ = 0f;

            if (input.Payload != null && input.Payload.Length >= 12)
            {
                skillSlot = BitConverter.ToInt32(input.Payload, 0);
                targetX = BitConverter.ToSingle(input.Payload, 4);
                targetZ = BitConverter.ToSingle(input.Payload, 8);
            }

            int actorId = PlayerIdUtils.ToActorId(input.Player);
            Log.Debug($"[SkillInputHandler] Handle: ActorId={actorId}, Slot={skillSlot}");

            Submit(driver, actorId, skillSlot, targetX, targetZ);
        }

        public bool Submit(ETMobaBattleDriver driver, int actorId, int slot, float targetX, float targetZ)
        {
            var inputPort = driver.InputPort;
            if (inputPort == null)
            {
                Log.Error($"[SkillInputHandler] InputPort is null! Driver not started properly.");
                return false;
            }

            var payload = MobaMoveCodec.Serialize(targetX, targetZ);
            var playerId = PlayerIdUtils.ToPlayerId(actorId);
            var frameIndex = new FrameIndex(driver.CurrentFrame);
            var command = new PlayerInputCommand(frameIndex, playerId, SkillInputOpCode, payload);

            inputPort.Submit(frameIndex, new List<PlayerInputCommand> { command });
            Log.Debug($"[SkillInputHandler] Submit: ActorId={actorId}, Slot={slot}");
            return true;
        }
    }
}
