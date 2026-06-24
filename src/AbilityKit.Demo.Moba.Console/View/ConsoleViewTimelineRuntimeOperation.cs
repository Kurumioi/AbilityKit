using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Session;

namespace AbilityKit.Demo.Moba.Console.View
{
    public readonly struct ConsoleViewTimelineAlignmentDecision
    {
        public ConsoleViewTimelineAlignmentDecision(bool shouldSeek, int frame, float secondsPerFrame)
        {
            ShouldSeek = shouldSeek;
            Frame = frame;
            SecondsPerFrame = secondsPerFrame;
        }

        public bool ShouldSeek { get; }

        public int Frame { get; }

        public float SecondsPerFrame { get; }
    }

    public sealed class ConsoleViewTimelineRuntimeOperation
    {
        private int _lastAlignedFrame;

        public int LastAlignedFrame => _lastAlignedFrame;

        public static ConsoleViewTimelineAlignmentDecision ResolveAlignment(int currentFrame, int lastAlignedFrame, int tickRate)
        {
            if (currentFrame == lastAlignedFrame)
            {
                return new ConsoleViewTimelineAlignmentDecision(false, currentFrame, 0f);
            }

            var secondsPerFrame = tickRate > 0 ? 1f / tickRate : 0f;
            return new ConsoleViewTimelineAlignmentDecision(true, currentFrame, secondsPerFrame);
        }

        public ConsoleViewTimelineAlignmentDecision SeekAllToCurrentFrame(
            ConsoleBattleContext context,
            ConsoleViewTimeline timeline)
        {
            if (context == null || timeline == null)
            {
                return new ConsoleViewTimelineAlignmentDecision(false, context?.LastFrame ?? 0, 0f);
            }

            var decision = ResolveAlignment(context.LastFrame, _lastAlignedFrame, context.Plan.TickRate);
            if (!decision.ShouldSeek)
            {
                return decision;
            }

            timeline.SeekAll(decision.Frame, decision.SecondsPerFrame);
            _lastAlignedFrame = decision.Frame;
            context.Hooks?.InvokeViewFrameAligned(new ViewFrameAlignedEvent(decision.Frame, context.LogicTimeSeconds));
            return decision;
        }
    }
}
