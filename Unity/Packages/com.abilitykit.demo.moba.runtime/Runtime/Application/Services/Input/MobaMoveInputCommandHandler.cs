using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 处理 MOBA 移动输入命令。
    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.Move)]
    public sealed class MobaMoveInputCommandHandler : IMobaInputCommandHandler
    {
        public bool Handle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command, out MobaInputCommandResult result)
        {
            if (context == null)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.ContextMissing,
                    $"ContextMissing(Frame={frame.Value},Player={command.Player.Value})");
                return false;
            }

            if (context.Phase == null || !context.Phase.InGame)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.NotInGame,
                    $"NotInGame(Frame={frame.Value},Player={command.Player.Value},HasPhase={context.Phase != null})");
                return false;
            }

            if (context.PlayerActorMap == null || !context.PlayerActorMap.TryGetActorId(command.Player, out int actorId))
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.ActorMapMissing,
                    $"ActorMapMissing(Player={command.Player.Value},HasMap={context.PlayerActorMap != null})");
                return false;
            }

            if (!context.TryGetEntity(actorId, out ActorEntity entity) || entity == null)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.ActorEntityMissing,
                    $"ActorEntityMissing(Actor={actorId})",
                    actorId);
                return false;
            }

            if (!entity.hasTransform)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.TransformMissing,
                    $"TransformMissing(Actor={actorId})",
                    actorId);
                return false;
            }

            if (!MobaMoveCodec.TryDeserialize(command.Payload, out float dx, out float dz, out var payloadError))
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.PayloadInvalid,
                    $"PayloadInvalid(Player={command.Player.Value},Actor={actorId},Error={payloadError})",
                    actorId);
                return false;
            }

            if (!entity.hasMoveInput) entity.AddMoveInput(dx, dz);
            else entity.ReplaceMoveInput(dx, dz);

            result = MobaInputCommandResult.Accepted(
                command,
                $"MoveAccepted(Player={command.Player.Value},Actor={actorId},Dx={dx:0.###},Dz={dz:0.###})",
                actorId);
            return true;
        }
    }
}
