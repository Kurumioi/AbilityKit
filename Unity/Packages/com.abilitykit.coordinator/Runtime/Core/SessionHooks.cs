using System;

namespace AbilityKit.Coordinator.Core
{
    /// <summary>
    /// Session Hooks
    ///
    /// Provides callback hooks for session lifecycle events.
    /// SubFeatures can subscribe to these hooks for coordination.
    /// </summary>
    public class SessionHooks
    {
        // ============== Session Lifecycle Hooks ==============

        /// <summary>
        /// Called before session starts
        /// </summary>
        public Action<SessionConfig> OnSessionStarting;

        /// <summary>
        /// Called when session starts
        /// </summary>
        public Action<SessionConfig> OnSessionStarted;

        /// <summary>
        /// Called when session stops
        /// </summary>
        public Action OnSessionStopping;

        /// <summary>
        /// Called when session ends
        /// </summary>
        public Action OnSessionStopped;

        /// <summary>
        /// Called when session encounters an error
        /// </summary>
        public Action<Exception> OnSessionFailed;

        // ============== Frame Sync Hooks ==============

        /// <summary>
        /// Called before each frame tick
        /// </summary>
        public Action<float> OnPreTick;

        /// <summary>
        /// Called after each frame tick
        /// </summary>
        public Action<float> OnPostTick;

        /// <summary>
        /// Called when first frame is received
        /// </summary>
        public Action OnFirstFrameReceived;

        // ============== View Hooks ==============

        /// <summary>
        /// Called when view binder is ready
        /// </summary>
        public Action<int> OnViewBinderReady;

        /// <summary>
        /// Called when views are rebound
        /// </summary>
        public Action OnViewsRebound;

        /// <summary>
        /// Called when frame is aligned for rendering
        /// </summary>
        public Action<int> OnViewFrameAligned;

        // ============== Helper Methods ==============

        /// <summary>
        /// Invoke session starting hook
        /// </summary>
        public void InvokeSessionStarting(SessionConfig config)
        {
            OnSessionStarting?.Invoke(config);
        }

        /// <summary>
        /// Invoke session started hook
        /// </summary>
        public void InvokeSessionStarted(SessionConfig config)
        {
            OnSessionStarted?.Invoke(config);
        }

        /// <summary>
        /// Invoke session stopping hook
        /// </summary>
        public void InvokeSessionStopping()
        {
            OnSessionStopping?.Invoke();
        }

        /// <summary>
        /// Invoke session stopped hook
        /// </summary>
        public void InvokeSessionStopped()
        {
            OnSessionStopped?.Invoke();
        }

        /// <summary>
        /// Invoke session failed hook
        /// </summary>
        public void InvokeSessionFailed(Exception ex)
        {
            OnSessionFailed?.Invoke(ex);
        }

        /// <summary>
        /// Invoke pre tick hook
        /// </summary>
        public void InvokePreTick(float deltaTime)
        {
            OnPreTick?.Invoke(deltaTime);
        }

        /// <summary>
        /// Invoke post tick hook
        /// </summary>
        public void InvokePostTick(float deltaTime)
        {
            OnPostTick?.Invoke(deltaTime);
        }

        /// <summary>
        /// Invoke first frame received hook
        /// </summary>
        public void InvokeFirstFrameReceived()
        {
            OnFirstFrameReceived?.Invoke();
        }

        /// <summary>
        /// Invoke view binder ready hook
        /// </summary>
        public void InvokeViewBinderReady(int frame)
        {
            OnViewBinderReady?.Invoke(frame);
        }

        /// <summary>
        /// Invoke views rebound hook
        /// </summary>
        public void InvokeViewsRebound()
        {
            OnViewsRebound?.Invoke();
        }

        /// <summary>
        /// Invoke view frame aligned hook
        /// </summary>
        public void InvokeViewFrameAligned(int frame)
        {
            OnViewFrameAligned?.Invoke(frame);
        }

        /// <summary>
        /// Clear all hooks
        /// </summary>
        public void Clear()
        {
            OnSessionStarting = null;
            OnSessionStarted = null;
            OnSessionStopping = null;
            OnSessionStopped = null;
            OnSessionFailed = null;
            OnPreTick = null;
            OnPostTick = null;
            OnFirstFrameReceived = null;
            OnViewBinderReady = null;
            OnViewsRebound = null;
            OnViewFrameAligned = null;
        }
    }
}
