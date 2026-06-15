#nullable enable

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// Gameplay-agnostic taxonomy of synchronization health events a client/server sync strategy can
    /// emit during a tick. This is the unified diagnostics vocabulary called for by the sync abstraction
    /// audit (§6.5): <see cref="SyncReconciliationReport"/> stays focused on predict/rollback correction,
    /// while broader lifecycle signals (snapshot flow, interpolation starvation, recovery requests, input
    /// acceptance, lag-compensated validation) are reported as discrete <see cref="SyncHealthEvent"/>s so
    /// the demo harness can aggregate them without inflating the reconciliation report.
    /// </summary>
    public enum SyncHealthEventKind
    {
        /// <summary>No event; the default/empty slot.</summary>
        None = 0,

        // Snapshot flow.
        SnapshotReceived = 1,
        SnapshotDropped = 2,
        SnapshotStale = 3,
        SnapshotGap = 4,

        // Remote interpolation playback.
        InterpolationStarved = 10,
        InterpolationRecovered = 11,

        // Local prediction / rollback.
        RollbackStarted = 20,
        ReplayCompleted = 21,

        // Recovery flows.
        FullSnapshotRequested = 30,
        FullSnapshotApplied = 31,
        KeyFrameRequested = 32,
        KeyFrameApplied = 33,
        AoiSliceRequested = 34,
        AoiSliceApplied = 35,

        // Input acceptance.
        InputAccepted = 40,
        InputRemapped = 41,
        InputRejected = 42,

        // Server-side validation.
        LagCompensatedValidationAccepted = 50,
        LagCompensatedValidationRejected = 51
    }

    /// <summary>
    /// Severity band for a <see cref="SyncHealthEvent"/>, letting the harness and UI separate routine
    /// signals from degradations and faults without hard-coding per-kind classification everywhere.
    /// </summary>
    public enum SyncHealthSeverity
    {
        /// <summary>Routine, expected signal (e.g. snapshot received, input accepted).</summary>
        Info = 0,

        /// <summary>Recoverable degradation (e.g. interpolation starved, snapshot dropped).</summary>
        Warning = 1,

        /// <summary>Fault requiring correction/recovery (e.g. snapshot gap, input rejected).</summary>
        Error = 2
    }
}
