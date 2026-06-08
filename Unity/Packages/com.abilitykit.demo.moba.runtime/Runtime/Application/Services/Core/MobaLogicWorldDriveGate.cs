using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaLogicWorldDriveBlockReason
    {
        None = 0,
        InvalidDeltaTime = 1,
        MissingPhaseService = 2,
        NotInGame = 3,
        MissingRuntimePort = 4,
        RuntimePortNotReady = 5,
        RuntimeValidationBlocked = 6,
    }

    public readonly struct MobaLogicWorldDriveDecision
    {
        public static readonly MobaLogicWorldDriveDecision Allowed = new MobaLogicWorldDriveDecision(true, MobaLogicWorldDriveBlockReason.None, null);

        public readonly bool CanDrive;
        public readonly MobaLogicWorldDriveBlockReason BlockReason;
        public readonly string Message;

        public MobaLogicWorldDriveDecision(bool canDrive, MobaLogicWorldDriveBlockReason blockReason, string message)
        {
            CanDrive = canDrive;
            BlockReason = blockReason;
            Message = message;
        }

        public static MobaLogicWorldDriveDecision Block(MobaLogicWorldDriveBlockReason reason, string message)
        {
            return new MobaLogicWorldDriveDecision(false, reason, message);
        }

        public override string ToString()
        {
            return CanDrive ? "Allowed" : $"Blocked(reason={BlockReason}, message={Message})";
        }
    }

    [WorldService(typeof(ILogicWorldDriveGate), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaLogicWorldDriveGate), WorldLifetime.Scoped)]
    public sealed class MobaLogicWorldDriveGate : ILogicWorldDriveGate
    {
        [WorldInject(required: false)] private MobaGamePhaseService _phase = null;
        [WorldInject(required: false)] private IMobaBattleRuntimePort _runtime = null;
        [WorldInject(required: false)] private IMobaRuntimeValidationHistory _validationHistory = null;

        private MobaLogicWorldDriveBlockReason _lastLoggedReason = MobaLogicWorldDriveBlockReason.None;
        private string _lastLoggedMessage;

        public MobaLogicWorldDriveDecision Evaluate(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.InvalidDeltaTime, $"deltaTime must be finite and non-negative. deltaTime={deltaTime}");
            }

            if (_phase == null)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.MissingPhaseService, "MobaGamePhaseService is required before driving the logic world.");
            }

            if (!_phase.CanDriveBattleLoop)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.NotInGame, "game phase cannot drive battle loop. " + _phase);
            }

            if (_runtime == null)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.MissingRuntimePort, "IMobaBattleRuntimePort is required before driving the logic world.");
            }

            var status = _runtime.Status;
            if (!status.IsReadyForBattleLoop)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.RuntimePortNotReady, "battle runtime port is not ready for the battle loop. " + status);
            }

            if (_validationHistory != null && _validationHistory.TryGetLastReport(out var report) && report != null && report.ShouldBlockStartup)
            {
                return MobaLogicWorldDriveDecision.Block(MobaLogicWorldDriveBlockReason.RuntimeValidationBlocked, "last runtime validation report blocks startup. " + report.FormatSummary());
            }

            return MobaLogicWorldDriveDecision.Allowed;
        }

        public bool CanDriveLogicWorld(float deltaTime)
        {
            var decision = Evaluate(deltaTime);
            if (decision.CanDrive)
            {
                _lastLoggedReason = MobaLogicWorldDriveBlockReason.None;
                _lastLoggedMessage = null;
                return true;
            }

            LogBlockedOnce(in decision);
            return false;
        }

        private void LogBlockedOnce(in MobaLogicWorldDriveDecision decision)
        {
            if (_lastLoggedReason == decision.BlockReason && string.Equals(_lastLoggedMessage, decision.Message, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastLoggedReason = decision.BlockReason;
            _lastLoggedMessage = decision.Message;
            MobaRuntimeLog.Warning(MobaRuntimeLogModule.Session, MobaRuntimeLogPurpose.Validation, nameof(MobaLogicWorldDriveGate), "Logic world drive blocked. " + decision);
        }
    }
}
