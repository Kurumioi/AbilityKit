using System;
using AbilityKit.Core.Common.Log;

namespace ET.Logic
{
    /// <summary>
    /// ETViewTimelineComponent System
    ///
    /// Responsibilities:
    /// - Manage entity interpolation states
    /// - Update render positions using interpolation
    /// - Handle frame seeking
    /// </summary>
    [EntitySystemOf(typeof(ETViewTimelineComponent))]
    [FriendOf(typeof(ETViewTimelineComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETViewTimelineComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETViewTimelineComponent self)
        {
            Log.Info("[ETViewTimeline] System awake");
            self.RenderTimeSeconds = 0;
            self.IsActive = false;
        }

        [EntitySystem]
        private static void Destroy(this ETViewTimelineComponent self)
        {
            Log.Info("[ETViewTimeline] System destroyed");
            if (self.InterpolationStates != null)
            {
                self.InterpolationStates.Clear();
            }
        }

        // ============== Lifecycle Methods ==============

        /// <summary>
        /// Attach to battle component
        /// </summary>
        public static void OnAttach(this ETViewTimelineComponent self, ETBattleComponent battle)
        {
            self.Owner = battle;
        }

        /// <summary>
        /// Detach from battle component
        /// </summary>
        public static void OnDetach(this ETViewTimelineComponent self, ETBattleComponent battle)
        {
            self.Owner = null;
        }

        /// <summary>
        /// Start the timeline
        /// </summary>
        public static void Start(this ETViewTimelineComponent self)
        {
            self.IsActive = true;
            Log.Info("[ETViewTimeline] Timeline started");
        }

        /// <summary>
        /// Stop the timeline
        /// </summary>
        public static void Stop(this ETViewTimelineComponent self)
        {
            self.IsActive = false;
            Log.Info("[ETViewTimeline] Timeline stopped");
        }

        /// <summary>
        /// Reset the timeline
        /// </summary>
        public static void Reset(this ETViewTimelineComponent self)
        {
            self.RenderTimeSeconds = 0;
            if (self.InterpolationStates != null)
            {
                self.InterpolationStates.Clear();
            }
            Log.Info("[ETViewTimeline] Timeline reset");
        }

        // ============== Interpolation State Management ==============

        /// <summary>
        /// Get or create interpolation state for an actor
        /// </summary>
        public static EntityInterpolationState GetOrCreateState(this ETViewTimelineComponent self, int actorId)
        {
            if (self.InterpolationStates == null)
            {
                return null;
            }

            if (!self.InterpolationStates.TryGetValue(actorId, out var state))
            {
                state = new EntityInterpolationState { ActorId = actorId };
                self.InterpolationStates[actorId] = state;
            }

            return state;
        }

        /// <summary>
        /// Remove interpolation state for an actor
        /// </summary>
        public static void RemoveState(this ETViewTimelineComponent self, int actorId)
        {
            if (self.InterpolationStates != null)
            {
                self.InterpolationStates.Remove(actorId);
            }
        }

        /// <summary>
        /// Clear all interpolation states
        /// </summary>
        public static void ClearAllStates(this ETViewTimelineComponent self)
        {
            if (self.InterpolationStates != null)
            {
                self.InterpolationStates.Clear();
            }
        }

        // ============== Position/Rotation Sampling ==============

        /// <summary>
        /// Add a position sample for an actor
        /// </summary>
        public static void AddPositionSample(this ETViewTimelineComponent self, int actorId, double time, float x, float y, float z)
        {
            var state = self.GetOrCreateState(actorId);
            if (state != null)
            {
                state.PositionBuffer.Add(time, x, y, z);
                state.CurrentX = x;
                state.CurrentY = y;
                state.CurrentZ = z;
            }
        }

        /// <summary>
        /// Add a rotation sample for an actor
        /// </summary>
        public static void AddRotationSample(this ETViewTimelineComponent self, int actorId, double time, float rotation)
        {
            var state = self.GetOrCreateState(actorId);
            if (state != null)
            {
                state.RotationBuffer.Add(time, rotation);
                state.CurrentRotation = rotation;
            }
        }

        // ============== Interpolation Update ==============

        /// <summary>
        /// Update render positions using interpolation
        /// Call this every frame with the current render time
        /// </summary>
        /// <param name="self">Timeline component</param>
        /// <param name="renderTime">Current render time</param>
        public static void UpdateInterpolation(this ETViewTimelineComponent self, double renderTime)
        {
            if (!self.IsActive || self.InterpolationStates == null)
                return;

            self.RenderTimeSeconds = renderTime;

            foreach (var kvp in self.InterpolationStates)
            {
                var state = kvp.Value;
                if (state.IsDead)
                    continue;

                double targetTime = renderTime - self.InterpolationBackTimeSeconds;

                if (state.PositionBuffer.TryEvaluate(targetTime, out float x, out float y, out float z))
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

        /// <summary>
        /// Seek all entities to a specific frame
        /// </summary>
        /// <param name="self">Timeline component</param>
        /// <param name="frame">Target frame</param>
        /// <param name="secondsPerFrame">Seconds per frame (default 1/30)</param>
        public static void SeekToFrame(this ETViewTimelineComponent self, int frame, float secondsPerFrame = 1f / 30f)
        {
            if (self.InterpolationStates == null)
                return;

            double targetTime = frame * secondsPerFrame;

            foreach (var kvp in self.InterpolationStates)
            {
                var state = kvp.Value;

                if (state.PositionBuffer.TryEvaluate(targetTime, out float x, out float y, out float z))
                {
                    state.RenderX = x;
                    state.RenderY = y;
                    state.RenderZ = z;
                    state.CurrentX = x;
                    state.CurrentY = y;
                    state.CurrentZ = z;
                }

                if (state.RotationBuffer.TryEvaluate(targetTime, out float rotation))
                {
                    state.RenderRotation = rotation;
                    state.CurrentRotation = rotation;
                }
            }

            self.RenderTimeSeconds = targetTime;
            Log.Info($"[ETViewTimeline] Seeked to frame {frame}");
        }

        // ============== Entity State Updates ==============

        /// <summary>
        /// Mark entity as dead
        /// </summary>
        public static void SetEntityDead(this ETViewTimelineComponent self, int actorId, bool isDead)
        {
            var state = self.GetOrCreateState(actorId);
            if (state != null)
            {
                state.IsDead = isDead;
            }
        }

        /// <summary>
        /// Get render position for an entity
        /// </summary>
        public static bool TryGetRenderPosition(this ETViewTimelineComponent self, int actorId, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (self.InterpolationStates == null)
                return false;

            if (self.InterpolationStates.TryGetValue(actorId, out var state))
            {
                x = state.RenderX;
                y = state.RenderY;
                z = state.RenderZ;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get render rotation for an entity
        /// </summary>
        public static bool TryGetRenderRotation(this ETViewTimelineComponent self, int actorId, out float rotation)
        {
            rotation = 0;

            if (self.InterpolationStates == null)
                return false;

            if (self.InterpolationStates.TryGetValue(actorId, out var state))
            {
                rotation = state.RenderRotation;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get current (confirmed) position for an entity
        /// </summary>
        public static bool TryGetCurrentPosition(this ETViewTimelineComponent self, int actorId, out float x, out float y, out float z)
        {
            x = y = z = 0;

            if (self.InterpolationStates == null)
                return false;

            if (self.InterpolationStates.TryGetValue(actorId, out var state))
            {
                x = state.CurrentX;
                y = state.CurrentY;
                z = state.CurrentZ;
                return true;
            }

            return false;
        }
    }
}
