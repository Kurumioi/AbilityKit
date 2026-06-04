using System;
using System.Collections.Generic;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Session configuration
    /// </summary>
    public struct SessionConfig
    {
        // ============== Identity ==============

        /// <summary>
        /// Session identifier
        /// </summary>
        public SessionId SessionId;

        /// <summary>
        /// Map or level identifier
        /// </summary>
        public int MapId;

        /// <summary>
        /// World identifier
        /// </summary>
        public int WorldId;

        /// <summary>
        /// World blueprint type (e.g. moba "battle")
        /// </summary>
        public string WorldType;

        // ============== Player ==============

        /// <summary>
        /// Local player identifier
        /// </summary>
        public int LocalPlayerId;

        /// <summary>
        /// Client identifier
        /// </summary>
        public int ClientId;

        // ============== Sync Mode ==============

        /// <summary>
        /// Synchronization mode
        /// </summary>
        public SyncMode SyncMode;

        /// <summary>
        /// Host mode
        /// </summary>
        public HostMode HostMode;

        /// <summary>
        /// Target frame rate (frames per second)
        /// </summary>
        public int TickRate;

        // ============== Features ==============

        /// <summary>
        /// Require an ILogicWorldDriveGate before the coordinator or local sync adapter can drive the logic world.
        /// </summary>
        public bool RequireLogicWorldDriveGate;

        /// <summary>
        /// Create player spawns through ISpawnService when the coordinator starts.
        /// </summary>
        public bool UseCoordinatorSpawnService;

        /// <summary>
        /// Enable replay recording
        /// </summary>
        public bool EnableReplayRecording;

        /// <summary>
        /// Enable replay playback
        /// </summary>
        public bool EnableReplayPlayback;

        /// <summary>
        /// Enable client-side prediction
        /// </summary>
        public bool EnableClientPrediction;

        /// <summary>
        /// Maximum prediction ahead frames
        /// </summary>
        public int MaxPredictionAheadFrames;

        // ============== Network ==============

        /// <summary>
        /// Server endpoint for remote connections
        /// </summary>
        public NetworkEndpoint ServerEndpoint;

        /// <summary>
        /// Room identifier
        /// </summary>
        public long RoomId;

        // ============== SubFeature Config ==============

        /// <summary>
        /// SubFeature configuration list
        /// </summary>
        public List<SubFeatureConfigItem> SubFeatures;

        /// <summary>
        /// Resolve the effective runtime policy from user-facing session options.
        /// </summary>
        public SessionRuntimePolicy ResolveRuntimePolicy()
        {
            return SessionRuntimePolicy.FromConfig(in this);
        }

        // ============== Factory Methods ==============

        /// <summary>
        /// Default configuration with 30 FPS tick rate
        /// </summary>
        public static SessionConfig Default => new SessionConfig
        {
            SessionId = SessionId.New(),
            WorldType = "battle",
            TickRate = 30,
            SyncMode = SyncMode.Lockstep,
            HostMode = HostMode.Local,
            UseCoordinatorSpawnService = true,
            EnableReplayRecording = false,
            EnableReplayPlayback = false,
            EnableClientPrediction = false,
            MaxPredictionAheadFrames = 3,
            ServerEndpoint = NetworkEndpoint.None,
            RoomId = 0
        };

        /// <summary>
        /// Create local single-player configuration
        /// </summary>
        public static SessionConfig CreateLocal(int playerId, int mapId = 1, int tickRate = 30)
        {
            return new SessionConfig
            {
                SessionId = SessionId.New(),
                MapId = mapId,
                WorldId = 1,
                LocalPlayerId = playerId,
                ClientId = playerId,
                SyncMode = SyncMode.Lockstep,
                HostMode = HostMode.Local,
                TickRate = tickRate,
                UseCoordinatorSpawnService = true,
                EnableReplayRecording = false,
                EnableReplayPlayback = false,
                EnableClientPrediction = false,
                MaxPredictionAheadFrames = 3,
                ServerEndpoint = NetworkEndpoint.None,
                RoomId = 0
            };
        }

        /// <summary>
        /// Create state sync configuration for client
        /// </summary>
        public static SessionConfig CreateStateSyncClient(int playerId, string serverHost, int serverPort, long roomId = 0)
        {
            return new SessionConfig
            {
                SessionId = SessionId.New(),
                WorldId = 1,
                LocalPlayerId = playerId,
                ClientId = playerId,
                SyncMode = SyncMode.StateSync,
                HostMode = HostMode.Client,
                TickRate = 30,
                UseCoordinatorSpawnService = true,
                EnableReplayRecording = true,
                EnableReplayPlayback = false,
                EnableClientPrediction = false,
                MaxPredictionAheadFrames = 0,
                ServerEndpoint = new NetworkEndpoint(serverHost, serverPort),
                RoomId = roomId
            };
        }

        /// <summary>
        /// Create hybrid multiplayer configuration (client prediction)
        /// </summary>
        public static SessionConfig CreateHybrid(int playerId, string serverHost, int serverPort, long roomId = 0)
        {
            return new SessionConfig
            {
                SessionId = SessionId.New(),
                WorldId = 1,
                LocalPlayerId = playerId,
                ClientId = playerId,
                SyncMode = SyncMode.Hybrid,
                HostMode = HostMode.Client,
                TickRate = 30,
                UseCoordinatorSpawnService = true,
                EnableReplayRecording = true,
                EnableReplayPlayback = false,
                EnableClientPrediction = true,
                MaxPredictionAheadFrames = 3,
                ServerEndpoint = new NetworkEndpoint(serverHost, serverPort),
                RoomId = roomId
            };
        }

        /// <summary>
        /// Create host configuration (for LAN multiplayer)
        /// </summary>
        public static SessionConfig CreateHost(int playerId, int mapId = 1, int tickRate = 30)
        {
            return new SessionConfig
            {
                SessionId = SessionId.New(),
                MapId = mapId,
                WorldId = 1,
                LocalPlayerId = playerId,
                ClientId = playerId,
                SyncMode = SyncMode.Lockstep,
                HostMode = HostMode.Host,
                TickRate = tickRate,
                UseCoordinatorSpawnService = true,
                EnableReplayRecording = true,
                EnableReplayPlayback = false,
                EnableClientPrediction = false,
                MaxPredictionAheadFrames = 0,
                ServerEndpoint = NetworkEndpoint.None,
                RoomId = SessionId.New().Value
            };
        }
    }

    /// <summary>
    /// Effective runtime policy derived from SessionConfig.
    /// </summary>
    public readonly struct SessionRuntimePolicy
    {
        public readonly SyncMode RequestedSyncMode;
        public readonly SyncMode EffectiveSyncMode;
        public readonly HostMode HostMode;
        public readonly bool RequiresNetwork;
        public readonly bool SupportsPrediction;
        public readonly bool EnableClientPrediction;
        public readonly int MaxPredictionAheadFrames;
        public readonly bool RequireLogicWorldDriveGate;
        public readonly bool UseCoordinatorSpawnService;

        public SessionRuntimePolicy(
            SyncMode requestedSyncMode,
            SyncMode effectiveSyncMode,
            HostMode hostMode,
            bool requiresNetwork,
            bool supportsPrediction,
            bool enableClientPrediction,
            int maxPredictionAheadFrames,
            bool requireLogicWorldDriveGate,
            bool useCoordinatorSpawnService)
        {
            RequestedSyncMode = requestedSyncMode;
            EffectiveSyncMode = effectiveSyncMode;
            HostMode = hostMode;
            RequiresNetwork = requiresNetwork;
            SupportsPrediction = supportsPrediction;
            EnableClientPrediction = enableClientPrediction;
            MaxPredictionAheadFrames = maxPredictionAheadFrames;
            RequireLogicWorldDriveGate = requireLogicWorldDriveGate;
            UseCoordinatorSpawnService = useCoordinatorSpawnService;
        }

        public static SessionRuntimePolicy FromConfig(in SessionConfig config)
        {
            var effectiveSyncMode = config.HostMode == HostMode.Local
                ? SyncMode.Lockstep
                : config.SyncMode;
            var supportsPrediction = effectiveSyncMode == SyncMode.Hybrid;
            var enablePrediction = supportsPrediction && config.EnableClientPrediction;

            return new SessionRuntimePolicy(
                requestedSyncMode: config.SyncMode,
                effectiveSyncMode: effectiveSyncMode,
                hostMode: config.HostMode,
                requiresNetwork: effectiveSyncMode != SyncMode.Lockstep,
                supportsPrediction: supportsPrediction,
                enableClientPrediction: enablePrediction,
                maxPredictionAheadFrames: enablePrediction ? Math.Max(0, config.MaxPredictionAheadFrames) : 0,
                requireLogicWorldDriveGate: config.RequireLogicWorldDriveGate,
                useCoordinatorSpawnService: config.UseCoordinatorSpawnService);
        }
    }

    /// <summary>
    /// SubFeature configuration item
    /// </summary>
    public struct SubFeatureConfigItem
    {
        /// <summary>
        /// SubFeature type name
        /// </summary>
        public string TypeName;

        /// <summary>
        /// Is enabled
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Configuration data (JSON serialized)
        /// </summary>
        public string ConfigJson;

        public SubFeatureConfigItem(string typeName, bool enabled = true, string configJson = null)
        {
            TypeName = typeName;
            Enabled = enabled;
            ConfigJson = configJson;
        }
    }
}
