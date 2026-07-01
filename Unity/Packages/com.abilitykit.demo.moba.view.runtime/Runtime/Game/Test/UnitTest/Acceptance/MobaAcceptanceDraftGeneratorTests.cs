using System.IO;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MobaAcceptanceDraftGeneratorTests : MobaAcceptanceTestBase
    {
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
            var loaded = LoadExpectation(outputPath);

            Assert.IsTrue(File.Exists(outputPath), "Draft export file missing: " + outputPath);
            Assert.AreEqual("skill_10010101_contract_draft", loaded.caseId);
            Assert.AreEqual(10010101, loaded.config.skillId);
            Assert.AreEqual("draft", loaded.category);
            Assert.AreEqual(MobaAcceptanceDraftGenerator.GeneratedFrom, loaded.generatedFrom);
        }
    }
}
