using System;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaGamePhase
    {
        Created = 0,
        Initializing = 1,
        Ready = 2,
        InGame = 3,
        Paused = 4,
        Settling = 5,
        Ended = 6,
        Faulted = 7,
    }

    public enum MobaGamePhaseTransitionFailureCode
    {
        None = 0,
        InvalidTarget = 1,
        IllegalTransition = 2,
    }

    public readonly struct MobaGamePhaseTransitionResult
    {
        public static readonly MobaGamePhaseTransitionResult Success = new MobaGamePhaseTransitionResult(true, MobaGamePhaseTransitionFailureCode.None, null);

        public readonly bool Succeeded;
        public readonly MobaGamePhaseTransitionFailureCode FailureCode;
        public readonly string Message;

        public MobaGamePhaseTransitionResult(bool succeeded, MobaGamePhaseTransitionFailureCode failureCode, string message)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message;
        }

        public static MobaGamePhaseTransitionResult Fail(MobaGamePhaseTransitionFailureCode failureCode, string message)
        {
            return new MobaGamePhaseTransitionResult(false, failureCode, message);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : $"{FailureCode}: {Message}";
        }
    }

    [WorldService(typeof(MobaGamePhaseService))]
    public sealed class MobaGamePhaseService : IService
    {
        public MobaGamePhase Phase { get; private set; } = MobaGamePhase.Created;
        public string LastTransitionReason { get; private set; }
        public long TransitionCount { get; private set; }
        public bool InGame => Phase == MobaGamePhase.InGame;
        public bool CanStartGame => Phase == MobaGamePhase.Created;
        public bool CanDriveBattleLoop => Phase == MobaGamePhase.InGame;
        public bool IsTerminal => Phase == MobaGamePhase.Ended || Phase == MobaGamePhase.Faulted;

        public void SetInGame(string reason = null)
        {
            RequireTransition(MobaGamePhase.InGame, reason ?? "battle entered game loop");
        }

        public void SetInitializing(string reason = null)
        {
            RequireTransition(MobaGamePhase.Initializing, reason ?? "battle initialization started");
        }

        public void SetReady(string reason = null)
        {
            RequireTransition(MobaGamePhase.Ready, reason ?? "battle initialization completed");
        }

        public void SetPaused(string reason = null)
        {
            RequireTransition(MobaGamePhase.Paused, reason ?? "battle paused");
        }

        public void SetSettling(string reason = null)
        {
            RequireTransition(MobaGamePhase.Settling, reason ?? "battle settling started");
        }

        public void SetEnded(string reason = null)
        {
            RequireTransition(MobaGamePhase.Ended, reason ?? "battle ended");
        }

        public void SetFaulted(string reason)
        {
            RequireTransition(MobaGamePhase.Faulted, string.IsNullOrWhiteSpace(reason) ? "battle faulted" : reason);
        }

        public MobaGamePhaseTransitionResult TryTransition(MobaGamePhase target, string reason = null)
        {
            if (!Enum.IsDefined(typeof(MobaGamePhase), target))
            {
                return MobaGamePhaseTransitionResult.Fail(MobaGamePhaseTransitionFailureCode.InvalidTarget, $"target phase is invalid. target={target}");
            }

            if (target == Phase) return MobaGamePhaseTransitionResult.Success;

            if (!CanTransition(Phase, target))
            {
                return MobaGamePhaseTransitionResult.Fail(MobaGamePhaseTransitionFailureCode.IllegalTransition, $"illegal game phase transition. current={Phase}, target={target}, reason={reason}");
            }

            Phase = target;
            LastTransitionReason = reason;
            TransitionCount++;
            MobaRuntimeLog.Info(MobaRuntimeLogModule.Session, MobaRuntimeLogPurpose.Lifecycle, nameof(MobaGamePhaseService), $"Game phase changed. phase={Phase}, reason={reason}, transitions={TransitionCount}");
            return MobaGamePhaseTransitionResult.Success;
        }

        public void Reset()
        {
            Phase = MobaGamePhase.Created;
            LastTransitionReason = "reset";
            TransitionCount = 0L;
        }

        public override string ToString()
        {
            return $"phase={Phase}, transitions={TransitionCount}, reason={LastTransitionReason}";
        }

        public void Dispose()
        {
            Reset();
        }

        private void RequireTransition(MobaGamePhase target, string reason)
        {
            var result = TryTransition(target, reason);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.ToString());
            }
        }

        private static bool CanTransition(MobaGamePhase current, MobaGamePhase target)
        {
            if (target == MobaGamePhase.Faulted) return true;

            switch (current)
            {
                case MobaGamePhase.Created:
                    return target == MobaGamePhase.Initializing || target == MobaGamePhase.Ready || target == MobaGamePhase.InGame;
                case MobaGamePhase.Initializing:
                    return target == MobaGamePhase.Ready || target == MobaGamePhase.InGame;
                case MobaGamePhase.Ready:
                    return target == MobaGamePhase.InGame || target == MobaGamePhase.Faulted;
                case MobaGamePhase.InGame:
                    return target == MobaGamePhase.Paused || target == MobaGamePhase.Settling || target == MobaGamePhase.Ended;
                case MobaGamePhase.Paused:
                    return target == MobaGamePhase.InGame || target == MobaGamePhase.Settling || target == MobaGamePhase.Ended;
                case MobaGamePhase.Settling:
                    return target == MobaGamePhase.Ended;
                default:
                    return false;
            }
        }
    }
}
