using System.IO;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MobaAcceptanceTests
    {
        private const string Skill10010101ExpectationPath = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010101.expected.json";
        private const string ExpectationDirectory = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations";
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
        public void AcceptanceExpectationDirectory_ExportsBatchSummary()
        {
            var batch = MobaAcceptanceRunner.RunExpectationDirectory(ExpectationDirectory, ArtifactDirectory, exportArtifacts: true, recursive: true);

            Assert.GreaterOrEqual(batch.total, 1);
            Assert.AreEqual(batch.total, batch.passed + batch.failed);
            Assert.IsTrue(batch.allPassed, "Acceptance batch has failed cases; inspect " + batch.batchSummaryJsonPath);
            Assert.IsTrue(File.Exists(batch.batchSummaryJsonPath), $"Batch summary artifact missing: {batch.batchSummaryJsonPath}");
        }
    }
}
