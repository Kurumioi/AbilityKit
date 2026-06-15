#nullable enable

using System;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Single time source for the sync stack: advances the local frame timeline and, when a
    /// <see cref="ServerClockEstimator"/> has observed round-trips, stamps the estimated server
    /// clock onto every produced <see cref="SyncTimeAnchor"/>. Acts as the only factory for anchors
    /// so prediction, interpolation, rewind, and demo playback all share one clock.
    /// </summary>
    public sealed class SyncClock
    {
        private readonly double _deltaSeconds;
        private readonly long _timelineTicksPerStep;
        private int _localFrame;

        public SyncClock(double deltaSeconds, long timelineTicksPerStep = 1L, ServerClockEstimator? serverClock = null)
        {
            if (deltaSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (timelineTicksPerStep <= 0L) throw new ArgumentOutOfRangeException(nameof(timelineTicksPerStep));

            _deltaSeconds = deltaSeconds;
            _timelineTicksPerStep = timelineTicksPerStep;
            ServerClock = serverClock;
        }

        /// <summary>
        /// Optional server-clock estimator. When present and primed with samples, anchors carry an
        /// estimated <see cref="SyncTimeAnchor.ServerTicks"/>.
        /// </summary>
        public ServerClockEstimator? ServerClock { get; }

        /// <summary>The next local frame index that <see cref="Advance"/> will emit.</summary>
        public int LocalFrame => _localFrame;

        /// <summary>Seconds advanced per <see cref="Advance"/> call.</summary>
        public double DeltaSeconds => _deltaSeconds;

        /// <summary>
        /// Produces the anchor for the current frame, then advances the local frame counter.
        /// </summary>
        public SyncTimeAnchor Advance()
        {
            var anchor = AnchorFor(_localFrame);
            _localFrame++;
            return anchor;
        }

        /// <summary>
        /// Builds the anchor for an explicit frame index without advancing internal state. Useful
        /// for replays or deterministic harness loops that own their own frame counter.
        /// </summary>
        public SyncTimeAnchor AnchorFor(int localFrame)
        {
            if (localFrame < 0) throw new ArgumentOutOfRangeException(nameof(localFrame));

            var timelineTicks = localFrame * _timelineTicksPerStep;
            var elapsedSeconds = localFrame * _deltaSeconds;
            var anchor = SyncTimeAnchor.FromLocalFrame(localFrame, timelineTicks, elapsedSeconds);

            if (ServerClock is { HasSample: true })
            {
                anchor = ServerClock.StampServerTicks(anchor, timelineTicks);
            }

            return anchor;
        }

        /// <summary>Resets the local frame counter back to zero.</summary>
        public void Reset()
        {
            _localFrame = 0;
        }
    }
}
