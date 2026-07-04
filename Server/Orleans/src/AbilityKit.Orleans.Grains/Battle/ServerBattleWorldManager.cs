using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Orleans.Grains.Gameplay;
using Microsoft.Extensions.Logging;
using IWorldStateSnapshotProvider = AbilityKit.Ability.Host.IWorldStateSnapshotProvider;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// Orleans battle host 使用的服务器侧玩法世界管理器。
/// 它同时注册 MOBA 与 Shooter 世界蓝图，并让房间到世界的生命周期状态保持玩法无关。
/// </summary>
public sealed class ServerBattleWorldManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly WorldTypeRegistry _worldRegistry;
    private readonly RegistryWorldFactory _worldFactory;
    private readonly WorldManager _worldManager;
    private readonly Dictionary<string, IWorld> _worlds = new();
    private readonly object _lock = new();

    public ServerBattleWorldManager(ILogger logger)
    {
        _logger = logger;
        var baseFactory = new SimpleWorldFactory();
        _worldRegistry = new WorldTypeRegistry();

        var gameplayModules = ServerGameplayModuleCatalog.Default;
        var blueprintRegistry = new WorldBlueprintRegistry();
        foreach (var blueprint in gameplayModules.CreateWorldBlueprints())
        {
            blueprintRegistry.Register(blueprint);
        }

        RegisterWorldTypes(_worldRegistry, baseFactory.Create, blueprintRegistry, gameplayModules.GetWorldTypes());

        _worldFactory = new RegistryWorldFactory(_worldRegistry);
        _worldManager = new WorldManager(_worldFactory);

        _logger.LogInformation("[ServerBattleWorldManager] Initialized");
    }

    public IWorld CreateBattleWorld(string roomId, int tickRate)
    {
        lock (_lock)
        {
            if (_worlds.TryGetValue(roomId, out var existingWorld))
            {
                _logger.LogWarning("[ServerBattleWorldManager] World already exists for room: {RoomId}", roomId);
                return existingWorld;
            }

            return CreateBattleWorldCore(roomId, GetDefaultWorldType());
        }
    }

    public IWorld CreateBattleWorld(string roomId, string worldType, int tickRate)
    {
        return CreateBattleWorld(roomId, worldType, tickRate, configureOptions: null);
    }

    public IWorld CreateBattleWorld(string roomId, string worldType, int tickRate, Action<WorldCreateOptions>? configureOptions)
    {
        lock (_lock)
        {
            return CreateBattleWorldCore(roomId, string.IsNullOrWhiteSpace(worldType) ? GetDefaultWorldType() : worldType, configureOptions);
        }
    }

    private static string GetDefaultWorldType()
    {
        var defaultWorldType = ServerGameplayCatalog.Default.DefaultDescriptor.DefaultWorldType;
        if (string.IsNullOrWhiteSpace(defaultWorldType))
        {
            throw new InvalidOperationException("Default server gameplay world type is not configured.");
        }

        return defaultWorldType;
    }

    private IWorld CreateBattleWorldCore(string roomId, string worldType, Action<WorldCreateOptions>? configureOptions = null)
    {
        if (_worlds.TryGetValue(roomId, out var existingWorld))
        {
            _logger.LogWarning("[ServerBattleWorldManager] World already exists for room: {RoomId}", roomId);
            return existingWorld;
        }

        var options = new WorldCreateOptions
        {
            WorldType = worldType,
            Id = new WorldId(roomId)
        };
        configureOptions?.Invoke(options);

        var world = _worldManager.Create(options);

        _worlds[roomId] = world;
        _logger.LogInformation(
            "[ServerBattleWorldManager] Created battle world for room: {RoomId}, WorldType: {WorldType}, WorldId: {WorldId}",
            roomId,
            world.WorldType,
            world.Id);

        return world;
    }

    private static void RegisterWorldTypes(
        WorldTypeRegistry registry,
        Func<WorldCreateOptions, IWorld> baseFactory,
        WorldBlueprintRegistry blueprintRegistry,
        IReadOnlyList<string> worldTypes)
    {
        if (worldTypes.Count == 0)
        {
            throw new InvalidOperationException("At least one server battle world type must be registered.");
        }

        for (var i = 0; i < worldTypes.Count; i++)
        {
            var worldType = worldTypes[i];
            if (string.IsNullOrWhiteSpace(worldType))
            {
                continue;
            }

            registry.Register(worldType, options => CreateWorldFromBlueprint(baseFactory, blueprintRegistry, options));
        }
    }

    private static IWorld CreateWorldFromBlueprint(
        Func<WorldCreateOptions, IWorld> baseFactory,
        WorldBlueprintRegistry blueprintRegistry,
        WorldCreateOptions options)
    {
        blueprintRegistry.Configure(options);
        return baseFactory(options);
    }

    public bool TryGetBattleWorld(string roomId, out IWorld? world)
    {
        lock (_lock)
        {
            return _worlds.TryGetValue(roomId, out world);
        }
    }

    public IWorldStateSnapshotProvider? GetSnapshotProvider(string roomId)
    {
        lock (_lock)
        {
            if (!_worlds.TryGetValue(roomId, out var world))
            {
                return null;
            }

            return world.Services.Resolve<IWorldStateSnapshotProvider>();
        }
    }

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

    public bool DestroyBattleWorld(string roomId)
    {
        lock (_lock)
        {
            if (_worlds.ContainsKey(roomId))
            {
                _worlds.Remove(roomId);
                _worldManager.Destroy(new WorldId(roomId));
                _logger.LogInformation("[ServerBattleWorldManager] Destroyed battle world for room: {RoomId}", roomId);
                return true;
            }

            return false;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _worlds.Clear();
            _worldManager.DisposeAll();
        }

        _logger.LogInformation("[ServerBattleWorldManager] Disposed all worlds");
    }
}
