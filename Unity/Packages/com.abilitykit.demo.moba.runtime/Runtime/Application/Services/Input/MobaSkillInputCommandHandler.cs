using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// ���� MOBA �����������
    /// </summary>
    [MobaInputCommandHandler(AbilityKit.Protocol.Moba.MobaOpCodes.Input.SkillInput)]
    public sealed class MobaSkillInputCommandHandler : IMobaInputCommandHandler
    {
        public bool Handle(MobaInputCommandContext context, FrameIndex frame, PlayerInputCommand command, out string failureReason)
        {
            failureReason = null;
            if (context == null)
            {
                failureReason = $"ContextMissing(Frame={frame.Value},Player={command.Player.Value})";
                Log.Warning($"[MobaSkillInputCommandHandler] Context missing. Frame={frame.Value}, PlayerId={command.Player}");
                return false;
            }

            if (context.Phase == null || !context.Phase.InGame)
            {
                failureReason = $"NotInGame(Frame={frame.Value},Player={command.Player.Value},HasPhase={context.Phase != null})";
                Log.Warning($"[MobaSkillInputCommandHandler] Not in game. Frame={frame.Value}, PlayerId={command.Player}, HasPhase={context.Phase != null}");
                return false;
            }

            if (context.PlayerActorMap == null || !context.PlayerActorMap.TryGetActorId(command.Player, out int actorId))
            {
                failureReason = $"ActorMapMissing(Player={command.Player.Value},HasMap={context.PlayerActorMap != null})";
                Log.Warning($"[MobaSkillInputCommandHandler] PlayerId={command.Player} not found in actor map. Frame={frame.Value}, HasMap={context.PlayerActorMap != null}");
                return false;
            }

            if (!context.TryGetEntity(actorId, out ActorEntity entity) || entity == null)
            {
                failureReason = $"ActorEntityMissing(Actor={actorId})";
                Log.Warning($"[MobaSkillInputCommandHandler] Entity for ActorId={actorId} not found");
                return false;
            }

            if (!entity.hasTransform)
            {
                failureReason = $"TransformMissing(Actor={actorId})";
                Log.Warning($"[MobaSkillInputCommandHandler] Entity ActorId={actorId} has no Transform");
                return false;
            }

            if (command.Payload == null || command.Payload.Length == 0)
            {
                failureReason = $"PayloadMissing(Player={command.Player.Value},Actor={actorId})";
                Log.Warning($"[MobaSkillInputCommandHandler] Empty payload. PlayerId={command.Player}, ActorId={actorId}");
                return false;
            }

            SkillInputEvent evt = SkillInputCodec.Deserialize(command.Payload);
            if (context.Skills == null)
            {
                failureReason = $"SkillExecutorMissing(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot})";
                Log.Warning($"[MobaSkillInputCommandHandler] SkillExecutor missing. PlayerId={command.Player}, ActorId={actorId}, Slot={evt.Slot}");
                return false;
            }

            var handled = context.Skills.TryHandleInput(actorId, in evt, out var failReason);
            if (!handled)
            {
                var reason = failReason ?? "unknown";
                failureReason = $"SkillRejected(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot},Target={evt.TargetActorId},Reason={reason})";
                Log.Warning($"[MobaSkillInputCommandHandler] Skill input rejected. PlayerId={command.Player}, ActorId={actorId}, Slot={evt.Slot}, Phase={evt.Phase}, TargetActorId={evt.TargetActorId}, Reason={reason}");
                return false;
            }

            var result = string.IsNullOrEmpty(failReason) ? "Success" : failReason;
            var running = context.Skills.TryGetRunningBySlot(actorId, evt.Slot, out var snapshot)
                ? $"Running=True,Skill={snapshot.SkillId},Stage={snapshot.Stage},ElapsedMs={snapshot.ElapsedMs},NextEvent={snapshot.NextEventIndex},Runtime={snapshot.InstanceId}"
                : "Running=False";
            failureReason = $"SkillAccepted(Player={command.Player.Value},Actor={actorId},Slot={evt.Slot},Phase={evt.Phase},Target={evt.TargetActorId},Result={result},{running})";
            return true;
        }
    }
}

