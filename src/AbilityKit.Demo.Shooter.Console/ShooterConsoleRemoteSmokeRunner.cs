using AbilityKit.Demo.Host.Console;
using System.Diagnostics;

namespace AbilityKit.Demo.Shooter.Console;

internal sealed class ShooterConsoleRemoteSmokeRunner
{
    private const string SmokeProjectRelativePath = "Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/AbilityKit.Orleans.ShooterSmoke.csproj";
    private const string MultiprocessSmokeScriptRelativePath = "Server/Orleans/tools/run_shooter_multiprocess_smoke.ps1";

    private readonly IConsoleOutput _output;

    public ShooterConsoleRemoteSmokeRunner(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public int Run()
    {
        var projectPath = ResolveRepositoryPath(SmokeProjectRelativePath);
        if (projectPath == null)
        {
            _output.Write(ConsoleOutputChannel.Error, $"ERROR message=\"Shooter remote smoke project not found: {Escape(SmokeProjectRelativePath)}\"");
            _output.Write(ConsoleOutputChannel.Error, "RESULT status=fail mode=remote-smoke reason=project-not-found");
            return 1;
        }

        var scriptPath = ResolveRepositoryPath(MultiprocessSmokeScriptRelativePath);
        if (scriptPath == null)
        {
            _output.Write(ConsoleOutputChannel.Error, $"ERROR message=\"Shooter multiprocess smoke script not found: {Escape(MultiprocessSmokeScriptRelativePath)}\"");
            _output.Write(ConsoleOutputChannel.Error, "RESULT status=fail mode=remote-smoke reason=script-not-found");
            return 1;
        }

        var workspaceRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, "../../../../"));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -JoinClients 1 -Inputs 3 -TimeoutSeconds 45",
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _output.Write(ConsoleOutputChannel.Sync, $"CASE name=\"Shooter Remote Multiprocess Gateway Smoke\" project=\"{Escape(NormalizePath(projectPath))}\" script=\"{Escape(NormalizePath(scriptPath))}\" status=running");

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) => WriteProcessLine(ConsoleOutputChannel.Sync, args.Data);
        process.ErrorDataReceived += (_, args) => WriteProcessLine(ConsoleOutputChannel.Error, args.Data);

        try
        {
            if (!process.Start())
            {
                _output.Write(ConsoleOutputChannel.Error, "ERROR message=\"Failed to start dotnet remote smoke process.\"");
                _output.Write(ConsoleOutputChannel.Error, "RESULT status=fail mode=remote-smoke reason=process-start-failed");
                return 1;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            _output.Write(ConsoleOutputChannel.Error, $"ERROR exception={Escape(ex.GetType().Name)} message=\"{Escape(ex.Message)}\"");
            _output.Write(ConsoleOutputChannel.Error, "RESULT status=fail mode=remote-smoke reason=exception");
            return 1;
        }

        var status = process.ExitCode == 0 ? "pass" : "fail";
        _output.Write(ConsoleOutputChannel.Sync, $"RESULT status={status} mode=remote-smoke exitCode={process.ExitCode}");
        return process.ExitCode;
    }

    private static string? ResolveRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            directory = directory.Parent;
        }

        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (baseDirectory != null)
        {
            var candidate = Path.Combine(baseDirectory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            baseDirectory = baseDirectory.Parent;
        }

        return null;
    }

    private void WriteProcessLine(ConsoleOutputChannel channel, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var normalized = line.Trim();
        if (normalized.StartsWith("Shooter TCP Gateway smoke passed.", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Shooter multiprocess ", StringComparison.OrdinalIgnoreCase))
        {
            _output.Write(ConsoleOutputChannel.Sync, $"CASE name=\"Shooter Remote Multiprocess Gateway Smoke\" status=completed detail=\"{Escape(normalized)}\"");
            return;
        }

        _output.Write(channel, $"REMOTE {normalized}");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
