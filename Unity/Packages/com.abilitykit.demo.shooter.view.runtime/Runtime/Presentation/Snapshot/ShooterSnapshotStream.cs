#nullable enable

using System;
using AbilityKit.Game.View.Presentation;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotStream : IViewStream<ShooterSnapshotViewBatch>
    {
        public const int DefaultBufferCapacity = 64;
        public const float DefaultPlaybackFramesPerSecond = 30f;
        public const float DefaultInterpolationDelayFrames = 2f;

        private readonly ShooterSnapshotViewBatch[] _buffer;
        private readonly ShooterSnapshotSamplingPolicy _samplingPolicy;
        private int _start;
        private int _count;
        private float _playbackFrame;
        private ShooterSnapshotViewBatchKey _lastSampledBatchKey;
        private bool _hasLastSampledBatchKey;
        private bool _playbackInitialized;

        public ShooterSnapshotStream()
            : this(DefaultBufferCapacity)
        {
        }

        public ShooterSnapshotStream(int bufferCapacity)
            : this(bufferCapacity, ShooterSnapshotSamplingPolicy.Default)
        {
        }

        public ShooterSnapshotStream(int bufferCapacity, ShooterSnapshotSamplingPolicy samplingPolicy)
        {
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));

            _buffer = new ShooterSnapshotViewBatch[bufferCapacity];
            _samplingPolicy = samplingPolicy ?? throw new ArgumentNullException(nameof(samplingPolicy));
            PlaybackFramesPerSecond = DefaultPlaybackFramesPerSecond;
            InterpolationDelayFrames = DefaultInterpolationDelayFrames;
        }

        public event Action<ShooterSnapshotViewBatch>? SnapshotApplied;

        event Action<ShooterSnapshotViewBatch>? IViewStream<ShooterSnapshotViewBatch>.BatchApplied
        {
            add => SnapshotApplied += value;
            remove => SnapshotApplied -= value;
        }

        public int BufferedSnapshotCount => _count;

        public int BufferCapacity => _buffer.Length;

        public float PlaybackFrame => _playbackFrame;

        public float PlaybackFramesPerSecond { get; set; }

        public float InterpolationDelayFrames { get; set; }

        public void Publish(in ShooterSnapshotViewBatch batch)
        {
            Store(in batch);
            SnapshotApplied?.Invoke(batch);
        }

        public bool TrySampleLatest(out ShooterSnapshotViewBatch batch)
        {
            if (_count == 0)
            {
                batch = default;
                return false;
            }

            batch = GetAt(_count - 1);
            return true;
        }

        public bool TrySample(float playbackFrame, out ShooterSnapshotViewBatch batch)
        {
            return TrySample(playbackFrame, out batch, out _);
        }

        public bool TrySample(float playbackFrame, out ShooterSnapshotViewBatch batch, out bool isContinuousSample)
        {
            if (!TryFindSampleWindow(playbackFrame, out var from, out var to))
            {
                batch = default;
                isContinuousSample = false;
                return false;
            }

            batch = _samplingPolicy.Sample(in from, in to, playbackFrame, out isContinuousSample);
            return true;
        }

        public bool TryAdvancePlayback(float deltaTime, out ShooterSnapshotViewBatch batch)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

            if (_count == 0)
            {
                batch = default;
                return false;
            }

            if (!_playbackInitialized)
            {
                var latest = GetAt(_count - 1);
                _playbackFrame = Math.Max(GetAt(0).Frame, latest.Frame - Math.Max(0f, InterpolationDelayFrames));
                _playbackInitialized = true;
            }
            else
            {
                _playbackFrame += deltaTime * Math.Max(0f, PlaybackFramesPerSecond);
            }

            if (!TrySample(_playbackFrame, out batch, out var isContinuousSample))
            {
                return false;
            }

            if (isContinuousSample)
            {
                return true;
            }

            var batchKey = ShooterSnapshotViewBatchKey.From(in batch);
            if (_hasLastSampledBatchKey && batchKey.Equals(_lastSampledBatchKey))
            {
                return false;
            }

            _lastSampledBatchKey = batchKey;
            _hasLastSampledBatchKey = true;
            return true;
        }

        public void Reset()
        {
            ReleaseStoredBatches();
            Array.Clear(_buffer, 0, _buffer.Length);
            _start = 0;
            _count = 0;
            _playbackFrame = 0f;
            _lastSampledBatchKey = default;
            _hasLastSampledBatchKey = false;
            _playbackInitialized = false;
        }

        private bool TryFindSampleWindow(float playbackFrame, out ShooterSnapshotViewBatch from, out ShooterSnapshotViewBatch to)
        {
            if (_count == 0)
            {
                from = default;
                to = default;
                return false;
            }

            from = GetAt(0);
            to = from;
            for (var i = 0; i < _count; i++)
            {
                var candidate = GetAt(i);
                if (candidate.Frame > playbackFrame)
                {
                    to = candidate;
                    return true;
                }

                from = candidate;
                to = candidate;
            }

            return true;
        }

        private void Store(in ShooterSnapshotViewBatch batch)
        {
            var insertIndex = (_start + _count) % _buffer.Length;
            if (_count == _buffer.Length)
            {
                insertIndex = _start;
                _buffer[insertIndex].ReleasePooledResources();
                _start = (_start + 1) % _buffer.Length;
            }
            else
            {
                _count++;
            }

            _buffer[insertIndex] = batch;
        }

        private void ReleaseStoredBatches()
        {
            for (var i = 0; i < _count; i++)
            {
                GetAt(i).ReleasePooledResources();
            }
        }

        private ShooterSnapshotViewBatch GetAt(int index)
        {
            return _buffer[(_start + index) % _buffer.Length];
        }

        private readonly struct ShooterSnapshotViewBatchKey : IEquatable<ShooterSnapshotViewBatchKey>
        {
            private ShooterSnapshotViewBatchKey(ulong sequence, int frame, ShooterViewBatchSource source, ShooterViewSnapshotKind snapshotKind)
            {
                Sequence = sequence;
                Frame = frame;
                Source = source;
                SnapshotKind = snapshotKind;
            }

            private ulong Sequence { get; }

            private int Frame { get; }

            private ShooterViewBatchSource Source { get; }

            private ShooterViewSnapshotKind SnapshotKind { get; }

            public static ShooterSnapshotViewBatchKey From(in ShooterSnapshotViewBatch batch)
            {
                return new ShooterSnapshotViewBatchKey(batch.Sequence, batch.Frame, batch.Source, batch.SnapshotKind);
            }

            public bool Equals(ShooterSnapshotViewBatchKey other)
            {
                return Sequence == other.Sequence &&
                    Frame == other.Frame &&
                    Source == other.Source &&
                    SnapshotKind == other.SnapshotKind;
            }

            public override bool Equals(object? obj)
            {
                return obj is ShooterSnapshotViewBatchKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Sequence, Frame, Source, SnapshotKind);
            }
        }
    }
}
