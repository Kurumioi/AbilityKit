using System.Diagnostics;
using System.Text.Json;
using Xunit;

public sealed class ShooterMultiprocessSmokeScriptContractTests
{
    private static readonly string ScriptPath = GetScriptPath();
    private static readonly string Script = File.ReadAllText(ScriptPath);
    private static readonly string ClientRunnerSource = File.ReadAllText(GetClientRunnerSourcePath());

    [Fact]
    public void ScriptParsesWithWindowsPowerShellAst()
    {
        var escapedPath = ScriptPath.Replace("'", "''", StringComparison.Ordinal);
        var command = "$tokens=$null;$errors=$null;" +
            $"$ast=[System.Management.Automation.Language.Parser]::ParseFile('{escapedPath}',[ref]$tokens,[ref]$errors);" +
            "if($errors.Count -gt 0){$errors|%{$_.ToString()};exit 1};" +
            "$required=@('Get-ShooterFaultMatrixPlan','Get-BoundedTimeoutSeconds','Get-FailureClassification','Assert-BoundedConvergence','Invoke-GatewayFaultCommand','Wait-ForPortClosed','Wait-ForClientReconnectReady');" +
            "$actual=@($ast.FindAll({param($n)$n -is [System.Management.Automation.Language.FunctionDefinitionAst]},$true).Name);" +
            "$missing=@($required|?{$_ -notin $actual});if($missing.Count -gt 0){Write-Error ('Missing functions: '+($missing -join ','));exit 2};'AST_OK'";

        var result = RunPowerShellCommand(command);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("AST_OK", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void MinimalPlanExpandsRecoverableRetryWithBoundedTiming()
    {
        using var plan = RunPlan("-Profile", "minimal", "-ScenarioTimeoutSeconds", "37", "-GlobalTimeoutSeconds", "91");
        var root = plan.RootElement;
        var scenarios = root.GetProperty("scenarios");

        Assert.Equal(2, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("minimal", root.GetProperty("profile").GetString());
        Assert.Equal(91, root.GetProperty("globalTimeoutSeconds").GetInt32());
        Assert.Equal(1, scenarios.GetArrayLength());

        var scenario = scenarios[0];
        Assert.Equal("recoverable-retry", scenario.GetProperty("name").GetString());
        Assert.Equal(37, scenario.GetProperty("timeoutSeconds").GetInt32());
        Assert.Equal(1, scenario.GetProperty("reconnectCount").GetInt32());
        Assert.Equal(3, scenario.GetProperty("recoverableFailureCount").GetInt32());
        Assert.InRange(scenario.GetProperty("convergenceTimeoutSeconds").GetInt32(), 5, 20);
    }

    [Fact]
    public void FullPlanExpandsFourIsolatedFaultScenarios()
    {
        using var plan = RunPlan(
            "-Profile", "full",
            "-TcpPort", "42001",
            "-SiloPort", "13111",
            "-OrleansGatewayPort", "32001",
            "-ScenarioTimeoutSeconds", "45");
        var root = plan.RootElement;
        var scenarios = root.GetProperty("scenarios").EnumerateArray().ToArray();

        Assert.True(root.GetProperty("globalTimeoutIsAutomatic").GetBoolean());
        Assert.Equal(720, root.GetProperty("globalTimeoutSeconds").GetInt32());
        Assert.Equal(
            new[] { "slow-consumer", "gateway-offline", "recoverable-retry", "reconnect-cycles" },
            scenarios.Select(item => item.GetProperty("name").GetString()).ToArray());
        Assert.Equal(new[] { 42001, 42011, 42021, 42031 }, scenarios.Select(item => item.GetProperty("tcpPort").GetInt32()).ToArray());
        Assert.Equal(new[] { 13111, 13121, 13131, 13141 }, scenarios.Select(item => item.GetProperty("siloPort").GetInt32()).ToArray());
        Assert.Equal(new[] { 32001, 32011, 32021, 32031 }, scenarios.Select(item => item.GetProperty("orleansGatewayPort").GetInt32()).ToArray());
        Assert.True(scenarios[0].GetProperty("slowConsumer").GetBoolean());
        Assert.True(scenarios[1].GetProperty("gatewayOffline").GetBoolean());
        Assert.Equal(3, scenarios[2].GetProperty("recoverableFailureCount").GetInt32());
        Assert.Equal(3, scenarios[3].GetProperty("reconnectCount").GetInt32());
        Assert.All(scenarios, scenario => Assert.Equal(60, scenario.GetProperty("startupTimeoutSeconds").GetInt32()));
        Assert.All(scenarios, scenario => Assert.Equal(60, scenario.GetProperty("setupTimeoutSeconds").GetInt32()));
        Assert.All(scenarios, scenario => Assert.Equal(180, scenario.GetProperty("executionTimeoutSeconds").GetInt32()));
        Assert.Equal(new[] { 0, 180, 360, 540 }, scenarios.Select(item => item.GetProperty("offsetSeconds").GetInt32()).ToArray());
    }

    [Fact]
    public void SelectedScenarioIsNotExpandedAsCharacters()
    {
        using var plan = RunPlan("-Profile", "custom", "-Scenario", "recoverable-retry");
        var scenarios = plan.RootElement.GetProperty("scenarios");

        Assert.Equal(1, scenarios.GetArrayLength());
        Assert.Equal("recoverable-retry", scenarios[0].GetProperty("name").GetString());
    }

    [Fact]
    public void ReconnectCyclesUsesThreeRealJoinClientDisconnectAndRecoveryCycles()
    {
        using var plan = RunPlan(
            "-Profile", "custom",
            "-Scenario", "reconnect-cycles",
            "-PayloadMode", "pure-state");
        var scenario = plan.RootElement.GetProperty("scenarios")[0];

        Assert.Equal("reconnect-cycles", scenario.GetProperty("name").GetString());
        Assert.Equal(3, scenario.GetProperty("reconnectCount").GetInt32());
        Assert.Equal(0, scenario.GetProperty("recoverableFailureCount").GetInt32());
        Assert.Contains("-ClientReconnectCount $(if ($i -eq 1) { $ReconnectCount } else { 0 })", Script, StringComparison.Ordinal);
        Assert.Contains("--state-sync-payload-mode', $PayloadMode", Script, StringComparison.Ordinal);
        Assert.Contains("for (var cycle = 1; cycle <= options.ReconnectCount; cycle++)", ClientRunnerSource, StringComparison.Ordinal);
        Assert.Contains("connection.Close();", ClientRunnerSource, StringComparison.Ordinal);
        Assert.Contains("reconnected.Flow.EntryKind != ShooterRoomGatewayEntryKind.Reconnect", ClientRunnerSource, StringComparison.Ordinal);
        Assert.Contains("getPushCount() <= pushesBeforeCycle", ClientRunnerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptRetainsArtifactAndFailureClassificationContracts()
    {
        Assert.Contains("$logDir = Join-Path $artifactRootPath $RunId", Script, StringComparison.Ordinal);
        Assert.Contains("Smoke run directory already exists", Script, StringComparison.Ordinal);
        Assert.Contains("if ($Status -ne 'running' -and (Test-Path $logDir))", Script, StringComparison.Ordinal);
        Assert.Contains("sha256 = (Get-FileHash", Script, StringComparison.Ordinal);
        Assert.Contains("Join-Path $logDir $diagnosticPath", Script, StringComparison.Ordinal);
        Assert.Contains("Test-Path -LiteralPath $resolvedDiagnosticPath", Script, StringComparison.Ordinal);
        Assert.Contains("$reachableTargetFrame = if ($reconnectCount -gt 0)", Script, StringComparison.Ordinal);
        Assert.Contains("$lastPushFrame", Script, StringComparison.Ordinal);
        Assert.Contains("StartedAtUtc = $startedAtUtc", Script, StringComparison.Ordinal);
        Assert.Contains("startedAtUtc = $client.StartedAtUtc.ToString('O')", Script, StringComparison.Ordinal);
        Assert.Contains("exitedAtUtc = $exitedAtUtc.ToString('O')", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("$client.Process.StartTime", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("$client.Process.ExitTime", Script, StringComparison.Ordinal);
        Assert.Contains("$childManifestStatus -ne 'passed'", Script, StringComparison.Ordinal);
        Assert.Contains("manifestStatus = $childManifestStatus", Script, StringComparison.Ordinal);
        Assert.Contains("New-Item -ItemType Directory -Force -Path $matrixRoot", Script, StringComparison.Ordinal);
        var buildIndex = Script.IndexOf("dotnet build $project", StringComparison.Ordinal);
        var matrixTimerIndex = Script.IndexOf("$matrixStartedAtUtc = [DateTime]::UtcNow", StringComparison.Ordinal);
        Assert.True(buildIndex >= 0 && matrixTimerIndex > buildIndex, "The fault-matrix timeout must start after the one-time build.");
        Assert.Contains("$applicationDll = Join-Path $projectDirectory", Script, StringComparison.Ordinal);
        Assert.Contains("$arguments = @($applicationDll)", Script, StringComparison.Ordinal);
        Assert.Contains("$serverArgs = @($applicationDll)", Script, StringComparison.Ordinal);
        Assert.Contains("Shooter smoke application artifact was not found", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("'run', '--project', $project, '-c', $Configuration, '--no-build'", Script, StringComparison.Ordinal);
        var serverListeningIndex = Script.IndexOf("Add-AssertionResult -Name 'server-listening'", StringComparison.Ordinal);
        var setupTimerIndex = Script.LastIndexOf("$scenarioDeadlineUtc = [DateTime]::UtcNow.AddSeconds($SetupTimeoutSeconds)", StringComparison.Ordinal);
        var allJoinsReadyIndex = Script.IndexOf("$timeoutPhase = 'active scenario'", StringComparison.Ordinal);
        var scenarioTimerIndex = Script.LastIndexOf("$scenarioDeadlineUtc = [DateTime]::UtcNow.AddSeconds($ScenarioTimeoutSeconds)", StringComparison.Ordinal);
        Assert.True(serverListeningIndex >= 0 && setupTimerIndex > serverListeningIndex, "The setup timeout must start after the server is listening.");
        Assert.True(allJoinsReadyIndex > setupTimerIndex && scenarioTimerIndex > allJoinsReadyIndex, "The active scenario timeout must start only after all join clients are ready.");
        Assert.Contains("$childTimeoutSeconds = [Math]::Min($remainingGlobalSeconds, $plan.executionTimeoutSeconds)", Script, StringComparison.Ordinal);
        Assert.Contains("-TimeoutSeconds', $TimeoutSeconds", Script, StringComparison.Ordinal);
        Assert.Contains("Wait-ForPort -Port $TcpPort -TimeoutSeconds $StartupTimeoutSeconds", Script, StringComparison.Ordinal);
        Assert.Contains("-Prefix 'SHOOTER_MP_CLIENT_READY' -TimeoutSeconds $SetupTimeoutSeconds", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("-Prefix 'SHOOTER_MP_CLIENT_READY' -TimeoutSeconds $TimeoutSeconds", Script, StringComparison.Ordinal);
        Assert.Contains("-CompletionReleasePath $(if ($Scenario -eq 'slow-consumer')", Script, StringComparison.Ordinal);
        Assert.Contains("Add-AssertionResult -Name 'slow-consumer-pressure-window-completed'", Script, StringComparison.Ordinal);
        Assert.Contains("$summaries += [PSCustomObject][ordered]@{", Script, StringComparison.Ordinal);
        Assert.Contains("$null -eq $diagnostic.observer.serverDroppedItems", Script, StringComparison.Ordinal);
        Assert.Contains("serverDroppedItems = [long]$diagnostic.observer.serverDroppedItems", Script, StringComparison.Ordinal);
        Assert.Contains("serverCoalescedItems = [long]$diagnostic.observer.serverCoalescedItems", Script, StringComparison.Ordinal);
        Assert.Contains("serverBaselineInvalidations = [long]$diagnostic.observer.serverBaselineInvalidations", Script, StringComparison.Ordinal);
        Assert.Contains("Measure-Object -Property serverDroppedItems -Sum", Script, StringComparison.Ordinal);
        Assert.Contains("Measure-Object -Property serverCoalescedItems -Sum", Script, StringComparison.Ordinal);
        Assert.Contains("Measure-Object -Property fullBaselinesApplied -Sum", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("Measure-Object -Property observerDropped -Sum", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("Measure-Object -Property observerCoalesced -Sum", Script, StringComparison.Ordinal);
        Assert.Contains("($deltasApplied + $resyncRequests + $fullBaselinesApplied) -lt 2", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("-not $activePlan.slowConsumer -or $fullBaselinesApplied -lt 2", Script, StringComparison.Ordinal);
        Assert.Contains("operationTimeoutSeconds = $TimeoutSeconds", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("-TimeoutSeconds', $remainingGlobalSeconds", Script, StringComparison.Ordinal);
        Assert.Contains("return 'PreconditionFailed'", Script, StringComparison.Ordinal);
        Assert.Contains("return 'FaultRecoveryFailed'", Script, StringComparison.Ordinal);
        Assert.Contains("$manifestFailureStage = if ($manifestFailureCategory -eq 'PreconditionFailed')", Script, StringComparison.Ordinal);
        Assert.Contains("Add-AssertionResult -Name 'scenario-completed' -Passed $true", Script, StringComparison.Ordinal);
        Assert.Contains("Add-AssertionResult -Name 'scenario-completed' -Passed $false", Script, StringComparison.Ordinal);
        Assert.Contains("Stop-Process -Id $child.Id -Force", Script, StringComparison.Ordinal);
        Assert.Contains("-Ports @($plan.tcpPort, $plan.siloPort, $plan.orleansGatewayPort)", Script, StringComparison.Ordinal);
        Assert.Contains("$childManifest.status = 'failed'", Script, StringComparison.Ordinal);
        var childTimeoutIndex = Script.IndexOf("if ($childTimedOut)", StringComparison.Ordinal);
        var matrixManifestIndex = Script.IndexOf("$matrixManifestPath = Join-Path $matrixRoot", StringComparison.Ordinal);
        Assert.True(childTimeoutIndex >= 0 && matrixManifestIndex > childTimeoutIndex, "A child timeout must flow through matrix manifest generation.");
        Assert.DoesNotContain("throw \"Shooter fault matrix scenario", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("-CommandPatterns @('AbilityKit.Orleans.ShooterSmoke.csproj'", Script, StringComparison.Ordinal);
        Assert.DoesNotContain("$arguments += $commonArgs", Script, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayOfflineScenarioProvesTransportOutageBeforeRecovery()
    {
        Assert.Contains(
            "-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $Port -TimeoutMilliseconds 250)",
            Script,
            StringComparison.Ordinal);
        Assert.Contains(
            "-ReconnectReleasePath $(if ($Scenario -eq 'gateway-offline' -and $i -eq 1)",
            Script,
            StringComparison.Ordinal);

        var reconnectReadyIndex = Script.IndexOf(
            "Add-AssertionResult -Name 'join-inputs-completed-before-fault'",
            StringComparison.Ordinal);
        var offlineAckIndex = Script.IndexOf(
            "Add-AssertionResult -Name 'gateway-offline-acknowledged'",
            StringComparison.Ordinal);
        var unreachableIndex = Script.IndexOf(
            "Add-AssertionResult -Name 'gateway-offline-unreachable'",
            StringComparison.Ordinal);
        var onlineAckIndex = Script.IndexOf(
            "Add-AssertionResult -Name 'gateway-online-acknowledged'",
            StringComparison.Ordinal);
        var reconnectReleaseIndex = Script.IndexOf(
            "Add-AssertionResult -Name 'join-reconnect-released-after-recovery'",
            StringComparison.Ordinal);

        Assert.True(reconnectReadyIndex >= 0, "The join client must finish inputs before the fault.");
        Assert.True(offlineAckIndex > reconnectReadyIndex, "The Gateway fault must start after the client reaches the reconnect gate.");
        Assert.True(unreachableIndex > offlineAckIndex, "Port unreachability must be proven after the offline acknowledgement.");
        Assert.True(onlineAckIndex > unreachableIndex, "Gateway recovery must happen after the offline port probe.");
        Assert.True(reconnectReleaseIndex > onlineAckIndex, "Reconnect must be released only after the Gateway is listening again.");
    }

    [Fact]
    public void LagCompensationAllowsDeterministicHistoryWindowRejection()
    {
        Assert.Contains(
            "$acceptableReasons = @('Hit', 'HistoryUnavailable', 'RewindWindowExceeded')",
            Script,
            StringComparison.Ordinal);
        Assert.Contains("if ($requestedFrame -lt 0)", Script, StringComparison.Ordinal);
        Assert.Contains("if ($accepted)", Script, StringComparison.Ordinal);
    }

    private static JsonDocument RunPlan(params string[] arguments)
    {
        var processArguments = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            ScriptPath,
            "-PlanOnly",
        };
        processArguments.AddRange(arguments);
        var result = RunPowerShell(processArguments);

        Assert.True(result.ExitCode == 0, $"PowerShell exited with {result.ExitCode}: {result.StandardError}");
        return JsonDocument.Parse(result.StandardOutput);
    }

    private static ProcessResult RunPowerShellCommand(string command) =>
        RunPowerShell(new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command });

    private static ProcessResult RunPowerShell(IEnumerable<string> arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30_000), "PowerShell contract test timed out.");
        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static string GetScriptPath() =>
        Path.Combine(GetOrleansWorkspacePath(), "tools", "run_shooter_multiprocess_smoke.ps1");

    private static string GetClientRunnerSourcePath() =>
        Path.Combine(
            GetOrleansWorkspacePath(),
            "src",
            "AbilityKit.Orleans.ShooterSmoke",
            "Runner",
            "ShooterSmokeClientProcessRunner.cs");

    private static string GetOrleansWorkspacePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "AbilityKit.Orleans.sln");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Orleans workspace from the test output directory.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
