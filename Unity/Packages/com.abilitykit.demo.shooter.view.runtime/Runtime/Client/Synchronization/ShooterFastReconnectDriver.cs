#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Wraps the gameplay-agnostic framework <see cref="FastReconnectSession"/> so the Shooter
    /// recovery layer can drive it (audit §10.4: first real consumer of FastReconnect).
    ///
    /// Shooter keeps its own snapshot import / replay / business-reason classification as the source
    /// of truth for routing; this driver mirrors each <see cref="ShooterClientRecoveryState"/>
    /// transition onto the framework's phase machine (Connected → Disconnected →
    /// {Resuming | AwaitingFullSnapshot} → Recovered) and collects the unified
    /// <see cref="SyncHealthEvent"/> stream the session emits, so the framework owns phase
    /// adjudication + health telemetry without re-implementing recovery logic.
    ///
    /// The reconciler tolerates the session's strict transition guards: any illegal step is skipped
    /// rather than thrown, keeping the wrap purely additive (design §6 rollback note).
    /// </summary>
    internal sealed class ShooterFastReconnectDriver
    {
        private readonly FastReconnectSession _session;
        private readonly List<SyncHealthEvent> _events = new List<SyncHealthEvent>();

        public ShooterFastReconnectDriver(int resumeWindowFrames)
        {
            _session = new FastReconnectSession(resumeWindowFrames < 1 ? 1 : resumeWindowFrames);
        }

        /// <summary>Current framework recovery phase, the projection target for Shooter's recovery state.</summary>
        public FastReconnectPhase Phase => _session.Phase;

        /// <summary>The framework resume window (frames) that splits short catch-up from full snapshot.</summary>
        public int ResumeWindowFrames => _session.ResumeWindowFrames;

        /// <summary>Health events accumulated since the last <see cref="ResetEventBuffer"/>.</summary>
        public IReadOnlyList<SyncHealthEvent> CollectedEvents => _events;

        /// <summary>Clears the per-operation health-event buffer; call at each public entry point.</summary>
        public void ResetEventBuffer()
        {
            _events.Clear();
        }

        /// <summary>
        /// Records a routine authoritative heartbeat (a clean snapshot receipt with no pending
        /// recovery). Emits the framework's <see cref="SyncHealthEventKind.SnapshotReceived"/>.
        /// </summary>
        public void Heartbeat(int authoritativeFrame)
        {
            var phase = _session.Phase;
            if (phase == FastReconnectPhase.Connected || phase == FastReconnectPhase.Recovered)
            {
                TryObserve(authoritativeFrame);
            }
        }

        /// <summary>
        /// Moves the session toward the phase that matches Shooter's new recovery state, taking one
        /// legal framework transition at a time and harvesting the emitted health events.
        /// </summary>
        public void Reconcile(FastReconnectPhase target, int authoritativeFrame, int gapHint)
        {
            if (target == FastReconnectPhase.Disconnected)
            {
                return;
            }

            var frame = authoritativeFrame < 0 ? 0 : authoritativeFrame;
            var gap = gapHint < 0 ? -gapHint : gapHint;

            for (var guard = 0; guard < 8; guard++)
            {
                var phase = _session.Phase;
                if (PhaseMatches(phase, target))
                {
                    return;
                }

                if (!Step(phase, target, frame, gap))
                {
                    return;
                }
            }
        }

        private bool Step(FastReconnectPhase current, FastReconnectPhase target, int frame, int gap)
        {
            switch (current)
            {
                case FastReconnectPhase.Connected:
                    // Leaving Connected toward any recovery/Recovered phase starts with a disconnect.
                    return TryDisconnect();

                case FastReconnectPhase.Recovered:
                    if (target == FastReconnectPhase.Connected)
                    {
                        return TryObserve(frame);
                    }
                    return TryDisconnect();

                case FastReconnectPhase.Disconnected:
                    // Choose the recovery path by framing the gap relative to the resume window so the
                    // session lands on the phase Shooter already decided on.
                    return target == FastReconnectPhase.AwaitingFullSnapshot
                        ? TryReconnect(LargeGapFrame(gap))
                        : TryReconnect(SmallGapFrame(gap));

                case FastReconnectPhase.Resuming:
                case FastReconnectPhase.AwaitingFullSnapshot:
                    // The only legal exit is completing recovery; subsequent iterations walk on toward
                    // Connected / a fresh recovery path if that is the requested target.
                    return TryComplete();

                default:
                    return false;
            }
        }

        private int SmallGapFrame(int gap)
        {
            var window = _session.ResumeWindowFrames;
            var bounded = gap;
            if (bounded < 0) bounded = 0;
            if (bounded > window) bounded = window;
            return _session.LastAckedServerFrame + bounded;
        }

        private int LargeGapFrame(int gap)
        {
            var window = _session.ResumeWindowFrames;
            var forced = gap > window ? gap : window + 1;
            return _session.LastAckedServerFrame + forced;
        }

        private bool TryObserve(int frame)
        {
            var safe = frame < _session.LastAckedServerFrame ? _session.LastAckedServerFrame : frame;
            try
            {
                Collect(_session.ObserveServerFrame(safe));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryDisconnect()
        {
            try
            {
                Collect(_session.Disconnect());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryReconnect(int currentServerFrame)
        {
            var safe = currentServerFrame < _session.LastAckedServerFrame
                ? _session.LastAckedServerFrame
                : currentServerFrame;
            try
            {
                Collect(_session.Reconnect(safe));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryComplete()
        {
            try
            {
                Collect(_session.CompleteRecovery());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Collect(in FastReconnectStepReport report)
        {
            var events = report.HealthEvents;
            for (var i = 0; i < events.Count; i++)
            {
                _events.Add(events[i]);
            }
        }

        private static bool PhaseMatches(FastReconnectPhase phase, FastReconnectPhase target)
        {
            return phase == target;
        }
    }
}
