using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Sync Adapter Interface
    ///
    /// Design:
    /// - Base interface for all sync adapters
    /// - Provides common properties and methods
    /// - Mode-specific behavior via marker interfaces
    /// </summary>
    public interface ISyncAdapter : IDisposable
    {
        // ============== Core Properties ==============

        /// <summary>
        /// Current synchronization mode
        /// </summary>
        Core.SyncMode Mode { get; }

        /// <summary>
        /// Current logic frame number
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// Logic time in seconds
        /// </summary>
        double LogicTimeSeconds { get; }

        /// <summary>
        /// Render time in seconds (for view interpolation)
        /// </summary>
        double RenderTimeSeconds { get; }

        /// <summary>
        /// Local player identifier
        /// </summary>
        int LocalPlayerId { get; }

        // ============== Core Events ==============

        /// <summary>
        /// Frame synchronization event (triggered each frame)
        /// </summary>
        event Action<int, double> OnFrameSync;

        // ============== Core Methods ==============

        /// <summary>
        /// Attach to session coordinator
        /// </summary>
        void Attach(ISessionCoordinator coordinator);

        /// <summary>
        /// Attach to session coordinator with driver host
        /// </summary>
        void Attach(ISessionCoordinator coordinator, IBattleDriverHost driverHost);

        /// <summary>
        /// Set the driver host after initial attachment
        /// </summary>
        void SetDriverHost(IBattleDriverHost driverHost);

        /// <summary>
        /// Frame update (called by tick loop)
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Submit local player input
        /// </summary>
        void SubmitInput(PlayerInput input);

        /// <summary>
        /// Get all entity states for rendering
        /// </summary>
        EntityState[] GetAllEntityStates();
    }

    // ============== Mode-Specific Interfaces ==============

    /// <summary>
    /// Local Sync Adapter (Lockstep mode)
    /// Marker interface for local-only sync adapters
    /// </summary>
    public interface ILocalSyncAdapter : ISyncAdapter
    {
        /// <summary>
        /// Always returns true for local mode
        /// </summary>
        bool IsConnected { get; }
    }

    /// <summary>
    /// Remote Sync Adapter (StateSync/Hybrid modes)
    /// Marker interface for adapters requiring network connection
    /// </summary>
    public interface IRemoteSyncAdapter : ISyncAdapter
    {
        /// <summary>
        /// Is connected to remote server
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connection state changed event
        /// </summary>
        event Action<bool> OnConnectionChanged;

        /// <summary>
        /// Entity state snapshot event (triggered when server snapshot received)
        /// </summary>
        event Action<EntityState[]> OnServerSnapshot;

        /// <summary>
        /// Connect to remote server
        /// </summary>
        void Connect(NetworkEndpoint endpoint, long roomId, long playerId);

        /// <summary>
        /// Disconnect from remote server
        /// </summary>
        void Disconnect();
    }

    /// <summary>
    /// Prediction Support Interface
    /// For adapters that support client-side prediction
    /// </summary>
    public interface IPredictionSyncAdapter : IRemoteSyncAdapter
    {
        /// <summary>
        /// Is prediction enabled
        /// </summary>
        bool IsPredictionEnabled { get; }

        /// <summary>
        /// Enable/disable prediction
        /// </summary>
        void SetPredictionEnabled(bool enabled);

        /// <summary>
        /// Get prediction ahead frames
        /// </summary>
        int PredictionAheadFrames { get; }

        /// <summary>
        /// Trigger reconciliation (rollback)
        /// </summary>
        void TriggerReconciliation(int confirmedFrame, EntityState[] serverState);
    }
}
