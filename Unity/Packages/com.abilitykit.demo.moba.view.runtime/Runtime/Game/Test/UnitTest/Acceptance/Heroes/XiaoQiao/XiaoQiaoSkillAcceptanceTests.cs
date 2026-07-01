using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class XiaoQiaoSkillAcceptanceTests : MobaAcceptanceTestBase
    {
        private const string Skill10020101ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020101.expected.json";
        private const string Skill10020101ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020101_scenario.expected.json";
        private const string Skill10020201ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020201.expected.json";
        private const string Skill10020201ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020201_scenario.expected.json";
        private const string Skill10020301ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020301.expected.json";
        private const string Skill10020301ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020301_scenario.expected.json";

        [Test]
        public void Skill10020101_ExportsTraceAndMatchesXiaoQiaoProjectileExpectation()
        {
            var summary = RunExpectationFile(Skill10020101ExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.projectileLaunched);
            Assert.Greater(summary.result.effectRootId, 0);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);
        }

        [Test]
        public void Skill10020101_ScenarioExportsTraceAndConfirmsProjectileDamage()
        {
            var summary = RunExpectationFile(Skill10020101ScenarioExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.projectileLaunched);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);

            var records = LoadTraceRecords(summary);
            var expectation = LoadExpectation(Skill10020101ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for projectile damage scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            MobaAcceptanceTraceAssert.AssertTraceNodeKindInRoot(records, effectRootId, "ProjectileLaunch", 30020101, "the projectile scenario should launch the configured projectile under the Xiao Qiao skill 1 effect root.");
            MobaAcceptanceTraceAssert.AssertSingleTargetDamageInRoot(records, effectRootId, expectation.config.effectId, expectedHitCount: 1, "the projectile scenario should damage exactly one target exactly once under the same effect root.");
        }

        [Test]
        public void Skill10020201_ExportsTraceAndMatchesXiaoQiaoAreaExpectation()
        {
            var summary = RunExpectationFile(Skill10020201ExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.areaSpawned);
            Assert.Greater(summary.result.effectRootId, 0);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);
        }

        [Test]
        public void Skill10020201_ScenarioExportsTraceAndConfirmsAreaDamage()
        {
            var summary = RunExpectationFile(Skill10020201ScenarioExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.areaSpawned);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);

            var records = LoadTraceRecords(summary);
            var expectation = LoadExpectation(Skill10020201ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for area scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            MobaAcceptanceTraceAssert.AssertTraceNodeKindInRoot(records, effectRootId, "AreaSpawn", 40020201, "area spawn should remain under the Xiao Qiao skill 2 effect root.");
            MobaAcceptanceTraceAssert.AssertTraceNodeKindInRoot(records, effectRootId, "AreaEnter", 40020201, "the spawned area should emit an enter trace when the target is inside the area.");
            MobaAcceptanceTraceAssert.AssertSingleTargetDamageInRoot(records, effectRootId, expectation.config.effectId, expectedHitCount: 1, "the area scenario should damage exactly one target exactly once under the same effect root.");
        }

        [Test]
        public void Skill10020301_ExportsTraceAndMatchesXiaoQiaoUltimateExpectation()
        {
            var summary = RunExpectationFile(Skill10020301ExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.buffApplied);
            Assert.Greater(summary.result.effectRootId, 0);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);
        }

        [Test]
        public void Skill10020301_ScenarioExportsTraceAndConfirmsIntervalDamage()
        {
            var summary = RunExpectationFile(Skill10020301ScenarioExpectationPath);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.buffApplied);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);

            var records = LoadTraceRecords(summary);
            var expectation = LoadExpectation(Skill10020301ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for interval damage scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            MobaAcceptanceTraceAssert.AssertTraceNodeKindInRoot(records, effectRootId, "BuffApply", 10020301, "the ultimate should apply its persistent buff under the same effect root.");
            MobaAcceptanceTraceAssert.AssertTraceNodeKindInRoot(records, effectRootId, "DamageApply", 10020301, "the interval tick should apply damage under the same effect root.");
            MobaAcceptanceTraceAssert.AssertRepeatHitDamagePattern(records, effectRootId, 10020301, expectedHitCount: 2, "the ultimate should hit the same target exactly twice under one skill runtime before the repeat-hit decay expectation is considered valid.");
        }

        [Test]
        public void Skill10020000_PassiveCastBuff_ShouldIncreaseMoveSpeedTemporarily()
        {
            using (var harness = MobaSkillConfigTestHarness.CreateForSinglePlayer(new[] { 10020101, 10020201, 10020301 }, heroId: 1002, attributeTemplateId: 1002))
            {
                harness.EnterGameAndWarmup(reason: "xiao qiao passive move speed contract");
                harness.AssertSlotSkill(1, 10020101);

                const int passiveBuffId = 10020001;
                const int passiveSkillId = 10020000;
                const float baseMoveSpeed = 5f;
                const float buffedMoveSpeed = 7f;
                var playerActorId = harness.AssertPlayerActorBound();

                Assert.AreEqual(baseMoveSpeed, harness.GetActorMoveSpeed(playerActorId), 0.01f, "Xiao Qiao base move speed should come from attribute template 1002 before casting.");
                Assert.IsFalse(harness.HasActorBuff(playerActorId, passiveBuffId), "Passive speed buff should not exist before any skill cast.");

                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                Assert.IsNotNull(skills, "SkillCastCoordinator should be available in the single-player harness world.");
                var castResult = skills.TryCastBySlot(playerActorId, slot: 1, aimPos: default, aimDir: new Vec3(1f, 0f, 0f), targetActorId: 0);
                Assert.IsTrue(castResult.Success, $"Xiao Qiao skill 1 cast should succeed in the passive contract test. failReason={castResult.FailReason}");

                harness.TickUntilTraceNode(MobaTraceKind.SkillCast, 10020101, maxTicks: 10, message: "Xiao Qiao skill 1 should emit SkillCast after direct cast.");
                harness.TickUntilTraceNode(MobaTraceKind.EffectExecution, 10020101, maxTicks: 10, message: "Xiao Qiao skill 1 should execute effect 10020101 after direct cast.");
                harness.TickUntilTraceNode(MobaTraceKind.BuffApply, passiveSkillId, maxTicks: 10, message: "Xiao Qiao passive should apply buff 10020001 after casting a skill.");

                Assert.IsTrue(harness.HasActorBuff(playerActorId, passiveBuffId), "Passive speed buff should exist immediately after the cast-triggered passive resolves.");
                Assert.AreEqual(buffedMoveSpeed, harness.GetActorMoveSpeed(playerActorId), 0.01f, "Passive speed buff should raise move speed by the configured modifier amount.");

                harness.TickSeconds(2.1f);

                Assert.IsFalse(harness.HasActorBuff(playerActorId, passiveBuffId), "Passive speed buff should expire after its configured 2 second duration.");
                Assert.AreEqual(baseMoveSpeed, harness.GetActorMoveSpeed(playerActorId), 0.01f, "Move speed should return to base value after passive buff expiration.");
            }
        }

        [Test]
        public void Skill10020301_RuntimeResetAfterRecast_ShouldRestartRepeatHitDecay()
        {
            var expectation = new MobaAcceptanceExpectation
            {
                caseId = "skill_10020301_runtime_reset_after_recast",
                description = "小乔三技能在上一次持续结束后再次释放时，应重新从首段满伤开始计算重复命中减伤",
                worldId = "skill_10020301_runtime_reset_after_recast_world",
                tickRate = 30,
                accelerated = true,
                category = "contract",
                tags = new[] { "xiao-qiao", "skill-3", "buff", "scenario", "runtime-reset" },
                scenario = new MobaAcceptanceScenarioExpectation
                {
                    scenarioId = "skill_10020301_runtime_reset_after_recast",
                    name = "小乔三技能二次释放应重置衰减",
                    description = "第一次释放在同一 runtime 内造成 80+40，等待持续结束并复位目标生命后，第二次释放应再次造成 80+40，而不是继承上次命中次数。",
                    worldId = "skill_10020301_runtime_reset_after_recast_world",
                    tickRate = 30,
                    accelerated = true,
                    category = "contract",
                    tags = new[] { "xiao-qiao", "skill-3", "buff", "scenario", "runtime-reset" },
                    actors = new[]
                    {
                        new MobaAcceptanceActorExpectation
                        {
                            alias = "caster",
                            playerId = "p1",
                            teamId = 1,
                            heroId = 1002,
                            attributeTemplateId = 1002,
                            skillIds = new[] { 10020101, 10020201, 10020301 },
                            hasSpawnPosition = true,
                            spawnPosition = new MobaAcceptanceVector3Expectation { x = 0f, y = 0f, z = 0f },
                            facingDirection = new MobaAcceptanceVector3Expectation { x = 1f, y = 0f, z = 0f }
                        },
                        new MobaAcceptanceActorExpectation
                        {
                            alias = "target",
                            playerId = "p2",
                            teamId = 2,
                            heroId = 1002,
                            attributeTemplateId = 1002,
                            hasSpawnPosition = true,
                            spawnPosition = new MobaAcceptanceVector3Expectation { x = 4f, y = 0f, z = 0f },
                            facingDirection = new MobaAcceptanceVector3Expectation { x = -1f, y = 0f, z = 0f }
                        }
                    },
                    timeline = new[]
                    {
                        new MobaAcceptanceTimelineStepExpectation { stepId = "first_cast", atMs = 0, action = "press", actorAlias = "caster", slot = 3, note = "第一次释放三技能" },
                        new MobaAcceptanceTimelineStepExpectation { stepId = "wait_for_first_two_hits", atMs = 1, action = "wait", durationMs = 2200, note = "等待第一次 runtime 的前两段伤害" },
                        new MobaAcceptanceTimelineStepExpectation { stepId = "reset_target_hp", atMs = 2300, action = "set_attr", actorAlias = "target", property = "hp", value = 180f, note = "复位目标生命值，准备验证下一次释放是否重新从满伤开始" },
                        new MobaAcceptanceTimelineStepExpectation { stepId = "wait_for_first_runtime_end", atMs = 2301, action = "wait", durationMs = 3100, note = "等待第一次三技能持续结束，确保下一次释放使用新的 skill runtime" },
                        new MobaAcceptanceTimelineStepExpectation { stepId = "second_cast", atMs = 5500, action = "press", actorAlias = "caster", slot = 3, note = "第二次释放三技能，衰减应从头开始" },
                        new MobaAcceptanceTimelineStepExpectation { stepId = "wait_for_second_two_hits", atMs = 5501, action = "wait", durationMs = 2200, note = "等待第二次 runtime 的前两段伤害" }
                    },
                    stateExpectations = new[]
                    {
                        new MobaAcceptanceStateExpectation
                        {
                            alias = "target",
                            property = "hp",
                            comparator = "eq",
                            expectedFloat = 60f,
                            note = "目标生命值在复位到 180 后，应再次承受 80+40 伤害并剩余 60 点生命值。"
                        }
                    }
                },
                mustContain = new[]
                {
                    new MobaAcceptanceTraceExpectation { kind = "SkillCast", configId = 10020301, minCount = 2, maxCount = 2 },
                    new MobaAcceptanceTraceExpectation { kind = "EffectExecution", configId = 10020301, minCount = 2, maxCount = 2 },
                    new MobaAcceptanceTraceExpectation { kind = "BuffApply", configId = 10020301, minCount = 2, maxCount = 2 },
                    new MobaAcceptanceTraceExpectation { kind = "DamageApply", configId = 10020301, minCount = 4, maxCount = 4 }
                },
                relationships = new[]
                {
                    new MobaAcceptanceRelationshipExpectation { parentKind = "SkillCast", parentConfigId = 10020301, childKind = "EffectExecution", childConfigId = 10020301 },
                    new MobaAcceptanceRelationshipExpectation { parentKind = "EffectExecution", parentConfigId = 10020301, childKind = "BuffApply", childConfigId = 10020301 },
                    new MobaAcceptanceRelationshipExpectation { parentKind = "EffectExecution", parentConfigId = 10020301, childKind = "DamageApply", childConfigId = 10020301 }
                }
            };

            var summary = RunExpectation(expectation);

            AssertPassed(summary);
            Assert.IsTrue(summary.result.buffApplied);
            AssertNoMissingTraceNodes(summary);
            AssertArtifactsExist(summary);

            var records = LoadTraceRecords(summary);
            var effectRoots = MobaAcceptanceTraceAssert.CollectEffectRootIds(records, 10020301);
            Assert.AreEqual(2, effectRoots.Count, "the recast reset scenario should create exactly two Xiao Qiao skill 3 effect roots.");
            MobaAcceptanceTraceAssert.AssertRepeatHitDamagePattern(records, effectRoots[0], 10020301, expectedHitCount: 2, "the first Xiao Qiao skill 3 runtime should apply exactly two ordered hits to the same target.");
            MobaAcceptanceTraceAssert.AssertRepeatHitDamagePattern(records, effectRoots[1], 10020301, expectedHitCount: 2, "the second Xiao Qiao skill 3 runtime should restart repeat-hit decay with exactly two ordered hits to the same target.");
        }
    }
}
