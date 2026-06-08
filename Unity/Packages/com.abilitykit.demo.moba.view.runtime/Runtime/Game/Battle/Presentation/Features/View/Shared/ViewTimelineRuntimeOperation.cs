using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    internal static class ViewTimelineRuntimeOperation
    {
        public static void SeekAllToCurrentFrame(IViewFeatureRuntime runtime)
        {
            if (runtime?.Timeline == null) return;

            var battleCtx = runtime.Context;
            if (battleCtx == null) return;

            var frame = battleCtx.LastFrame;
            if (frame == runtime.LastAlignedFrame) return;

            var tickRate = battleCtx.Plan.TickRate;
            var secondsPerFrame = tickRate > 0 ? 1f / tickRate : 0f;
            runtime.Timeline.SeekAll(frame, secondsPerFrame);

            runtime.LastAlignedFrame = frame;

            var worldId = battleCtx.RuntimeWorldId;
            battleCtx.Hooks?.ViewFrameAligned.Invoke(new ViewFrameAlignedEvent(isConfirmed: runtime.IsConfirmed, worldId: worldId, frame: frame));
        }
    }
}
