using System;
using AbilityKit.Ability.Host.Builder;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.World.Svelto;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Gameplay;

public sealed class ShooterSveltoGameplayScenarioRunnerTests
{
    [Fact]
    public void JsonCatalogBuildsWaveSurvivalScenario()
    {
        var scenario = ShooterSveltoGameplayScenarioCatalog.WaveSurvival;

        Assert.Equal("svelto-wave-survival", scenario.Id);
        Assert.Equal("Svelto Wave Survival", scenario.DisplayName);
        Assert.Equal(4, scenario.ShooterCount);
        Assert.Equal(96, scenario.TargetCount);
        Assert.Equal(180, scenario.TickCount);
        Assert.Equal(1f / 30f, scenario.TickDeltaTime, 6);
        Assert.Equal(18f, scenario.ArenaRadius);
        Assert.Equal("burst-rifle", scenario.Loadout.Name);
        Assert.Equal(3, scenario.Loadout.ProjectilesPerShot);
        Assert.Equal(180, scenario.BattleFlow.DurationFrames);
        Assert.Equal(96, scenario.BattleFlow.VictoryTargetDefeats);
        Assert.Equal(36, scenario.BattleFlow.MaxActiveEnemies);
        Assert.Equal(3, scenario.BattleFlow.Waves.Length);
        Assert.Equal(30, scenario.BattleFlow.Waves[1].StartFrame);
        Assert.Equal(32, scenario.BattleFlow.Waves[2].EnemyCount);
        Assert.Equal(2, scenario.BattleFlow.Waves[0].EnemyHp);
    }

    [Fact]
    public void CustomScenarioJsonCanOverrideLoadoutAndBattleFlow()
    {
        const string json = @"
{
  ""id"": ""custom-json-scenario"",
  ""displayName"": ""Custom Json Scenario"",
  ""description"": ""通过 JSON 覆盖武器、时间、波次和敌人刷新参数。"",
  ""shooterCount"": 2,
  ""targetCount"": 12,
  ""tickCount"": 90,
  ""tickDeltaTime"": 0.05,
  ""arenaRadius"": 10.0,
  ""loadout"": {
    ""loadoutId"": 7,
    ""name"": ""json-shotgun"",
    ""projectileSpeed"": 12.5,
    ""projectileLifeFrames"": 24,
    ""damage"": 3,
    ""cooldownFrames"": 5,
    ""projectilesPerShot"": 5,
    ""spreadDegrees"": 18.0
  },
  ""battleFlow"": {
    ""durationFrames"": 90,
    ""victoryTargetDefeats"": 10,
    ""maxActiveEnemies"": 6,
    ""waves"": [
      { ""waveId"": 1, ""startFrame"": 0, ""spawnFrameInterval"": 3, ""enemyCount"": 4, ""enemyHp"": 2, ""spawnRadius"": 8.0 },
      { ""waveId"": 2, ""startFrame"": 30, ""spawnFrameInterval"": 4, ""enemyCount"": 6, ""enemyHp"": 5, ""spawnRadius"": 9.0 }
    ]
  }
}";

        var scenario = ShooterSveltoGameplayScenarioJsonParser.ParseScenario(json);

        Assert.Equal("custom-json-scenario", scenario.Id);
        Assert.Equal("Custom Json Scenario", scenario.DisplayName);
        Assert.Equal(2, scenario.ShooterCount);
        Assert.Equal(12, scenario.TargetCount);
        Assert.Equal(90, scenario.TickCount);
        Assert.Equal(0.05f, scenario.TickDeltaTime, 6);
        Assert.Equal(10f, scenario.ArenaRadius);
        Assert.Equal(7, scenario.Loadout.LoadoutId);
        Assert.Equal("json-shotgun", scenario.Loadout.Name);
        Assert.Equal(12.5f, scenario.Loadout.ProjectileSpeed);
        Assert.Equal(24, scenario.Loadout.ProjectileLifeFrames);
        Assert.Equal(3, scenario.Loadout.Damage);
        Assert.Equal(5, scenario.Loadout.CooldownFrames);
        Assert.Equal(5, scenario.Loadout.ProjectilesPerShot);
        Assert.Equal(18f, scenario.Loadout.SpreadDegrees);
        Assert.Equal(90, scenario.BattleFlow.DurationFrames);
        Assert.Equal(10, scenario.BattleFlow.VictoryTargetDefeats);
        Assert.Equal(6, scenario.BattleFlow.MaxActiveEnemies);
        Assert.Equal(2, scenario.BattleFlow.Waves.Length);
        Assert.Equal(30, scenario.BattleFlow.Waves[1].StartFrame);
        Assert.Equal(5, scenario.BattleFlow.Waves[1].EnemyHp);
    }

