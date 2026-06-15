using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Demo.Moba.Worlds.Blueprints;
using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using Microsoft.Extensions.Logging;
using IWorldStateSnapshotProvider = AbilityKit.Ability.Host.IWorldStateSnapshotProvider;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// Server 端的 Moba 战斗世界管理器
/// 负责创建和管理 Moba 战斗世界
/// </summary>
public sealed class ServerMobaWorldManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly WorldTypeRegistry _worldRegistry;
    private readonly RegistryWorldFactory _worldFactory;
    private readonly WorldManager _worldManager;
    private readonly Dictionary<string, IWorld> _worlds = new();
    private readonly object _lock = new();

    public ServerMobaWorldManager(ILogger logger)
    {
        _logger = logger;

        // 创建基础工厂
        var baseFactory = new SimpleWorldFactory();

        // 创建 World 注册表
        _worldRegistry = new WorldTypeRegistry();
        
        // 注册逻辑 World Blueprint。Moba 使用 Entitas，Shooter 使用自己的 ECS 后端适配。
        var blueprintRegistry = MobaWorldBlueprintsRegistration.CreateDefaultRegistry();
        blueprintRegistry.Register(new ShooterBattleWorldBlueprint());
        MobaWorldBlueprintsRegistration.RegisterAll(
            _worldRegistry,
            baseFactory.Create,
            blueprintRegistry,
            MobaBattleWorldBlueprint.Type,
            MobaLobbyWorldBlueprint.Type,
            ShooterGameplay.WorldType);

        // 创建 World 工厂和管理器
        _worldFactory = new RegistryWorldFactory(_worldRegistry);
        _worldManager = new WorldManager(_worldFactory);
        
        _logger.LogInformation("[ServerMobaWorldManager] Initialized");
    }

    /// <summary>
    /// 创建战斗世界
    /// </summary>
    public IWorld CreateBattleWorld(string roomId, int tickRate)
    {
        lock (_lock)
        {
            if (_worlds.TryGetValue(roomId, out var existingWorld))
            {
                _logger.LogWarning("[ServerMobaWorldManager] World already exists for room: {RoomId}", roomId);
                return existingWorld;
            }

            return CreateBattleWorldCore(roomId, MobaBattleWorldBlueprint.Type);
        }
    }

    /// <summary>
    /// 创建指定玩法类型的战斗世界。
    /// </summary>
    public IWorld CreateBattleWorld(string roomId, string worldType, int tickRate)
    {
        lock (_lock)
        {
            return CreateBattleWorldCore(roomId, string.IsNullOrWhiteSpace(worldType) ? MobaBattleWorldBlueprint.Type : worldType);
        }
    }

    private IWorld CreateBattleWorldCore(string roomId, string worldType)
    {
        if (_worlds.TryGetValue(roomId, out var existingWorld))
        {
            _logger.LogWarning("[ServerMobaWorldManager] World already exists for room: {RoomId}", roomId);
            return existingWorld;
        }

        var options = new WorldCreateOptions
        {
            WorldType = worldType,
            Id = new WorldId(roomId)
        };

        IWorld world;
        if (string.Equals(worldType, ShooterGameplay.WorldType, StringComparison.Ordinal))
        {
            new ShooterBattleWorldBlueprint().Configure(options);
            world = new ShooterLogicWorld(options);
        }
        else
        {
            world = _worldManager.Create(options);
        }
        _worlds[roomId] = world;

        _logger.LogInformation("[ServerMobaWorldManager] Created battle world for room: {RoomId}, WorldType: {WorldType}, WorldId: {WorldId}",
            roomId, world.WorldType, world.Id);

        return world;
    }

    /// <summary>
    /// 获取战斗世界
    /// </summary>
    public bool TryGetBattleWorld(string roomId, out IWorld world)
    {
        lock (_lock)
        {
            return _worlds.TryGetValue(roomId, out world);
        }
    }

    /// <summary>
    /// 获取快照提供者
    /// </summary>
    public IWorldStateSnapshotProvider? GetSnapshotProvider(string roomId)
    {
        lock (_lock)
        {
            if (!_worlds.TryGetValue(roomId, out var world))
                return null;

            return world.Services.Resolve<IWorldStateSnapshotProvider>();
        }
    }

    /// <summary>
    /// Tick 世界
    /// </summary>
    public void TickWorld(string roomId, float deltaTime)
    {
        lock (_lock)
        {
            if (_worlds.TryGetValue(roomId, out var world))
            {
                world.Tick(deltaTime);
            }
        }
    }

    /// <summary>
    /// 销毁战斗世界
    /// </summary>
    public bool DestroyBattleWorld(string roomId)
    {
        lock (_lock)
        {
            if (_worlds.TryGetValue(roomId, out var world))
            {
                world.Dispose();
                _worlds.Remove(roomId);
                _logger.LogInformation("[ServerMobaWorldManager] Destroyed battle world for room: {RoomId}", roomId);
                return true;
            }
            return false;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var world in _worlds.Values)
            {
                world.Dispose();
            }
            _worlds.Clear();
            _worldManager.DisposeAll();
        }
        _logger.LogInformation("[ServerMobaWorldManager] Disposed all worlds");
    }
}
