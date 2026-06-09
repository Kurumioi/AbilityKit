using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaInputCommandFailureCode
    {
        None = 0,
        ContextMissing = 1,
        NotInGame = 2,
        ActorMapMissing = 3,
        ActorEntityMissing = 4,
        TransformMissing = 5,
        PayloadMissing = 6,
        SkillExecutorMissing = 7,
        SkillRejected = 8,
        MissingHandler = 9,
        HandlerRejected = 10,
        PayloadInvalid = 11,
    }

    public readonly struct MobaInputCommandResult
    {
        public readonly bool Succeeded;
        public readonly MobaInputCommandFailureCode FailureCode;
        public readonly string Message;
        public readonly string PlayerId;
        public readonly int ActorId;
        public readonly int OpCode;

        public MobaInputCommandResult(
            bool succeeded,
            MobaInputCommandFailureCode failureCode,
            string message,
            string playerId,
            int actorId,
            int opCode)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message;
            PlayerId = playerId;
            ActorId = actorId;
            OpCode = opCode;
        }

        public static MobaInputCommandResult Accepted(PlayerInputCommand command, string message, int actorId = 0)
        {
            return new MobaInputCommandResult(true, MobaInputCommandFailureCode.None, message, command.Player.Value, actorId, command.OpCode);
        }

        public static MobaInputCommandResult Rejected(
            PlayerInputCommand command,
            MobaInputCommandFailureCode failureCode,
            string message,
            int actorId = 0)
        {
            return new MobaInputCommandResult(false, failureCode, message, command.Player.Value, actorId, command.OpCode);
        }

        public override string ToString()
        {
            return Succeeded
                ? $"Accepted(Player={PlayerId},Actor={ActorId},Op={OpCode},Message={Message})"
                : $"Rejected(Code={FailureCode},Player={PlayerId},Actor={ActorId},Op={OpCode},Message={Message})";
        }
    }

    public static class MobaInputDiagnostics
    {
        public static void RecordCommandRejected(
            IMobaBattleDiagnosticsService diagnostics,
            FrameIndex frame,
            in MobaInputCommandResult result,
            string owner)
        {
            if (result.Succeeded) return;

            var key = "input.command.rejected." + result.FailureCode;
            var message = $"Input command rejected. frame={frame.Value} code={result.FailureCode} player={result.PlayerId} actor={result.ActorId} op={result.OpCode} reason={result.Message}";
            if (diagnostics != null)
            {
                diagnostics.Warning(key, message);
                diagnostics.Counter("moba.input.command.rejected");
                diagnostics.Counter(key);
                return;
            }

            MobaRuntimeLog.Warning(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Rejection, owner, message);
        }

        public static void RecordBatchWarning(
            IMobaBattleDiagnosticsService diagnostics,
            string key,
            string message,
            string owner)
        {
            if (diagnostics != null)
            {
                diagnostics.Warning(key, message);
                diagnostics.Counter(key);
                return;
            }

            MobaRuntimeLog.Warning(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Rejection, owner, message);
        }

        public static void RecordBatchAccepted(IMobaBattleDiagnosticsService diagnostics, int acceptedCount, int handledCount)
        {
            diagnostics?.Counter("moba.input.batch.accepted");
            diagnostics?.Sample("moba.input.batch.accepted.count", acceptedCount);
            diagnostics?.Sample("moba.input.batch.handled.count", handledCount);
        }
    }
}
