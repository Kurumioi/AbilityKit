namespace AbilityKit.Orleans.Gateway.HttpApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

internal static class GatewaySkillAnalysisArtifacts
{
    public const string DefaultArtifactDirectory = "sample-web-output-analysis";
    private const string ArtifactRootDirectory = "artifacts";
    private const string AnalysisFileSuffix = ".analysis.json";
    private const string AnalysisSearchPattern = "*.analysis.json";
    private const string ExpectedSchemaVersion = "abilitykit-analysis.v1";
    private static readonly string[] BuiltInArtifactDirectories =
    {
        DefaultArtifactDirectory,
        "artifacts/moba-acceptance",
        "artifacts/admin-combat-analysis-runs"
    };

    public static AdminSkillAnalysisArtifactDirectoryListHttpResponse ListArtifactDirectories()
    {
        var root = ResolveArtifactRootDirectory();
        var warnings = new List<string>();
        if (!Directory.Exists(root.FullPath))
        {
            warnings.Add($"Artifact root does not exist: {root.DisplayPath}");
        }

        var directories = new Dictionary<string, AdminSkillAnalysisArtifactDirectoryHttpResponse>(StringComparer.OrdinalIgnoreCase);
        foreach (var builtIn in BuiltInArtifactDirectories)
        {
            AddArtifactDirectory(directories, builtIn);
        }

        if (Directory.Exists(root.FullPath))
        {
            foreach (var directory in Directory.EnumerateDirectories(root.FullPath, "*", SearchOption.TopDirectoryOnly))
            {
                AddArtifactDirectory(directories, NormalizePath(directory));
            }
        }

        return new AdminSkillAnalysisArtifactDirectoryListHttpResponse(
            root.DisplayPath,
            directories.Values.OrderByDescending(item => item.LastWriteUtcTicks).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
            warnings.ToArray(),
            DateTime.UtcNow.Ticks);
    }

    public static AdminSkillAnalysisArtifactListHttpResponse ListArtifacts(string? artifactDirectory)
    {
        var directory = ResolveArtifactDirectory(artifactDirectory);
        var warnings = new List<string>();
        if (!directory.IsAllowed)
        {
            warnings.Add(directory.ErrorMessage ?? "Analysis artifact directory is outside the allowed roots.");
            return new AdminSkillAnalysisArtifactListHttpResponse(directory.DisplayPath, Array.Empty<AdminSkillAnalysisArtifactListItemHttpResponse>(), warnings.ToArray(), DateTime.UtcNow.Ticks);
        }

        if (!Directory.Exists(directory.FullPath))
        {
            warnings.Add($"Analysis artifact directory does not exist: {directory.DisplayPath}");
            return new AdminSkillAnalysisArtifactListHttpResponse(directory.DisplayPath, Array.Empty<AdminSkillAnalysisArtifactListItemHttpResponse>(), warnings.ToArray(), DateTime.UtcNow.Ticks);
        }

        var artifacts = Directory.EnumerateFiles(directory.FullPath, AnalysisSearchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => BuildArtifactListItem(directory.FullPath, path, warnings))
            .Where(item => item is not null)
            .Cast<AdminSkillAnalysisArtifactListItemHttpResponse>()
            .OrderByDescending(item => item.GeneratedAtUtcTicks)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AdminSkillAnalysisArtifactListHttpResponse(directory.DisplayPath, artifacts, warnings.ToArray(), DateTime.UtcNow.Ticks);
    }

    public static IResult GetArtifact(string fileName, string? artifactDirectory)
    {
        var validation = ValidateAnalysisFileName(fileName);
        if (!validation.IsValid)
        {
            return Results.BadRequest(BuildError("InvalidAnalysisFileName", validation.ErrorMessage ?? "fileName is invalid.", "fileName"));
        }

        var directory = ResolveArtifactDirectory(artifactDirectory);
        if (!directory.IsAllowed)
        {
            return Results.BadRequest(BuildError("ArtifactDirectoryOutOfBounds", directory.ErrorMessage ?? "Analysis artifact directory is outside the allowed roots.", "artifactDirectory"));
        }

        var safeFileName = validation.FileName;
        var path = Path.Combine(directory.FullPath, safeFileName);
        var warnings = new List<string>();
        if (!File.Exists(path))
        {
            return Results.NotFound(new AdminSkillAnalysisArtifactHttpResponse(
                directory.DisplayPath,
                safeFileName,
                NormalizePath(path),
                null,
                warnings.Concat(new[] { $"Analysis artifact does not exist: {NormalizePath(path)}" }).ToArray(),
                DateTime.UtcNow.Ticks));
        }

        var artifact = ReadJsonNode(path, warnings);
        return Results.Ok(new AdminSkillAnalysisArtifactHttpResponse(
            directory.DisplayPath,
            safeFileName,
            NormalizePath(path),
            artifact,
            warnings.ToArray(),
            DateTime.UtcNow.Ticks));
    }

    private static void AddArtifactDirectory(Dictionary<string, AdminSkillAnalysisArtifactDirectoryHttpResponse> directories, string artifactDirectory)
    {
        var directory = ResolveArtifactDirectory(artifactDirectory);
        if (!directory.IsAllowed) return;

        var exists = Directory.Exists(directory.FullPath);
        var analysisCount = exists ? Directory.EnumerateFiles(directory.FullPath, AnalysisSearchPattern, SearchOption.TopDirectoryOnly).Count() : 0;
        var lastWriteTicks = exists ? Directory.GetLastWriteTimeUtc(directory.FullPath).Ticks : 0;
        directories[directory.DisplayPath] = new AdminSkillAnalysisArtifactDirectoryHttpResponse(
            directory.DisplayPath,
            Path.GetFileName(directory.DisplayPath.TrimEnd('/')),
            exists,
            analysisCount,
            lastWriteTicks);
    }

