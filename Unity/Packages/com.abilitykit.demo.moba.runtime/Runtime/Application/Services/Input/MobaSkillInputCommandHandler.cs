using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 处理 MOBA 技能输入命令。
    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.SkillInput)]
    public sealed class MobaSkillInputCommandHandler : IMobaInputCommandHandler
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

            if (command.Payload == null || command.Payload.Length == 0)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.PayloadMissing,
                    $"PayloadMissing(Player={command.Player.Value},Actor={actorId})",
                    actorId);
                return false;
            }

            if (!SkillInputCodec.TryDeserialize(command.Payload, out SkillInputEvent evt, out var payloadError))
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.PayloadInvalid,
                    $"PayloadInvalid(Player={command.Player.Value},Actor={actorId},Error={payloadError})",
                    actorId);
                return false;
            }

            if (context.Skills == null)
            {
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.SkillExecutorMissing,
                    $"SkillExecutorMissing(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot})",
                    actorId);
                return false;
            }

            var handled = context.Skills.TryHandleInput(actorId, in evt, out var failReason);
            if (!handled)
            {
                var reason = failReason ?? "unknown";
                result = MobaInputCommandResult.Rejected(
                    command,
                    MobaInputCommandFailureCode.SkillRejected,
                    $"SkillRejected(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot},Target={evt.TargetActorId},Reason={reason})",
                    actorId);
                return false;
            }

            var castResult = string.IsNullOrEmpty(failReason) ? "Success" : failReason;
            var running = context.Skills.TryGetRunningBySlot(actorId, evt.Slot, out var snapshot)
                ? $"Running=True,Skill={snapshot.SkillId},Stage={snapshot.Stage},ElapsedMs={snapshot.ElapsedMs},NextEvent={snapshot.NextEventIndex},Runtime={snapshot.InstanceId}"
                : "Running=False";
            result = MobaInputCommandResult.Accepted(
                command,
                $"SkillAccepted(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot},Phase={evt.Phase},Target={evt.TargetActorId},Result={castResult},{running})",
                actorId);
            return true;
        }
    }
}
