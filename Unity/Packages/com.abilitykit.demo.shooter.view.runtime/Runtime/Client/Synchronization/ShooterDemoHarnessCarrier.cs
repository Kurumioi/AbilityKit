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
    /// Thin adapter that lets the framework demo harness drive an existing Shooter sync strategy.
    /// It does not own Shooter lifecycle; callers start/configure the underlying controller before running scenarios.
    /// </summary>
    public sealed class ShooterDemoHarnessCarrier : ISyncDemoCarrier, ISyncDemoCarrierCapabilities
    {
        public const string DefaultCarrierName = "Shooter";

        private readonly IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> _strategy;
        private readonly Func<NetworkConditioningStats> _networkStats;
        private readonly Func<double> _remoteJitter;
        private readonly Func<long> _acceptedHits;
        private readonly Func<long> _rejectedHits;

        public ShooterDemoHarnessCarrier(
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
            if (profile.ClientPlayback != ClientPlaybackPolicy.PredictRollback)
            {
                return SyncDemoCapabilityResult.Unsupported("Shooter carrier currently supports predict rollback playback only.");
            }

            if (!profile.Snapshot.HasFlag(SnapshotPolicy.FullSnapshot) && !profile.Snapshot.HasFlag(SnapshotPolicy.AuthorityOverride))
            {
                return SyncDemoCapabilityResult.Unsupported("Shooter rollback carrier requires full or authority override snapshots.");
            }

            return SyncDemoCapabilityResult.Supported;
        }

        public DemoHarnessStepTelemetry Step(in DemoHarnessStepContext context)
        {
            LastTimeAnchor = context.TimeAnchor;
            var tick = _strategy.Tick(context.DeltaSeconds);
            var report = _strategy.GetReconciliationReport();

            // §4.4.5: forward the framework FastReconnect health events the Shooter controller
            // collected this step into the shared DemoHarness telemetry stream.
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
