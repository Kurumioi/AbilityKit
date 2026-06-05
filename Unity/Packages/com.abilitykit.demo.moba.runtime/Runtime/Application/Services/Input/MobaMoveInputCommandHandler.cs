using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 处理 MOBA 移动输入命令�?    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.Move)]
    public sealed class MobaMoveInputCommandHandler : IMobaInputCommandHandler
    {
        public bool Handle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command, out string failureReason)
        {
            failureReason = null;
            if (context == null)
            {
                failureReason = $"ContextMissing(Frame={frame.Value},Player={command.Player.Value})";
                Log.Warning($"[MobaMoveInputCommandHandler] Context missing. Frame={frame.Value}, PlayerId={command.Player}");
                return false;
            }

            if (context.Phase == null || !context.Phase.InGame)
            {
                failureReason = $"NotInGame(Frame={frame.Value},Player={command.Player.Value},HasPhase={context.Phase != null})";
                Log.Warning($"[MobaMoveInputCommandHandler] Not in game. Frame={frame.Value}, PlayerId={command.Player}, HasPhase={context.Phase != null}");
                return false;
            }

            if (context.PlayerActorMap == null || !context.PlayerActorMap.TryGetActorId(command.Player, out int actorId))
            {
                failureReason = $"ActorMapMissing(Player={command.Player.Value},HasMap={context.PlayerActorMap != null})";
                Log.Warning($"[MobaMoveInputCommandHandler] PlayerId={command.Player} not found in actor map. Frame={frame.Value}, HasMap={context.PlayerActorMap != null}");
                return false;
            }
            if (!context.TryGetEntity(actorId, out ActorEntity entity) || entity == null)
            {
                failureReason = $"ActorEntityMissing(Actor={actorId})";
                Log.Warning($"[MobaMoveInputCommandHandler] Entity for ActorId={actorId} not found");
                return false;
            }
            if (!entity.hasTransform)
            {
                failureReason = $"TransformMissing(Actor={actorId})";
                Log.Warning($"[MobaMoveInputCommandHandler] Entity ActorId={actorId} has no Transform");
                return false;
            }

            MobaMoveCodec.Deserialize(command.Payload, out float dx, out float dz);
            if (!entity.hasMoveInput) entity.AddMoveInput(dx, dz);
            else entity.ReplaceMoveInput(dx, dz);

            failureReason = $"MoveAccepted(Player={command.Player.Value},Actor={actorId},Dx={dx:0.###},Dz={dz:0.###})";
            return true;
        }
    }
}