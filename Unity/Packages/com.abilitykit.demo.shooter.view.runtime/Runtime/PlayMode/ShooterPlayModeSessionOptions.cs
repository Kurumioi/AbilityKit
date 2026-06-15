#nullable enable

using System;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public readonly struct ShooterPlayModeSessionOptions
    {
        public static ShooterPlayModeSessionOptions Default => new(
            NetworkSyncModel.PredictRollback,
            ShooterAcceptanceLab.DefaultTickRate,
            playerCount: 2,
            randomSeed: 3901,
            controlledPlayerId: 1,
            enableAuthoritativeWorld: true,
            latencyMs: 0,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: null);

        public ShooterPlayModeSessionOptions(
            NetworkSyncModel syncModel,
            int tickRate,
            int playerCount,
            int randomSeed,
            int controlledPlayerId,
            bool enableAuthoritativeWorld,
            int latencyMs,
            int jitterMs,
            float packetLossRate,
            float reorderRate,
            int bandwidthKbps,
            float worldScale,
            string? networkName)
        {
            SyncModel = syncModel;
            TickRate = tickRate;
            PlayerCount = playerCount;
            RandomSeed = randomSeed;
            ControlledPlayerId = controlledPlayerId;
            EnableAuthoritativeWorld = enableAuthoritativeWorld;
            LatencyMs = latencyMs;
            JitterMs = jitterMs;
            PacketLossRate = packetLossRate;
            ReorderRate = reorderRate;
            BandwidthKbps = bandwidthKbps;
            WorldScale = worldScale;
            NetworkName = networkName;
        }

        public NetworkSyncModel SyncModel { get; }
        public int TickRate { get; }
        public int PlayerCount { get; }
        public int RandomSeed { get; }
        public int ControlledPlayerId { get; }
        public bool EnableAuthoritativeWorld { get; }
        public int LatencyMs { get; }
        public int JitterMs { get; }
        public float PacketLossRate { get; }
        public float ReorderRate { get; }
        public int BandwidthKbps { get; }
        public float WorldScale { get; }
        public string? NetworkName { get; }

        public ShooterPlayModeSessionOptions Normalized()
        {
            var tickRate = TickRate <= 0 ? ShooterAcceptanceLab.DefaultTickRate : TickRate;
            var playerCount = Math.Max(1, PlayerCount);
            var controlledPlayerId = Math.Min(Math.Max(ControlledPlayerId, 1), playerCount);
            var worldScale = WorldScale <= 0f ? 1f : WorldScale;

            return new ShooterPlayModeSessionOptions(
                SyncModel,
                tickRate,
                playerCount,
                RandomSeed,
                controlledPlayerId,
                EnableAuthoritativeWorld,
                Math.Max(0, LatencyMs),
                Math.Max(0, JitterMs),
                Clamp01(PacketLossRate),
                Clamp01(ReorderRate),
                Math.Max(0, BandwidthKbps),
                worldScale,
                NetworkName);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