    private static AdminSkillAnalysisArtifactListItemHttpResponse? BuildArtifactListItem(string directory, string path, List<string> warnings)
    {
        var artifact = ReadJsonNode(path, warnings);
        if (artifact is null) return null;

        var fileName = Path.GetFileName(path);
        var schemaVersion = ReadString(artifact, "schemaVersion") ?? ReadString(artifact, "SchemaVersion") ?? string.Empty;
        var session = artifact["session"] ?? artifact["Session"];
        var time = artifact["time"] ?? artifact["Time"];
        var trace = artifact["trace"] ?? artifact["Trace"];
        var roots = trace?["roots"] as JsonArray ?? trace?["Roots"] as JsonArray;
        var rootCount = roots?.Count ?? 0;
        var nodeCount = CountTraceNodes(roots);
        var fullPath = Path.GetFullPath(path);
        var generatedAtTicks = ReadGeneratedAtUtcTicks(session);
        if (!string.Equals(schemaVersion, ExpectedSchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Unexpected analysis schema in {NormalizePath(path)}: {schemaVersion}");
        }

        return new AdminSkillAnalysisArtifactListItemHttpResponse(
            fileName,
            ReadString(session, "sessionId") ?? ReadString(session, "SessionId") ?? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName)),
            schemaVersion,
            ReadString(session, "project") ?? ReadString(session, "Project"),
            ReadString(session, "scenario") ?? ReadString(session, "Scenario"),
            rootCount,
            nodeCount,
            ReadInt(time, "startFrame") == 0 ? ReadInt(time, "StartFrame") : ReadInt(time, "startFrame"),
            ReadInt(time, "endFrame") == 0 ? ReadInt(time, "EndFrame") : ReadInt(time, "endFrame"),
            generatedAtTicks,
            new FileInfo(fullPath).Length,
            NormalizePath(fullPath));
    }

    private static long ReadGeneratedAtUtcTicks(JsonNode? session)
    {
        var generatedAtUtc = ReadString(session, "generatedAtUtc") ?? ReadString(session, "GeneratedAtUtc");
        if (DateTimeOffset.TryParse(generatedAtUtc, out var parsed)) return parsed.UtcTicks;
        return ReadLong(session, "generatedAtUnixMs") > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(ReadLong(session, "generatedAtUnixMs")).UtcTicks
            : 0;
    }

    private static int CountTraceNodes(JsonArray? roots)
    {
        if (roots is null) return 0;
        var count = 0;
        foreach (var root in roots)
        {
            count += (root?["nodes"] as JsonArray ?? root?["Nodes"] as JsonArray)?.Count ?? 0;
        }

        return count;
    }

    private static JsonNode? ReadJsonNode(string path, List<string> warnings)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read analysis artifact {NormalizePath(path)}: {exception.Message}");
            return null;
        }
    }

    private static ArtifactDirectoryResolution ResolveArtifactRootDirectory()
    {
        var baseDirectory = ResolveWorkspaceRoot();
        var fullPath = Path.GetFullPath(ArtifactRootDirectory, baseDirectory);
        return new ArtifactDirectoryResolution(fullPath, NormalizePath(fullPath), true, null);
    }

    private static ArtifactDirectoryResolution ResolveArtifactDirectory(string? artifactDirectory)
    {
        var baseDirectory = ResolveWorkspaceRoot();
        var requested = string.IsNullOrWhiteSpace(artifactDirectory) ? DefaultArtifactDirectory : artifactDirectory.Trim();
        var fullPath = Path.IsPathRooted(requested)
            ? Path.GetFullPath(requested)
            : Path.GetFullPath(requested, baseDirectory);
        var artifactRoot = Path.GetFullPath(ArtifactRootDirectory, baseDirectory);
        var sampleRoot = Path.GetFullPath(DefaultArtifactDirectory, baseDirectory);
        var isAllowed = IsPathUnderDirectory(fullPath, artifactRoot) || IsSameOrUnderDirectory(fullPath, sampleRoot);
        var displayPath = NormalizePath(fullPath);
        var error = isAllowed
            ? null
            : $"Analysis artifact directory must stay under {NormalizePath(artifactRoot)} or {NormalizePath(sampleRoot)}. Requested: {displayPath}";
        return new ArtifactDirectoryResolution(fullPath, displayPath, isAllowed, error);
    }

    private static AnalysisFileNameValidation ValidateAnalysisFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new AnalysisFileNameValidation(string.Empty, false, "fileName is required.");
        }

        var safeFileName = Path.GetFileName(fileName.Trim());
        if (!string.Equals(fileName.Trim(), safeFileName, StringComparison.Ordinal) || !safeFileName.EndsWith(AnalysisFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return new AnalysisFileNameValidation(safeFileName, false, "fileName must be a local *.analysis.json file name.");
        }

        return new AnalysisFileNameValidation(safeFileName, true, null);
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

    private static bool IsSameOrUnderDirectory(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) || IsPathUnderDirectory(path, root);
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

    private static long ReadLong(JsonNode? node, string name)
    {
        return node?[name]?.GetValue<long>() ?? 0;
    }

    private sealed record ArtifactDirectoryResolution(string FullPath, string DisplayPath, bool IsAllowed, string? ErrorMessage);

    private sealed record AnalysisFileNameValidation(string FileName, bool IsValid, string? ErrorMessage);
}
