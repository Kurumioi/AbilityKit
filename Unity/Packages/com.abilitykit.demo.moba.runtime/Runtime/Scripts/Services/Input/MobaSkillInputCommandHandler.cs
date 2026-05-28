using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;

/// <summary>
/// 文件名称: MobaSkillInputCommandHandler.cs
/// 
/// 功能描述: 处理 MOBA 技能输入命令。
/// 
/// 创建日期: 2026-05-27
/// 修改日期: 2026-05-27
/// </summary>
namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 技能输入处理器。
    /// </summary>
    [MobaInputCommandHandler((int)MobaOpCode.SkillInput)]
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