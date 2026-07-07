#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;

namespace AbilityKit.Coordinator
{
    public delegate void SessionConfigConfigurator(ref SessionConfig config);

    /// <summary>
    /// 将已创建的世界适配为 SessionCoordinator 主机契约。
    /// </summary>
    public sealed class ExistingWorldSessionCoordinatorHost : ISessionCoordinatorHost, ISessionCoordinatorConfigPolicy
    {
        private readonly ExistingWorldHost _worldHost;
        private readonly SessionConfigConfigurator? _configureSession;

        public ExistingWorldSessionCoordinatorHost(
            IWorld world,
            IEnumerable<object>? serviceOverrides = null,
            SessionConfigConfigurator? configureSession = null,
            bool initializeExistingWorld = false)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            var hostedWorld = new ExistingWorldOverlay(world, serviceOverrides, initializeExistingWorld);
            _worldHost = new ExistingWorldHost(hostedWorld);
            _configureSession = configureSession;
        }

        public ExistingWorldSessionCoordinatorHost(
            IWorld world,
            SessionConfigConfigurator? configureSession,
            params object[] serviceOverrides)
            : this(world, serviceOverrides, configureSession)
        {
        }

        public void ConfigureSession(ref SessionConfig config)
        {
            _configureSession?.Invoke(ref config);
        }

        public IWorldHost CreateWorldHost(SessionConfig config)
        {
            return _worldHost;
        }

        public void ConfigureWorldCreateOptions(in SessionConfig config, WorldCreateOptions options)
        {
        }

        public void RegisterServices(IWorld world, SessionConfig config)
        {
        }

        public void LoadConfig(IWorld world, SessionConfig config)
        {
        }

        public PlayerSpawnData[] CreatePlayerSpawnData(SessionConfig config)
        {
            return Array.Empty<PlayerSpawnData>();
        }

        private sealed class ExistingWorldOverlay : IWorld
        {
            private readonly IWorld _inner;
            private readonly ExistingWorldOverlayResolver _services;
            private readonly bool _initializeInner;

            public ExistingWorldOverlay(IWorld inner, IEnumerable<object>? serviceOverrides, bool initializeInner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _services = new ExistingWorldOverlayResolver(inner.Services, serviceOverrides);
                _initializeInner = initializeInner;
            }

            public WorldId Id => _inner.Id;
            public string WorldType => _inner.WorldType;
            public IWorldResolver Services => _services;

            public void Initialize()
            {
                if (_initializeInner)
                {
                    _inner.Initialize();
                }
            }

            public void Tick(float deltaTime)
            {
                _inner.Tick(deltaTime);
            }

            public void Dispose()
            {
            }
        }

        private sealed class ExistingWorldOverlayResolver : IWorldResolver
        {
            private readonly IWorldResolver _inner;
            private readonly object[] _serviceOverrides;

            public ExistingWorldOverlayResolver(IWorldResolver inner, IEnumerable<object>? serviceOverrides)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _serviceOverrides = serviceOverrides == null ? Array.Empty<object>() : ToArray(serviceOverrides);
            }

            public object Resolve(Type serviceType)
            {
                if (TryResolveOverride(serviceType, out var instance))
                {
                    return instance;
                }

                return _inner.Resolve(serviceType);
            }

            public T Resolve<T>()
            {
                if (TryResolveOverride(typeof(T), out var instance))
                {
                    return (T)instance;
                }

                return _inner.Resolve<T>();
            }

            public bool TryResolve(Type serviceType, out object instance)
            {
                if (TryResolveOverride(serviceType, out instance))
                {
                    return true;
                }

                return _inner.TryResolve(serviceType, out instance);
            }

            public bool TryResolve<T>(out T instance)
            {
                if (TryResolveOverride(typeof(T), out var resolved))
                {
                    instance = (T)resolved;
                    return true;
                }

                return _inner.TryResolve(out instance);
            }

            private bool TryResolveOverride(Type serviceType, out object instance)
            {
                if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

                for (int i = 0; i < _serviceOverrides.Length; i++)
                {
                    var candidate = _serviceOverrides[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (serviceType.IsInstanceOfType(candidate))
                    {
                        instance = candidate;
                        return true;
                    }
                }

                instance = default!;
                return false;
            }

            private static object[] ToArray(IEnumerable<object> services)
            {
                if (services is object[] array)
                {
                    return array;
                }

                return new List<object>(services).ToArray();
            }
        }

        private sealed class ExistingWorldHost : IWorldHost
        {
            private readonly ExistingWorldManager _manager;

            public ExistingWorldHost(IWorld world)
            {
                _manager = new ExistingWorldManager(world);
            }

            public IWorldManager Worlds => _manager;

            public IWorld CreateWorld(WorldCreateOptions options)
            {
                return _manager.World;
            }

            public bool DestroyWorld(WorldId id)
            {
                return false;
            }

            public bool TryGetWorld(WorldId id, out IWorld world)
            {
                return _manager.TryGet(id, out world);
            }

            public void Tick(float deltaTime)
            {
                _manager.Tick(deltaTime);
            }
        }

        private sealed class ExistingWorldManager : IWorldManager
        {
            private readonly IReadOnlyDictionary<WorldId, IWorld> _worlds;

            public ExistingWorldManager(IWorld world)
            {
                World = world ?? throw new ArgumentNullException(nameof(world));
                _worlds = new Dictionary<WorldId, IWorld> { [world.Id] = world };
            }

            public IWorld World { get; }
            public IReadOnlyDictionary<WorldId, IWorld> Worlds => _worlds;

            public IWorld Create(WorldCreateOptions options)
            {
                return World;
            }

            public bool TryGet(WorldId id, out IWorld world)
            {
                if (id.Equals(World.Id))
                {
                    world = World;
                    return true;
                }

                world = default!;
                return false;
            }

            public bool Destroy(WorldId id)
            {
                return false;
            }

            public void Tick(float deltaTime)
            {
                World.Tick(deltaTime);
            }

            public void DisposeAll()
            {
            }
        }
    }
}
