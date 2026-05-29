using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Battle Sync Adapter Interface
    ///
    /// Design:
    /// - Base interface for all sync adapters
    /// - Provides common properties and methods
    /// - Mode-specific behavior via marker interfaces
    /// </summary>
    public interface IETBattleSyncAdapter : IDisposable
    {
        // ============== Core Properties ==============

        /// <summary>
        /// Current synchronization mode
        /// </summary>
        SyncMode Mode { get; }

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
        /// Local player ActorId
        /// </summary>
        int LocalActorId { get; }

        // ============== Core Events ==============

        /// <summary>
        /// Frame synchronization event (triggered each frame)
        /// </summary>
        event Action<int, double> OnFrameSync;

        // ============== Core Methods ==============

        /// <summary>
        /// Initialize the sync adapter
        /// </summary>
        void Initialize(ETMobaBattleDriver driver, in BattleStartPlan plan);

        /// <summary>
        /// Frame update (called by tick loop)
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Submit local player input
        /// </summary>
        void SubmitInput(PlayerInputCommand input);

        /// <summary>
        /// Get all actor states for view rendering
        /// </summary>
        ActorStateSnapshotData[] GetAllActorStates();
    }

    // ============== Mode-Specific Interfaces ==============

    /// <summary>
    /// Local Sync Adapter (Lockstep mode)
    ///
    /// Marker interface for local-only sync adapters
    /// No network connection required
    /// </summary>
    public interface IETLocalSyncAdapter : IETBattleSyncAdapter
    {
        /// <summary>
        /// Always returns true for local mode
        /// </summary>
        bool IsConnected { get; }
    }

    /// <summary>
    /// Remote Sync Adapter (StateSync/Hybrid modes)
    ///
    /// Marker interface for adapters requiring network connection
    /// </summary>
    public interface IETRemoteSyncAdapter : IETBattleSyncAdapter
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
        /// Actor state snapshot event (triggered when server snapshot received)
        /// </summary>
        event Action<ActorStateSnapshotData[]> OnActorStateSnapshot;

        /// <summary>
        /// Connect to remote server
        /// </summary>
        void Connect(string host, int port, long roomId, long playerId);

        /// <summary>
        /// Disconnect from remote server
        /// </summary>
        void Disconnect();
    }

    // ============== Prediction Support Interface ==============

    /// <summary>
    /// Prediction Support Interface
    ///
    /// For adapters that support client-side prediction
    /// </summary>
    public interface IETPredictionSyncAdapter : IETBattleSyncAdapter
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
        void TriggerReconciliation(int confirmedFrame, ActorStateSnapshotData[] serverState);
    }

    // ============== Replay Support Interface ==============

    /// <summary>
    /// Replay Support Interface
    ///
    /// For adapters that support replay recording/playback
    /// </summary>
    public interface IETReplaySyncAdapter : IETBattleSyncAdapter
    {
        /// <summary>
        /// Is recording enabled
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Is playback enabled
        /// </summary>
        bool IsPlaybackEnabled { get; }

        /// <summary>
        /// Start recording
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Stop recording and return recorded data
        /// </summary>
        byte[] StopRecording();

        /// <summary>
        /// Start playback from recorded data
        /// </summary>
        void StartPlayback(byte[] recordedData);

        /// <summary>
        /// Stop playback
        /// </summary>
        void StopPlayback();

        /// <summary>
        /// Seek to specific frame during playback
        /// </summary>
        void SeekToFrame(int frame);
    }

    // ============== Data Structures ==============

    /// <summary>
    /// Actor state snapshot data
    /// </summary>
    public struct ActorStateSnapshotData
    {
        public int ActorId;
        public float X;
        public float Y;
        public float Z;
        public float Rotation;
        public float VelocityX;
        public float VelocityZ;
        public float Hp;
        public float HpMax;
        public int TeamId;
    }
}
