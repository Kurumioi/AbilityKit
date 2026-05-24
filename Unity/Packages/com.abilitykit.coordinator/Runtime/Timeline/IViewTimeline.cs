using System;

namespace AbilityKit.Coordinator.Timeline
{
    /// <summary>
    /// View Timeline Interface
    ///
    /// Design:
    /// - Manages entity interpolation states
    /// - Provides frame-based rendering with smooth interpolation
    /// - Supports frame seeking for replay
    /// </summary>
    public interface IViewTimeline : IDisposable
    {
        // ============== Properties ==============

        /// <summary>
        /// Current render time in seconds
        /// </summary>
        double RenderTimeSeconds { get; }

        /// <summary>
        /// Interpolation delay (back time) in seconds
        /// </summary>
        double InterpolationBackTimeSeconds { get; set; }

        /// <summary>
        /// Is timeline active
        /// </summary>
        bool IsActive { get; }

        // ============== Lifecycle ==============

        /// <summary>
        /// Start the timeline
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the timeline
        /// </summary>
        void Stop();

        /// <summary>
        /// Reset the timeline
        /// </summary>
        void Reset();

        // ============== Entity State Management ==============

        /// <summary>
        /// Add a position sample for an entity
        /// </summary>
        void AddPositionSample(int entityId, double time, float x, float y, float z);

        /// <summary>
        /// Add a rotation sample for an entity
        /// </summary>
        void AddRotationSample(int entityId, double time, float rotation);

        /// <summary>
        /// Mark entity as dead
        /// </summary>
        void SetEntityDead(int entityId, bool isDead);

        /// <summary>
        /// Remove entity state
        /// </summary>
        void RemoveEntity(int entityId);

        // ============== Interpolation Update ==============

        /// <summary>
        /// Update interpolation (call every frame)
        /// </summary>
        void UpdateInterpolation(double renderTime);

        // ============== Frame Seeking ==============

        /// <summary>
        /// Seek all entities to a specific frame
        /// </summary>
        void SeekToFrame(int frame, float secondsPerFrame);

        // ============== State Query ==============

        /// <summary>
        /// Get interpolated render position
        /// </summary>
        bool TryGetRenderPosition(int entityId, out float x, out float y, out float z);

        /// <summary>
        /// Get interpolated render rotation
        /// </summary>
        bool TryGetRenderRotation(int entityId, out float rotation);

        /// <summary>
        /// Get current (confirmed) position
        /// </summary>
        bool TryGetCurrentPosition(int entityId, out float x, out float y, out float z);
    }

    /// <summary>
    /// Entity Interpolation State
    /// Stores position and rotation samples for interpolation
    /// </summary>
    public class EntityInterpolationState
    {
        /// <summary>
        /// Entity ID
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Position sample buffer
        /// </summary>
        public IVectorSampleBuffer PositionBuffer { get; set; }

        /// <summary>
        /// Rotation sample buffer
        /// </summary>
        public ISampleBuffer RotationBuffer { get; set; }

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
    /// Sample Buffer Interface
    /// </summary>
    public interface ISampleBuffer
    {
        int Count { get; }
        void Add(double time, float value);
        bool TryEvaluate(double time, out float value);
        void Clear();
    }

    /// <summary>
    /// Vector Sample Buffer Interface (for position)
    /// </summary>
    public interface IVectorSampleBuffer
    {
        int Count { get; }
        void Add(double time, float x, float y, float z);
        bool TryEvaluate(double time, out float x, out float y, out float z);
        void Clear();
    }
}
