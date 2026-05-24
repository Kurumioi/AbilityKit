using System;
using System.IO;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Config.Core;

// Alias to avoid namespace conflicts
using MobaConfigRegistry = AbilityKit.Demo.Moba.Config.BattleDemo.MobaConfigRegistry;
using MobaConfigDatabase = AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase;
using DamagePipelineService = AbilityKit.Demo.Moba.Services.DamagePipelineService;
using MobaActorLookupService = AbilityKit.Demo.Moba.Services.MobaActorLookupService;
using MobaDamageService = AbilityKit.Demo.Moba.Services.MobaDamageService;
using MobaBuffService = AbilityKit.Demo.Moba.Services.MobaBuffService;
using MobaEffectExecutionService = AbilityKit.Demo.Moba.Services.MobaEffectExecutionService;
using SkillExecutor = AbilityKit.Demo.Moba.Services.SkillExecutor;
using IMobaSkillPipelineLibrary = AbilityKit.Demo.Moba.Services.IMobaSkillPipelineLibrary;
using MobaWorldBootstrapModule = AbilityKit.Demo.Moba.Systems.MobaWorldBootstrapModule;
using MO = AbilityKit.Demo.Moba.Config.BattleDemo.MO;

// Alias for AbilityKit.Core types to avoid conflict with ET.Log
using AKLog = AbilityKit.Core.Common.Log;

namespace ET.Logic
{
    /// <summary>
    /// moba.core World initializer
    /// Responsible for initializing moba.core World container and services in ET environment
    ///
    /// Uses the same pattern as Console Demo:
    /// 1. Register platform-specific TextAsset loader (ETTextAssetLoader)
    /// 2. Use MobaWorldBootstrapModule to initialize other services
    /// </summary>
    public sealed class MobaCoreWorldInitializer : IDisposable
    {
        private WorldContainer? _container;
        private bool _isInitialized;
        private bool _disposed;
        private readonly string _configResourcesDir;

        /// <summary>
        /// IWorldResolver interface for getting moba.core services
        /// </summary>
        public IWorldResolver Resolver => _container ?? throw new InvalidOperationException("World not initialized");

        /// <summary>
        /// Whether already initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configResourcesDir">Config resources directory (relative to config root, default "moba")</param>
        public MobaCoreWorldInitializer(string configResourcesDir = "moba")
        {
            _configResourcesDir = configResourcesDir;
        }

        /// <summary>
        /// Initialize moba.core World
        /// </summary>
        /// <returns>Whether initialization succeeded</returns>
        public bool Initialize()
        {
            if (_isInitialized)
            {
                global::ET.Log.Warning("[MobaCore] Already initialized");
                return true;
            }

            try
            {
                var configBasePath = GetConfigBasePath();
                global::ET.Log.Info($"[MobaCore] Initializing with config base path: {configBasePath}");

                // === Step 0: Configure AbilityKit.Core Log Sink ===
                if (AKLog.Log.Sink is AKLog.NullLogSink)
                {
                    var etSink = new ETLogSink();
                    AKLog.Log.SetSink(etSink);
                    global::ET.Log.Info("[MobaCore] AbilityKit.Core Log.Sink initialized with ETLogSink");
                }

                // Create WorldContainerBuilder
                var builder = new WorldContainerBuilder();

                // === Step 1: Register platform-specific TextAsset loader ===
                var textAssetLoader = new ETTextAssetLoader(configBasePath);
                builder.Register<ITextAssetLoader>(WorldLifetime.Singleton, _ => textAssetLoader);

                // === Step 2: Register config table registry ===
                builder.Register<IMobaConfigTableRegistry>(WorldLifetime.Singleton, _ => MobaConfigRegistry.Instance);

                // === Step 3: Add moba.core Bootstrap Module ===
                builder.AddModule(new MobaWorldBootstrapModule());

                // === Step 4: Build container ===
                _container = builder.Build();
                _isInitialized = true;

                global::ET.Log.Info("[MobaCore] moba.core World initialized successfully");

                // Verify critical services
                VerifyServices();

                return true;
            }
            catch (Exception ex)
            {
                global::ET.Log.Error($"[MobaCore] Failed to initialize: {ex}");
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Verify critical services are correctly registered
        /// </summary>
        private void VerifyServices()
        {
            if (_container == null) return;

            global::ET.Log.Info("[MobaCore] Verifying services...");

            VerifyService<DamagePipelineService>("DamagePipelineService");
            VerifyService<MobaActorLookupService>("MobaActorLookupService");
            VerifyService<MobaDamageService>("MobaDamageService");
            VerifyService<MobaBuffService>("MobaBuffService");
            VerifyService<MobaEffectExecutionService>("MobaEffectExecutionService");
            VerifyService<SkillExecutor>("SkillExecutor");
            VerifyService<IMobaSkillPipelineLibrary>("IMobaSkillPipelineLibrary");
            VerifyService<MobaConfigDatabase>("MobaConfigDatabase");

            global::ET.Log.Info("[MobaCore] Service verification complete");
        }

        private void VerifyService<T>(string name) where T : class
        {
            try
            {
                var service = _container?.Resolve<T>();
                global::ET.Log.Info($"[MobaCore] {name}: {(service != null ? "OK" : "NULL")}");
            }
            catch (Exception ex)
            {
                global::ET.Log.Warning($"[MobaCore] {name}: NOT_FOUND ({ex.Message})");
            }
        }

        /// <summary>
        /// Get DamagePipelineService
        /// </summary>
        public DamagePipelineService? GetDamageService()
        {
            if (_container == null) return null;
            try
            {
                return _container.Resolve<DamagePipelineService>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get MobaActorLookupService
        /// </summary>
        public MobaActorLookupService? GetActorLookupService()
        {
            if (_container == null) return null;
            try
            {
                return _container.Resolve<MobaActorLookupService>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get MobaConfigDatabase
        /// </summary>
        public MobaConfigDatabase? GetConfigDatabase()
        {
            if (_container == null) return null;
            try
            {
                return _container.Resolve<MobaConfigDatabase>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Try to resolve service
        /// </summary>
        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (_container == null) return false;
            try
            {
                service = _container.Resolve<T>();
                return service != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            global::ET.Log.Info("[MobaCore] Disposing moba.core World...");

            _container?.Dispose();
            _container = null;
            _isInitialized = false;

            global::ET.Log.Info("[MobaCore] moba.core World disposed");
        }

        /// <summary>
        /// Get config base path
        /// </summary>
        private string GetConfigBasePath()
        {
            // Try multiple possible paths
            var possiblePaths = new[]
            {
                // Relative to exe directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Configs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Configs"),
                // Current working directory
                Path.Combine(Environment.CurrentDirectory, "Configs"),
                Path.Combine(Environment.CurrentDirectory, "..", "Configs"),
                // Copy from Console Demo configs
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AbilityKit.Demo.Moba.Console", "Configs"),
            };

            foreach (var path in possiblePaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (Directory.Exists(normalizedPath))
                {
                    global::ET.Log.Info($"[MobaCore] Found config path: {normalizedPath}");
                    return normalizedPath;
                }
            }

            // Walk up the directory tree to find Configs
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                var configPath = Path.Combine(current.FullName, "Configs");
                if (Directory.Exists(configPath))
                {
                    global::ET.Log.Info($"[MobaCore] Found config path by walking up: {configPath}");
                    return configPath;
                }
                current = current.Parent;
            }

            // Return exe-level Configs as fallback
            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
            global::ET.Log.Warning($"[MobaCore] Config path not found, using fallback: {fallback}");
            return fallback;
        }
    }
}
