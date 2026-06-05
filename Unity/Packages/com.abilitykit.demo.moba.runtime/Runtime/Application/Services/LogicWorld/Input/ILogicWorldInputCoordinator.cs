using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services.LogicWorld
{
    public readonly struct LogicWorldInputSubmitResult
    {
        public readonly bool Succeeded;
        public readonly int AcceptedCount;
        public readonly int HandledCount;
        public readonly string Message;

        public LogicWorldInputSubmitResult(bool succeeded, int acceptedCount, int handledCount, string message)
        {
            Succeeded = succeeded;
            AcceptedCount = acceptedCount;
            HandledCount = handledCount;
            Message = message;
        }

        public static LogicWorldInputSubmitResult Accepted(int acceptedCount, int handledCount)
        {
            return Accepted(acceptedCount, handledCount, null);
        }

        public static LogicWorldInputSubmitResult Accepted(int acceptedCount, int handledCount, string message)
        {
            return new LogicWorldInputSubmitResult(true, acceptedCount, handledCount, message);
        }

        public static LogicWorldInputSubmitResult Rejected(string message)
        {
            return new LogicWorldInputSubmitResult(false, 0, 0, message);
        }

        public override string ToString()
        {
            return Succeeded
                ? $"Success: Accepted={AcceptedCount}, Handled={HandledCount}, Message={Message}"
                : $"Rejected: {Message}";
        }
    }

    /// <summary>
    /// 逻辑世界输入协调器统一接口，承接外部输入批次并交由具体逻辑层处理。
    /// </summary>
    public interface ILogicWorldInputCoordinator
    {
        void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);

        LogicWorldInputSubmitResult TrySubmit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
