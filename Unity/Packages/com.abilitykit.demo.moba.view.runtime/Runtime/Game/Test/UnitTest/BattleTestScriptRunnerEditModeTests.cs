using System;
using System.IO;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Demo.Moba.Testing;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Game.Flow;
using AbilityKit.Protocol.Moba;
using AbilityKit.Triggering.Runtime.Plan.Json;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class BattleTestScriptRunnerEditModeTests
    {
        [Test]
        public void SharedRunner_CanDriveUnityHeadlessViewRuntimeDriver()
        {
            var script = BattleTestScenarioLibrary.CreateViewPresentationRisk(cycles: 1);
            var ctx = BattleContext.Rent();

            try
            {
                ctx.Plan = BattleStartPlanBuilder
                    .ForWorld("world", "type", "client", "player", tickRate: 30, inputDelayFrames: 0)
                    .Build();

                var driver = new ViewRuntimeBattleTestDriver(ctx);

                var result = new BattleTestScriptRunner().Run(script, driver);

                Assert.IsTrue(result.Completed, result.ErrorMessage);
                Assert.AreEqual(script.Steps.Count, result.StepCount);
                Assert.AreEqual(script.TotalDurationTicks, result.TickCount);
                Assert.AreEqual(script.TotalDurationTicks, driver.TickCount);
                Assert.AreEqual(script.TotalDurationTicks, ctx.LastFrame);
                Assert.GreaterOrEqual(driver.SkillApplyCount, 1);
                Assert.Greater(driver.SampleTime, 0d);
            }
            finally
            {
                BattleContext.Return(ctx);
            }
        }

        [Test]
        public void ViewRuntimeBattleTestDriver_TracksStepKindsAndStoresLastResult()
        {
            var script = new BattleTestScriptBuilder()
                .Move(1f, 0f, durationTicks: 2)
                .Skill(1)
                .Wait(3)
                .Idle(2)
                .Build("view-runtime-driver-coverage");
            var ctx = BattleContext.Rent();

            try
            {
                var driver = new ViewRuntimeBattleTestDriver(ctx);

                var result = new BattleTestScriptRunner().Run(script, driver);

                Assert.IsTrue(result.Completed, result.ErrorMessage);
                Assert.AreSame(result, driver.LastResult);
                Assert.AreEqual(script.TotalDurationTicks, driver.TickCount);
                Assert.AreEqual(2, driver.MoveApplyCount);
                Assert.AreEqual(1, driver.SkillApplyCount);
                Assert.AreEqual(3, driver.WaitApplyCount);
                Assert.AreEqual(2, driver.IdleApplyCount);
                Assert.AreEqual(script.TotalDurationTicks, ctx.LastFrame);
                Assert.AreEqual(ctx.LastFrame / (double)driver.TickRate, ctx.LogicTimeSeconds);
            }
            finally
            {
                BattleContext.Return(ctx);
            }
        }

        [Test]
        public void ViewRuntimeBattleTestHarness_CanRunScenarioWithOwnedContext()
        {
            using var harness = new ViewRuntimeBattleTestHarness();
            harness.ConfigureDefaultPlan();

            var script = BattleTestScenarioLibrary.CreateViewPresentationRisk(cycles: 1);
            var result = harness.Run(script);

            Assert.IsTrue(result.Completed, result.ErrorMessage);
            Assert.AreSame(result, harness.LastResult);
            Assert.IsNotNull(harness.LastDriver);
            Assert.AreEqual(script.TotalDurationTicks, harness.LastDriver.TickCount);
            Assert.AreEqual(script.TotalDurationTicks, harness.Context.LastFrame);
            Assert.GreaterOrEqual(harness.LastDriver.SkillApplyCount, 1);
        }

        [Test]
        public void ViewRuntimeBattleTestHarness_CanRunFullFlowAndAutoReleaseFirstSkill()
        {
            using var harness = new ViewRuntimeBattleTestHarness();
            harness.ConfigureDefaultPlan();

            var script = new BattleTestScriptBuilder()
                .Move(1f, 0f, durationTicks: 5)
                .Wait(2)
                .Skill(1)
                .Wait(10)
                .Idle(3)
                .Build("full-flow-auto-release-first-skill");

            var result = harness.Run(script);

            Assert.IsTrue(result.Completed, result.ErrorMessage);
            Assert.AreSame(result, harness.LastResult);
            Assert.IsNotNull(harness.LastDriver);
            Assert.AreEqual(script.TotalDurationTicks, result.TickCount);
            Assert.AreEqual(script.TotalDurationTicks, harness.LastDriver.TickCount);
            Assert.AreEqual(script.TotalDurationTicks, harness.Context.LastFrame);
            Assert.AreEqual(1, harness.LastDriver.SkillApplyCount);
            Assert.AreEqual(1, harness.LastDriver.LastReleasedSkillSlot);
            CollectionAssert.AreEqual(new[] { 1 }, harness.LastDriver.ReleasedSkillSlots);
            Assert.Greater(harness.LastDriver.SampleTime, 0d);
        }

        [Test]
        public void SkillSlotOne_CastExecutesConfiguredTimelineEffect()
        {
            const int expectedSkillId = 10010101;
            const int expectedCastFlowId = 10010101;
            const int expectedEffectId = 10001;
            const int expectedDebugLogActionId = 589451731;
            const int expectedShootProjectileActionId = 508656420;
            const int expectedProjectileLauncherId = 100001;
            const int expectedProjectileId = 200001;

            using var harness = MobaSkillConfigTestHarness.CreateForSinglePlayer(
                skillIds: new[] { expectedSkillId },
                worldId: "skill_effect_assertion_world");

            harness.AssertSkillUsesCastFlow(expectedSkillId, expectedCastFlowId);
            harness.AssertCastFlowContainsTimelineEffect(expectedCastFlowId, expectedEffectId);
            harness.AssertProjectileConfigExists(expectedProjectileLauncherId, expectedProjectileId);
            harness.AssertTriggerPlanContainsActions(expectedEffectId, expectedDebugLogActionId, expectedShootProjectileActionId);

            harness.EnterGameAndWarmup(reason: "editmode skill effect assertion");
            harness.AssertSlotSkill(slot: 1, expectedSkillId: expectedSkillId);

            var effectTrace = harness.CastSkillSlotAndTickUntilEffect(
                slot: 1,
                skillId: expectedSkillId,
                effectId: expectedEffectId);
            harness.AssertSkillCastTrace(expectedSkillId);
            harness.AssertActionExecutedUnderEffect(effectTrace.RootId, expectedDebugLogActionId, "debug_log");
            harness.AssertActionExecutedUnderEffect(effectTrace.RootId, expectedShootProjectileActionId, "shoot_projectile");
            harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, expectedProjectileLauncherId, expectedProjectileId);
        }

        [Test]
        public void MoziPassive_UsesGenericCounterTriggerPlanAndFourthHitProjectile()
        {
            const int passiveCounterTriggerId = 10040001;
            const int passiveProjectileTriggerId = 10040002;
            const int expectedProjectileLauncherId = 31040201;
            const int expectedProjectileId = 30040201;

            using var harness = MobaSkillConfigTestHarness.CreateForSinglePlayer(
                skillIds: new[] { 10040101, 10040201, 10040301 },
                worldId: "mozi_passive_trigger_plan_assertion_world");

            harness.AssertProjectileConfigExists(expectedProjectileLauncherId, expectedProjectileId);
            harness.AssertTriggerPlanContainsActions(passiveCounterTriggerId, (int)TriggeringConstants.AdvanceGameplayCounterId.Value);
            harness.AssertTriggerPlanContainsActions(
                passiveProjectileTriggerId,
                (int)TriggeringConstants.ShootProjectileId.Value,
                (int)TriggeringConstants.DebugLogId.Value);
            AssertResourceCopiesMatch("ability/triggers/passives/trigger_10040000.json");
        }

        [Test]
        public void ZhaoYunAndMoziTriggerResources_AreSyncedBetweenAssetsAndPackageRuntime()
        {
            AssertResourceCopiesMatch("ability/triggers/passives/trigger_10030000.json");
            AssertResourceCopiesMatch("ability/triggers/passives/trigger_10040000.json");
            AssertResourceCopiesMatch("ability/triggers/skills/trigger_10030101.json");
            AssertResourceCopiesMatch("ability/triggers/skills/trigger_10040101.json");
        }

        [Test]
        public void SharedRunner_PropagatesRiskTagsForUnityPresentationCoverage()
        {
            var script = new ViewPresentationRiskScenario { Cycles = 1 }.CreateScript();
            var driver = new RecordingUnityHeadlessDriver();

            var result = new BattleTestScriptRunner().Run(script, driver);

            Assert.IsTrue(result.Completed, result.ErrorMessage);
            CollectionAssert.Contains(script.RiskTags, BattleTestScenarioLibrary.EntityRiskTag);
            CollectionAssert.Contains(script.RiskTags, BattleTestScenarioLibrary.FloatingTextRiskTag);
            CollectionAssert.Contains(script.RiskTags, BattleTestScenarioLibrary.ProjectileRiskTag);
            CollectionAssert.Contains(script.RiskTags, BattleTestScenarioLibrary.VfxRiskTag);
            CollectionAssert.Contains(script.RiskTags, BattleTestScenarioLibrary.SnapshotEventRiskTag);
            Assert.AreEqual(script.TotalDurationTicks, driver.AppliedTickCount);
        }

        private static void AssertResourceCopiesMatch(string resourcePath)
        {
            var assetsPath = Path.Combine("Assets", "Resources", resourcePath.Replace('/', Path.DirectorySeparatorChar));
            var packagePath = Path.Combine("Packages", "com.abilitykit.demo.moba.view.runtime", "Resources", resourcePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.IsTrue(File.Exists(assetsPath), $"Assets resource missing: {assetsPath}");
            Assert.IsTrue(File.Exists(packagePath), $"Package runtime resource missing: {packagePath}");
            Assert.AreEqual(File.ReadAllText(assetsPath), File.ReadAllText(packagePath), $"Resource copy mismatch: {resourcePath}");
        }

        private sealed class RecordingUnityHeadlessDriver : IBattleTestScriptDriver
        {
            public int AppliedTickCount { get; private set; }

            public void Apply(BattleTestStep step)
            {
                AppliedTickCount++;
            }

            public void Tick()
            {
            }
        }
    }
}
