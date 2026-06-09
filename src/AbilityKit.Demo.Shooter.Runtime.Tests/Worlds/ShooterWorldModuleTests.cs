using System;
using AbilityKit.Ability.Host.Builder;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Runtime;

public sealed class ShooterWorldModuleTests
{
    [Fact]
    public void ConfigureRegistersRuntimePortWithWorldEntityStore()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        Assert.True(container.TryResolve<IShooterEcsEntityStore>(out var store));
        Assert.True(container.TryResolve<ShooterBattleState>(out var state));
        Assert.True(container.TryResolve<IShooterBattleSimulation>(out var simulation));
        Assert.True(container.TryResolve<IShooterSveltoWorld>(out var shooterSveltoWorld));
        Assert.True(container.TryResolve<IShooterBattleRuntimePort>(out var runtime));
        Assert.True(container.TryResolve<ISveltoWorldContext>(out var svelto));
        Assert.IsType<ShooterSveltoEcsEntityStore>(store);
        Assert.IsType<ShooterBattleSimulation>(simulation);
        Assert.IsType<ShooterSveltoWorld>(shooterSveltoWorld);
        Assert.Same(store, shooterSveltoWorld.EntityStore);
        Assert.Same(svelto, shooterSveltoWorld.Context);

        var start = new ShooterStartGamePayload(
            "world-module",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.True(store.Players.ContainsKey(1));
        Assert.True(state.Players.ContainsKey(1));
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.Same(store, container.Resolve<IShooterEcsEntityStore>());
        Assert.Same(state, container.Resolve<ShooterBattleState>());
        Assert.Same(shooterSveltoWorld, container.Resolve<IShooterSveltoWorld>());
    }
    [Fact]
    public void RuntimeSyncsSveltoEntitiesIncrementally()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        var runtime = container.Resolve<IShooterBattleRuntimePort>();
        var svelto = container.Resolve<ISveltoWorldContext>();
        var start = new ShooterStartGamePayload(
            "incremental-sync",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.True(svelto.EntitiesDB.TryQueryMappedEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players, out var players));
        Assert.Equal(0f, players.Entity(1u).X);

        Assert.Equal(1, runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) }));
        Assert.True(runtime.Tick(1f / 30f));

        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles));
        players = svelto.EntitiesDB.QueryMappedEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
        var updatedPlayer = players.Entity(1u);
        Assert.True(updatedPlayer.X > 0f);
        Assert.True(svelto.EntitiesDB.Exists<ShooterSveltoProjectileComponent>(1u, ShooterSveltoGroups.Projectiles));

        var emptySnapshot = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            77ul,
            runtime.CurrentFrame,
            runtime.CurrentFrame,
            ShooterPackedSnapshotFlags.Full,
            0u,
            0,
            Array.Empty<ShooterPackedEntityChunk>(),
            Array.Empty<byte>());

        Assert.True(runtime.ImportPackedSnapshot(in emptySnapshot));
        Assert.Equal(0, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.Equal(0, svelto.EntitiesDB.Count<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles));
    }

    [Fact]
    public void BlueprintCreatedWorldResolvesShooterSveltoServices()
    {
        var blueprints = new WorldBlueprintRegistry()
            .Register(new ShooterBattleWorldBlueprint());
        var baseFactory = new RegistryWorldFactory(new WorldTypeRegistry()
            .Register(ShooterGameplay.WorldType, options => new TestWorld(options)));
        var manager = new WorldManager(new DefaultWorldFactory(blueprints, baseFactory));

        var world = manager.Create(new WorldCreateOptions(new WorldId("shooter-world-1"), ShooterGameplay.WorldType));

        Assert.Equal(ShooterGameplay.WorldType, world.WorldType);
        Assert.True(world.Services.TryResolve<IShooterBattleRuntimePort>(out var runtime));
        Assert.True(world.Services.TryResolve<ISveltoWorldContext>(out var svelto));
        Assert.True(world.Services.TryResolve<IShooterSveltoWorld>(out var shooterSveltoWorld));
        Assert.Same(svelto, shooterSveltoWorld.Context);

        var start = new ShooterStartGamePayload(
            "blueprint-world",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));

        manager.DisposeAll();
    }

    private sealed class TestWorld : IWorld
    {
        private readonly WorldContainer _container;

        public TestWorld(WorldCreateOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            Id = options.Id;
            WorldType = options.WorldType;
            var builder = options.ServiceBuilder ?? new WorldContainerBuilder();
            for (int i = 0; i < options.Modules.Count; i++)
            {
                builder.AddModule(options.Modules[i]);
            }

            _container = builder.Build();
        }

        public WorldId Id { get; }

        public string WorldType { get; }

        public IWorldResolver Services => _container;

        public void Initialize()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
