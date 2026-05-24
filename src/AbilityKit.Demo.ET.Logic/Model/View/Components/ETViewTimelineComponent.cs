using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// View Timeline Component
    ///
    /// Responsibilities:
    /// - Store entity interpolation states
    /// - Manage timeline for frame-based rendering
    /// - Support frame seeking and interpolation
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETViewTimelineComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Owner battle component reference
        /// </summary>
        public ETBattleComponent Owner { get; set; }

        /// <summary>
        /// Entity interpolation states (ActorId -> State)
        /// </summary>
        public Dictionary<int, EntityInterpolationState> InterpolationStates { get; set; }

        /// <summary>
        /// Current render time in seconds
        /// </summary>
        public double RenderTimeSeconds { get; set; }

        /// <summary>
        /// Interpolation delay (back time) in seconds
        /// </summary>
        public double InterpolationBackTimeSeconds { get; set; } = 0.1;

        /// <summary>
        /// Fixed delta time for frame calculation
        /// </summary>
        public float FixedDeltaSeconds { get; set; } = 1f / 30f;

        /// <summary>
        /// Is timeline active
        /// </summary>
        public bool IsActive { get; set; }

        public void Awake()
        {
            InterpolationStates = new Dictionary<int, EntityInterpolationState>();
            RenderTimeSeconds = 0;
            IsActive = false;
        }

        public void Destroy()
        {
            Owner = null;
            InterpolationStates?.Clear();
            InterpolationStates = null;
        }
    }

    /// <summary>
    /// Entity Interpolation State
    /// Stores position and rotation samples for interpolation
    /// </summary>
    public class EntityInterpolationState
    {
        /// <summary>
        /// Actor ID
        /// </summary>
        public int ActorId { get; set; }

        /// <summary>
        /// Position sample buffer
        /// </summary>
        public PositionSampleBuffer PositionBuffer { get; } = new();

        /// <summary>
        /// Rotation sample buffer
        /// </summary>
        public RotationSampleBuffer RotationBuffer { get; } = new();

        /// <summary>
        /// Current (confirmed) position
        /// </summary>
        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public float CurrentZ { get; set; }

        /// <summary>
        /// Current (confirmed) rotation
        /// </summary>
        public float CurrentRotation { get; set; }

        /// <summary>
        /// Render (interpolated) position
        /// </summary>
        public float RenderX { get; set; }
        public float RenderY { get; set; }
        public float RenderZ { get; set; }

        /// <summary>
        /// Render (interpolated) rotation
        /// </summary>
        public float RenderRotation { get; set; }

        /// <summary>
        /// Is entity dead
        /// </summary>
        public bool IsDead { get; set; }
    }

    /// <summary>
    /// Position Sample Buffer
    /// Fixed capacity ring buffer for position sampling
    /// </summary>
    public sealed class PositionSampleBuffer
    {
        private const int Capacity = 4;

        private readonly PositionSample[] _samples = new PositionSample[Capacity];
        private int _count;
        private int _head;

        public int Count => _count;

        public void Add(double time, float x, float y, float z)
        {
            var sample = new PositionSample
            {
                Time = time,
                X = x,
                Y = y,
                Z = z
            };

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

            if (_count == 0)
                return false;

            PositionSample before = default;
            PositionSample after = default;
            bool hasBefore = false;
            bool hasAfter = false;

            int head = _head;
            for (int i = 0; i < _count; i++)
            {
                var sample = _samples[(head + i) % Capacity];
                if (sample.Time <= time)
                {
                    before = sample;
                    hasBefore = true;
                    if (i + 1 < _count)
                    {
                        after = _samples[(head + i + 1) % Capacity];
                        hasAfter = true;
                    }
                    break;
                }
                after = sample;
                hasAfter = true;
            }

            if (!hasBefore)
            {
                before = _samples[head];
                hasBefore = true;
            }

            if (!hasAfter)
            {
                int lastIndex = (_head + _count - 1) % Capacity;
                after = _samples[lastIndex];
                hasAfter = true;
            }

            if (!hasBefore || !hasAfter)
                return false;

            if (before.Time == after.Time)
            {
                x = before.X;
                y = before.Y;
                z = before.Z;
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

        private struct PositionSample
        {
            public double Time;
            public float X;
            public float Y;
            public float Z;
        }
    }

    /// <summary>
    /// Rotation Sample Buffer
    /// Fixed capacity ring buffer for rotation sampling
    /// </summary>
    public sealed class RotationSampleBuffer
    {
        private const int Capacity = 4;

        private readonly RotationSample[] _samples = new RotationSample[Capacity];
        private int _count;
        private int _head;

        public int Count => _count;

        public void Add(double time, float rotation)
        {
            var sample = new RotationSample
            {
                Time = time,
                Rotation = rotation
            };

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

        public bool TryEvaluate(double time, out float rotation)
        {
            rotation = 0;

            if (_count == 0)
                return false;

            RotationSample before = default;
            RotationSample after = default;
            bool hasBefore = false;
            bool hasAfter = false;

            int head = _head;
            for (int i = 0; i < _count; i++)
            {
                var sample = _samples[(head + i) % Capacity];
                if (sample.Time <= time)
                {
                    before = sample;
                    hasBefore = true;
                    if (i + 1 < _count)
                    {
                        after = _samples[(head + i + 1) % Capacity];
                        hasAfter = true;
                    }
                    break;
                }
                after = sample;
                hasAfter = true;
            }

            if (!hasBefore)
            {
                before = _samples[head];
                hasBefore = true;
            }

            if (!hasAfter)
            {
                int lastIndex = (_head + _count - 1) % Capacity;
                after = _samples[lastIndex];
                hasAfter = true;
            }

            if (!hasBefore || !hasAfter)
                return false;

            if (before.Time == after.Time)
            {
                rotation = before.Rotation;
                return true;
            }

            double t = (time - before.Time) / (after.Time - before.Time);
            rotation = InterpolateAngle(before.Rotation, after.Rotation, (float)t);

            return true;
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
        }

        private static float InterpolateAngle(float from, float to, float t)
        {
            float delta = to - from;
            while (delta > 180) delta -= 360;
            while (delta < -180) delta += 360;
            return from + delta * t;
        }

        private struct RotationSample
        {
            public double Time;
            public float Rotation;
        }
    }
}
