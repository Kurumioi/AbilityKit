using System;
using AbilityKit.Game.Battle.Agent;

namespace AbilityKit.Game.Flow
{
    internal readonly struct GatewayTimeSyncRuntimeOptions
    {
        public readonly uint OpCode;
        public readonly int IntervalMs;
        public readonly double Alpha;
        public readonly int TimeoutMs;

        public GatewayTimeSyncRuntimeOptions(uint opCode, int intervalMs, double alpha, int timeoutMs)
        {
            OpCode = opCode;
            IntervalMs = intervalMs;
            Alpha = alpha;
            TimeoutMs = timeoutMs;
        }
    }

    internal readonly struct GatewayTimeSyncSample
    {
        public readonly double RttSeconds;
        public readonly double OffsetSeconds;

        public GatewayTimeSyncSample(double rttSeconds, double offsetSeconds)
        {
            RttSeconds = rttSeconds;
            OffsetSeconds = offsetSeconds;
        }
    }

    internal readonly struct GatewayTimeSyncEwma
    {
        public readonly bool HasClockSync;
        public readonly double ClockOffsetSecondsEwma;
        public readonly double RttSecondsEwma;
        public readonly int Samples;

        public GatewayTimeSyncEwma(bool hasClockSync, double clockOffsetSecondsEwma, double rttSecondsEwma, int samples)
        {
            HasClockSync = hasClockSync;
            ClockOffsetSecondsEwma = clockOffsetSecondsEwma;
            RttSecondsEwma = rttSecondsEwma;
            Samples = samples;
        }
    }

    internal static class GatewayTimeSyncHelper
    {
        public static GatewayTimeSyncRuntimeOptions ResolveRuntimeOptions(in BattleStartPlanTimeSyncOptions timeSync)
        {
            var alpha = timeSync.Alpha;
            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;

            var intervalMs = timeSync.IntervalMs;
            if (intervalMs <= 0) intervalMs = 1000;

            var timeoutMs = timeSync.TimeoutMs;
            if (timeoutMs <= 0) timeoutMs = 2000;

            return new GatewayTimeSyncRuntimeOptions(timeSync.OpCode, intervalMs, alpha, timeoutMs);
        }

        public static GatewayTimeSyncSample CalculateSample(
            long clientSendTicks,
            long clientReceiveTicks,
            long serverNowTicks,
            long serverTickFrequency,
            double localTickFrequency)
        {
            if (serverTickFrequency <= 0)
            {
                throw new InvalidOperationException("Gateway time sync requires a positive server tick frequency.");
            }

            if (localTickFrequency <= 0)
            {
                throw new InvalidOperationException("Gateway time sync requires a positive local tick frequency.");
            }

            var rttSeconds = (clientReceiveTicks - clientSendTicks) / localTickFrequency;
            if (rttSeconds < 0) rttSeconds = 0;

            var serverNowSeconds = serverNowTicks / (double)serverTickFrequency;
            var localNowSeconds = clientReceiveTicks / localTickFrequency;
            var serverNowEstimatedAtReceive = serverNowSeconds + (rttSeconds * 0.5);
            var offsetSeconds = localNowSeconds - serverNowEstimatedAtReceive;

            return new GatewayTimeSyncSample(rttSeconds, offsetSeconds);
        }

        public static GatewayTimeSyncEwma ApplySample(
            bool hasClockSync,
            double currentClockOffsetSecondsEwma,
            double currentRttSecondsEwma,
            int currentSamples,
            in GatewayTimeSyncSample sample,
            double alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;

            if (!hasClockSync)
            {
                return new GatewayTimeSyncEwma(
                    hasClockSync: true,
                    clockOffsetSecondsEwma: sample.OffsetSeconds,
                    rttSecondsEwma: sample.RttSeconds,
                    samples: 1);
            }

            return new GatewayTimeSyncEwma(
                hasClockSync: true,
                clockOffsetSecondsEwma: (alpha * sample.OffsetSeconds) + ((1.0 - alpha) * currentClockOffsetSecondsEwma),
                rttSecondsEwma: (alpha * sample.RttSeconds) + ((1.0 - alpha) * currentRttSecondsEwma),
                samples: currentSamples + 1);
        }
    }
}
