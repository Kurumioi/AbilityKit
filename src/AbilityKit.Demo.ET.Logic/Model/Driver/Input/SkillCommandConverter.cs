using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace ET.Logic
{
    [ETInputCommandConverter(typeof(SkillCommand))]
    public sealed class SkillCommandConverter : IETInputCommandConverter
    {
        public Type CommandType => typeof(SkillCommand);

        public bool TryConvert(object command, FrameIndex frameIndex, out PlayerInputCommand playerCommand)
        {
            if (!(command is SkillCommand skill))
            {
                playerCommand = default;
                return false;
            }

            var skillEvent = new SkillInputEvent(
                slot: skill.SkillSlot,
                phase: SkillInputPhase.Press,
                targetActorId: skill.TargetActorId,
                aimPos: new Vec3(skill.TargetX, 0, skill.TargetY));
            var payload = SkillInputCodec.Serialize(in skillEvent);
            playerCommand = new PlayerInputCommand(
                frameIndex,
                new PlayerId(skill.PlayerId),
                MobaOpCodes.Input.SkillInput,
                payload);
            Log.Debug($"[SkillCommandConverter] PlayerId={skill.PlayerId}, Slot={skill.SkillSlot}, TargetActorId={skill.TargetActorId}");
            return true;
        }
    }
}
