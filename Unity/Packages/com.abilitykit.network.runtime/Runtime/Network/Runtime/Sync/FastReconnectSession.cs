#nullable enable

using System;
using System.Collections.Generic;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// The lifecycle phase a <see cref="FastReconnectSession"/> is currently in. A session starts
    /// <see cref="Connected"/>, enters <see cref="Disconnected"/> on an outage, then on reconnect picks
    /// one of two recovery paths based on how far behind the client fell: <see cref="Resuming"/> (small
    /// gap, incremental catch-up from buffered deltas) or <see cref="AwaitingFullSnapshot"/> (large gap,
    /// a fresh authoritative snapshot is required). Either path ends in <see cref="Recovered"/>.
    /// </summary>
    public enum FastReconnectPhase
    {
        Connected = 0,
        Disconnected = 1,
        Resuming = 2,
        AwaitingFullSnapshot = 3,
        Recovered = 4
    }

    /// <summary>
    /// Result of a single <see cref="FastReconnectSession"/> transition: the phase the session is now in,
    /// the gameplay-agnostic reconciliation diagnostics for the transition, and any health events emitted.
    /// Carriers forward <see cref="Reconciliation"/> and <see cref="HealthEvents"/> straight into a
    /// <see cref="DemoHarness"/> step telemetry so the harness can aggregate the reconnect picture using
    /// the same plumbing as every other sync model.
    /// </summary>
    public readonly struct FastReconnectStepReport
    {
        private static readonly SyncHealthEvent[] EmptyEvents = Array.Empty<SyncHealthEvent>();

        private readonly SyncHealthEvent[]? _healthEvents;

        public FastReconnectStepReport(
            FastReconnectPhase phase,
            SyncReconciliationReport reconciliation,
            params SyncHealthEvent[]? healthEvents)
        {
            Phase = phase;
            Reconciliation = reconciliation;
            _healthEvents = healthEvents != null && healthEvents.Length > 0 ? healthEvents : null;
        }

        public FastReconnectPhase Phase { get; }

        public SyncReconciliationReport Reconciliation { get; }

        public IReadOnlyList<SyncHealthEvent> HealthEvents => _healthEvents ?? EmptyEvents;
    }

    /// <summary>
    /// Gameplay-agnostic implementation of the <see cref="NetworkSyncModel.FastReconnect"/> capability
    /// (audit migration step 7). The session is the reusable runtime behind the
    /// <see cref="NetworkSyncProfiles.FastReconnect"/> profile (Recovery =
    /// <see cref="RecoveryPolicy.ReconnectResume"/> | <see cref="RecoveryPolicy.RequestFullSnapshot"/>):
    /// it tracks the last acknowledged authoritative frame, and on reconnect decides between an
    /// incremental resume and a full-snapshot rebuild based on the size of the frame gap relative to a
    /// configurable resume window. Each transition surfaces unified <see cref="SyncHealthEvent"/>s
    /// (<see cref="SyncHealthEventKind.SnapshotGap"/>, <see cref="SyncHealthEventKind.FullSnapshotRequested"/>,
    /// <see cref="SyncHealthEventKind.FullSnapshotApplied"/>, <see cref="SyncHealthEventKind.InterpolationRecovered"/>)
    /// plus a <see cref="SyncReconciliationReport"/>, so demos drive reconnect entirely through the
    /// framework instead of re-implementing bespoke recovery state.
    /// </summary>
    public sealed class FastReconnectSession
    {
        private readonly int _resumeWindowFrames;

        private FastReconnectPhase _phase = FastReconnectPhase.Connected;
        private int _lastAckedServerFrame;
        private int _currentServerFrame;
        private int _pendingGapFrames;

        /// <param name="resumeWindowFrames">
        /// Maximum authoritative-frame gap that can be recovered by an incremental resume. A reconnect
        /// whose gap is at or below this window resumes from buffered deltas; a larger gap forces a full
        /// snapshot rebuild.
        /// </param>
        public FastReconnectSession(int resumeWindowFrames = 32)
        {
            if (resumeWindowFrames <= 0) throw new ArgumentOutOfRangeException(nameof(resumeWindowFrames));

            _resumeWindowFrames = resumeWindowFrames;
        }

        public FastReconnectPhase Phase => _phase;

        public int ResumeWindowFrames => _resumeWindowFrames;

        /// <summary>The latest authoritative frame the client has fully acknowledged.</summary>
        public int LastAckedServerFrame => _lastAckedServerFrame;

        /// <summary>The authoritative frame observed at the most recent reconnect (0 while connected).</summary>
        public int CurrentServerFrame => _currentServerFrame;

        /// <summary>
        /// The frame gap that the in-flight recovery is closing. Valid while <see cref="Phase"/> is
        /// <see cref="FastReconnectPhase.Resuming"/> or <see cref="FastReconnectPhase.AwaitingFullSnapshot"/>.
        /// </summary>
        public int PendingGapFrames => _pendingGapFrames;

        /// <summary>
        /// Records a routine authoritative heartbeat while connected, acknowledging <paramref name="serverFrame"/>.
        /// Emits an informational <see cref="SyncHealthEventKind.SnapshotReceived"/> event.
        /// </summary>
        public FastReconnectStepReport ObserveServerFrame(int serverFrame)
        {
            if (serverFrame < 0) throw new ArgumentOutOfRangeException(nameof(serverFrame));
            if (_phase != FastReconnectPhase.Connected && _phase != FastReconnectPhase.Recovered)
            {
                throw new InvalidOperationException($"Cannot observe a server frame while {_phase}; reconnect must complete first.");
            }
            if (serverFrame < _lastAckedServerFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(serverFrame), serverFrame, "Authoritative frame cannot move backwards.");
            }

            _phase = FastReconnectPhase.Connected;
            _lastAckedServerFrame = serverFrame;
            _currentServerFrame = serverFrame;
            _pendingGapFrames = 0;

            return new FastReconnectStepReport(
                _phase,
                SyncReconciliationReport.None,
                SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived, serverFrame));
        }

        /// <summary>
        /// Marks the connection as lost. Local playback starves until <see cref="Reconnect"/> is called.
        /// Emits a <see cref="SyncHealthEventKind.InterpolationStarved"/> warning at the last acked frame.
        /// </summary>
        public FastReconnectStepReport Disconnect()
        {
            if (_phase == FastReconnectPhase.Disconnected)
            {
                throw new InvalidOperationException("Session is already disconnected.");
            }

            _phase = FastReconnectPhase.Disconnected;

            var report = new SyncReconciliationReport(
                SyncReconciliationReason.SnapshotTimeout,
                SyncRecoveryState.Normal,
                needsFullSnapshot: false,
                clientFrame: _lastAckedServerFrame,
                authoritativeFrame: _lastAckedServerFrame,
                clientStateHash: 0u,
                authoritativeStateHash: 0u,
                replayTicks: 0);

            return new FastReconnectStepReport(
                _phase,
                report,
                SyncHealthEvent.Warning(SyncHealthEventKind.InterpolationStarved, _lastAckedServerFrame));
        }

        /// <summary>
        /// Re-establishes the connection at <paramref name="currentServerFrame"/> and chooses a recovery
        /// path. Always emits a <see cref="SyncHealthEventKind.SnapshotGap"/> warning carrying the gap; a
        /// gap within the resume window enters <see cref="FastReconnectPhase.Resuming"/>, otherwise it
        /// requests a full snapshot (<see cref="SyncHealthEventKind.FullSnapshotRequested"/>) and enters
        /// <see cref="FastReconnectPhase.AwaitingFullSnapshot"/>. Call <see cref="CompleteRecovery"/> to finish.
        /// </summary>
        public FastReconnectStepReport Reconnect(int currentServerFrame)
        {
            if (_phase != FastReconnectPhase.Disconnected)
            {
                throw new InvalidOperationException($"Cannot reconnect while {_phase}; the session must be disconnected first.");
            }
            if (currentServerFrame < _lastAckedServerFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(currentServerFrame), currentServerFrame, "Authoritative frame cannot move backwards.");
            }

            _currentServerFrame = currentServerFrame;
            var gap = currentServerFrame - _lastAckedServerFrame;
            _pendingGapFrames = gap;

            var gapEvent = SyncHealthEvent.Warning(SyncHealthEventKind.SnapshotGap, currentServerFrame, gap);

            if (gap <= _resumeWindowFrames)
            {
                _phase = FastReconnectPhase.Resuming;
                var resumeReport = new SyncReconciliationReport(
                    SyncReconciliationReason.FrameTooFarBehind,
                    SyncRecoveryState.CatchUp,
                    needsFullSnapshot: false,
                    clientFrame: _lastAckedServerFrame,
                    authoritativeFrame: currentServerFrame,
                    clientStateHash: 0u,
                    authoritativeStateHash: 0u,
                    replayTicks: gap);

                return new FastReconnectStepReport(_phase, resumeReport, gapEvent);
            }

            _phase = FastReconnectPhase.AwaitingFullSnapshot;
            var snapshotReport = new SyncReconciliationReport(
                SyncReconciliationReason.FrameTooFarBehind,
                SyncRecoveryState.AwaitingFullSnapshot,
                needsFullSnapshot: true,
                clientFrame: _lastAckedServerFrame,
                authoritativeFrame: currentServerFrame,
                clientStateHash: 0u,
                authoritativeStateHash: 0u,
                replayTicks: 0);

            return new FastReconnectStepReport(
                _phase,
                snapshotReport,
                gapEvent,
                SyncHealthEvent.Info(SyncHealthEventKind.FullSnapshotRequested, currentServerFrame, gap));
        }

        /// <summary>
        /// Finishes the in-flight recovery: a resume catches the client up to the authoritative frame,
        /// a full-snapshot path applies the snapshot first
        /// (<see cref="SyncHealthEventKind.FullSnapshotApplied"/>). Both paths end with
        /// <see cref="SyncHealthEventKind.InterpolationRecovered"/> and leave the session
        /// <see cref="FastReconnectPhase.Recovered"/> with the gap closed.
        /// </summary>
        public FastReconnectStepReport CompleteRecovery()
        {
            if (_phase != FastReconnectPhase.Resuming && _phase != FastReconnectPhase.AwaitingFullSnapshot)
            {
                throw new InvalidOperationException($"Cannot complete recovery while {_phase}; reconnect must be in progress.");
            }

            var gap = _pendingGapFrames;
            var recoveredFrame = _currentServerFrame;
            var wasFullSnapshot = _phase == FastReconnectPhase.AwaitingFullSnapshot;

            _lastAckedServerFrame = recoveredFrame;
            _pendingGapFrames = 0;
            _phase = FastReconnectPhase.Recovered;

            var report = new SyncReconciliationReport(
                SyncReconciliationReason.FrameTooFarBehind,
                SyncRecoveryState.Recovered,
                needsFullSnapshot: false,
                clientFrame: recoveredFrame,
                authoritativeFrame: recoveredFrame,
                clientStateHash: 0u,
                authoritativeStateHash: 0u,
                replayTicks: wasFullSnapshot ? 0 : gap);

            var recovered = SyncHealthEvent.Info(SyncHealthEventKind.InterpolationRecovered, recoveredFrame, gap);

            return wasFullSnapshot
                ? new FastReconnectStepReport(
                    _phase,
                    report,
                    SyncHealthEvent.Info(SyncHealthEventKind.FullSnapshotApplied, recoveredFrame, gap),
                    recovered)
                : new FastReconnectStepReport(_phase, report, recovered);
        }
    }
}
