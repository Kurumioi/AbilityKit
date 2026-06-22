#nullable enable

using System;
using System.Diagnostics;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public readonly struct ShooterSveltoGameplayBenchmarkProfile
    {
        public ShooterSveltoGameplayBenchmarkProfile(string id, string displayName, ShooterSveltoGameplayScenarioConfig scenario, int iterations)
            : this(id, displayName, scenario, iterations, ShooterSveltoGameplayEntityBudgetProfile.Default)
        {
        }

        public ShooterSveltoGameplayBenchmarkProfile(
            string id,
            string displayName,
            ShooterSveltoGameplayScenarioConfig scenario,
            int iterations,
            ShooterSveltoGameplayEntityBudgetProfile entityBudget)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Profile id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Profile display name is required.", nameof(displayName));

            Id = id;
            DisplayName = displayName;
            Scenario = scenario;
            Iterations = iterations < 1 ? 1 : iterations;
            EntityBudget = entityBudget.MaxEntityCount <= 0
                ? ShooterSveltoGameplayEntityBudgetProfile.Default
                : entityBudget;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public ShooterSveltoGameplayScenarioConfig Scenario { get; }
        public int Iterations { get; }
        public ShooterSveltoGameplayEntityBudgetProfile EntityBudget { get; }
    }

    public readonly struct ShooterSveltoGameplayEntityBudgetProfile
    {
        public ShooterSveltoGameplayEntityBudgetProfile(int maxEntityCount, int activeSyncBudget)
        {
            var limits = new ShooterEntityLimitOptions(maxEntityCount);
            MaxEntityCount = limits.MaxEntityCount;
            ActiveSyncBudget = activeSyncBudget < 1 ? MaxEntityCount : Math.Min(activeSyncBudget, MaxEntityCount);
        }

        public int MaxEntityCount { get; }
        public int ActiveSyncBudget { get; }
        public static ShooterSveltoGameplayEntityBudgetProfile Default => new ShooterSveltoGameplayEntityBudgetProfile(ShooterEntityLimitOptions.DefaultMaxEntityCount, ShooterEntityLimitOptions.DefaultMaxEntityCount);
    }

    public readonly struct ShooterSveltoGameplayEntityBudgetDiagnostics
    {
        public ShooterSveltoGameplayEntityBudgetDiagnostics(
            int maxEntityCount,
            int activeSyncBudget,
            int requestedInitialEntityCount,
            int clampedInitialEntityCount,
            int initialEntityBudgetHeadroom,
            bool initialEntitiesWithinBudget,
            long totalActiveSyncBudgetFrames)
            : this(
                maxEntityCount,
                activeSyncBudget,
                requestedInitialEntityCount,
                clampedInitialEntityCount,
                initialEntityBudgetHeadroom,
                initialEntitiesWithinBudget,
                totalActiveSyncBudgetFrames,
                0L,
                0L,
                0L,
                0L)
        {
        }

        public ShooterSveltoGameplayEntityBudgetDiagnostics(
            int maxEntityCount,
            int activeSyncBudget,
            int requestedInitialEntityCount,
            int clampedInitialEntityCount,
            int initialEntityBudgetHeadroom,
            bool initialEntitiesWithinBudget,
            long totalActiveSyncBudgetFrames,
            long elapsedTicks,
            long allocatedBytes,
            long averageFrameTicks,
            long averageFrameAllocatedBytes)
        {
            MaxEntityCount = maxEntityCount;
            ActiveSyncBudget = activeSyncBudget;
            RequestedInitialEntityCount = requestedInitialEntityCount;
            ClampedInitialEntityCount = clampedInitialEntityCount;
            InitialEntityBudgetHeadroom = initialEntityBudgetHeadroom;
            InitialEntitiesWithinBudget = initialEntitiesWithinBudget;
            TotalActiveSyncBudgetFrames = totalActiveSyncBudgetFrames;
            ElapsedTicks = elapsedTicks;
            AllocatedBytes = allocatedBytes;
            AverageFrameTicks = averageFrameTicks;
            AverageFrameAllocatedBytes = averageFrameAllocatedBytes;
        }

        public int MaxEntityCount { get; }
        public int ActiveSyncBudget { get; }
        public int RequestedInitialEntityCount { get; }
        public int ClampedInitialEntityCount { get; }
        public int InitialEntityBudgetHeadroom { get; }
        public bool InitialEntitiesWithinBudget { get; }
        public long TotalActiveSyncBudgetFrames { get; }
        public long ElapsedTicks { get; }
        public long AllocatedBytes { get; }
        public long AverageFrameTicks { get; }
        public long AverageFrameAllocatedBytes { get; }
    }

    public readonly struct ShooterSveltoGameplayBenchmarkResult
    {
        public ShooterSveltoGameplayBenchmarkResult(
            string profileId,
            string scenarioId,
            int iterations,
            int framesPerIteration,
            int initialEntityCount,
            long totalFrames,
            long totalInitialEntityFrames,
            bool deterministic,
            ShooterSveltoGameplayScenarioResult firstResult,
            ShooterSveltoGameplayScenarioResult lastResult,
            ShooterSveltoGameplayEntityBudgetDiagnostics entityBudget)
        {
            ProfileId = profileId;
            ScenarioId = scenarioId;
            Iterations = iterations;
            FramesPerIteration = framesPerIteration;
            InitialEntityCount = initialEntityCount;
            TotalFrames = totalFrames;
            TotalInitialEntityFrames = totalInitialEntityFrames;
            Deterministic = deterministic;
            FirstResult = firstResult;
            LastResult = lastResult;
            EntityBudget = entityBudget;
        }

        public string ProfileId { get; }
        public string ScenarioId { get; }
        public int Iterations { get; }
        public int FramesPerIteration { get; }
        public int InitialEntityCount { get; }
        public long TotalFrames { get; }
        public long TotalInitialEntityFrames { get; }
        public bool Deterministic { get; }
        public ShooterSveltoGameplayScenarioResult FirstResult { get; }
        public ShooterSveltoGameplayScenarioResult LastResult { get; }
        public ShooterSveltoGameplayEntityBudgetDiagnostics EntityBudget { get; }
    }

    public static class ShooterSveltoGameplayBenchmarkProfiles
    {
        public static ShooterSveltoGameplayBenchmarkProfile ProjectileStormBaseline { get; } = new ShooterSveltoGameplayBenchmarkProfile(
            "svelto-projectile-storm-baseline",
            "Svelto Projectile Storm Baseline",
            ShooterSveltoGameplayScenarioCatalog.ProjectileStorm,
            iterations: 3);

        public static ShooterSveltoGameplayBenchmarkProfile WaveSurvivalBaseline { get; } = new ShooterSveltoGameplayBenchmarkProfile(
            "svelto-wave-survival-baseline",
            "Svelto Wave Survival Baseline",
            ShooterSveltoGameplayScenarioCatalog.WaveSurvival,
            iterations: 3);

        public static ShooterSveltoGameplayBenchmarkProfile LargeScaleEntityBudget { get; } = new ShooterSveltoGameplayBenchmarkProfile(
            "svelto-large-scale-entity-budget",
            "Svelto Large Scale Entity Budget",
            new ShooterSveltoGameplayScenarioConfig(
                "svelto-large-scale-entity-budget",
                "Svelto Large Scale Entity Budget",
                "大规模 Shooter 实体预算压测入口，复用 Svelto gameplay runner 输出预算诊断。",
                shooterCount: 256,
                targetCount: 4096,
                tickCount: 60,
                tickDeltaTime: 1f / 30f,
                arenaRadius: 64f,
                loadout: ShooterSveltoGameplayScenarioCatalog.DefaultLoadout,
                battleFlow: new ShooterSveltoGameplayBattleFlowConfig(
                    durationFrames: 60,
                    victoryTargetDefeats: 4096,
                    maxActiveEnemies: 512,
                    waves: new[]
                    {
                        new ShooterSveltoGameplayWaveConfig(1, 0, 1, 512, 2, 32f),
                        new ShooterSveltoGameplayWaveConfig(2, 15, 1, 512, 3, 40f)
                    })),
            iterations: 2,
            entityBudget: new ShooterSveltoGameplayEntityBudgetProfile(
                ShooterEntityLimitOptions.DefaultMaxEntityCount,
                activeSyncBudget: 2048));
    }

    public static class ShooterSveltoGameplayBenchmark
    {
        public static ShooterSveltoGameplayBenchmarkResult Run(
            IShooterSveltoGameplayScenarioRunner runner,
            in ShooterSveltoGameplayBenchmarkProfile profile)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            ShooterSveltoGameplayScenarioResult first = default;
            ShooterSveltoGameplayScenarioResult last = default;
            var deterministic = true;
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < profile.Iterations; i++)
            {
                var result = runner.Run(profile.Scenario);
                if (i == 0)
                {
                    first = result;
                }
                else if (!HasSameDeterministicOutcome(in first, in result))
                {
                    deterministic = false;
                }

                last = result;
            }

            stopwatch.Stop();
            var allocatedBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            var initialEntityCount = profile.Scenario.ShooterCount + profile.Scenario.TargetCount;
            var totalFrames = (long)profile.Iterations * profile.Scenario.TickCount;
            var entityBudget = CreateEntityBudgetDiagnostics(in profile, initialEntityCount, totalFrames, stopwatch.ElapsedTicks, allocatedBytes);
            return new ShooterSveltoGameplayBenchmarkResult(
                profile.Id,
                profile.Scenario.Id,
                profile.Iterations,
                profile.Scenario.TickCount,
                initialEntityCount,
                totalFrames,
                totalFrames * initialEntityCount,
                deterministic,
                first,
                last,
                entityBudget);
        }

        private static ShooterSveltoGameplayEntityBudgetDiagnostics CreateEntityBudgetDiagnostics(
            in ShooterSveltoGameplayBenchmarkProfile profile,
            int requestedInitialEntityCount,
            long totalFrames,
            long elapsedTicks,
            long allocatedBytes)
        {
            var limits = new ShooterEntityLimitOptions(profile.EntityBudget.MaxEntityCount);
            var clampedInitialEntityCount = limits.ClampRequestedCount(requestedInitialEntityCount);
            var safeTotalFrames = Math.Max(1L, totalFrames);
            return new ShooterSveltoGameplayEntityBudgetDiagnostics(
                limits.MaxEntityCount,
                profile.EntityBudget.ActiveSyncBudget,
                requestedInitialEntityCount,
                clampedInitialEntityCount,
                limits.MaxEntityCount - clampedInitialEntityCount,
                requestedInitialEntityCount <= limits.MaxEntityCount,
                totalFrames * profile.EntityBudget.ActiveSyncBudget,
                elapsedTicks,
                allocatedBytes,
                elapsedTicks / safeTotalFrames,
                allocatedBytes / safeTotalFrames);
        }

        private static bool HasSameDeterministicOutcome(
            in ShooterSveltoGameplayScenarioResult expected,
            in ShooterSveltoGameplayScenarioResult actual)
        {
            return string.Equals(expected.ScenarioId, actual.ScenarioId, StringComparison.Ordinal)
                && expected.Frames == actual.Frames
                && expected.Shooters == actual.Shooters
                && expected.Targets == actual.Targets
                && expected.ProjectilesSpawned == actual.ProjectilesSpawned
                && expected.ProjectilesExpired == actual.ProjectilesExpired
                && expected.Hits == actual.Hits
                && expected.DefeatedTargets == actual.DefeatedTargets
                && expected.ActiveProjectiles == actual.ActiveProjectiles
                && expected.RemainingTargetHp == actual.RemainingTargetHp
                && expected.EnemyHits == actual.EnemyHits
                && expected.StateHash == actual.StateHash;
        }
    }
}
