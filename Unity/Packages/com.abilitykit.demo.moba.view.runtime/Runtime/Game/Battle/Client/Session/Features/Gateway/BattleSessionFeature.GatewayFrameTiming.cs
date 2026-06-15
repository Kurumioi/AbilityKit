using System;
using System.Diagnostics;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Agent;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private bool TryGetWorldStartAnchor(WorldId worldId, out GatewayWorldStartAnchor anchor)
        {
            anchor = default;
            if (string.IsNullOrEmpty(worldId.Value)) return false;
            return _gatewayWorldStartAnchors.TryGetValue(worldId, out anchor) && anchor.ServerTickFrequency != 0;
        }

        private int ResolveIdealFrameRaw(WorldId worldId)
        {
            if (!TryGetWorldStartAnchor(worldId, out var anchor)) return 0;
            if (!_state.GatewayRoomTimeSync.HasClockSync) return 0;

            var localNowSeconds = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

            var startServerSeconds = anchor.StartServerTicks / (double)anchor.ServerTickFrequency;
            var localStartSeconds = startServerSeconds + _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma;

            var elapsed = localNowSeconds - localStartSeconds;
            if (elapsed < 0) elapsed = 0;

            var dt = anchor.FixedDeltaSeconds;
            if (dt <= 0) return 0;

            var frames = (int)Math.Floor(elapsed / dt);
            return anchor.StartFrame + frames;
        }

        private int ResolveIdealFrameSafetyMarginFrames(WorldId worldId)
        {
            if (!TryGetWorldStartAnchor(worldId, out var anchor)) return 0;
            if (!_state.GatewayRoomTimeSync.HasClockSync) return 0;

            var dt = anchor.FixedDeltaSeconds;
            if (dt <= 0) return 0;

            var timeSync = _plan.TimeSync;
            var constMargin = timeSync.IdealFrameSafetyConstMarginFrames;
            if (constMargin < 0) constMargin = 0;

            var rttFactor = timeSync.IdealFrameSafetyRttFactor;
            if (rttFactor < 0) rttFactor = 0;

            var rttFrames = (int)Math.Ceiling((_state.GatewayRoomTimeSync.RttSecondsEwma / dt) * rttFactor);
            if (rttFrames < 0) rttFrames = 0;

            var margin = constMargin;
            if (rttFrames > margin) margin = rttFrames;

            var minMargin = timeSync.IdealFrameSafetyMinMarginFrames;
            var maxMargin = timeSync.IdealFrameSafetyMaxMarginFrames;
            if (minMargin < 0) minMargin = 0;
            if (maxMargin < minMargin) maxMargin = minMargin;

            if (margin < minMargin) margin = minMargin;
            if (margin > maxMargin) margin = maxMargin;

            return margin;
        }

        private int ResolveIdealFrameLimit(WorldId worldId)
        {
            if (!TryGetWorldStartAnchor(worldId, out var anchor)) return 0;
            if (!_state.GatewayRoomTimeSync.HasClockSync) return 0;

            var localNowSeconds = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

            var startServerSeconds = anchor.StartServerTicks / (double)anchor.ServerTickFrequency;
            var localStartSeconds = startServerSeconds + _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma;

            var elapsed = localNowSeconds - localStartSeconds;
            if (elapsed < 0) elapsed = 0;

            var dt = anchor.FixedDeltaSeconds;
            if (dt <= 0) return 0;

            var frames = (int)Math.Floor(elapsed / dt);
            var idealRaw = anchor.StartFrame + frames;

            var margin = ResolveIdealFrameSafetyMarginFrames(worldId);

            var limit = idealRaw - margin;
            if (limit < anchor.StartFrame) limit = anchor.StartFrame;
            return limit;
        }
    }
}
