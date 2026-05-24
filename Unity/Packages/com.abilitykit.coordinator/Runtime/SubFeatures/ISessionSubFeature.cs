using System;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// Session SubFeature Interface
    ///
    /// Design:
    /// - SubFeatures are modular components that extend session functionality
    /// - Each SubFeature handles a specific aspect of the session lifecycle
    /// - SubFeatures are attached to session and receive lifecycle callbacks
    /// </summary>
    public interface ISessionSubFeature
    {
        /// <summary>
        /// SubFeature name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Priority (higher is called first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Called when the SubFeature is attached to a session
        /// </summary>
        void OnAttach(ISessionHost host);

        /// <summary>
        /// Called when the SubFeature is detached from a session
        /// </summary>
        void OnDetach();

        /// <summary>
        /// Called each frame when the session is running
        /// </summary>
        void OnTick(float deltaTime);
    }

    /// <summary>
    /// Session SubFeature with PreTick support
    /// </summary>
    public interface ISessionPreTickSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Called before the main session tick
        /// </summary>
        void OnPreTick(float deltaTime);
    }

    /// <summary>
    /// Session SubFeature with PostTick support
    /// </summary>
    public interface ISessionPostTickSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Called after the main session tick
        /// </summary>
        void OnPostTick(float deltaTime);
    }

    /// <summary>
    /// Session SubFeature with Lifecycle support
    /// </summary>
    public interface ISessionLifecycleSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Called when the session is about to start
        /// </summary>
        void OnSessionStarting();

        /// <summary>
        /// Called when the session is about to stop
        /// </summary>
        void OnSessionStopping();
    }

    /// <summary>
    /// Session Events SubFeature Interface
    /// For SubFeatures that need to raise session lifecycle events
    /// </summary>
    public interface ISessionEventsSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Called when session start is requested
        /// </summary>
        void OnStartSessionRequested();

        /// <summary>
        /// Raise session started event
        /// </summary>
        void RaiseSessionStarted();

        /// <summary>
        /// Raise session failed event
        /// </summary>
        void RaiseSessionFailed(Exception ex);
    }

    /// <summary>
    /// Session TickLoop SubFeature Interface
    /// For SubFeatures that need to drive the main tick loop
    /// </summary>
    public interface ISessionTickLoopSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Called during main tick phase
        /// </summary>
        void OnMainTick(float deltaTime);

        /// <summary>
        /// Start the tick loop
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the tick loop
        /// </summary>
        void Stop();

        /// <summary>
        /// Check if running
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// Session Snapshot Routing SubFeature Interface
    /// For SubFeatures that need to handle frame snapshots
    /// </summary>
    public interface ISessionSnapshotRoutingSubFeature : ISessionSubFeature
    {
        /// <summary>
        /// Initialize routing
        /// </summary>
        void InitializeRouting();

        /// <summary>
        /// Called when a frame is received
        /// </summary>
        void OnFrameReceived(int frameIndex);
    }
}
