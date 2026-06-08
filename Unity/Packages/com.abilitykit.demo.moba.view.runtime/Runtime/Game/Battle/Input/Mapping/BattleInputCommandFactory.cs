using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    internal static class BattleInputCommandFactory
    {
        public static PlayerInputCommand CreateMove(int frame, PlayerId playerId, float dx, float dz)
        {
            return Create(frame, playerId, MobaOpCodes.Input.Move, MobaMoveCodec.Serialize(dx, dz));
        }

        public static PlayerInputCommand CreateSkillSlot(int frame, PlayerId playerId, int slot)
        {
            return Create(frame, playerId, SkillSlotToOpCode(slot), Array.Empty<byte>());
        }

        public static PlayerInputCommand CreateSkillAimRelease(int frame, PlayerId playerId, int slot, float dx, float dz)
        {
            var aimPos = new Vec3(dx, 0f, dz);
            var aimDir = new Vec3(dx, 0f, dz);
            var evt = new SkillInputEvent(slot: slot, phase: SkillInputPhase.Release, aimPos: in aimPos, aimDir: in aimDir);
            return Create(frame, playerId, MobaOpCodes.Input.SkillInput, SkillInputCodec.Serialize(in evt));
        }

        private static PlayerInputCommand Create(int frame, PlayerId playerId, int opCode, byte[] payload)
        {
            return new PlayerInputCommand(new FrameIndex(frame), playerId, opCode, payload);
        }

        private static int SkillSlotToOpCode(int slot)
        {
            return slot == 1 ? MobaOpCodes.Input.Skill1 : slot == 2 ? MobaOpCodes.Input.Skill2 : MobaOpCodes.Input.Skill3;
        }
    }
}
