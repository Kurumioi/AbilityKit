using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 处理 MOBA 移动输入命令。
    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.Move)]
    public sealed class MobaMoveInputCommandHandler : IMobaInputCommandHandler
    {
        public void Handle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command)
        {
            if (!context.Phase.InGame) return;
            if (!context.PlayerActorMap.TryGetActorId(command.Player, out int actorId))
            {
                Log.Info($"[MobaMoveInputCommandHandler] PlayerId={command.Player} not found in actor map");
                return;
            }
            if (!context.TryGetEntity(actorId, out ActorEntity entity) || entity == null)
            {
                Log.Info($"[MobaMoveInputCommandHandler] Entity for ActorId={actorId} not found");
                return;
            }
            if (!entity.hasTransform)
            {
                Log.Info($"[MobaMoveInputCommandHandler] Entity ActorId={actorId} has no Transform");
                return;
            }

            MobaMoveCodec.Deserialize(command.Payload, out float dx, out float dz);
            if (!entity.hasMoveInput) entity.AddMoveInput(dx, dz);
            else entity.ReplaceMoveInput(dx, dz);

            Log.Info($"[MobaMoveInputCommandHandler] ActorId={actorId}, MoveInput=({dx:F2}, {dz:F2})");
        }
    }
}