    [Fact]
    public void ScenarioJsonUsesDefaultLoadoutAndBattleFlowWhenOmitted()
    {
        const string json = @"
{
  ""id"": ""minimal-json-scenario"",
  ""displayName"": ""Minimal Json Scenario"",
  ""description"": ""只配置场景基础字段，其余玩法参数使用默认 JSON。"",
  ""shooterCount"": 1,
  ""targetCount"": 1,
  ""tickCount"": 1,
  ""tickDeltaTime"": 0.033333334,
  ""arenaRadius"": 8.0
}";

        var scenario = ShooterSveltoGameplayScenarioJsonParser.ParseScenario(json);

        Assert.Equal("minimal-json-scenario", scenario.Id);
        Assert.Equal(ShooterSveltoGameplayScenarioCatalog.DefaultLoadout.Name, scenario.Loadout.Name);
        Assert.Equal(ShooterSveltoGameplayBattleFlowConfig.Default.DurationFrames, scenario.BattleFlow.DurationFrames);
        Assert.Equal(ShooterSveltoGameplayBattleFlowConfig.Default.Waves.Length, scenario.BattleFlow.Waves.Length);
    }

    [Fact]
    public void ProjectileStormScenarioRunsWithStableDeterministicResult()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        var runner = container.Resolve<IShooterSveltoGameplayScenarioRunner>();
        var first = runner.Run(ShooterSveltoGameplayScenarioCatalog.ProjectileStorm);
        var second = runner.Run(ShooterSveltoGameplayScenarioCatalog.ProjectileStorm);

