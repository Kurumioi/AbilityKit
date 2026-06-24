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

            var input = new GatewayFrameTimingInput(
                in anchor,
                _state.GatewayRoomTimeSync.HasClockSync,
                _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma,
                _state.GatewayRoomTimeSync.RttSecondsEwma,
                _plan.TimeSync);
            return GatewayFrameTimingHelper.ResolveIdealFrameRaw(in input, Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        }

        private int ResolveIdealFrameSafetyMarginFrames(WorldId worldId)
        {
            if (!TryGetWorldStartAnchor(worldId, out var anchor)) return 0;
            if (!_state.GatewayRoomTimeSync.HasClockSync) return 0;

            var input = new GatewayFrameTimingInput(
                in anchor,
                _state.GatewayRoomTimeSync.HasClockSync,
                _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma,
                _state.GatewayRoomTimeSync.RttSecondsEwma,
                _plan.TimeSync);
            return GatewayFrameTimingHelper.ResolveIdealFrameSafetyMarginFrames(in input);
        }

        private int ResolveIdealFrameLimit(WorldId worldId)
        {
            if (!TryGetWorldStartAnchor(worldId, out var anchor)) return 0;
            if (!_state.GatewayRoomTimeSync.HasClockSync) return 0;

            var input = new GatewayFrameTimingInput(
                in anchor,
                _state.GatewayRoomTimeSync.HasClockSync,
                _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma,
                _state.GatewayRoomTimeSync.RttSecondsEwma,
                _plan.TimeSync);
            return GatewayFrameTimingHelper.ResolveIdealFrameLimit(in input, Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        }
    }
}
