#nullable enable

using System;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;
using AbilityKit.Protocol.Shooter;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.DemoHarness;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Thin adapter that lets the framework demo harness drive an existing Shooter
    /// authoritative-interpolation sync strategy. Mirrors <see cref="ShooterDemoHarnessCarrier"/>
    /// but accepts <see cref="ClientPlaybackPolicy.AuthoritativeInterpolation"/> profiles instead
    /// of predict-rollback, so the acceptance matrix can validate both sync models through the
    /// same <see cref="DemoHarnessRunner"/> pipeline.
    /// </summary>
    public sealed class ShooterInterpolationDemoHarnessCarrier : ISyncDemoCarrier, ISyncDemoCarrierCapabilities
    {
        public const string DefaultCarrierName = "ShooterInterpolation";

        private readonly IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> _strategy;
        private readonly Func<NetworkConditioningStats> _networkStats;
        private readonly Func<double> _remoteJitter;
        private readonly Func<long> _acceptedHits;
        private readonly Func<long> _rejectedHits;

        public ShooterInterpolationDemoHarnessCarrier(
            IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> strategy,
            Func<NetworkConditioningStats>? networkStats = null,
            Func<double>? remoteJitter = null,
            Func<long>? acceptedHits = null,
            Func<long>? rejectedHits = null,
            string carrierName = DefaultCarrierName)
        {
            if (string.IsNullOrWhiteSpace(carrierName)) throw new ArgumentException("Carrier name is required.", nameof(carrierName));

            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _networkStats = networkStats ?? (() => default);
            _remoteJitter = remoteJitter ?? (() => 0d);
            _acceptedHits = acceptedHits ?? (() => 0L);
            _rejectedHits = rejectedHits ?? (() => 0L);
            CarrierName = carrierName;
        }

        public string CarrierName { get; }

        public NetworkSyncModel SyncModel => _strategy.SyncModel;

        public SyncTimeAnchor LastTimeAnchor { get; private set; }

        public SyncDemoCapabilityResult Supports(in NetworkSyncProfile profile, in NetworkConditionProfile networkProfile)
        {
            if (profile.ClientPlayback != ClientPlaybackPolicy.AuthoritativeInterpolation)
            {
                return SyncDemoCapabilityResult.Unsupported("Shooter interpolation carrier supports authoritative interpolation playback only.");
            }

            if (!profile.Snapshot.HasFlag(SnapshotPolicy.FixedRateStateStream))
            {
                return SyncDemoCapabilityResult.Unsupported("Shooter interpolation carrier requires FixedRateStateStream snapshots.");
            }

            return SyncDemoCapabilityResult.Supported;
        }

        public DemoHarnessStepTelemetry Step(in DemoHarnessStepContext context)
        {
            LastTimeAnchor = context.TimeAnchor;
            var tick = _strategy.Tick(context.DeltaSeconds);
            var report = _strategy.GetReconciliationReport();

            var healthEvents = CollectFastReconnectHealthEvents();

            return new DemoHarnessStepTelemetry(
                tick,
                report,
                _networkStats(),
                _remoteJitter(),
                _acceptedHits(),
                _rejectedHits(),
                healthEvents);
        }

        private SyncHealthEvent[]? CollectFastReconnectHealthEvents()
        {
            if (_strategy is not IShooterClientSyncController controller)
            {
                return null;
            }

            var events = controller.LastFastReconnectHealthEvents;
            if (events == null || events.Count == 0)
            {
                return null;
            }

            var buffer = new SyncHealthEvent[events.Count];
            for (var i = 0; i < events.Count; i++)
            {
                buffer[i] = events[i];
            }

            return buffer;
        }
    }
}
