using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private TimeSyncStatsSnapshot BuildCurrentTimeSyncStats(uint opCode, int intervalMs, double alpha, int timeoutMs)
        {
            var worldId = _plan.WorldId != null ? new WorldId(_plan.WorldId) : default;
            return BuildTimeSyncStats(worldId, opCode, intervalMs, alpha, timeoutMs);
        }

        private void UpdateTimeSyncStatsByWorld(uint opCode, int intervalMs, double alpha, int timeoutMs)
        {
            if (BattleFlowDebugProvider.TimeSyncStatsByWorld == null)
            {
                BattleFlowDebugProvider.TimeSyncStatsByWorld = new Dictionary<string, TimeSyncStatsSnapshot>();
            }

            foreach (var kv in _gatewayWorldStartAnchors)
            {
                BattleFlowDebugProvider.TimeSyncStatsByWorld[kv.Key.Value] =
                    BuildTimeSyncStats(kv.Key, opCode, intervalMs, alpha, timeoutMs);
            }

            if (_plan.WorldId != null)
            {
                BattleFlowDebugProvider.TimeSyncStatsByWorld[_plan.WorldId] = BattleFlowDebugProvider.TimeSyncStats;
            }
        }

        private TimeSyncStatsSnapshot BuildTimeSyncStats(WorldId worldId, uint opCode, int intervalMs, double alpha, int timeoutMs)
        {
            var hasAnchor = TryGetWorldStartAnchor(worldId, out var anchor);

            return new TimeSyncStatsSnapshot
            {
                OpCode = opCode,
                IntervalMs = intervalMs,
                Alpha = alpha,
                TimeoutMs = timeoutMs,

                HasAnchor = hasAnchor,
                AnchorStartServerTicks = anchor.StartServerTicks,
                AnchorServerTickFrequency = anchor.ServerTickFrequency,
                AnchorStartFrame = anchor.StartFrame,
                AnchorFixedDeltaSeconds = anchor.FixedDeltaSeconds,

                HasClockSync = _state.GatewayRoomTimeSync.HasClockSync,
                OffsetSecondsEwma = _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma,
                RttSecondsEwma = _state.GatewayRoomTimeSync.RttSecondsEwma,
                Samples = _state.GatewayRoomTimeSync.Samples,

                IdealFrameRaw = ResolveIdealFrameRaw(worldId),
                IdealFrameSafetyMarginFrames = ResolveIdealFrameSafetyMarginFrames(worldId),
                IdealFrameLimit = ResolveIdealFrameLimit(worldId)
            };
        }
    }
}
