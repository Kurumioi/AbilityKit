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
    public void ConfigureRegistersRuntimePortWithSveltoEntityManager()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        Assert.True(container.TryResolve<IShooterEntityManager>(out var entities));
        Assert.True(container.TryResolve<ShooterBattleState>(out var state));
        Assert.True(container.TryResolve<IShooterBattleRules>(out var rules));
        Assert.True(container.TryResolve<IShooterBattleSimulation>(out var simulation));
        Assert.True(container.TryResolve<IShooterSveltoWorld>(out var shooterSveltoWorld));
        Assert.True(container.TryResolve<IShooterBattleRuntimePort>(out var runtime));
        Assert.True(container.TryResolve<ISveltoWorldContext>(out var svelto));
        Assert.IsType<ShooterBattleRules>(rules);
        Assert.IsType<ShooterBattleSimulation>(simulation);
        Assert.IsType<ShooterSveltoWorld>(shooterSveltoWorld);
        Assert.IsType<ShooterEntityManager>(entities);
        Assert.Same(svelto, shooterSveltoWorld.Context);
        Assert.Same(entities, state.Entities);

        var start = new ShooterStartGamePayload(
            "world-module",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.True(entities.HasPlayer(1));
        Assert.True(entities.TryGetPlayer(1, out var player));
        Assert.Equal(1, player.PlayerId);
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.Same(entities, container.Resolve<IShooterEntityManager>());
        Assert.Same(state, container.Resolve<ShooterBattleState>());
        Assert.Same(shooterSveltoWorld, container.Resolve<IShooterSveltoWorld>());
    }

    [Fact]
    public void ConfigureKeepsExplicitBattleRulesOverride()
    {
        var customRules = new ShooterBattleRules(
            playerSpeed: 9f,
            bulletSpeed: 21f,
            bulletLifeFrames: 7,
            hitRadius: 1.5f,
            hitDamage: 3);
        var container = new WorldContainerBuilder()
            .RegisterInstance<IShooterBattleRules>(customRules)
            .AddModule(new ShooterWorldModule())
            .Build();

        Assert.Same(customRules, container.Resolve<IShooterBattleRules>());
    }

    [Fact]
    public void RuntimeUsesInjectedBattleRulesForMovementAndProjectile()
    {
        var rules = new ShooterBattleRules(
            playerSpeed: 30f,
            bulletSpeed: 60f,
            bulletLifeFrames: 3,
            hitRadius: 0.45f,
            hitDamage: 1);
        var container = new WorldContainerBuilder()
            .RegisterInstance<IShooterBattleRules>(rules)
            .AddModule(new ShooterWorldModule())
            .Build();

        var runtime = container.Resolve<IShooterBattleRuntimePort>();
        var entities = container.Resolve<IShooterEntityManager>();
        var start = new ShooterStartGamePayload(
            "custom-rules",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.Equal(1, runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) }));
        Assert.True(runtime.Tick(0.5f));

        Assert.True(entities.TryGetPlayer(1, out var player));
        Assert.Equal(15f, player.X);
        Assert.True(entities.TryGetProjectile(1, out var projectile));
        Assert.Equal(60f, projectile.VelocityX);
        Assert.Equal(2, projectile.RemainingFrames);

        var snapshot = runtime.GetSnapshot();
        var fireEvent = Assert.Single(snapshot.Events);
        Assert.Equal((int)ShooterEventType.Fire, fireEvent.EventType);
        Assert.Equal(1, fireEvent.SourcePlayerId);
        Assert.Equal(0, fireEvent.TargetPlayerId);
        Assert.Equal(projectile.BulletId, fireEvent.BulletId);
    }

    [Fact]
    public void RuntimeEmitsNamedHitEventWhenProjectileHitsPlayer()
    {
        var rules = new ShooterBattleRules(
            playerSpeed: 0f,
            bulletSpeed: 0f,
            bulletLifeFrames: 3,
            hitRadius: 0.45f,
            hitDamage: 2);
        var container = new WorldContainerBuilder()
            .RegisterInstance<IShooterBattleRules>(rules)
            .AddModule(new ShooterWorldModule())
            .Build();

        var runtime = container.Resolve<IShooterBattleRuntimePort>();
        var entities = container.Resolve<IShooterEntityManager>();
        var start = new ShooterStartGamePayload(
            "hit-event",
            30,
            2,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 0.6f, 0f)
            });

        Assert.True(runtime.StartGame(in start));
        Assert.Equal(1, runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) }));
        Assert.True(runtime.Tick(0f));

        Assert.False(entities.TryGetProjectile(1, out _));
        Assert.True(entities.TryGetPlayer(2, out var target));
        Assert.Equal(ShooterGameplay.DefaultPlayerHp - 2, target.Hp);

        var snapshot = runtime.GetSnapshot();
        Assert.Equal(2, snapshot.Events.Length);
        Assert.Equal((int)ShooterEventType.Fire, snapshot.Events[0].EventType);
        Assert.Equal((int)ShooterEventType.Hit, snapshot.Events[1].EventType);
        Assert.Equal(1, snapshot.Events[1].SourcePlayerId);
        Assert.Equal(2, snapshot.Events[1].TargetPlayerId);
        Assert.Equal(1, snapshot.Events[1].BulletId);
        Assert.Equal(2, snapshot.Events[1].Value);
    }

    [Fact]
    public void ShooterAutoModuleUsesShooterStartupDomainOnly()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        Assert.True(container.TryResolve<IShooterEntityManager>(out _));
        Assert.False(container.TryResolve<AbilityKit.Demo.Moba.Tests.ForeignWorldService>(out _));
    }

    [Fact]
    public void RuntimeWritesSveltoEntitiesIncrementally()
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
            Array.Empty<byte>(),
            Array.Empty<ShooterPackedComponentChunk>());

        Assert.True(runtime.ImportPackedSnapshot(in emptySnapshot));
        Assert.Equal(0, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.Equal(0, svelto.EntitiesDB.Count<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles));
    }

    [Fact]
    public void BlueprintRegistrationCreatedWorldResolvesShooterSveltoServices()
    {
        var registry = new WorldTypeRegistry();
        ShooterWorldBlueprintsRegistration.RegisterAll(registry);
        var manager = new WorldManager(new RegistryWorldFactory(registry));

        var world = manager.Create(new WorldCreateOptions(new WorldId("shooter-world-1"), ShooterGameplay.WorldType));

        Assert.Equal(ShooterGameplay.WorldType, world.WorldType);
        Assert.True(world.Services.TryResolve<IShooterBattleRuntimePort>(out var runtime));
        Assert.True(world.Services.TryResolve<IShooterEntityManager>(out var entities));
        Assert.True(world.Services.TryResolve<ISveltoWorldContext>(out var svelto));
        Assert.True(world.Services.TryResolve<IShooterSveltoWorld>(out var shooterSveltoWorld));
        Assert.Same(svelto, shooterSveltoWorld.Context);
        Assert.Same(entities, world.Services.Resolve<ShooterBattleState>().Entities);

        var start = new ShooterStartGamePayload(
            "blueprint-world",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.True(entities.HasPlayer(1));
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));

        manager.DisposeAll();
    }

    [Fact]
    public void ShooterWorldHostCreatesAndDrivesBattleWorldRuntime()
    {
        var host = new ShooterWorldHost();
        var world = host.CreateBattleWorld("host-world-1");

        Assert.Equal(ShooterGameplay.WorldType, world.WorldType);
        Assert.True(host.TryGetBattleWorld("host-world-1", out var resolvedWorld));
        Assert.Same(world, resolvedWorld);
        Assert.True(world.Services.TryResolve<IShooterBattleRuntimePort>(out var runtime));
        Assert.True(world.Services.TryResolve<ISveltoWorldContext>(out var svelto));

        var start = new ShooterStartGamePayload(
            "host-world",
            30,
            1,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(runtime.StartGame(in start));
        Assert.Equal(0, runtime.CurrentFrame);

        host.Tick(1f / 30f);

        Assert.Equal(1, runtime.CurrentFrame);
        Assert.Equal(1, svelto.EntitiesDB.Count<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players));
        Assert.True(host.DestroyBattleWorld("host-world-1"));
        Assert.False(host.TryGetBattleWorld("host-world-1", out _));
    }
}
