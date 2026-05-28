using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Coordinator;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World;
using AbilityKit.Demo.Moba.Worlds.Blueprints;

namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// MOBA 会话 Coordinator Host（跨平台复用）
    /// 负责创建 HostRuntime、配置世界蓝图与 moba.core 服务
    /// </summary>
    public sealed class MobaSessionCoordinatorHost : ISessionCoordinatorHost
    {
        private readonly ITextAssetLoader _textAssetLoader;
        private HostRuntime _hostRuntime;
        private PlayerSpawnData[] _pendingSpawns;

        public MobaSessionCoordinatorHost(ITextAssetLoader textAssetLoader)
        {
            _textAssetLoader = textAssetLoader;
        }

        public HostRuntime HostRuntime => _hostRuntime;

        public void SetPendingSpawns(PlayerSpawnData[] spawns)
        {
            _pendingSpawns = spawns;
        }

        public IWorldHost CreateWorldHost(SessionConfig config)
        {
            var worldTypeRegistry = new WorldTypeRegistry();
            MobaWorldBlueprintsRegistration.RegisterAll(worldTypeRegistry, options => new EntitasWorld(options));

            var worldManager = new WorldManager(new RegistryWorldFactory(worldTypeRegistry));
            _hostRuntime = new HostRuntime(worldManager, new HostRuntimeOptions());

            Log.Info("[MobaSessionCoordinatorHost] HostRuntime created with moba world blueprints");
            return _hostRuntime;
        }

        public void ConfigureWorldCreateOptions(in SessionConfig config, WorldCreateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Id = new WorldId(config.WorldId > 0 ? config.WorldId.ToString() : "1");
            options.WorldType = string.IsNullOrEmpty(config.WorldType)
                ? MobaBattleWorldBlueprint.Type
                : config.WorldType;

            options.ServiceBuilder ??= WorldServiceContainerFactory.CreateDefaultOnly();

            if (_textAssetLoader != null)
            {
                options.ServiceBuilder.TryRegister<ITextAssetLoader>(
                    WorldLifetime.Singleton,
                    _ => _textAssetLoader);
            }

            options.ServiceBuilder.TryRegister<IMobaConfigTableRegistry>(
                WorldLifetime.Singleton,
                _ => MobaConfigRegistry.Instance);

            options.ServiceBuilder.TryRegister<ICollisionService>(
                WorldLifetime.Singleton,
                _ => new CollisionService());
        }

        public void RegisterServices(IWorld world, SessionConfig config)
        {
            if (world?.Services == null)
            {
                return;
            }

            Log.Info($"[MobaSessionCoordinatorHost] World services ready, SyncMode={config.SyncMode}");
        }

        public void LoadConfig(IWorld world, SessionConfig config)
        {
            if (world?.Services == null)
            {
                return;
            }

            if (world.Services.TryResolve<MobaConfigDatabase>(out var database))
            {
                Log.Info($"[MobaSessionCoordinatorHost] MobaConfigDatabase loaded, tables={database != null}");
            }
            else
            {
                Log.Warning("[MobaSessionCoordinatorHost] MobaConfigDatabase not resolved");
            }
        }

        public PlayerSpawnData[] CreatePlayerSpawnData(SessionConfig config)
        {
            if (_pendingSpawns != null && _pendingSpawns.Length > 0)
            {
                return _pendingSpawns;
            }

            return new[]
            {
                PlayerSpawnData.CreateLocalPlayer(config.LocalPlayerId, 1001, 0f, 0f)
            };
        }
    }
}
