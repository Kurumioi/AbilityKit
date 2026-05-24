using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Coordinator;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Demo Coordinator Host
    ///
    /// Responsibilities:
    /// - Create HostRuntime (IWorldHost)
    /// - Register services
    /// - Load configuration files
    /// - Create player spawn data
    ///
    /// This bridges AbilityKit.Coordinator with ET's component system.
    /// </summary>
    public sealed class ETCoordinatorHost : ISessionCoordinatorHost
    {
        // ============== State ==============

        private readonly ITextAssetLoader _configLoader;
        private HostRuntime _hostRuntime;

        // ============== Constructor ==============

        public ETCoordinatorHost(ITextAssetLoader configLoader)
        {
            _configLoader = configLoader;
        }

        // ============== ISessionCoordinatorHost Implementation ==============

        /// <summary>
        /// Create WorldHost (HostRuntime)
        /// 
        /// Note: Full WorldManager integration requires moba.core initialization.
        /// For now, this creates a basic HostRuntime. The actual battle world
        /// is created by ETMobaBattleDriver.Initialize() using moba.core's bootstrap.
        /// </summary>
        public IWorldHost CreateWorldHost(SessionConfig config)
        {
            // Create HostRuntime with empty world manager
            // The actual battle world is created by ETMobaBattleDriver
            _hostRuntime = new HostRuntime(new EmptyWorldManager(), new HostRuntimeOptions());

            Log.Info($"[ETCoordinatorHost] Created HostRuntime");

            return _hostRuntime;
        }

        /// <summary>
        /// Register services to the world
        /// 
        /// Note: In the full integration, services are registered by moba.core's bootstrap module.
        /// This method is called to allow any additional platform-specific registrations.
        /// </summary>
        public void RegisterServices(IWorld world, SessionConfig config)
        {
            // Services are registered by moba.core's bootstrap module
            // This method is a hook for platform-specific registrations if needed
            Log.Info($"[ETCoordinatorHost] Services will be registered by moba.core for SyncMode={config.SyncMode}");
        }

        /// <summary>
        /// Load configuration files
        /// </summary>
        public void LoadConfig(IWorld world, SessionConfig config)
        {
            if (_configLoader == null)
            {
                Log.Warning("[ETCoordinatorHost] No config loader, skipping config load");
                return;
            }

            // Load character config
            LoadCharacterConfig();

            // Load skill config
            LoadSkillConfig();

            // Load buff config
            LoadBuffConfig();

            Log.Info("[ETCoordinatorHost] Configuration loaded");
        }

        /// <summary>
        /// Create player spawn data
        /// </summary>
        public PlayerSpawnData[] CreatePlayerSpawnData(SessionConfig config)
        {
            // Create test players with default positions
            return new[]
            {
                PlayerSpawnData.CreateLocalPlayer(1, 1001, 0f, 0f),
                PlayerSpawnData.CreateLocalPlayer(2, 1001, 20f, 20f),
            };
        }

        // ============== Public Methods ==============

        /// <summary>
        /// Get the HostRuntime
        /// </summary>
        public HostRuntime GetHostRuntime() => _hostRuntime;

        // ============== Private Methods ==============

        private void LoadCharacterConfig()
        {
            if (_configLoader.TryLoadText("characters.json", out string json) && !string.IsNullOrEmpty(json))
            {
                Log.Debug("[ETCoordinatorHost] Character config loaded");
            }
            else
            {
                Log.Debug("[ETCoordinatorHost] Character config not found, using defaults");
            }
        }

        private void LoadSkillConfig()
        {
            if (_configLoader.TryLoadText("skills.json", out string json) && !string.IsNullOrEmpty(json))
            {
                Log.Debug("[ETCoordinatorHost] Skill config loaded");
            }
            else
            {
                Log.Debug("[ETCoordinatorHost] Skill config not found, using defaults");
            }
        }

        private void LoadBuffConfig()
        {
            if (_configLoader.TryLoadText("buffs.json", out string json) && !string.IsNullOrEmpty(json))
            {
                Log.Debug("[ETCoordinatorHost] Buff config loaded");
            }
            else
            {
                Log.Debug("[ETCoordinatorHost] Buff config not found, using defaults");
            }
        }

        // ============== Empty World Manager (Placeholder) ==============

        private sealed class EmptyWorldManager : IWorldManager
        {
            public IReadOnlyDictionary<WorldId, IWorld> Worlds => null;

            public IWorld Create(WorldCreateOptions options) => null;

            public bool TryGet(WorldId id, out IWorld world)
            {
                world = null;
                return false;
            }

            public bool Destroy(WorldId id) => false;

            public void Tick(float deltaTime) { }

            public void DisposeAll() { }
        }
    }
}
