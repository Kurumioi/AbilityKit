using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// Session Host Interface
    ///
    /// Design:
    /// - SubFeatures access session functionality through this interface
    /// - Provides dependency injection point for SubFeatures
    /// </summary>
    public interface ISessionHost
    {
        // ============== Session Properties ==============

        /// <summary>
        /// Session state
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// Current frame number
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// Current logic time in seconds
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// Session configuration
        /// </summary>
        SessionConfig Config { get; }

        /// <summary>
        /// Session hooks
        /// </summary>
        SessionHooks Hooks { get; }

        // ============== SubFeature Management ==============

        /// <summary>
        /// Get a service from the session
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Try to get a service from the session
        /// </summary>
        bool TryGetService<T>(out T service) where T : class;

        /// <summary>
        /// Register a service
        /// </summary>
        void RegisterService<T>(T service) where T : class;
    }
}
