using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Demo.Moba.Services;
using UnityEngine;

namespace AbilityKit.Game.Test.UnitTest
{
    public static class MobaAcceptanceTraceExporter
    {
        public const string DefaultArtifactDirectory = "artifacts/moba-acceptance";

        public static MobaAcceptanceTraceRecord[] CaptureTraceRecords(MobaSkillConfigTestHarness harness, string caseId)
        {
            if (harness == null) throw new ArgumentNullException(nameof(harness));

            var records = new List<MobaAcceptanceTraceRecord>(64);
            var seen = new HashSet<long>();
            var frame = harness.FrameTime.Frame.Value;
            var timeMs = (int)Math.Round(harness.FrameTime.Time * 1000d);

            foreach (MobaTraceKind kind in Enum.GetValues(typeof(MobaTraceKind)))
            {
                if (kind == MobaTraceKind.None) continue;

                foreach (var node in harness.Trace.GetNodesByKind((int)kind))
                {
                    if (!node.IsValid || !seen.Add(node.ContextId)) continue;
                    var metadata = node.Metadata;
                    records.Add(new MobaAcceptanceTraceRecord
                    {
                        caseId = caseId,
                        frame = frame,
                        timeMs = timeMs,
                        rootId = node.RootId,
                        parentId = node.ParentId,
                        nodeId = node.ContextId,
                        kind = ((MobaTraceKind)node.Kind).ToString(),
                        kindValue = node.Kind,
                        configId = metadata != null ? metadata.ConfigId : 0,
                        sourceActorId = metadata != null ? metadata.SourceActorId : 0,
                        targetActorId = metadata != null ? metadata.TargetActorId : 0,
                        sourceId = metadata != null ? metadata.SourceId : 0,
                        targetId = metadata != null ? metadata.TargetId : 0,
                        originSourceId = metadata != null ? metadata.OriginSourceId : 0,
                        originTargetId = metadata != null ? metadata.OriginTargetId : 0,
                        originSource = metadata != null ? metadata.OriginSource : null,
                        originTarget = metadata != null ? metadata.OriginTarget : null,
                        isRoot = node.IsRoot,
                        isEnded = node.IsEnded,
                        endedFrame = node.EndedFrame,
                        endReason = node.EndReason,
                        childCount = node.ChildCount
                    });
                }
            }

            records.Sort(CompareTraceRecord);
            return records.ToArray();
        }

        public static MobaAcceptanceSummary BuildSummary(
            MobaSkillConfigTestHarness harness,
            MobaAcceptanceExpectation expectation,
            MobaAcceptanceTraceRecord[] records,
            string traceJsonlPath,
            string summaryJsonPath)
        {
            if (harness == null) throw new ArgumentNullException(nameof(harness));
            if (expectation == null) throw new ArgumentNullException(nameof(expectation));
            if (records == null) throw new ArgumentNullException(nameof(records));

            var effectRootId = FindRootId(records, "EffectExecution", expectation.config != null ? expectation.config.effectId : 0);
            return new MobaAcceptanceSummary
            {
                caseId = expectation.caseId,
                worldId = expectation.worldId,
                tickRate = expectation.tickRate,
                accelerated = expectation.accelerated,
                scenario = expectation.scenario,
                actors = expectation.scenario != null && expectation.scenario.actors != null && expectation.scenario.actors.Length > 0 ? expectation.scenario.actors : expectation.actors,
                setupActions = expectation.scenario != null && expectation.scenario.setupActions != null && expectation.scenario.setupActions.Length > 0 ? expectation.scenario.setupActions : expectation.setupActions,
                timeline = expectation.scenario != null && expectation.scenario.timeline != null && expectation.scenario.timeline.Length > 0 ? expectation.scenario.timeline : expectation.timeline,
                stateExpectations = expectation.scenario != null && expectation.scenario.stateExpectations != null && expectation.scenario.stateExpectations.Length > 0 ? expectation.scenario.stateExpectations : expectation.stateExpectations,
                contextExpectations = expectation.scenario != null && expectation.scenario.contextExpectations != null && expectation.scenario.contextExpectations.Length > 0 ? expectation.scenario.contextExpectations : expectation.contextExpectations,
                input = expectation.input,
                config = expectation.config,
                result = new MobaAcceptanceResult
                {
                    passed = true,
                    skillCastTraceFound = Contains(records, "SkillCast", expectation.config != null ? expectation.config.skillId : 0, 0),
                    effectExecutionTraceFound = Contains(records, "EffectExecution", expectation.config != null ? expectation.config.effectId : 0, 0),
                    allExpectedActionsExecuted = ContainsAllActions(records, expectation, effectRootId),
                    projectileLaunched = expectation.config == null || expectation.config.expectedProjectile == null || Contains(records, "ProjectileLaunch", expectation.config.expectedProjectile.projectileId, effectRootId),
                    effectRootId = effectRootId,
                    finalFrame = harness.FrameTime.Frame.Value,
                    finalTimeMs = (int)Math.Round(harness.FrameTime.Time * 1000d),
                    traceNodeCount = records.Length
                },
                traceCounts = CountByKind(records),
                traceJsonlPath = NormalizePath(traceJsonlPath),
                summaryJsonPath = NormalizePath(summaryJsonPath)
            };
        }