        Assert.Equal(ShooterSveltoGameplayScenarioCatalog.ProjectileStorm.Id, first.ScenarioId);
        Assert.Equal(first.ScenarioId, second.ScenarioId);
        Assert.Equal(first.Frames, second.Frames);
        Assert.Equal(first.Shooters, second.Shooters);
        Assert.Equal(first.Targets, second.Targets);
        Assert.Equal(first.ProjectilesSpawned, second.ProjectilesSpawned);
        Assert.Equal(first.ProjectilesExpired, second.ProjectilesExpired);
        Assert.Equal(first.Hits, second.Hits);
        Assert.Equal(first.DefeatedTargets, second.DefeatedTargets);
        Assert.Equal(first.ActiveProjectiles, second.ActiveProjectiles);
        Assert.Equal(first.RemainingTargetHp, second.RemainingTargetHp);
        Assert.Equal(first.StateHash, second.StateHash);
        Assert.Equal(64, first.Shooters);
        Assert.True(first.Targets > 0);
        Assert.True(first.ProjectilesSpawned > 0);
        Assert.True(first.Hits >= 0);
        Assert.True(first.StateHash != 0);
        Assert.True(container.Resolve<ISveltoWorldContext>().EntitiesDB.Count<ShooterSveltoTransformComponent>(ShooterSveltoGroups.GameplayShooters) > 0);
    }

    [Fact]
    public void WaveSurvivalSpawnsFrequentWeakEnemiesThatAttackAndCanBeDefeated()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        var runner = container.Resolve<IShooterSveltoGameplayScenarioRunner>();
        var result = runner.Run(ShooterSveltoGameplayScenarioCatalog.WaveSurvival);

        Assert.Equal(ShooterSveltoGameplayScenarioCatalog.WaveSurvival.Id, result.ScenarioId);
        Assert.True(result.Targets > 0);
        Assert.True(result.ProjectilesSpawned > ShooterSveltoGameplayScenarioCatalog.WaveSurvival.ShooterCount);
        Assert.True(result.Hits > 0);
        Assert.True(result.DefeatedTargets > 0);
        Assert.True(result.EnemyHits > 0);
        Assert.True(result.RemainingTargetHp < result.Targets * 3);
    }

    [Fact]
    public void BenchmarkProfileRunsScenarioRepeatedlyWithDeterministicOutcome()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        var runner = container.Resolve<IShooterSveltoGameplayScenarioRunner>();
        var profile = ShooterSveltoGameplayBenchmarkProfiles.ProjectileStormBaseline;
        var result = ShooterSveltoGameplayBenchmark.Run(runner, in profile);

        Assert.Equal(profile.Id, result.ProfileId);
        Assert.Equal(profile.Scenario.Id, result.ScenarioId);
        Assert.Equal(profile.Iterations, result.Iterations);
        Assert.Equal(profile.Scenario.TickCount, result.FramesPerIteration);
        Assert.Equal(profile.Scenario.ShooterCount + profile.Scenario.TargetCount, result.InitialEntityCount);
        Assert.Equal((long)profile.Iterations * profile.Scenario.TickCount, result.TotalFrames);
        Assert.Equal(result.TotalFrames * result.InitialEntityCount, result.TotalInitialEntityFrames);
        Assert.True(result.Deterministic);
        Assert.Equal(result.FirstResult.StateHash, result.LastResult.StateHash);
        Assert.True(result.LastResult.ProjectilesSpawned > 0);
        Assert.Equal(ShooterEntityLimitOptions.DefaultMaxEntityCount, result.EntityBudget.MaxEntityCount);
        Assert.Equal(ShooterEntityLimitOptions.DefaultMaxEntityCount, result.EntityBudget.ActiveSyncBudget);
        Assert.Equal(result.InitialEntityCount, result.EntityBudget.RequestedInitialEntityCount);
        Assert.Equal(result.InitialEntityCount, result.EntityBudget.ClampedInitialEntityCount);
        Assert.Equal(result.EntityBudget.MaxEntityCount - result.InitialEntityCount, result.EntityBudget.InitialEntityBudgetHeadroom);
        Assert.True(result.EntityBudget.InitialEntitiesWithinBudget);
        Assert.Equal(result.TotalFrames * result.EntityBudget.ActiveSyncBudget, result.EntityBudget.TotalActiveSyncBudgetFrames);
        Assert.True(result.EntityBudget.ElapsedTicks > 0);
        Assert.True(result.EntityBudget.AllocatedBytes >= 0);
        Assert.True(result.EntityBudget.AverageFrameTicks >= 0);
        Assert.True(result.EntityBudget.AverageFrameAllocatedBytes >= 0);
    }

    [Fact]
    public void LargeScaleEntityBudgetProfileRunsWithExplicitBudgetDiagnostics()
    {
        var container = new WorldContainerBuilder()
            .AddModule(new ShooterWorldModule())
            .Build();

        var runner = container.Resolve<IShooterSveltoGameplayScenarioRunner>();
        var profile = ShooterSveltoGameplayBenchmarkProfiles.LargeScaleEntityBudget;
        var result = ShooterSveltoGameplayBenchmark.Run(runner, in profile);

        Assert.Equal("svelto-large-scale-entity-budget", result.ProfileId);
        Assert.Equal(profile.Scenario.Id, result.ScenarioId);
        Assert.Equal(2, result.Iterations);
        Assert.Equal(4352, result.InitialEntityCount);
        Assert.Equal(ShooterEntityLimitOptions.DefaultMaxEntityCount, result.EntityBudget.MaxEntityCount);
        Assert.Equal(2048, result.EntityBudget.ActiveSyncBudget);
        Assert.Equal(result.InitialEntityCount, result.EntityBudget.RequestedInitialEntityCount);
        Assert.Equal(result.InitialEntityCount, result.EntityBudget.ClampedInitialEntityCount);
        Assert.Equal(5648, result.EntityBudget.InitialEntityBudgetHeadroom);
        Assert.True(result.EntityBudget.InitialEntitiesWithinBudget);
        Assert.Equal(result.TotalFrames * 2048, result.EntityBudget.TotalActiveSyncBudgetFrames);
        Assert.True(result.Deterministic);
        Assert.Equal(result.FirstResult.StateHash, result.LastResult.StateHash);
        Assert.True(result.LastResult.ProjectilesSpawned > 0);
    }
}
