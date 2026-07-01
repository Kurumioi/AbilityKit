using System.Linq;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterPureStateSnapshotRuntimeTests
{
    [Fact]
    public void PureStateSnapshotExportsPlayersAndProjectilesFromRuntime()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "pure-state-smoke",
            30,
            7001,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));

        var payload = runtime.ExportPureStateSnapshot(77ul, isFullBaseline: true);
        var player = Assert.Single(payload.Entities, e => e.EntityKind == ShooterPackedEntityKinds.Player && e.EntityId == 1);
        var projectile = Assert.Single(payload.Entities, e => e.EntityKind == ShooterPackedEntityKinds.Projectile);

        Assert.Equal(ShooterPureStateSyncCodec.CurrentVersion, payload.Version);
        Assert.Equal(77ul, payload.WorldId);
        Assert.Equal(runtime.CurrentFrame, payload.Frame);
        Assert.Equal(ShooterPureStateSnapshotKinds.FullBaseline, payload.SnapshotKind);
        Assert.Equal(payload.Frame, payload.BaselineFrame);
        Assert.Equal(runtime.ComputeStateHash(), payload.BaselineHash);
        Assert.Equal(runtime.ComputeStateHash(), payload.StateHash);
        Assert.Equal(ShooterPureStateEntityLayers.KeyInteraction, player.EntityLayer);
        Assert.Equal(ShooterPureStateDeltaKinds.Spawn, player.DeltaKind);
        Assert.Equal(ShooterPureStateEntityFlags.Alive | ShooterPureStateEntityFlags.Visible, player.Flags);
        Assert.Equal(ShooterPureStateEntityLayers.Combat, projectile.EntityLayer);
        Assert.Equal(ShooterPureStateDeltaKinds.Spawn, projectile.DeltaKind);
        Assert.Equal(1, projectile.OwnerId);
        Assert.Equal(payload.Entities.Length, payload.VisibilityHints.Length);
    }

    [Fact]
    public void PureStateDeltaSnapshotCarriesBaselineIdentityAndUpdateDeltas()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "pure-state-delta",
            30,
            7002,
            new[]
            {
                new ShooterStartPlayer(1, "P1", -1f, 0f),
                new ShooterStartPlayer(2, "P2", 2f, 0f)
            });

        Assert.True(runtime.StartGame(in start));
        Assert.True(runtime.Tick(1f / 30f));
        var baseline = runtime.ExportPureStateSnapshot(88ul, isFullBaseline: true);

        runtime.SubmitInput(1, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, false) });
        Assert.True(runtime.Tick(1f / 30f));

        var delta = runtime.ExportPureStateSnapshot(
            88ul,
            isFullBaseline: false,
            settings: ShooterPureStateSyncSettings.Default,
            baselineFrame: baseline.Frame,
            baselineHash: baseline.StateHash);

        Assert.Equal(ShooterPureStateSnapshotKinds.Delta, delta.SnapshotKind);
        Assert.Equal(baseline.Frame, delta.BaselineFrame);
        Assert.Equal(baseline.StateHash, delta.BaselineHash);
        Assert.All(delta.Entities, entity => Assert.Equal(ShooterPureStateDeltaKinds.Update, entity.DeltaKind));
        Assert.Equal(runtime.CurrentFrame, delta.ServerTick);
        Assert.Equal(runtime.ComputeStateHash(), delta.StateHash);
    }

    [Fact]
    public void PureStateDeltaSnapshotRespectsActiveSyncBudgetAndKeepsPlayersFirst()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "pure-state-budget",
            30,
            7003,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });
        var settings = new ShooterPureStateSyncSettings(
            maxEntityCount: 10,
            activeSyncBudget: 2,
            baselineIntervalFrames: 60,
            deltaIntervalFrames: 1,
            lowFrequencyIntervalFrames: 10,
            interpolationDelayFrames: 3);

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        var baseline = runtime.ExportPureStateSnapshot(89ul, isFullBaseline: true, settings: settings);

        runtime.SubmitInput(1, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        var delta = runtime.ExportPureStateSnapshot(
            89ul,
            isFullBaseline: false,
            settings: settings,
            baselineFrame: baseline.Frame,
            baselineHash: baseline.StateHash);

        Assert.Equal(2, delta.Entities.Length);
        Assert.All(delta.Entities, entity => Assert.Equal(ShooterPackedEntityKinds.Player, entity.EntityKind));
        Assert.Contains(delta.Entities, entity => entity.EntityId == 1);
        Assert.Contains(delta.Entities, entity => entity.EntityId == 2);
        Assert.Equal(delta.Entities.Length, delta.VisibilityHints.Length);
    }

    [Fact]
    public void PureStateDeltaSnapshotMarksLowFrequencyFrames()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "pure-state-low-frequency",
            30,
            7004,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });
        var settings = new ShooterPureStateSyncSettings(
            maxEntityCount: 10,
            activeSyncBudget: 10,
            baselineIntervalFrames: 60,
            deltaIntervalFrames: 1,
            lowFrequencyIntervalFrames: 2,
            interpolationDelayFrames: 3);

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        var baseline = runtime.ExportPureStateSnapshot(90ul, isFullBaseline: true, settings: settings);

        runtime.SubmitInput(1, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        var lowFrequency = runtime.ExportPureStateSnapshot(
            90ul,
            isFullBaseline: false,
            settings: settings,
            baselineFrame: baseline.Frame,
            baselineHash: baseline.StateHash);

        Assert.Equal(ShooterPureStateSnapshotKinds.LowFrequency, lowFrequency.SnapshotKind);
        var projectiles = lowFrequency.Entities.Where(entity => entity.EntityKind == ShooterPackedEntityKinds.Projectile).ToArray();
        Assert.NotEmpty(projectiles);
        Assert.All(projectiles, projectile => Assert.True((projectile.Flags & ShooterPureStateEntityFlags.LowFrequency) != 0));
        Assert.Contains(lowFrequency.VisibilityHints, hint => hint.EntityKind == ShooterPackedEntityKinds.Projectile && (hint.Flags & ShooterPureStateEntityFlags.LowFrequency) != 0);
    }

    [Fact]
    public void PureStateSnapshotBudgetProfilesKeepPayloadAndVisibilityHintsAligned()
    {
        var profiles = new[]
        {
            (Name: "small", PlayerCount: 2, ActiveSyncBudget: 4, ExpectedEntities: 4),
            (Name: "medium", PlayerCount: 4, ActiveSyncBudget: 8, ExpectedEntities: 8),
            (Name: "mass", PlayerCount: 8, ActiveSyncBudget: 16, ExpectedEntities: 16)
        };

        foreach (var profile in profiles)
        {
            var runtime = new ShooterBattleRuntimePort();
            var players = Enumerable.Range(1, profile.PlayerCount)
                .Select(index => new ShooterStartPlayer(index, $"P{index}", index * 2f, 0f))
                .ToArray();
            var settings = new ShooterPureStateSyncSettings(
                maxEntityCount: ShooterEntityLimitOptions.DefaultMaxEntityCount,
                activeSyncBudget: profile.ActiveSyncBudget,
                baselineIntervalFrames: 60,
                deltaIntervalFrames: 1,
                lowFrequencyIntervalFrames: 10,
                interpolationDelayFrames: 3);
            var start = new ShooterStartGamePayload(
                $"pure-state-{profile.Name}-budget",
                30,
                7100 + profile.PlayerCount,
                players);

            Assert.True(runtime.StartGame(in start));
            var commands = players
                .Select(player => new ShooterPlayerCommand(player.PlayerId, 0f, 0f, 1f, 0f, true))
                .ToArray();
            runtime.SubmitInput(0, commands);
            Assert.True(runtime.Tick(1f / 30f));

            var payload = runtime.ExportPureStateSnapshot(100ul + (ulong)profile.PlayerCount, isFullBaseline: false, settings: settings);

            Assert.Equal(profile.ExpectedEntities, payload.Entities.Length);
            Assert.Equal(payload.Entities.Length, payload.VisibilityHints.Length);
            Assert.Equal(profile.PlayerCount, payload.Entities.Count(entity => entity.EntityKind == ShooterPackedEntityKinds.Player));
            Assert.Equal(profile.PlayerCount, payload.Entities.Count(entity => entity.EntityKind == ShooterPackedEntityKinds.Projectile));
            Assert.All(payload.VisibilityHints, hint => Assert.True(hint.Priority > 0));
        }
    }

    [Fact]
    public void PureStateSnapshotAppliesInterestScopeBeforeBudgetCut()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "pure-state-aoi",
            30,
            7005,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 40f, 0f),
                new ShooterStartPlayer(3, "P3", 2f, 0f)
            });
        var settings = new ShooterPureStateSyncSettings(
            maxEntityCount: 20,
            activeSyncBudget: 20,
            baselineIntervalFrames: 60,
            deltaIntervalFrames: 1,
            lowFrequencyIntervalFrames: 10,
            interpolationDelayFrames: 3);
        var scope = new ShooterPureStateInterestScope(
            observerPlayerId: 1,
            centerX: 0f,
            centerY: 0f,
            radius: 8f,
            maxEntities: 2);

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(2, 0f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));

        var payload = runtime.ExportPureStateSnapshot(91ul, isFullBaseline: false, settings: settings, interestScope: scope);

        Assert.Equal(2, payload.Entities.Length);
        Assert.Equal(1, payload.Entities[0].EntityId);
        Assert.Contains(payload.Entities, entity => entity.EntityKind == ShooterPackedEntityKinds.Player && entity.EntityId == 3);
        Assert.DoesNotContain(payload.Entities, entity => entity.EntityKind == ShooterPackedEntityKinds.Player && entity.EntityId == 2);
        Assert.DoesNotContain(payload.Entities, entity => entity.EntityKind == ShooterPackedEntityKinds.Projectile);
        Assert.Equal(payload.Entities.Length, payload.VisibilityHints.Length);
        Assert.All(payload.VisibilityHints, hint => Assert.True(hint.Priority > 0));
    }
}
