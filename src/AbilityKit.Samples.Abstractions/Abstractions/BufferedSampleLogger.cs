using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// Stable output protocol metadata shared by sample hosts and exporters.
    /// </summary>
    public static class SampleOutputContract
    {
        /// <summary>Current structured output schema version.</summary>
        public const string SchemaVersion = "sample-output.v1";
    }

    /// <summary>
    /// Structured log record kind.
    /// </summary>
    public enum SampleLogKind
    {
        /// <summary>Plain informational text.</summary>
        Info,
        /// <summary>Warning text.</summary>
        Warn,
        /// <summary>Error text.</summary>
        Error,
        /// <summary>Section title.</summary>
        Section,
        /// <summary>Blank line.</summary>
        Line,
        /// <summary>Visual divider.</summary>
        Divider,
        /// <summary>Bullet item.</summary>
        Bullet,
        /// <summary>Numbered item.</summary>
        Numbered,
        /// <summary>Key/value pair.</summary>
        KeyValue
    }

    /// <summary>
    /// Structured log entry captured by <see cref="BufferedSampleLogger"/>.
    /// </summary>
    public readonly struct SampleLogEntry
    {
        /// <summary>
        /// Creates a structured log entry.
        /// </summary>
        public SampleLogEntry(SampleLogKind kind, string text, string? key = null, int? number = null, int sequence = 0)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            Key = key ?? string.Empty;
            Number = number;
            Sequence = sequence;
        }

        /// <summary>Record kind.</summary>
        public SampleLogKind Kind { get; }
        /// <summary>Main text.</summary>
        public string Text { get; }
        /// <summary>Optional key for key/value records.</summary>
        public string Key { get; }
        /// <summary>Optional item number for numbered records.</summary>
        public int? Number { get; }
        /// <summary>Stable zero-based order within one sample run.</summary>
        public int Sequence { get; }
    }

    /// <summary>
    /// Logger that stores structured records for UI hosts.
    /// </summary>
    public sealed class BufferedSampleLogger : ILogger
    {
        private readonly List<SampleLogEntry> _entries = new();
        private int _nextSequence;

        /// <summary>
        /// Captured records.
        /// </summary>
        public IReadOnlyList<SampleLogEntry> Entries => _entries;

        /// <inheritdoc />
        public void Info(string message) => Add(SampleLogKind.Info, message);
        /// <inheritdoc />
        public void Warn(string message) => Add(SampleLogKind.Warn, message);
        /// <inheritdoc />
        public void Error(string message) => Add(SampleLogKind.Error, message);
        /// <inheritdoc />
        public void Section(string title) => Add(SampleLogKind.Section, title);
        /// <inheritdoc />
        public void Line() => Add(SampleLogKind.Line, string.Empty);
        /// <inheritdoc />
        public void Divider() => Add(SampleLogKind.Divider, string.Empty);
        /// <inheritdoc />
        public void Bullet(string text) => Add(SampleLogKind.Bullet, text);
        /// <inheritdoc />
        public void Numbered(int num, string text) => Add(SampleLogKind.Numbered, text, number: num);
        /// <inheritdoc />
        public void KeyValue(string key, string value) => Add(SampleLogKind.KeyValue, value, key);
        /// <inheritdoc />
        public void Flush() { }

        /// <summary>
        /// Clears all captured records.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _nextSequence = 0;
        }

        private void Add(SampleLogKind kind, string text, string? key = null, int? number = null)
        {
            _entries.Add(new SampleLogEntry(kind, text, key, number, _nextSequence++));
        }
    }
}
