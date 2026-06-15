#nullable enable

using System;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Protocol-agnostic estimator that turns NTP-style time-sync round trips into a running estimate
    /// of the offset between the local clock and an authoritative server clock, plus the round-trip
    /// time (RTT). It deliberately takes only raw tick values so it stays decoupled from any specific
    /// wire type (e.g. a gateway time-sync response): callers feed it <c>clientSendTicks</c>,
    /// <c>serverReceiveTicks</c> and <c>clientReceiveTicks</c> and it folds each sample in.
    ///
    /// Like a real NTP client, the most accurate samples are the ones with the lowest RTT (least
    /// queuing/jitter), so the estimator keeps the offset associated with the lowest RTT observed so
    /// far rather than averaging noisy samples. This makes <see cref="ServerTicksNow"/> stable under
    /// jitter while still converging quickly when a better (lower-RTT) sample arrives.
    /// </summary>
    public sealed class ServerClockEstimator
    {
        private long _bestRttTicks;
        private long _bestOffsetTicks;
        private int _sampleCount;

        /// <param name="serverTickFrequency">
        /// How many server ticks represent one second. Used to convert RTT/offset to seconds for
        /// diagnostics. Must be positive; non-positive values are treated as 1 to avoid divide-by-zero.
        /// </param>
        public ServerClockEstimator(long serverTickFrequency)
        {
            ServerTickFrequency = serverTickFrequency <= 0L ? 1L : serverTickFrequency;
        }

        /// <summary>How many server ticks represent one second.</summary>
        public long ServerTickFrequency { get; }

        /// <summary>Whether at least one round-trip sample has been observed.</summary>
        public bool HasSample => _sampleCount > 0;

        /// <summary>Number of round-trip samples folded in so far.</summary>
        public int SampleCount => _sampleCount;

        /// <summary>
        /// The best (lowest) round-trip time observed so far, in ticks. Zero until the first sample.
        /// </summary>
        public long BestRoundTripTicks => _bestRttTicks;

        /// <summary>The best round-trip time observed so far, in seconds.</summary>
        public double BestRoundTripSeconds => (double)_bestRttTicks / ServerTickFrequency;

        /// <summary>
        /// The estimated offset to add to a local clock value to obtain server time, in ticks. This is
        /// the offset associated with the lowest-RTT sample. Zero until the first sample.
        /// </summary>
        public long OffsetTicks => _bestOffsetTicks;

        /// <summary>The estimated clock offset in seconds.</summary>
        public double OffsetSeconds => (double)_bestOffsetTicks / ServerTickFrequency;

        /// <summary>
        /// Folds one round-trip time-sync sample into the estimate. All values share the same tick unit
        /// (<see cref="ServerTickFrequency"/>). The local send/receive stamps are taken on the same
        /// local clock; the server stamp is taken on the server clock.
        ///
        /// RTT is <c>clientReceive - clientSend</c>. Assuming a symmetric path, the server stamp was
        /// taken at local time <c>clientSend + RTT/2</c>, so the offset is
        /// <c>serverReceive - (clientSend + RTT/2)</c>. The sample is only adopted when its RTT is the
        /// lowest seen so far (or it is the first sample), mirroring NTP best-sample selection.
        /// </summary>
        /// <param name="clientSendTicks">Local clock when the request was sent.</param>
        /// <param name="serverReceiveTicks">Server clock when the server stamped the response.</param>
        /// <param name="clientReceiveTicks">Local clock when the response was received.</param>
        /// <returns><c>true</c> if this sample became the new best estimate; otherwise <c>false</c>.</returns>
        public bool ObserveRoundTrip(long clientSendTicks, long serverReceiveTicks, long clientReceiveTicks)
        {
            long rtt = clientReceiveTicks - clientSendTicks;
            if (rtt < 0L)
            {
                // A negative RTT means the stamps are inconsistent (clock went backwards or out-of-order
                // delivery); reject rather than poison the estimate.
                return false;
            }

            // Server stamp is assumed taken at the midpoint of the round trip on the local timeline.
            long localMidpoint = clientSendTicks + rtt / 2L;
            long offset = serverReceiveTicks - localMidpoint;

            if (_sampleCount == 0 || rtt < _bestRttTicks)
            {
                _bestRttTicks = rtt;
                _bestOffsetTicks = offset;
                _sampleCount++;
                return true;
            }

            _sampleCount++;
            return false;
        }

        /// <summary>
        /// Converts a local clock value to the estimated authoritative server clock value by applying
        /// the current offset. Returns <paramref name="localTicks"/> unchanged until a sample exists.
        /// </summary>
        public long ToServerTicks(long localTicks)
        {
            return HasSample ? localTicks + _bestOffsetTicks : localTicks;
        }

        /// <summary>
        /// Stamps <paramref name="anchor"/> with the estimated server clock for its
        /// <paramref name="localTicks"/>. When no sample has been observed yet the anchor is returned
        /// unchanged so callers never publish a fabricated server time.
        /// </summary>
        public SyncTimeAnchor StampServerTicks(SyncTimeAnchor anchor, long localTicks)
        {
            return HasSample ? anchor.WithServerTicks(ToServerTicks(localTicks)) : anchor;
        }

        /// <summary>Clears all observed samples, returning the estimator to its initial state.</summary>
        public void Reset()
        {
            _bestRttTicks = 0L;
            _bestOffsetTicks = 0L;
            _sampleCount = 0;
        }
    }
}
