using System;

namespace AbilityKit.Coordinator.Core
{
    /// <summary>
    /// Session state enumeration
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// Session created but not started
        /// </summary>
        Idle,

        /// <summary>
        /// Session is initializing
        /// </summary>
        Initializing,

        /// <summary>
        /// Session is running
        /// </summary>
        Running,

        /// <summary>
        /// Session is paused
        /// </summary>
        Paused,

        /// <summary>
        /// Session is stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Session has stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Session encountered an error
        /// </summary>
        Error
    }

    /// <summary>
    /// Sync mode enumeration
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// Local lockstep simulation (single player or LAN)
        /// </summary>
        Lockstep = 0,

        /// <summary>
        /// Server authoritative with snapshots
        /// </summary>
        SnapshotAuthority = 1,

        /// <summary>
        /// State synchronization from server
        /// </summary>
        StateSync = 2,

        /// <summary>
        /// Client prediction with server reconciliation
        /// </summary>
        Hybrid = 3
    }

    /// <summary>
    /// Host mode enumeration
    /// </summary>
    public enum HostMode
    {
        /// <summary>
        /// Single player or offline mode
        /// </summary>
        Local = 0,

        /// <summary>
        /// Host player in multiplayer
        /// </summary>
        Host = 1,

        /// <summary>
        /// Client player in multiplayer
        /// </summary>
        Client = 2
    }
}
