#nullable enable

using System;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// A single gameplay-agnostic synchronization health signal emitted during one sync tick. Carriers
    /// surface these alongside <see cref="SyncReconciliationReport"/> so the demo harness can aggregate a
    /// uniform health picture (snapshot flow, interpolation, recovery, input, validation) instead of
    /// continuing to widen the reconciliation report. The event is intentionally lightweight: a kind, a
    /// severity, the frame it relates to and a single numeric payload whose meaning depends on the kind
    /// (e.g. replay tick count, dropped snapshot count, remapped frame).
    /// </summary>
    public readonly struct SyncHealthEvent : IEquatable<SyncHealthEvent>
    {
        public SyncHealthEvent(SyncHealthEventKind kind, SyncHealthSeverity severity, int frame = 0, long value = 0L)
        {
            if (frame < 0) throw new ArgumentOutOfRangeException(nameof(frame));

            Kind = kind;
            Severity = severity;
            Frame = frame;
            Value = value;
        }

        public SyncHealthEventKind Kind { get; }

        public SyncHealthSeverity Severity { get; }

        public int Frame { get; }

        public long Value { get; }

        public bool HasEvent => Kind != SyncHealthEventKind.None;

        public static SyncHealthEvent None { get; } = default;

        /// <summary>Creates an <see cref="SyncHealthSeverity.Info"/> event of the given kind.</summary>
        public static SyncHealthEvent Info(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Info, frame, value);
        }

        /// <summary>Creates a <see cref="SyncHealthSeverity.Warning"/> event of the given kind.</summary>
        public static SyncHealthEvent Warning(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Warning, frame, value);
        }

        /// <summary>Creates a <see cref="SyncHealthSeverity.Error"/> event of the given kind.</summary>
        public static SyncHealthEvent Error(SyncHealthEventKind kind, int frame = 0, long value = 0L)
        {
            return new SyncHealthEvent(kind, SyncHealthSeverity.Error, frame, value);
        }

        public bool Equals(SyncHealthEvent other)
        {
            return Kind == other.Kind &&
                   Severity == other.Severity &&
                   Frame == other.Frame &&
                   Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is SyncHealthEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Severity, Frame, Value);
        }

        public static bool operator ==(SyncHealthEvent left, SyncHealthEvent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SyncHealthEvent left, SyncHealthEvent right)
        {
            return !left.Equals(right);
        }
    }
}
