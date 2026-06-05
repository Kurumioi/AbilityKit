using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaInputSubmitFailureCode
    {
        None = 0,
        NotRunning = 1,
        MissingInputPort = 2,
        MissingInputCoordinator = 3,
        NullOrEmptyCommands = 4,
        InvalidFrame = 5,
        RejectedByInputCoordinator = 6,
        NoCommandHandled = 7,
        PartialCommandHandled = 8,
    }

    public readonly struct MobaInputSubmitResult
    {
        public static readonly MobaInputSubmitResult Success = new MobaInputSubmitResult(true, MobaInputSubmitFailureCode.None, null, 0);

        public readonly bool Succeeded;
        public readonly MobaInputSubmitFailureCode FailureCode;
        public readonly string Message;
        public readonly int CommandCount;

        public MobaInputSubmitResult(bool succeeded, MobaInputSubmitFailureCode failureCode, string message, int commandCount)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Message = message;
            CommandCount = commandCount;
        }

        public static MobaInputSubmitResult Accepted(int commandCount)
        {
            return Accepted(commandCount, null);
        }

        public static MobaInputSubmitResult Accepted(int commandCount, string message)
        {
            return new MobaInputSubmitResult(true, MobaInputSubmitFailureCode.None, message, commandCount);
        }

        public static MobaInputSubmitResult Fail(MobaInputSubmitFailureCode failureCode, string message)
        {
            return new MobaInputSubmitResult(false, failureCode, message, 0);
        }

        public override string ToString()
        {
            return Succeeded
                ? $"Success: Commands={CommandCount}, Message={Message}"
                : $"{FailureCode}: {Message}";
        }
    }

    public interface IMobaBattleInputPort
    {
        MobaInputSubmitResult Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
