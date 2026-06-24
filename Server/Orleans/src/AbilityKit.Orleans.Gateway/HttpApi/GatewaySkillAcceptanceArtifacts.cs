namespace AbilityKit.Orleans.Gateway.HttpApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

internal static class GatewaySkillAcceptanceArtifacts
{
    public const string DefaultArtifactDirectory = "artifacts/moba-acceptance";
    private const string ArtifactRootDirectory = "artifacts";
    private const string BatchSummaryFileName = "batch_summary.json";
    private const string SummaryFileSuffix = "_summary.json";
    private const string TraceFileSuffix = "_trace.jsonl";
    private const string SummarySearchPattern = "*_summary.json";
    private const int DefaultTraceLimit = 500;
    private const int MaxTraceLimit = 5000;

    public static AdminSkillAcceptanceBatchHttpResponse GetBatch(string? artifactDirectory)
    {
        var directory = ResolveArtifactDirectory(artifactDirectory);
        var warnings = new List<string>();
        if (!directory.IsAllowed)
        {
            warnings.Add(directory.ErrorMessage ?? "Artifact directory is outside the allowed artifact root.");
            return new AdminSkillAcceptanceBatchHttpResponse(
                directory.DisplayPath,
                false,
                null,
                Array.Empty<AdminSkillAcceptanceCaseListItemHttpResponse>(),
                warnings.ToArray(),
                DateTime.UtcNow.Ticks);
        }

        var batchPath = Path.Combine(directory.FullPath, BatchSummaryFileName);

        if (!Directory.Exists(directory.FullPath))
        {
            warnings.Add($"Artifact directory does not exist: {directory.DisplayPath}");
            return new AdminSkillAcceptanceBatchHttpResponse(
                directory.DisplayPath,
                false,
                null,
                Array.Empty<AdminSkillAcceptanceCaseListItemHttpResponse>(),
                warnings.ToArray(),
                DateTime.UtcNow.Ticks);
        }

        JsonNode? batch = null;
        if (File.Exists(batchPath))
        {
            batch = ReadJsonNode(batchPath, warnings);
        }
        else
        {
            warnings.Add($"Batch summary does not exist: {NormalizePath(batchPath)}");
        }

        var cases = Directory.EnumerateFiles(directory.FullPath, SummarySearchPattern, SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), BatchSummaryFileName, StringComparison.OrdinalIgnoreCase))
            .Select(path => BuildCaseListItem(directory.FullPath, path, warnings))
            .Where(item => item is not null)
            .Cast<AdminSkillAcceptanceCaseListItemHttpResponse>()
            .OrderBy(item => item.CaseId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AdminSkillAcceptanceBatchHttpResponse(
            directory.DisplayPath,
            batch is not null,
            batch,
            cases,
            warnings.ToArray(),
            DateTime.UtcNow.Ticks);
    }

    public static IResult GetCase(string caseId, string? artifactDirectory, int? traceLimit)
    {
        var caseIdValidation = ValidateCaseId(caseId);
        if (!caseIdValidation.IsValid)
        {
            return Results.BadRequest(BuildError("InvalidCaseId", caseIdValidation.ErrorMessage ?? "caseId is invalid.", "caseId"));
        }

        var directory = ResolveArtifactDirectory(artifactDirectory);
        if (!directory.IsAllowed)
        {
            return Results.BadRequest(BuildError("ArtifactDirectoryOutOfBounds", directory.ErrorMessage ?? "Artifact directory is outside the allowed artifact root.", "artifactDirectory"));
        }

        var safeCaseId = caseIdValidation.CaseId;
        var summaryPath = Path.Combine(directory.FullPath, safeCaseId + SummaryFileSuffix);
        var tracePath = Path.Combine(directory.FullPath, safeCaseId + TraceFileSuffix);
        var warnings = new List<string>();

        if (!File.Exists(summaryPath))
        {
            return Results.NotFound(new AdminSkillAcceptanceCaseHttpResponse(
                safeCaseId,
                directory.DisplayPath,
                null,
                Array.Empty<JsonNode>(),
                NormalizePath(summaryPath),
                NormalizePath(tracePath),
                warnings.Concat(new[] { $"Summary artifact does not exist: {NormalizePath(summaryPath)}" }).ToArray(),
                DateTime.UtcNow.Ticks));
        }

        var summary = ReadJsonNode(summaryPath, warnings);
        var records = ReadTraceRecords(tracePath, traceLimit ?? DefaultTraceLimit, warnings);

        return Results.Ok(new AdminSkillAcceptanceCaseHttpResponse(
            safeCaseId,
            directory.DisplayPath,
            summary,
            records,
            NormalizePath(summaryPath),
            NormalizePath(tracePath),
            warnings.ToArray(),
            DateTime.UtcNow.Ticks));
    }

    public static AdminSkillAcceptanceRunPlanHttpResponse GetRunPlan(string? artifactDirectory)
    {
        var directory = ResolveArtifactDirectory(artifactDirectory);
        var allowedScripts = BuildAllowedScripts(directory.FullPath);
        return new AdminSkillAcceptanceRunPlanHttpResponse(
            false,
            directory.IsAllowed
                ? "Scenario execution from AdminConsole is intentionally disabled in this phase. Use local Unity/unit-test or CI scripts to produce artifacts, then refresh this report viewer."
                : directory.ErrorMessage ?? "Artifact directory is outside the allowed artifact root.",
            directory.DisplayPath,
            "read-only-boundary",
            false,
            BuildExecutionStrategies(),
            allowedScripts,
            new[]
            {
                "Authenticated admin session",
                "Explicit operator reason",
                "Environment allowExecution=true configuration",
                "Script id must exist in server-side allow-list"
            },
            new[]
            {
                "operationId",
                "requestedBy",
                "requestedAtUtcTicks",
                "artifactDirectory",
                "scriptId or ciWorkflowId",
                "scenarioFilter",
                "exitCode",
                "logPath",
                "producedArtifactDirectory"
            },
            new[]
            {
                "Read-only artifact browsing is available now.",
                "A controlled run endpoint should wrap an allow-listed script or CI job before enabling execution.",
                "Scenario JSON remains the stable contract shared by Web, Unity Editor, CLI and CI.",
                "The gateway must never execute arbitrary command text from the browser.",
                $"Artifact browsing is constrained to the workspace {ArtifactRootDirectory}/ root."
            },
            DateTime.UtcNow.Ticks);
    }

    private static AdminSkillAcceptanceExecutionStrategyHttpResponse[] BuildExecutionStrategies()
    {
        return new[]
        {
            new AdminSkillAcceptanceExecutionStrategyHttpResponse(
                "local-script",
                "本机 allow-listed 脚本包装",
                "AdminConsole may only request a predefined script id; raw command, arbitrary path and user supplied shell text stay forbidden.",
                "planned",
                "Use a server-side allow-list to launch a checked-in script that regenerates Scenario artifacts under the configured artifact directory."),
            new AdminSkillAcceptanceExecutionStrategyHttpResponse(
                "ci-job",
                "CI Job 包装",
                "AdminConsole may enqueue a fixed CI workflow with scenario path and artifact target parameters; execution remains outside the gateway process.",
                "recommended",
                "Prefer CI for shared environments so logs, approvals, retention and artifact publishing are handled by the build system."),
            new AdminSkillAcceptanceExecutionStrategyHttpResponse(
                "manual-refresh",
                "手动生成后刷新",
                "Unity/unit-test/headless runner produces artifacts first; this API only reads the resulting JSON/JSONL files.",
                "available",
                "Current phase keeps the gateway read-only while preserving Scenario JSON as the stable contract.")
        };
    }

    private static AdminSkillAcceptanceAllowedScriptHttpResponse[] BuildAllowedScripts(string directory)
    {
        var scripts = new[]
        {
            new SkillAcceptanceAllowedScriptDefinition(
                "moba-acceptance-local",
                "MOBA Scenario 本机验收",
                "Unity/Packages/com.abilitykit.demo.moba.view.runtime/Runtime/Game/Test/UnitTest/MobaAcceptanceRunner.cs",
                "unity-test-runner",
                new[] { "--scenarioDir <checked-in-or-mounted-scenario-dir>", "--artifactDirectory " + NormalizePath(directory) },
                new[] { BatchSummaryFileName, SummarySearchPattern, "*" + TraceFileSuffix }),
            new SkillAcceptanceAllowedScriptDefinition(
                "moba-acceptance-ci",
                "MOBA Scenario CI 验收",
                ".github/workflows/moba-acceptance.yml",
                "ci-workflow",
                new[] { "scenarioFilter", "artifactDirectory" },
                new[] { "moba-acceptance artifact bundle", "gateway-readable JSON/JSONL reports" })
        };

        return scripts.Select(script => new AdminSkillAcceptanceAllowedScriptHttpResponse(
            script.Id,
            script.DisplayName,
            NormalizePath(script.RelativePath),
            script.Shell,
            File.Exists(Path.GetFullPath(script.RelativePath, Directory.GetCurrentDirectory())),
            script.Arguments,
            script.Produces)).ToArray();
    }

    private static AdminSkillAcceptanceCaseListItemHttpResponse? BuildCaseListItem(string directory, string summaryPath, List<string> warnings)
    {
        var summary = ReadJsonNode(summaryPath, warnings);
        if (summary is null) return null;

        var caseId = ReadString(summary, "caseId") ?? Path.GetFileName(summaryPath).Replace(SummaryFileSuffix, string.Empty, StringComparison.OrdinalIgnoreCase);
        var result = summary["result"];
        var tracePath = ReadString(summary, "traceJsonlPath");
        if (string.IsNullOrWhiteSpace(tracePath))
        {
            tracePath = Path.Combine(directory, caseId + TraceFileSuffix);
        }

        return new AdminSkillAcceptanceCaseListItemHttpResponse(
            caseId,
            ReadString(summary, "description") ?? ReadString(summary["scenario"], "description"),
            ReadString(summary, "worldId"),
            ReadInt(summary, "tickRate"),
            ReadBool(summary, "accelerated"),
            ReadNullableBool(result, "passed"),
            ReadInt(result, "finalFrame"),
            ReadInt(result, "finalTimeMs"),
            ReadInt(result, "traceNodeCount"),
            NormalizePath(summaryPath),
            NormalizePath(tracePath));
    }

    private static JsonNode? ReadJsonNode(string path, List<string> warnings)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read json artifact {NormalizePath(path)}: {exception.Message}");
            return null;
        }
    }

    private static JsonNode[] ReadTraceRecords(string tracePath, int limit, List<string> warnings)
    {
        if (!File.Exists(tracePath))
        {
            warnings.Add($"Trace artifact does not exist: {NormalizePath(tracePath)}");
            return Array.Empty<JsonNode>();
        }

        var effectiveLimit = Math.Clamp(limit <= 0 ? DefaultTraceLimit : limit, 1, MaxTraceLimit);
        var records = new List<JsonNode>(Math.Min(effectiveLimit, 256));
        var lineNumber = 0;
        foreach (var line in File.ReadLines(tracePath))
        {
            lineNumber++;
            if (records.Count >= effectiveLimit) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var node = JsonNode.Parse(line);
                if (node is not null) records.Add(node);
            }
            catch (Exception exception)
            {
                warnings.Add($"Failed to parse trace line {lineNumber}: {exception.Message}");
            }
        }

        return records.ToArray();
    }

    private static ArtifactDirectoryResolution ResolveArtifactDirectory(string? artifactDirectory)
    {
        var baseDirectory = ResolveWorkspaceRoot();
        var requested = string.IsNullOrWhiteSpace(artifactDirectory) ? DefaultArtifactDirectory : artifactDirectory.Trim();
        var fullPath = Path.IsPathRooted(requested)
            ? Path.GetFullPath(requested)
            : Path.GetFullPath(requested, baseDirectory);
        var artifactRoot = Path.GetFullPath(ArtifactRootDirectory, baseDirectory);
        var isAllowed = IsPathUnderDirectory(fullPath, artifactRoot);
        var displayPath = NormalizePath(fullPath);
        var error = isAllowed
            ? null
            : $"Artifact directory must stay under {NormalizePath(artifactRoot)}. Requested: {displayPath}";
        return new ArtifactDirectoryResolution(fullPath, displayPath, isAllowed, error);
    }

    private static string ResolveWorkspaceRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LICENSE"))
                && Directory.Exists(Path.Combine(directory.FullName, "Server"))
                && Directory.Exists(Path.Combine(directory.FullName, "Unity")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static SkillAcceptanceCaseIdValidation ValidateCaseId(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return new SkillAcceptanceCaseIdValidation(string.Empty, false, "caseId is required.");
        }

        var trimmed = caseId.Trim();
        var safeCaseId = SanitizeCaseId(trimmed);
        if (string.IsNullOrWhiteSpace(safeCaseId) || !string.Equals(trimmed, safeCaseId, StringComparison.Ordinal))
        {
            return new SkillAcceptanceCaseIdValidation(safeCaseId, false, "caseId contains unsupported characters.");
        }

        return new SkillAcceptanceCaseIdValidation(safeCaseId, true, null);
    }

    private static string SanitizeCaseId(string caseId)
    {
        var trimmed = caseId.Trim();
        var chars = trimmed.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.').ToArray();
        return new string(chars);
    }

    private static bool IsPathUnderDirectory(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static AdminApiErrorHttpResponse BuildError(string code, string message, string target)
    {
        return new AdminApiErrorHttpResponse(code, message, target, DateTime.UtcNow.Ticks);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string? ReadString(JsonNode? node, string name)
    {
        return node?[name]?.GetValue<string>();
    }

    private static int ReadInt(JsonNode? node, string name)
    {
        return node?[name]?.GetValue<int>() ?? 0;
    }

    private static bool ReadBool(JsonNode? node, string name)
    {
        return node?[name]?.GetValue<bool>() ?? false;
    }

    private static bool? ReadNullableBool(JsonNode? node, string name)
    {
        return node?[name]?.GetValue<bool>();
    }

    private sealed record ArtifactDirectoryResolution(string FullPath, string DisplayPath, bool IsAllowed, string? ErrorMessage);

    private sealed record SkillAcceptanceCaseIdValidation(string CaseId, bool IsValid, string? ErrorMessage);

    private sealed record SkillAcceptanceAllowedScriptDefinition(string Id, string DisplayName, string RelativePath, string Shell, string[] Arguments, string[] Produces);
}
