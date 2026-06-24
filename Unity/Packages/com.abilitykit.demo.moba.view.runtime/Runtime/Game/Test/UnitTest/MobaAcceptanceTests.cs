using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MobaAcceptanceTests
    {
        private const string Skill10010101ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010101.expected.json";
        private const string Skill10010401ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010401.expected.json";
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
