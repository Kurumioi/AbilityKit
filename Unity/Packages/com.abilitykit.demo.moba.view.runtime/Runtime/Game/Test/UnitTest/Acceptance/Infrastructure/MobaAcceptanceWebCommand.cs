using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AbilityKit.Game.Test.UnitTest
{
    public static class MobaAcceptanceWebCommand
    {
        private const string ScenarioArgument = "-mobaAcceptanceScenario";
        private const string OutputArgument = "-mobaAcceptanceOutput";

        private static readonly Dictionary<string, string> ExpectationPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["lianpo-skill1-dash"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010101_scenario.expected.json",
            ["lianpo-skill2-area"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010201_scenario.expected.json",
            ["lianpo-skill3-combo"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10010301_scenario.expected.json",
            ["xiaoqiao-skill1-projectile"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020101_scenario.expected.json",
            ["xiaoqiao-skill2-area"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020201_scenario.expected.json",
            ["xiaoqiao-skill3-ultimate"] = "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/Expectations/skill_10020301_scenario.expected.json"
        };

        public static void RunFromCommandLine()
        {
            var scenarioId = GetArgumentValue(ScenarioArgument);
            var outputDirectory = GetArgumentValue(OutputArgument);
            try
            {
                if (string.IsNullOrWhiteSpace(scenarioId) || !ExpectationPaths.TryGetValue(scenarioId, out var expectationPath))
                {
                    throw new InvalidOperationException("Unknown Web acceptance scenario: " + scenarioId);
                }
                if (string.IsNullOrWhiteSpace(outputDirectory))
                {
                    throw new InvalidOperationException("Missing required output directory.");
                }

                var summary = MobaAcceptanceRunner.RunSkillExpectationFile(expectationPath, outputDirectory, exportArtifacts: true);
                if (summary == null || summary.result == null || !summary.result.passed)
                {
                    throw new InvalidOperationException("Acceptance DSL scenario did not pass: " + scenarioId);
                }

                Debug.Log("[MobaAcceptanceWebCommand] Completed DSL scenario " + scenarioId + " caseId=" + summary.caseId);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string GetArgumentValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
            }

            return null;
        }
    }
}
