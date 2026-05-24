using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Session Coordinator Host Interface
    ///
    /// Design:
    /// - Provides platform-specific implementations
    /// - Creates WorldHost, registers services, loads config
    /// - Implemented by game projects (ET Demo, Console Demo, etc.)
    /// </summary>
    public interface ISessionCoordinatorHost
    {
        /// <summary>
        /// Create a world host instance
        /// </summary>
        IWorldHost CreateWorldHost(SessionConfig config);

        /// <summary>
        /// Register services to the world
        /// </summary>
        void RegisterServices(IWorld world, SessionConfig config);

        /// <summary>
        /// Load session configuration
        /// </summary>
        void LoadConfig(IWorld world, SessionConfig config);

        /// <summary>
        /// Create player spawn data
        /// </summary>
        PlayerSpawnData[] CreatePlayerSpawnData(SessionConfig config);
    }
}