        public static void Export(string artifactDirectory, MobaAcceptanceSummary summary, MobaAcceptanceTraceRecord[] records)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (records == null) throw new ArgumentNullException(nameof(records));

            var directory = string.IsNullOrEmpty(artifactDirectory) ? DefaultArtifactDirectory : artifactDirectory;
            Directory.CreateDirectory(directory);

            var tracePath = string.IsNullOrEmpty(summary.traceJsonlPath)
                ? Path.Combine(directory, summary.caseId + "_trace.jsonl")
                : summary.traceJsonlPath;
            var summaryPath = string.IsNullOrEmpty(summary.summaryJsonPath)
                ? Path.Combine(directory, summary.caseId + "_summary.json")
                : summary.summaryJsonPath;

            using (var writer = new StreamWriter(tracePath, false))
            {
                for (var i = 0; i < records.Length; i++)
                {
                    writer.WriteLine(JsonUtility.ToJson(records[i]));
                }
            }

            File.WriteAllText(summaryPath, JsonUtility.ToJson(summary, true));
        }

        public static string GetTraceJsonlPath(string artifactDirectory, string caseId)
        {
            return Path.Combine(string.IsNullOrEmpty(artifactDirectory) ? DefaultArtifactDirectory : artifactDirectory, caseId + "_trace.jsonl");
        }

        public static string GetSummaryJsonPath(string artifactDirectory, string caseId)
        {
            return Path.Combine(string.IsNullOrEmpty(artifactDirectory) ? DefaultArtifactDirectory : artifactDirectory, caseId + "_summary.json");
        }

        private static int CompareTraceRecord(MobaAcceptanceTraceRecord x, MobaAcceptanceTraceRecord y)
        {
            var rootCompare = x.rootId.CompareTo(y.rootId);
            if (rootCompare != 0) return rootCompare;
            return x.nodeId.CompareTo(y.nodeId);
        }

        private static bool Contains(MobaAcceptanceTraceRecord[] records, string kind, int configId, long rootId)
        {
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record.kind == kind && record.configId == configId && (rootId <= 0 || record.rootId == rootId)) return true;
            }

            return false;
        }

        private static long FindRootId(MobaAcceptanceTraceRecord[] records, string kind, int configId)
        {
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                if (record.kind == kind && record.configId == configId) return record.rootId;
            }

            return 0;
        }

        private static bool ContainsAllActions(MobaAcceptanceTraceRecord[] records, MobaAcceptanceExpectation expectation, long effectRootId)
        {
            var actions = expectation.config != null ? expectation.config.expectedActions : null;
            if (actions == null) return true;

            for (var i = 0; i < actions.Length; i++)
            {
                if (!Contains(records, "EffectAction", actions[i].actionId, effectRootId)) return false;
            }

            return true;
        }

        private static MobaAcceptanceTraceCount[] CountByKind(MobaAcceptanceTraceRecord[] records)
        {
            var counts = new Dictionary<string, int>();
            for (var i = 0; i < records.Length; i++)
            {
                var kind = records[i].kind ?? string.Empty;
                counts.TryGetValue(kind, out var count);
                counts[kind] = count + 1;
            }

            var result = new List<MobaAcceptanceTraceCount>(counts.Count);
            foreach (var pair in counts)
            {
                result.Add(new MobaAcceptanceTraceCount { kind = pair.Key, count = pair.Value });
            }

            result.Sort((a, b) => string.CompareOrdinal(a.kind, b.kind));
            return result.ToArray();
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');
        }
    }
}
