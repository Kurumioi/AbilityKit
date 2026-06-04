using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 澶勭悊 MOBA 鎶€鑳借緭鍏ュ懡浠ゃ€?    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.SkillInput)]
    public sealed class MobaSkillInputCommandHandler : IMobaInputCommandHandler
    {
        public void Handle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command)
        {
            if (!context.Phase.InGame) return;
            if (!context.PlayerActorMap.TryGetActorId(command.Player, out int actorId)) return;
            if (!context.TryGetEntity(actorId, out ActorEntity entity) || entity == null) return;
            if (!entity.hasTransform) return;
            if (command.Payload == null || command.Payload.Length == 0) return;

            SkillInputEvent evt = SkillInputCodec.Deserialize(command.Payload);
            context.Skills?.HandleInput(actorId, in evt);
        }
    }
}

