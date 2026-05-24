using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Session Coordinator Interface
    ///
    /// Design:
    /// - Main interface for session coordination
    /// - Manages world lifecycle, sync adapters, and subfeatures
    /// - Provides unified access to session resources
    /// </summary>
    public interface ISessionCoordinator : IDisposable
    {
        // ============== Identity ==============

        /// <summary>
        /// Session identifier
        /// </summary>
        SessionId SessionId { get; }

        /// <summary>
        /// Session configuration
        /// </summary>
        SessionConfig Config { get; }

        /// <summary>
        /// Current session state
        /// </summary>
        SessionState State { get; }

        // ============== World Access ==============

        /// <summary>
        /// World host instance
        /// </summary>
        IWorldHost WorldHost { get; }

        /// <summary>
        /// Current world instance
        /// </summary>
        IWorld World { get; }

        /// <summary>
        /// World resolver for service access
        /// </summary>
        IWorldResolver WorldResolver { get; }

        // ============== Sync ==============

        /// <summary>
        /// Sync adapter instance
        /// </summary>
        ISyncAdapter SyncAdapter { get; }

        /// <summary>
        /// View timeline for interpolation
        /// </summary>
        Timeline.IViewTimeline ViewTimeline { get; }

        // ============== Driver & View ==============

        /// <summary>
        /// Set the battle driver host
        /// </summary>
        void SetDriverHost(IBattleDriverHost driverHost);

        /// <summary>
        /// Get the battle driver host
        /// </summary>
        IBattleDriverHost? DriverHost { get; }

        /// <summary>
        /// Set the view event sink
        /// </summary>
        void SetViewEventSink(IViewEventSink sink);

        /// <summary>
        /// Get the view event sink
        /// </summary>
        IViewEventSink? ViewEventSink { get; }

        // ============== Lifecycle ==============

        /// <summary>
        /// Initialize the session coordinator
        /// </summary>
        void Initialize(SessionConfig config, ISessionCoordinatorHost host);

        /// <summary>
        /// Start the session
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the session
        /// </summary>
        void Stop();

        /// <summary>
        /// Destroy the session and release resources
        /// </summary>
        void Destroy();

        // ============== Input ==============

        /// <summary>
        /// Submit local player input
        /// </summary>
        void SubmitLocalInput(PlayerInput input);

        // ============== SubFeature Access ==============

        /// <summary>
        /// Get session hooks for event subscription
        /// </summary>
        SessionHooks Hooks { get; }

        /// <summary>
        /// Resolve a service from the world
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Try to resolve a service from the world
        /// </summary>
        bool TryResolve<T>(out T service) where T : class;
    }
}
