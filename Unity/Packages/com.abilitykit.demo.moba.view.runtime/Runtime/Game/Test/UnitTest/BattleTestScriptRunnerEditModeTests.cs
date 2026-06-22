using System;
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
            const string playerIdValue = "p1";
            const string worldType = "battle";
            const int expectedSkillId = 10010101;
            const int expectedCastFlowId = 10010101;
            const int expectedEffectId = 10001;
            const int expectedDebugLogActionId = 589451731;
            const int expectedShootProjectileActionId = 508656420;
            const int expectedProjectileLauncherId = 100001;
            const int expectedProjectileId = 200001;
            const float fixedDelta = 1f / 30f;

            var playerId = new PlayerId(playerIdValue);
            var world = CreateHeadlessMobaWorld(
                worldId: new WorldId("skill_effect_assertion_world"),
                worldType: worldType,
                playerId: playerId,
                skillIds: new[] { expectedSkillId });

            try
            {
                var config = world.Services.Resolve<MobaConfigDatabase>();
                Assert.IsTrue(config.TryGetSkill(expectedSkillId, out var skill), $"Skill config missing: {expectedSkillId}");
                Assert.AreEqual(expectedCastFlowId, skill.CastFlowId);
                Assert.IsTrue(config.TryGetSkillFlow(expectedCastFlowId, out var flow), $"Skill flow config missing: {expectedCastFlowId}");
                Assert.IsTrue(ContainsTimelineEffect(flow.Phases, expectedEffectId), $"Skill flow {expectedCastFlowId} must contain timeline effect {expectedEffectId}.");
                Assert.IsTrue(config.TryGetProjectileLauncher(expectedProjectileLauncherId, out var launcher), $"Projectile launcher config missing: {expectedProjectileLauncherId}");
                Assert.IsNotNull(launcher);
                Assert.IsTrue(config.TryGetProjectile(expectedProjectileId, out var projectile), $"Projectile config missing: {expectedProjectileId}");
                Assert.IsNotNull(projectile);

                var triggerPlans = world.Services.Resolve<TriggerPlanJsonDatabase>();
                Assert.IsTrue(triggerPlans.TryGetPlanByTriggerId(expectedEffectId, out var triggerPlan), $"Trigger plan missing for effect {expectedEffectId}.");
                AssertPlanContainsAction(triggerPlan, expectedDebugLogActionId, $"Effect {expectedEffectId} must compile trigger action debug_log({expectedDebugLogActionId}).");
                AssertPlanContainsAction(triggerPlan, expectedShootProjectileActionId, $"Effect {expectedEffectId} must compile trigger action shoot_projectile({expectedShootProjectileActionId}).");

                var phase = world.Services.Resolve<MobaLogicWorldRunGateService>();
                phase.SetInGame("editmode skill effect assertion");

                for (var i = 0; i < 3; i++)
                {
                    world.Tick(fixedDelta);
                }

                var playerActorMap = world.Services.Resolve<MobaPlayerActorMapService>();
                Assert.IsTrue(playerActorMap.TryGetActorId(playerId, out var actorId), $"Player actor binding missing: {playerIdValue}");

                var loadout = world.Services.Resolve<MobaSkillLoadoutService>();
                Assert.IsTrue(loadout.TryGetSkillId(actorId, 1, out var slotSkillId), $"Slot 1 skill missing for actor {actorId}.");
                Assert.AreEqual(expectedSkillId, slotSkillId);

                var input = world.Services.Resolve<IWorldInputSink>();
                var trace = world.Services.Resolve<MobaTraceRegistry>();
                var frameTime = world.Services.Resolve<IFrameTime>();
                var castFrame = new FrameIndex(frameTime.Frame.Value + 1);
                var skillInput = new SkillInputEvent(slot: 1, phase: SkillInputPhase.Press);
                var command = new PlayerInputCommand(castFrame, playerId, MobaOpCodes.Input.SkillInput, SkillInputCodec.Serialize(in skillInput));

                input.Submit(castFrame, new[] { command });

                for (var i = 0; i < 25; i++)
                {
                    world.Tick(fixedDelta);
                }

                var effectTrace = AssertTraceNode(trace, MobaTraceKind.EffectExecution, expectedEffectId, $"EffectExecution trace missing for configured effect {expectedEffectId}.");
                AssertTraceNode(trace, MobaTraceKind.SkillCast, expectedSkillId, $"SkillCast trace missing for skill {expectedSkillId}.");
                AssertTraceNodeInRoot(trace, effectTrace.RootId, MobaTraceKind.EffectAction, expectedDebugLogActionId, $"Trigger action debug_log({expectedDebugLogActionId}) was not executed under effect {expectedEffectId}.");
                AssertTraceNodeInRoot(trace, effectTrace.RootId, MobaTraceKind.EffectAction, expectedShootProjectileActionId, $"Trigger action shoot_projectile({expectedShootProjectileActionId}) was not executed under effect {expectedEffectId}.");
                AssertTraceNodeInRoot(trace, effectTrace.RootId, MobaTraceKind.ProjectileLaunch, expectedProjectileId, $"shoot_projectile did not launch configured projectile {expectedProjectileId} from launcher {expectedProjectileLauncherId}.");
            }
            finally
            {
                world.Dispose();
            }
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

        private static IWorld CreateHeadlessMobaWorld(WorldId worldId, string worldType, PlayerId playerId, int[] skillIds)
        {
            var registry = new WorldTypeRegistry().RegisterEntitasWorld(worldType);
            var manager = new WorldManager(new RegistryWorldFactory(registry));
            var launchSpec = new MobaBattleLaunchSpec(
                battleId: worldId.Value,
                matchId: worldId.Value,
                worldId: worldId.Value,
                worldType: worldType,
                clientId: "editmode_skill_effect_test",
                localPlayerId: playerId,
                mapId: 1,
                gameplayId: 0,
                ruleSetId: 0,
                configVersion: 0,
                protocolVersion: 0,
                randomSeed: 123,
                tickRate: 30,
                inputDelayFrames: 0,
                launchMode: MobaBattleLaunchMode.ViewFastEnter,
                syncMode: MobaBattleLaunchSyncMode.Hybrid,
                authorityMode: MobaBattleLaunchAuthorityMode.LocalAuthority,
                players: new[]
                {
                    new MobaPlayerLoadout(
                        playerId,
                        teamId: 1,
                        heroId: 1,
                        attributeTemplateId: 1001,
                        level: 1,
                        basicAttackSkillId: 1,
                        skillIds: skillIds,
                        spawnIndex: 0,
                        unitSubType: (int)UnitSubType.Hero,
                        mainType: (int)EntityMainType.Unit,
                        hasSpawnPosition: 1,
                        spawnX: 0f,
                        spawnY: 0f,
                        spawnZ: 0f)
                },
                enterGamePayload: Array.Empty<byte>());

            var builder = WorldServiceContainerFactory.CreateWithAttributes(
                WorldServiceProfile.All,
                new[]
                {
                    typeof(WorldServiceContainerFactory).Assembly,
                    typeof(MobaWorldBootstrapModule).Assembly,
                    typeof(BattleTestScriptRunnerEditModeTests).Assembly
                },
                new[] { "AbilityKit" });

            builder.AddModule(new MobaConfigWorldModule());
            builder.RegisterInstance(launchSpec.ToWorldInitData(MobaWorldBootstrapModule.InitOpCode));
            builder.TryRegister<IFrameTime>(WorldLifetime.Singleton, _ => new FrameTime());

            var options = new WorldCreateOptions(worldId, worldType)
            {
                ServiceBuilder = builder,
            };
            options.SetEntitasContextsFactory(new MobaEntitasContextsFactory());
            return manager.Create(options);
        }

        private static bool ContainsTimelineEffect(System.Collections.Generic.IReadOnlyList<AbilityKit.Demo.Moba.Share.Config.SkillPhaseDTO> phases, int effectId)
        {
            if (phases == null) return false;
            for (var i = 0; i < phases.Count; i++)
            {
                var phase = phases[i];
                if (phase == null) continue;

                var events = phase.Timeline != null ? phase.Timeline.Events : null;
                if (events != null)
                {
                    for (var j = 0; j < events.Length; j++)
                    {
                        if (events[j] != null && events[j].EffectId == effectId) return true;
                    }
                }

                if (ContainsTimelineEffect(phase.Children, effectId)) return true;
                if (phase.Repeat != null && ContainsTimelineEffect(new[] { phase.Repeat.Phase }, effectId)) return true;
            }

            return false;
        }

        private static void AssertPlanContainsAction(AbilityKit.Triggering.Runtime.Plan.TriggerPlan<object> plan, int actionId, string message)
        {
            var actions = plan.Actions;
            if (actions != null)
            {
                for (var i = 0; i < actions.Length; i++)
                {
                    if ((int)actions[i].Id.Value == actionId)
                    {
                        return;
                    }
                }
            }

            Assert.Fail(message);
        }

        private static AbilityKit.Trace.TraceSnapshot<MobaTraceMetadata> AssertTraceNode(MobaTraceRegistry trace, MobaTraceKind kind, int configId, string message)
        {
            foreach (var node in trace.GetNodesByKind((int)kind))
            {
                if (node.Metadata != null && node.Metadata.ConfigId == configId)
                {
                    return node;
                }
            }

            Assert.Fail(message);
            return default;
        }

        private static AbilityKit.Trace.TraceSnapshot<MobaTraceMetadata> AssertTraceNodeInRoot(MobaTraceRegistry trace, long rootId, MobaTraceKind kind, int configId, string message)
        {
            foreach (var node in trace.GetNodesByRoot(rootId))
            {
                if (node.Kind == (int)kind && node.Metadata != null && node.Metadata.ConfigId == configId)
                {
                    return node;
                }
            }

            Assert.Fail(message);
            return default;
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
