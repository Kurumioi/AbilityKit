#nullable enable

using System;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Protocol-agnostic seam between a time-sync exchange and the sync clock. It owns a
    /// <see cref="ServerClockEstimator"/>, folds each request/response round-trip into it, and hands
    /// out a <see cref="SyncClock"/> wired to that estimator so produced anchors carry the estimated
    /// server clock once enough samples have converged.
    /// <para>
    /// The bridge takes raw <see cref="long"/> tick values only; mapping a concrete wire message
    /// (e.g. a room gateway time-sync response) happens at the protocol boundary so this package
    /// never depends on any protocol package. Caller is responsible for expressing all three tick
    /// stamps in the same tick unit established by <see cref="TickFrequency"/>.
    /// </para>
    /// </summary>
    public sealed class TimeSyncBridge
    {
        private readonly ServerClockEstimator _estimator;

        public TimeSyncBridge(long tickFrequency)
        {
            _estimator = new ServerClockEstimator(tickFrequency);
        }

        /// <summary>The tick unit shared by client and server stamps.</summary>
        public long TickFrequency => _estimator.ServerTickFrequency;

        /// <summary>True once at least one round-trip has been folded in.</summary>
        public bool HasConverged => _estimator.HasSample;

        /// <summary>Number of round-trips observed so far.</summary>
        public int SampleCount => _estimator.SampleCount;

        /// <summary>Best (lowest) round-trip latency observed, in seconds.</summary>
        public double BestRoundTripSeconds => _estimator.BestRoundTripSeconds;

        /// <summary>Current best-estimate client-to-server clock offset, in seconds.</summary>
        public double OffsetSeconds => _estimator.OffsetSeconds;

        /// <summary>The underlying estimator, exposed for diagnostics and clock wiring.</summary>
        public ServerClockEstimator Estimator => _estimator;

        /// <summary>
        /// Folds one time-sync exchange into the estimator. The three stamps mirror a typical
        /// request/response: the client sends at <paramref name="clientSendTicks"/>, the server
        /// reports its clock as <paramref name="serverNowTicks"/>, and the client receives the
        /// response at <paramref name="clientReceiveTicks"/>.
        /// </summary>
        /// <returns>True when this sample improved the best round-trip estimate.</returns>
        public bool ObserveResponse(long clientSendTicks, long serverNowTicks, long clientReceiveTicks)
        {
            return _estimator.ObserveRoundTrip(clientSendTicks, serverNowTicks, clientReceiveTicks);
        }

        /// <summary>
        /// Produces a <see cref="SyncClock"/> wired to this bridge's estimator. Anchors it emits
        /// carry an estimated server clock as soon as <see cref="HasConverged"/> is true.
        /// </summary>
        public SyncClock CreateClock(double deltaSeconds, long timelineTicksPerStep = 1L)
        {
            return new SyncClock(deltaSeconds, timelineTicksPerStep, _estimator);
        }
    }
}
