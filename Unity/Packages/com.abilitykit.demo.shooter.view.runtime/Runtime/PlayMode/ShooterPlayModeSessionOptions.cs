#nullable enable

using System;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public readonly struct ShooterPlayModeSessionOptions
    {
        public static ShooterPlayModeSessionOptions Default => FromTemplate(
            ShooterAcceptanceCatalog.GetSyncTemplate("predict-rollback-authority"));

        public static ShooterPlayModeSessionOptions FromTemplate(in ShooterSyncTemplate template)
        {
            var network = ShooterAcceptanceCatalog.GetNetworkEnvironment(template.NetworkEnvironmentId);
            return new ShooterPlayModeSessionOptions(
                template.SyncModel,
                ShooterAcceptanceLab.DefaultTickRate,
                template.RecommendedPlayerCount,
                randomSeed: 3901,
                controlledPlayerId: 1,
                enableAuthoritativeWorld: template.EnableAuthoritativeWorld,
                latencyMs: network.Profile.BaseLatencyMs,
                jitterMs: network.Profile.JitterMs,
                packetLossRate: (float)network.Profile.PacketLossRate,
                reorderRate: (float)network.Profile.ReorderRate,
                bandwidthKbps: network.Profile.BandwidthKbps,
                worldScale: 1f,
                networkName: template.DisplayName,
                syncTemplateId: template.Id);
        }

        public static ShooterPlayModeSessionOptions FromTemplateId(string templateId)
        {
            return FromTemplate(ShooterAcceptanceCatalog.GetSyncTemplate(templateId));
        }

        public static ShooterPlayModeSessionOptions FromTemplate(
            in ShooterSyncTemplate template,
            int randomSeed,
            int controlledPlayerId,
            float worldScale)
        {
            var options = FromTemplate(in template);
            return new ShooterPlayModeSessionOptions(
                options.SyncModel,
                options.TickRate,
                options.PlayerCount,
                randomSeed,
                controlledPlayerId,
                options.EnableAuthoritativeWorld,
                options.LatencyMs,
                options.JitterMs,
                options.PacketLossRate,
                options.ReorderRate,
                options.BandwidthKbps,
                worldScale,
                options.NetworkName,
                options.SyncTemplateId);
        }

        public static ShooterPlayModeSessionOptions LegacyDefault => new(
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
            networkName: null,
            syncTemplateId: null);

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
            string? networkName,
            string? syncTemplateId = null)
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
            SyncTemplateId = syncTemplateId;
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
        public string? SyncTemplateId { get; }

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
                NetworkName,
                SyncTemplateId);
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
