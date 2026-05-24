using System;
using System.Collections.Generic;

namespace AbilityKit.Coordinator.Timeline
{
    /// <summary>
    /// Default View Timeline Implementation
    ///
    /// Design:
    /// - Manages entity interpolation states
    /// - Provides frame-based rendering with smooth interpolation
    /// - Supports frame seeking for replay
    /// </summary>
    public sealed class ViewTimeline : IViewTimeline
    {
        private readonly Dictionary<int, EntityInterpolationState> _interpolationStates = new();
        private bool _disposed;

        public double RenderTimeSeconds { get; private set; }
        public double InterpolationBackTimeSeconds { get; set; } = 0.1;
        public bool IsActive { get; private set; }

        // ============== Lifecycle ==============

        public void Start()
        {
            if (_disposed) return;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void Reset()
        {
            RenderTimeSeconds = 0;
            _interpolationStates.Clear();
        }

        // ============== Entity State Management ==============

        private EntityInterpolationState GetOrCreateState(int entityId)
        {
            if (!_interpolationStates.TryGetValue(entityId, out var state))
            {
                state = new EntityInterpolationState
                {
                    EntityId = entityId,
                    PositionBuffer = new VectorSampleBuffer(),
                    RotationBuffer = new ScalarSampleBuffer()
                };
                _interpolationStates[entityId] = state;
            }
            return state;
        }

        public void AddPositionSample(int entityId, double time, float x, float y, float z)
        {
            if (_disposed) return;

            var state = GetOrCreateState(entityId);
            ((IVectorSampleBuffer)state.PositionBuffer).Add(time, x, y, z);
            state.CurrentX = x;
            state.CurrentY = y;
            state.CurrentZ = z;
        }

        public void AddRotationSample(int entityId, double time, float rotation)
        {
            if (_disposed) return;

            var state = GetOrCreateState(entityId);
            state.RotationBuffer.Add(time, rotation);
            state.CurrentRotation = rotation;
        }

        public void SetEntityDead(int entityId, bool isDead)
        {
            if (_disposed) return;

            var state = GetOrCreateState(entityId);
            state.IsDead = isDead;
        }

        public void RemoveEntity(int entityId)
        {
            _interpolationStates.Remove(entityId);
        }

        // ============== Interpolation Update ==============

        public void UpdateInterpolation(double renderTime)
        {
            if (_disposed || !IsActive) return;

            RenderTimeSeconds = renderTime;
            double targetTime = renderTime - InterpolationBackTimeSeconds;

            foreach (var kvp in _interpolationStates)
            {
                var state = kvp.Value;
                if (state.IsDead) continue;

                // Interpolate position
                if (((IVectorSampleBuffer)state.PositionBuffer).TryEvaluate(targetTime, out float x, out float y, out float z))
                {
                    state.RenderX = x;
                    state.RenderY = y;
                    state.RenderZ = z;
                }
                else
                {
                    state.RenderX = state.CurrentX;
                    state.RenderY = state.CurrentY;
                    state.RenderZ = state.CurrentZ;
                }

                // Interpolate rotation
                if (state.RotationBuffer.TryEvaluate(targetTime, out float rotation))
                {
                    state.RenderRotation = rotation;
                }
                else
                {
                    state.RenderRotation = state.CurrentRotation;
                }
            }
        }

        // ============== Frame Seeking ==============

        public void SeekToFrame(int frame, float secondsPerFrame = 1f / 30f)
        {
            if (_disposed) return;

            double targetTime = frame * secondsPerFrame;

            foreach (var kvp in _interpolationStates)
            {
                var state = kvp.Value;

                // Seek position
                if (((IVectorSampleBuffer)state.PositionBuffer).TryEvaluate(targetTime, out float x, out float y, out float z))
                {
                    state.RenderX = state.CurrentX = x;
                    state.RenderY = state.CurrentY = y;
                    state.RenderZ = state.CurrentZ = z;
                }

                // Seek rotation
                if (state.RotationBuffer.TryEvaluate(targetTime, out float rotation))
                {
                    state.RenderRotation = state.CurrentRotation = rotation;
                }
            }

            RenderTimeSeconds = targetTime;
        }

        // ============== State Query ==============

        public bool TryGetRenderPosition(int entityId, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (_disposed) return false;

            if (_interpolationStates.TryGetValue(entityId, out var state))
            {
                x = state.RenderX;
                y = state.RenderY;
                z = state.RenderZ;
                return true;
            }

            return false;
        }

        public bool TryGetRenderRotation(int entityId, out float rotation)
        {
            rotation = 0;

            if (_disposed) return false;

            if (_interpolationStates.TryGetValue(entityId, out var state))
            {
                rotation = state.RenderRotation;
                return true;
            }

            return false;
        }

        public bool TryGetCurrentPosition(int entityId, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (_disposed) return false;

            if (_interpolationStates.TryGetValue(entityId, out var state))
            {
                x = state.CurrentX;
                y = state.CurrentY;
                z = state.CurrentZ;
                return true;
            }

            return false;
        }

        // ============== IDisposable ==============

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _interpolationStates.Clear();
            IsActive = false;
        }
    }

