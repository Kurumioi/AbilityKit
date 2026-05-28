using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Protocol.Moba.StateSync;

/// <summary>
/// 文件名称: MobaMoveInputCommandHandler.cs
/// 
/// 功能描述: 处理 MOBA 移动输入命令。
/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 移动输入处理器。
    /// </summary>
    [MobaInputCommandHandler((int)MobaOpCode.Move)]
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