using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MobaAcceptanceTests
    {
        private const string Skill10010101ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010101.expected.json";
        private const string Skill10010401ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010401.expected.json";
        private const string Skill10020101ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020101.expected.json";
        private const string Skill10020101ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020101_scenario.expected.json";
        private const string Skill10020201ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020201.expected.json";
        private const string Skill10020201ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020201_scenario.expected.json";
        private const string Skill10020301ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020301.expected.json";
        private const string Skill10020301ScenarioExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020301_scenario.expected.json";
        private const string ExpectationDirectory = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations";
        private const string SkillFlowsPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Resources/moba/skill_flows.json";
        private const string SkillTriggerDirectory = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Resources/ability/triggers/skills";
        private const string ArtifactDirectory = "artifacts/moba-acceptance";

        [Test]
        public void Skill10010101_ExportsTraceAndMatchesGoldenExpectation()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10010101ExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.projectileLaunched);
            Assert.Greater(summary.result.effectRootId, 0);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");
        }

        [Test]
        public void Skill10010401_ExportsTraceAndMatchesBuffGoldenExpectation()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10010401ExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.buffApplied);
            Assert.Greater(summary.result.effectRootId, 0);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");
        }

        [Test]
        public void Skill10020101_ExportsTraceAndMatchesXiaoQiaoProjectileExpectation()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020101ExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.projectileLaunched);
            Assert.Greater(summary.result.effectRootId, 0);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");
        }

        [Test]
        public void Skill10020101_ScenarioExportsTraceAndConfirmsProjectileDamage()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020101ScenarioExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.projectileLaunched);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");

            var records = MobaAcceptanceRunner.LoadTraceRecords(summary.traceJsonlPath);
            var expectation = MobaAcceptanceRunner.LoadExpectation(Skill10020101ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for projectile damage scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            AssertTraceNodeKindInRoot(records, effectRootId, "ProjectileLaunch", 30020101, "the projectile scenario should launch the configured projectile under the Xiao Qiao skill 1 effect root.");
            AssertSingleTargetDamageInRoot(records, effectRootId, expectation.config.effectId, expectedHitCount: 1, "the projectile scenario should damage exactly one target exactly once under the same effect root.");
        }

        [Test]
        public void Skill10020201_ExportsTraceAndMatchesXiaoQiaoAreaExpectation()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020201ExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.areaSpawned);
            Assert.Greater(summary.result.effectRootId, 0);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");
        }

        [Test]
        public void Skill10020201_ScenarioExportsTraceAndConfirmsAreaDamage()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020201ScenarioExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.areaSpawned);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");

            var records = MobaAcceptanceRunner.LoadTraceRecords(summary.traceJsonlPath);
            var expectation = MobaAcceptanceRunner.LoadExpectation(Skill10020201ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for area scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            AssertTraceNodeKindInRoot(records, effectRootId, "AreaSpawn", 40020201, "area spawn should remain under the Xiao Qiao skill 2 effect root.");
            AssertTraceNodeKindInRoot(records, effectRootId, "AreaEnter", 40020201, "the spawned area should emit an enter trace when the target is inside the area.");
            AssertSingleTargetDamageInRoot(records, effectRootId, expectation.config.effectId, expectedHitCount: 1, "the area scenario should damage exactly one target exactly once under the same effect root.");
        }

        private static void AssertTraceNodeKindInRoot(MobaAcceptanceTraceRecord[] records, long rootId, string kind, int configId, string message)
        {
            Assert.IsNotNull(records, "Trace records are required for strict area validation.");

            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (!string.Equals(record.kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
                if (record.configId != configId) continue;
                if (rootId > 0 && record.rootId != rootId) continue;
                return;
            }

            Assert.Fail(message + $" kind={kind}, configId={configId}, rootId={rootId}.");
        }

        [Test]
        public void Skill10020301_ExportsTraceAndMatchesXiaoQiaoUltimateExpectation()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020301ExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.skillCastTraceFound);
            Assert.IsTrue(summary.result.effectExecutionTraceFound);
            Assert.IsTrue(summary.result.allExpectedActionsExecuted);
            Assert.IsTrue(summary.result.buffApplied);
            Assert.Greater(summary.result.effectRootId, 0);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");
        }

        [Test]
        public void Skill10020301_ScenarioExportsTraceAndConfirmsIntervalDamage()
        {
            var summary = MobaAcceptanceRunner.RunSkillExpectationFile(Skill10020301ScenarioExpectationPath, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.buffApplied);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");

            var records = MobaAcceptanceRunner.LoadTraceRecords(summary.traceJsonlPath);
            var expectation = MobaAcceptanceRunner.LoadExpectation(Skill10020301ScenarioExpectationPath);
            Assert.IsTrue(MobaAcceptanceExpectationAssert.TryGetEffectRootId(records, expectation.config.effectId, out var effectRootId), "Missing effect root trace for interval damage scenario.");
            MobaAcceptanceExpectationAssert.AssertMatches(expectation, records);

            AssertTraceNodeKindInRoot(records, effectRootId, "BuffApply", 10020301, "the ultimate should apply its persistent buff under the same effect root.");
            AssertTraceNodeKindInRoot(records, effectRootId, "DamageApply", 10020301, "the interval tick should apply damage under the same effect root.");
            AssertRepeatHitDamagePattern(records, effectRootId, 10020301, expectedHitCount: 2, "the ultimate should hit the same target exactly twice under one skill runtime before the repeat-hit decay expectation is considered valid.");
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
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "first_cast",
                            atMs = 0,
                            action = "press",
                            actorAlias = "caster",
                            slot = 3,
                            note = "第一次释放三技能"
                        },
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "wait_for_first_two_hits",
                            atMs = 1,
                            action = "wait",
                            durationMs = 2200,
                            note = "等待第一次 runtime 的前两段伤害"
                        },
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "reset_target_hp",
                            atMs = 2300,
                            action = "set_attr",
                            actorAlias = "target",
                            property = "hp",
                            value = 180f,
                            note = "复位目标生命值，准备验证下一次释放是否重新从满伤开始"
                        },
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "wait_for_first_runtime_end",
                            atMs = 2301,
                            action = "wait",
                            durationMs = 3100,
                            note = "等待第一次三技能持续结束，确保下一次释放使用新的 skill runtime"
                        },
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "second_cast",
                            atMs = 5500,
                            action = "press",
                            actorAlias = "caster",
                            slot = 3,
                            note = "第二次释放三技能，衰减应从头开始"
                        },
                        new MobaAcceptanceTimelineStepExpectation
                        {
                            stepId = "wait_for_second_two_hits",
                            atMs = 5501,
                            action = "wait",
                            durationMs = 2200,
                            note = "等待第二次 runtime 的前两段伤害"
                        }
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

            var summary = MobaAcceptanceRunner.RunSkillExpectation(expectation, ArtifactDirectory, exportArtifacts: true);

            Assert.IsTrue(summary.result.passed);
            Assert.IsTrue(summary.result.buffApplied);
            Assert.AreEqual(0, summary.result.missingExpectedTraceNodeCount, summary.coverage != null ? summary.coverage.missingTraceNodes : string.Empty);
            Assert.IsTrue(File.Exists(summary.traceJsonlPath), $"Trace jsonl artifact missing: {summary.traceJsonlPath}");
            Assert.IsTrue(File.Exists(summary.summaryJsonPath), $"Summary json artifact missing: {summary.summaryJsonPath}");

            var records = MobaAcceptanceRunner.LoadTraceRecords(summary.traceJsonlPath);
            var effectRoots = CollectEffectRootIds(records, 10020301);
            Assert.AreEqual(2, effectRoots.Count, "the recast reset scenario should create exactly two Xiao Qiao skill 3 effect roots.");
            AssertRepeatHitDamagePattern(records, effectRoots[0], 10020301, expectedHitCount: 2, "the first Xiao Qiao skill 3 runtime should apply exactly two ordered hits to the same target.");
            AssertRepeatHitDamagePattern(records, effectRoots[1], 10020301, expectedHitCount: 2, "the second Xiao Qiao skill 3 runtime should restart repeat-hit decay with exactly two ordered hits to the same target.");
        }

        private static void AssertSingleTargetDamageInRoot(MobaAcceptanceTraceRecord[] records, long rootId, int configId, int expectedHitCount, string message)
        {
            var hits = CollectTraceNodesInRoot(records, rootId, "DamageApply", configId);
            Assert.AreEqual(expectedHitCount, hits.Count, message + $" configId={configId}, rootId={rootId}, actual={hits.Count}, expected={expectedHitCount}.");
            Assert.Greater(hits[0].targetActorId, 0, message + " hit must target a valid actor.");

            for (var i = 1; i < hits.Count; i++)
            {
                Assert.AreEqual(hits[0].targetActorId, hits[i].targetActorId, message + $" hitIndex={i} should remain on the same target actor.");
            }
        }

        private static void AssertRepeatHitDamagePattern(MobaAcceptanceTraceRecord[] records, long rootId, int configId, int expectedHitCount, string message)
        {
            var hits = CollectTraceNodesInRoot(records, rootId, "DamageApply", configId);
            Assert.AreEqual(expectedHitCount, hits.Count, message + $" configId={configId}, rootId={rootId}, actual={hits.Count}, expected={expectedHitCount}.");

            var targetActorId = hits[0].targetActorId;
            Assert.Greater(targetActorId, 0, message + " first hit must target a valid actor.");

            for (var i = 0; i < hits.Count; i++)
            {
                Assert.AreEqual(targetActorId, hits[i].targetActorId, message + $" hitIndex={i} should remain on the same target actor.");
                if (i == 0) continue;
                Assert.GreaterOrEqual(hits[i].timeMs, hits[i - 1].timeMs, message + $" hitIndex={i} should not occur earlier than the previous hit.");
                Assert.Greater(hits[i].frame, hits[i - 1].frame, message + $" hitIndex={i} should occur on a later frame than the previous hit.");
            }
        }

        private static List<MobaAcceptanceTraceRecord> CollectTraceNodesInRoot(MobaAcceptanceTraceRecord[] records, long rootId, string kind, int configId)
        {
            Assert.IsNotNull(records, "Trace records are required for strict root trace validation.");

            var matches = new List<MobaAcceptanceTraceRecord>();
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (!string.Equals(record.kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
                if (record.configId != configId) continue;
                if (rootId > 0 && record.rootId != rootId) continue;
                matches.Add(record);
            }

            return matches;
        }

        private static List<long> CollectEffectRootIds(MobaAcceptanceTraceRecord[] records, int effectId)
        {
            Assert.IsNotNull(records, "Trace records are required for strict effect-root validation.");

            var rootIds = new List<long>();
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (!string.Equals(record.kind, "EffectExecution", StringComparison.OrdinalIgnoreCase)) continue;
                if (record.configId != effectId) continue;
                if (record.rootId <= 0) continue;
                if (rootIds.Contains(record.rootId)) continue;
                rootIds.Add(record.rootId);
            }

            return rootIds;
        }

        [Test]
        public void TimelineEffectSkills_WithConfiguredTriggers_HaveFormalAcceptanceContracts()
        {
            var skillFlowsPath = MobaAcceptanceRunner.ResolveProjectRelativePath(SkillFlowsPath);
            var triggerDirectory = MobaAcceptanceRunner.ResolveProjectRelativePath(SkillTriggerDirectory);
            var expectationDirectory = MobaAcceptanceRunner.ResolveProjectRelativePath(ExpectationDirectory);
            var expectedSkillIds = FindSkillIdsWithConfiguredSkillTriggers(skillFlowsPath, triggerDirectory);
            var coveredSkillIds = FindCoveredSkillIds(expectationDirectory, formalOnly: true);

            CollectionAssert.IsSubsetOf(expectedSkillIds, coveredSkillIds, "Every configured timeline effect skill must have at least one formal contract/golden *.expected.json before the skill is considered done; draft files do not satisfy this gate.");
        }

        [Test]
        public void AcceptanceExpectationDirectory_ExportsBatchSummary()
        {
            var batch = MobaAcceptanceRunner.RunExpectationDirectory(ExpectationDirectory, ArtifactDirectory, exportArtifacts: true, recursive: true);

            Assert.GreaterOrEqual(batch.total, 1);
            Assert.AreEqual(batch.total, batch.passed + batch.failed);
            Assert.IsTrue(string.IsNullOrEmpty(batch.categoryFilter));
            Assert.IsTrue(batch.allPassed, "Acceptance batch has failed cases; inspect " + batch.batchSummaryJsonPath);
            Assert.IsTrue(File.Exists(batch.batchSummaryJsonPath), $"Batch summary artifact missing: {batch.batchSummaryJsonPath}");
        }

        [Test]
        public void ContractCategory_ShouldContainBuffAcceptanceContract()
        {
            var expectation = MobaAcceptanceRunner.LoadExpectation(Skill10010401ExpectationPath);
            Assert.AreEqual("contract", MobaAcceptanceRunner.ResolveCategory(expectation));
            Assert.IsFalse(MobaAcceptanceRunner.HasTag(expectation, "golden"));
            Assert.IsTrue(MobaAcceptanceRunner.HasTag(expectation, "buff"));
        }

        [Test]
        public void GoldenCategory_ShouldContainProjectileGoldenSample()
        {
            var expectation = MobaAcceptanceRunner.LoadExpectation(Skill10010101ExpectationPath);
            Assert.AreEqual("golden", MobaAcceptanceRunner.ResolveCategory(expectation));
            Assert.IsTrue(MobaAcceptanceRunner.HasTag(expectation, "golden"));
            Assert.IsTrue(MobaAcceptanceRunner.HasTag(expectation, "projectile"));
        }

        [Test]
        public void AcceptanceRunner_ShouldFilterByCategoryAndTag()
        {
            var contractBatch = MobaAcceptanceRunner.RunContractExpectationDirectory(ExpectationDirectory, ArtifactDirectory, exportArtifacts: false, recursive: true, tagFilter: "buff");
            var goldenBatch = MobaAcceptanceRunner.RunGoldenExpectationDirectory(ExpectationDirectory, ArtifactDirectory, exportArtifacts: false, recursive: true, tagFilter: "projectile");

            Assert.AreEqual("contract", contractBatch.categoryFilter);
            Assert.AreEqual("buff", contractBatch.tagFilter);
            Assert.GreaterOrEqual(contractBatch.total, 1);
            Assert.IsTrue(contractBatch.allPassed);
            Assert.AreEqual("golden", goldenBatch.categoryFilter);
            Assert.AreEqual("projectile", goldenBatch.tagFilter);
            Assert.GreaterOrEqual(goldenBatch.total, 1);
            Assert.IsTrue(goldenBatch.allPassed);
        }

        [Test]
        public void DraftGenerator_ShouldGenerateProjectileContractDraft()
        {
            var draft = MobaAcceptanceDraftGenerator.GenerateContractDraftForSkill(10010101, SkillFlowsPath, SkillTriggerDirectory);

            Assert.AreEqual(10010101, draft.config.skillId);
            Assert.AreEqual(10001, draft.config.effectId);
            Assert.AreEqual("draft", draft.category);
            Assert.AreEqual(MobaAcceptanceDraftGenerator.GeneratedFrom, draft.generatedFrom);
            Assert.IsTrue(draft.mustContain != null && draft.mustContain.Length >= 3);
            Assert.IsTrue(draft.relationships != null && draft.relationships.Length >= 2);
        }

        [Test]
        public void DraftGenerator_ShouldGenerateBuffContractDraft()
        {
            var draft = MobaAcceptanceDraftGenerator.GenerateContractDraftForSkill(10010401, SkillFlowsPath, SkillTriggerDirectory);

            Assert.AreEqual(10010401, draft.config.skillId);
            Assert.AreEqual(10004, draft.config.effectId);
            Assert.AreEqual("draft", draft.category);
            Assert.AreEqual(MobaAcceptanceDraftGenerator.GeneratedFrom, draft.generatedFrom);
            Assert.IsTrue(draft.mustContain != null && draft.mustContain.Length >= 3);
            Assert.IsTrue(draft.relationships != null && draft.relationships.Length >= 2);
        }

        [Test]
        public void DraftGenerator_ShouldExportContractDraftFile()
        {
            var outputDirectory = Path.Combine(ArtifactDirectory, "draft-generator-tests");
            var outputPath = MobaAcceptanceDraftGenerator.ExportContractDraftForSkill(10010101, SkillFlowsPath, SkillTriggerDirectory, outputDirectory);
            var loaded = MobaAcceptanceRunner.LoadExpectation(outputPath);

            Assert.IsTrue(File.Exists(outputPath), "Draft export file missing: " + outputPath);
            Assert.AreEqual("skill_10010101_contract_draft", loaded.caseId);
            Assert.AreEqual(10010101, loaded.config.skillId);
            Assert.AreEqual("draft", loaded.category);
            Assert.AreEqual(MobaAcceptanceDraftGenerator.GeneratedFrom, loaded.generatedFrom);
        }

        [Test]
        public void ScenarioSetupActions_ShouldMoveActorAndSetAttributes()
        {
            var expectation = CreateSetupActionSmokeExpectation(
                "scenario_setup_move_attr_smoke",
                new[]
                {
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "move_to",
                        actorAlias = "target",
                        position = new MobaAcceptanceVector3Expectation { x = 4f, y = 0f, z = 2f }
                    },
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "set_attr",
                        actorAlias = "target",
                        property = "hp",
                        value = 777f
                    },
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "set_attr",
                        actorAlias = "target",
                        property = "max_hp",
                        value = 1200f
                    }
                },
                new[]
                {
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "target",
                        property = "position",
                        comparator = "eq",
                        expectedVector = new MobaAcceptanceVector3Expectation { x = 4f, y = 0f, z = 2f },
                        tolerance = new MobaAcceptanceVector3Expectation { x = 0.01f, y = 0.01f, z = 0.01f }
                    },
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "target",
                        property = "hp",
                        comparator = "eq",
                        expectedFloat = 777f
                    },
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "target",
                        property = "maxHp",
                        comparator = "eq",
                        expectedFloat = 1200f
                    }
                });

            var summary = MobaAcceptanceRunner.RunSkillExpectation(expectation, ArtifactDirectory, exportArtifacts: false);

            Assert.IsTrue(summary.result.passed);
        }

        [Test]
        public void ScenarioSetupActions_ShouldSpawnActorAndBindAlias()
        {
            var expectation = CreateSetupActionSmokeExpectation(
                "scenario_setup_spawn_actor_smoke",
                new[]
                {
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "spawn_actor",
                        alias = "summon",
                        teamId = 1,
                        heroId = 1,
                        attributeTemplateId = 1001,
                        unitSubType = 2,
                        mainType = 1,
                        sourceAlias = "caster",
                        sourceKind = "Summon",
                        position = new MobaAcceptanceVector3Expectation { x = 2f, y = 0f, z = 3f }
                    }
                },
                new[]
                {
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "summon",
                        property = "exists",
                        comparator = "eq",
                        expectedBool = true
                    },
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "summon",
                        property = "teamId",
                        comparator = "eq",
                        expectedInt = 1
                    },
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "summon",
                        property = "position",
                        comparator = "eq",
                        expectedVector = new MobaAcceptanceVector3Expectation { x = 2f, y = 0f, z = 3f },
                        tolerance = new MobaAcceptanceVector3Expectation { x = 0.01f, y = 0.01f, z = 0.01f }
                    }
                });

            var summary = MobaAcceptanceRunner.RunSkillExpectation(expectation, ArtifactDirectory, exportArtifacts: false);

            Assert.IsTrue(summary.result.passed);
        }

        [Test]
        public void ScenarioSetupActions_ShouldAddAndRemoveBuffs()
        {
            var addExpectation = CreateSetupActionSmokeExpectation(
                "scenario_setup_add_buff_smoke",
                new[]
                {
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "add_buff",
                        targetAlias = "target",
                        sourceAlias = "caster",
                        buffId = 1,
                        durationOverrideMs = 10000
                    }
                },
                new[]
                {
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "target",
                        property = "hasBuff",
                        comparator = "eq",
                        expectedInt = 1,
                        expectedBool = true
                    }
                });
            var addSummary = MobaAcceptanceRunner.RunSkillExpectation(addExpectation, ArtifactDirectory, exportArtifacts: false);
            Assert.IsTrue(addSummary.result.passed);
            Assert.IsTrue(addSummary.result.buffApplied);

            var removeExpectation = CreateSetupActionSmokeExpectation(
                "scenario_setup_remove_buff_smoke",
                new[]
                {
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "add_buff",
                        targetAlias = "target",
                        sourceAlias = "caster",
                        buffId = 1,
                        durationOverrideMs = 10000
                    },
                    new MobaAcceptanceSetupActionExpectation
                    {
                        action = "remove_buff",
                        targetAlias = "target",
                        sourceAlias = "caster",
                        buffId = 1,
                        removeAll = true
                    }
                },
                new[]
                {
                    new MobaAcceptanceStateExpectation
                    {
                        alias = "target",
                        property = "hasBuff",
                        comparator = "eq",
                        expectedInt = 1,
                        expectedBool = false
                    }
                });
            var removeSummary = MobaAcceptanceRunner.RunSkillExpectation(removeExpectation, ArtifactDirectory, exportArtifacts: false);
            Assert.IsTrue(removeSummary.result.passed);
            Assert.IsTrue(removeSummary.result.buffApplied);
        }

        private static MobaAcceptanceExpectation CreateSetupActionSmokeExpectation(string caseId, MobaAcceptanceSetupActionExpectation[] setupActions, MobaAcceptanceStateExpectation[] stateExpectations)
        {
            return new MobaAcceptanceExpectation
            {
                caseId = caseId,
                worldId = caseId + "_world",
                tickRate = 30,
                accelerated = true,
                category = "draft",
                tags = new[] { "scenario", "setup_actions", "smoke" },
                scenario = new MobaAcceptanceScenarioExpectation
                {
                    category = "draft",
                    tags = new[] { "scenario", "setup_actions", "smoke" },
                    actors = new[]
                    {
                        new MobaAcceptanceActorExpectation
                        {
                            alias = "caster",
                            playerId = "p1",
                            teamId = 1,
                            heroId = 1,
                            attributeTemplateId = 1001,
                            hasSpawnPosition = true,
                            spawnPosition = new MobaAcceptanceVector3Expectation { x = 0f, y = 0f, z = 0f }
                        },
                        new MobaAcceptanceActorExpectation
                        {
                            alias = "target",
                            playerId = "p2",
                            teamId = 2,
                            heroId = 1,
                            attributeTemplateId = 1001,
                            hasSpawnPosition = true,
                            spawnPosition = new MobaAcceptanceVector3Expectation { x = 6f, y = 0f, z = 0f }
                        }
                    },
                    setupActions = setupActions,
                    stateExpectations = stateExpectations
                }
            };
        }

        private static int[] FindSkillIdsWithConfiguredSkillTriggers(string skillFlowsPath, string triggerDirectory)
        {
            Assert.IsTrue(File.Exists(skillFlowsPath), "Skill flow config missing: " + skillFlowsPath);
            Assert.IsTrue(Directory.Exists(triggerDirectory), "Skill trigger directory missing: " + triggerDirectory);

            var json = File.ReadAllText(skillFlowsPath);
            var result = new List<int>();
            var cursor = 0;
            while (TryReadNextIntProperty(json, "\"Id\"", ref cursor, out var skillId))
            {
                var nextEntry = json.IndexOf("\"Id\"", cursor, StringComparison.Ordinal);
                var entryEnd = nextEntry >= 0 ? nextEntry : json.Length;
                var entry = json.Substring(cursor, entryEnd - cursor);
                var effectCursor = 0;
                while (TryReadNextIntProperty(entry, "\"EffectId\"", ref effectCursor, out var effectId))
                {
                    if (File.Exists(Path.Combine(triggerDirectory, "trigger_" + effectId + ".json")))
                    {
                        result.Add(skillId);
                        break;
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }

        private static int[] FindCoveredSkillIds(string expectationDirectory, bool formalOnly)
        {
            Assert.IsTrue(Directory.Exists(expectationDirectory), "Expectation directory missing: " + expectationDirectory);

            var files = Directory.GetFiles(expectationDirectory, "*.expected.json", SearchOption.AllDirectories);
            var result = new List<int>();
            for (var i = 0; i < files.Length; i++)
            {
                var expectation = MobaAcceptanceRunner.LoadExpectation(files[i]);
                var category = MobaAcceptanceRunner.ResolveCategory(expectation);
                if (formalOnly && string.Equals(category, "draft", StringComparison.OrdinalIgnoreCase)) continue;
                if (formalOnly && !string.Equals(category, "contract", StringComparison.OrdinalIgnoreCase) && !string.Equals(category, "golden", StringComparison.OrdinalIgnoreCase)) continue;
                if (expectation.config != null && expectation.config.skillId > 0) result.Add(expectation.config.skillId);
            }

            result.Sort();
            return result.ToArray();
        }

        private static bool TryReadNextIntProperty(string json, string propertyName, ref int cursor, out int value)
        {
            value = 0;
            var propertyIndex = json.IndexOf(propertyName, cursor, StringComparison.Ordinal);
            if (propertyIndex < 0) return false;

            var colonIndex = json.IndexOf(':', propertyIndex + propertyName.Length);
            if (colonIndex < 0) return false;

            var start = colonIndex + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            var end = start;
            if (end < json.Length && json[end] == '-') end++;
            while (end < json.Length && char.IsDigit(json[end])) end++;
            cursor = end;
            return int.TryParse(json.Substring(start, end - start), out value);
        }
    }
}