    // ============== Sample Buffer Implementations ==============

    /// <summary>
    /// Scalar Sample Buffer (for rotation)
    /// </summary>
    public sealed class ScalarSampleBuffer : ISampleBuffer
    {
        private const int Capacity = 4;
        private readonly Sample[] _samples = new Sample[Capacity];
        private int _count;
        private int _head;

        public int Count => _count;

        public void Add(double time, float value)
        {
            var sample = new Sample { Time = time, Value = value };

            if (_count >= Capacity)
            {
                _head = (_head + 1) % Capacity;
            }
            else
            {
                _count++;
            }

            int insertIndex = (_head + _count - 1) % Capacity;
            _samples[insertIndex] = sample;
        }

        public bool TryEvaluate(double time, out float value)
        {
            value = 0;

            if (_count == 0) return false;

            Sample before = default;
            Sample after = default;
            bool hasBefore = false, hasAfter = false;

            for (int i = 0; i < _count; i++)
            {
                var sample = _samples[(_head + i) % Capacity];
                if (sample.Time <= time)
                {
                    before = sample;
                    hasBefore = true;
                    if (i + 1 < _count)
                    {
                        after = _samples[(_head + i + 1) % Capacity];
                        hasAfter = true;
                    }
                    break;
                }
                after = sample;
                hasAfter = true;
            }

            if (!hasBefore) { before = _samples[_head]; hasBefore = true; }
            if (!hasAfter) { after = _samples[(_head + _count - 1) % Capacity]; hasAfter = true; }

            if (!hasBefore || !hasAfter) return false;

            if (before.Time == after.Time)
            {
                value = before.Value;
                return true;
            }

            double t = (time - before.Time) / (after.Time - before.Time);
            value = before.Value + (float)(t * (after.Value - before.Value));
            return true;
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
        }

        private struct Sample
        {
            public double Time;
            public float Value;
        }
    }

    /// <summary>
    /// Vector Sample Buffer (for position)
    /// </summary>
    public sealed class VectorSampleBuffer : IVectorSampleBuffer
    {
        private const int Capacity = 4;
        private readonly VectorSample[] _samples = new VectorSample[Capacity];
        private int _count;
        private int _head;

        public int Count => _count;

        public void Add(double time, float x, float y, float z)
        {
            var sample = new VectorSample { Time = time, X = x, Y = y, Z = z };

            if (_count >= Capacity)
            {
                _head = (_head + 1) % Capacity;
            }
            else
            {
                _count++;
            }

            int insertIndex = (_head + _count - 1) % Capacity;
            _samples[insertIndex] = sample;
        }

        public bool TryEvaluate(double time, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (_count == 0) return false;

            VectorSample before = default;
            VectorSample after = default;
            bool hasBefore = false, hasAfter = false;

            for (int i = 0; i < _count; i++)
            {
                var sample = _samples[(_head + i) % Capacity];
                if (sample.Time <= time)
                {
                    before = sample;
                    hasBefore = true;
                    if (i + 1 < _count)
                    {
                        after = _samples[(_head + i + 1) % Capacity];
                        hasAfter = true;
                    }
                    break;
                }
                after = sample;
                hasAfter = true;
            }

            if (!hasBefore) { before = _samples[_head]; hasBefore = true; }
            if (!hasAfter) { after = _samples[(_head + _count - 1) % Capacity]; hasAfter = true; }

            if (!hasBefore || !hasAfter) return false;

            if (before.Time == after.Time)
            {
                x = before.X; y = before.Y; z = before.Z;
                return true;
            }

            double t = (time - before.Time) / (after.Time - before.Time);
            x = before.X + (float)(t * (after.X - before.X));
            y = before.Y + (float)(t * (after.Y - before.Y));
            z = before.Z + (float)(t * (after.Z - before.Z));
            return true;
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
        }

        private struct VectorSample
        {
            public double Time;
            public float X;
            public float Y;
            public float Z;
        }
    }
}
