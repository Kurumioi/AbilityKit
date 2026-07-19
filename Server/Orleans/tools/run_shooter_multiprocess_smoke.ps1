param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001,
    [int]$SiloPort = 12111,
    [int]$OrleansGatewayPort = 31001,
    [string]$ArtifactRoot = 'artifacts\shooter_multiprocess_smoke',
    [string]$RunId = '',
    [string]$ReplayExtension = '.record.bin',
    [int]$JoinClients = 1,
    [int]$Inputs = 3,
    [int]$Seed = 20260610,
    [int]$TimeoutSeconds = 30,
    [int]$StartupTimeoutSeconds = 60,
    [int]$SetupTimeoutSeconds = 60,
    [switch]$WaitForMatchEnd,
    [switch]$ReconnectJoinClient,
    [int]$ReconnectCount = 0,
    [int]$ReconnectDelayMs = 500,
    [int]$RecoverableFailureCount = 0,
    [int]$RetryBackoffMaxMs = 2000,
    [ValidateSet('minimal', 'full', 'custom')]
    [string]$Profile = 'minimal',
    [ValidateSet('', 'slow-consumer', 'gateway-offline', 'recoverable-retry', 'reconnect-cycles')]
    [string]$Scenario = '',
    [int]$ScenarioTimeoutSeconds = 45,
    [int]$GlobalTimeoutSeconds = 0,
    [switch]$PlanOnly,
    [int]$ConditionLatencyMs = 0,
    [int]$ConditionJitterMs = 0,
    [double]$ConditionPacketLossRate = 0,
    [int]$ConditionSeed = 20260610,
    [switch]$NoReplay,
    [switch]$NoCleanup,
    [ValidateSet('packed', 'pure-state')]
    [string]$PayloadMode = 'packed'
)

$ErrorActionPreference = 'Stop'

if ($TimeoutSeconds -le 0) {
    throw 'TimeoutSeconds must be > 0.'
}
if ($StartupTimeoutSeconds -le 0) {
    throw 'StartupTimeoutSeconds must be > 0.'
}
if ($SetupTimeoutSeconds -le 0) {
    throw 'SetupTimeoutSeconds must be > 0.'
}
if ($ScenarioTimeoutSeconds -le 5) {
    throw 'ScenarioTimeoutSeconds must be > 5 to reserve convergence time.'
}
if ($GlobalTimeoutSeconds -lt 0) {
    throw 'GlobalTimeoutSeconds must be >= 0; use 0 for an automatically derived matrix budget.'
}

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

function Get-ShooterFaultMatrixPlan {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('minimal', 'full', 'custom')]
        [string]$SelectedProfile,
        [string]$SelectedScenario,
        [int]$BaseTcpPort,
        [int]$BaseSiloPort,
        [int]$BaseOrleansGatewayPort,
        [int]$StartupTimeoutSeconds,
        [int]$SetupTimeoutSeconds,
        [int]$PerScenarioTimeoutSeconds
    )

    [string[]]$names = if (-not [string]::IsNullOrWhiteSpace($SelectedScenario)) {
        @([string]$SelectedScenario)
    }
    elseif ($SelectedProfile -eq 'full') {
        @('slow-consumer', 'gateway-offline', 'recoverable-retry', 'reconnect-cycles')
    }
    else {
        @('recoverable-retry')
    }

    $plans = @()
    for ($i = 0; $i -lt $names.Count; $i++) {
        $name = $names[$i]
        $executionTimeoutSeconds = $StartupTimeoutSeconds + $SetupTimeoutSeconds + $PerScenarioTimeoutSeconds + 15
        $plans += [pscustomobject][ordered]@{
            name = $name
            offsetSeconds = $i * $executionTimeoutSeconds
            startupTimeoutSeconds = $StartupTimeoutSeconds
            setupTimeoutSeconds = $SetupTimeoutSeconds
            timeoutSeconds = $PerScenarioTimeoutSeconds
            executionTimeoutSeconds = $executionTimeoutSeconds
            tcpPort = $BaseTcpPort + ($i * 10)
            siloPort = $BaseSiloPort + ($i * 10)
            orleansGatewayPort = $BaseOrleansGatewayPort + ($i * 10)
            reconnectCount = if ($name -eq 'reconnect-cycles') { 3 } elseif ($name -eq 'slow-consumer') { 0 } else { 1 }
            recoverableFailureCount = if ($name -eq 'recoverable-retry') { 3 } else { 0 }
            gatewayOffline = $name -eq 'gateway-offline'
            slowConsumer = $name -eq 'slow-consumer'
            convergenceTimeoutSeconds = [Math]::Max(5, [Math]::Min(20, $PerScenarioTimeoutSeconds - 5))
        }
    }

    return @($plans)
}

$matrixPlan = @(Get-ShooterFaultMatrixPlan `
    -SelectedProfile $Profile `
    -SelectedScenario $Scenario `
    -BaseTcpPort $TcpPort `
    -BaseSiloPort $SiloPort `
    -BaseOrleansGatewayPort $OrleansGatewayPort `
    -StartupTimeoutSeconds $StartupTimeoutSeconds `
    -SetupTimeoutSeconds $SetupTimeoutSeconds `
    -PerScenarioTimeoutSeconds $ScenarioTimeoutSeconds)
$effectiveGlobalTimeoutSeconds = if ($GlobalTimeoutSeconds -gt 0) {
    $GlobalTimeoutSeconds
}
else {
    [int](($matrixPlan | Measure-Object -Property executionTimeoutSeconds -Sum).Sum)
}

if ($PlanOnly) {
    [ordered]@{
        schemaVersion = 2
        profile = $Profile
        globalTimeoutSeconds = $effectiveGlobalTimeoutSeconds
        globalTimeoutIsAutomatic = $GlobalTimeoutSeconds -le 0
        scenarios = $matrixPlan
    } | ConvertTo-Json -Depth 6
    return
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'
$projectDirectory = Split-Path -Parent $project
$applicationDll = Join-Path $projectDirectory "bin\$Configuration\net10.0\AbilityKit.Orleans.ShooterSmoke.dll"
if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = '{0:yyyyMMdd-HHmmss-fff}-{1}' -f [DateTime]::UtcNow, $PID
}

if ([string]::IsNullOrWhiteSpace($Scenario)) {
    $matrixRoot = if ([System.IO.Path]::IsPathRooted($ArtifactRoot)) {
        [System.IO.Path]::GetFullPath($ArtifactRoot)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
    }
    New-Item -ItemType Directory -Force -Path $matrixRoot | Out-Null
    $matrixResults = @()
    $matrixFailure = $null
    if (-not $NoBuild) {
        Write-Host 'Building Shooter smoke project once for the fault matrix...' -ForegroundColor Cyan
        dotnet build $project -c $Configuration '-p:UseSharedCompilation=false' '-p:nodeReuse=false'
        if ($LASTEXITCODE -ne 0) {
            throw "Shooter smoke project build failed with exit code $LASTEXITCODE."
        }
    }

    $matrixStartedAtUtc = [DateTime]::UtcNow
    $matrixDeadlineUtc = $matrixStartedAtUtc.AddSeconds($effectiveGlobalTimeoutSeconds)
    foreach ($plan in $matrixPlan) {
        $remainingGlobalSeconds = [int][Math]::Floor(($matrixDeadlineUtc - [DateTime]::UtcNow).TotalSeconds)
        if ($remainingGlobalSeconds -le 0) {
            $matrixFailure = "Shooter fault matrix exceeded global timeout of $effectiveGlobalTimeoutSeconds seconds before scenario '$($plan.name)' started."
            break
        }

        $childRunId = "$RunId-$($plan.name)"
        $childManifestPath = Join-Path (Join-Path $matrixRoot $childRunId) 'manifest.json'
        $childArguments = @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $MyInvocation.MyCommand.Path,
            '-NoBuild', '-Configuration', $Configuration,
            '-TcpPort', $plan.tcpPort, '-SiloPort', $plan.siloPort,
            '-OrleansGatewayPort', $plan.orleansGatewayPort,
            '-ArtifactRoot', $ArtifactRoot, '-RunId', $childRunId,
            '-ReplayExtension', $ReplayExtension, '-JoinClients', $JoinClients,
            '-Inputs', $Inputs, '-Seed', $Seed,
            '-TimeoutSeconds', $TimeoutSeconds,
            '-StartupTimeoutSeconds', $StartupTimeoutSeconds,
            '-SetupTimeoutSeconds', $SetupTimeoutSeconds,
            '-ScenarioTimeoutSeconds', $ScenarioTimeoutSeconds,
            '-GlobalTimeoutSeconds', $effectiveGlobalTimeoutSeconds,
            '-Profile', 'custom', '-Scenario', $plan.name,
            '-ReconnectDelayMs', $ReconnectDelayMs,
            '-RetryBackoffMaxMs', $RetryBackoffMaxMs,
            '-PayloadMode', $PayloadMode)
        if ($NoReplay) { $childArguments += '-NoReplay' }
        if ($NoCleanup) { $childArguments += '-NoCleanup' }
        if ($WaitForMatchEnd) { $childArguments += '-WaitForMatchEnd' }

        $childStartedAtUtc = [DateTime]::UtcNow
        $child = Start-Process -FilePath 'powershell.exe' -ArgumentList $childArguments -PassThru -NoNewWindow
        $childTimeoutSeconds = [Math]::Min($remainingGlobalSeconds, $plan.executionTimeoutSeconds)
        $childTimedOut = -not $child.WaitForExit($childTimeoutSeconds * 1000)
        if ($childTimedOut) {
            $matrixFailure = "Shooter fault matrix scenario '$($plan.name)' exceeded its bounded execution timeout of $childTimeoutSeconds seconds."
            Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue
            $null = $child.WaitForExit(5000)
            Stop-AbilityKitServices `
                -Ports @($plan.tcpPort, $plan.siloPort, $plan.orleansGatewayPort) `
                -GraceSeconds 1
        }
        $child.Refresh()
        $childExitCode = if ($childTimedOut) { -1 } else { [int]$child.ExitCode }
        $childManifest = if (Test-Path -LiteralPath $childManifestPath) {
            Get-Content -LiteralPath $childManifestPath -Raw | ConvertFrom-Json
        }
        else {
            $null
        }
        if ($childTimedOut -and $null -ne $childManifest) {
            $childManifest.status = 'failed'
            $childManifest.completedAtUtc = [DateTime]::UtcNow.ToString('O')
            $childManifest.error = $matrixFailure
            $childManifest.failure = [pscustomobject][ordered]@{
                category = 'FaultRecoveryFailed'
                stage = 'matrix-timeout'
                message = $matrixFailure
            }
            $childManifest.assertions = @($childManifest.assertions) + [pscustomobject][ordered]@{
                name = 'scenario-completed'
                passed = $false
                details = $matrixFailure
            }
            $childManifestTemporaryPath = "$childManifestPath.tmp"
            $childManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $childManifestTemporaryPath -Encoding utf8
            Move-Item -LiteralPath $childManifestTemporaryPath -Destination $childManifestPath -Force
        }
        $childManifestStatus = if ($null -eq $childManifest) { 'missing' } else { [string]$childManifest.status }
        $matrixResults += [ordered]@{
            scenario = $plan.name
            runId = $childRunId
            processId = $child.Id
            startedAtUtc = $childStartedAtUtc.ToString('O')
            completedAtUtc = [DateTime]::UtcNow.ToString('O')
            exitCode = $childExitCode
            manifestPath = "$childRunId/manifest.json"
            manifestStatus = $childManifestStatus
        }
        if ($childExitCode -ne 0 -or $childManifestStatus -ne 'passed') {
            if ($null -eq $matrixFailure) {
                $matrixFailure = "Shooter fault matrix scenario '$($plan.name)' failed. ExitCode=$childExitCode, ManifestStatus=$childManifestStatus."
            }
            break
        }
    }

    $matrixManifestPath = Join-Path $matrixRoot "$RunId-matrix.json"
    [ordered]@{
        schemaVersion = 2
        runId = $RunId
        profile = $Profile
        status = if ($null -eq $matrixFailure) { 'passed' } else { 'failed' }
        error = $matrixFailure
        startedAtUtc = $matrixStartedAtUtc.ToString('O')
        completedAtUtc = [DateTime]::UtcNow.ToString('O')
        scenarios = $matrixResults
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $matrixManifestPath -Encoding utf8
    if ($null -ne $matrixFailure) {
        [Console]::Error.WriteLine("$matrixFailure Manifest=$matrixManifestPath")
        exit 1
    }
    Write-Host "Shooter fault matrix passed. Manifest=$matrixManifestPath" -ForegroundColor Green
    return
}

$activePlan = $matrixPlan[0]
if (-not [string]::IsNullOrWhiteSpace($Scenario)) {
    $ReconnectCount = if ($ReconnectJoinClient -and $Profile -eq 'custom') { [Math]::Max(1, $ReconnectCount) } else { $activePlan.reconnectCount }
    $RecoverableFailureCount = $activePlan.recoverableFailureCount
    if ($Scenario -eq 'slow-consumer') {
        $PayloadMode = 'pure-state'
    }
}
if ($RunId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
    throw 'RunId must start with an alphanumeric character and contain only alphanumeric characters, dot, underscore, or hyphen.'
}

$artifactRootPath = if ([System.IO.Path]::IsPathRooted($ArtifactRoot)) {
    [System.IO.Path]::GetFullPath($ArtifactRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
}
$logDir = Join-Path $artifactRootPath $RunId
if (Test-Path $logDir) {
    throw "Smoke run directory already exists: $logDir"
}
$serverLog = Join-Path $logDir 'server.log'
$replayDir = Join-Path $logDir 'records'
$diagnosticDir = Join-Path $logDir 'diagnostics'
$manifestPath = Join-Path $logDir 'manifest.json'
$manifestStartedAtUtc = [DateTime]::UtcNow
$manifestStatus = 'running'
$manifestError = $null
$manifestFailureCategory = $null
$manifestFailureStage = $null
$scenarioEstablished = $false
$faultTimeline = @()
$assertionResults = @()
$firstDivergence = $null
$convergenceSummaries = @()
$processTimeline = @()
$faultControlPath = Join-Path $logDir 'gateway-fault-command.json'
$reconnectReleasePath = Join-Path $logDir 'gateway-reconnect.release'
$completionReleasePath = Join-Path $logDir 'scenario-completion.release'
$scenarioDeadlineUtc = $null
$timeoutPhase = 'startup'
$timeoutBudgetSeconds = $StartupTimeoutSeconds
$roomId = $null
$server = $null
$serverCorrelationId = "$RunId/shooter-mp-server"
$clientLogs = @()
$manifestClients = @()
$startedProcesses = New-Object System.Collections.Generic.List[System.Diagnostics.Process]

function Write-RunManifest {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('running', 'passed', 'failed')]
        [string]$Status,
        [string]$ErrorMessage = ''
    )

    $artifacts = @()
    if ($Status -ne 'running' -and (Test-Path $logDir)) {
        $artifacts = @(Get-ChildItem -LiteralPath $logDir -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -ne $manifestPath -and $_.FullName -ne "$manifestPath.tmp" } |
            Sort-Object FullName |
            ForEach-Object {
                [ordered]@{
                    path = ConvertTo-RunRelativePath -Path $_.FullName
                    bytes = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            })
    }

    $manifest = [ordered]@{
        schemaVersion = 2
        runId = $RunId
        status = $Status
        failure = if ($null -eq $manifestFailureCategory) { $null } else { [ordered]@{ category = $manifestFailureCategory; stage = $manifestFailureStage; message = $ErrorMessage } }
        startedAtUtc = $manifestStartedAtUtc.ToString('O')
        completedAtUtc = if ($Status -eq 'running') { $null } else { [DateTime]::UtcNow.ToString('O') }
        artifactDirectory = '.'
        manifestPath = 'manifest.json'
        processId = $PID
        machineName = [Environment]::MachineName
        configuration = $Configuration
        scenario = [ordered]@{
            name = if ([string]::IsNullOrWhiteSpace($Scenario)) { 'custom' } else { $Scenario }
            profile = $Profile
            mode = if ($WaitForMatchEnd) { 'end-to-end' } elseif ($ReconnectCount -gt 0 -or $ConditionLatencyMs -gt 0 -or $ConditionJitterMs -gt 0 -or $ConditionPacketLossRate -gt 0) { 'resilience' } else { 'sync' }
            payloadMode = $PayloadMode
            joinClients = $JoinClients
            inputs = $Inputs
            seed = $Seed
            waitForMatchEnd = [bool]$WaitForMatchEnd
            reconnectJoinClient = $ReconnectCount -gt 0
            reconnectCount = $ReconnectCount
            reconnectDelayMs = $ReconnectDelayMs
            recoverableFailureCount = $RecoverableFailureCount
            retryBackoffMaxMs = $RetryBackoffMaxMs
            operationTimeoutSeconds = $TimeoutSeconds
            startupTimeoutSeconds = $StartupTimeoutSeconds
            setupTimeoutSeconds = $SetupTimeoutSeconds
            timeoutSeconds = $ScenarioTimeoutSeconds
            executionTimeoutSeconds = $activePlan.executionTimeoutSeconds
            convergenceTimeoutSeconds = $activePlan.convergenceTimeoutSeconds
            networkCondition = [ordered]@{
                latencyMs = $ConditionLatencyMs
                jitterMs = $ConditionJitterMs
                packetLossRate = $ConditionPacketLossRate
                seed = $ConditionSeed
            }
        }
        ports = [ordered]@{
            tcpGateway = $TcpPort
            silo = $SiloPort
            orleansGateway = $OrleansGatewayPort
        }
        roomId = $roomId
        replayEnabled = -not [bool]$NoReplay
        error = if ([string]::IsNullOrWhiteSpace($ErrorMessage)) { $null } else { $ErrorMessage }
        processes = @(
            [ordered]@{
                role = 'orchestrator'
                processId = $PID
                correlationId = "$RunId/shooter-mp-orchestrator"
                stdoutPath = $null
                stderrPath = $null
            }
            if ($null -ne $server) {
                [ordered]@{
                    role = 'server'
                    processId = $server.Id
                    correlationId = $serverCorrelationId
                    stdoutPath = 'server.log'
                    stderrPath = 'server.err.log'
                }
            }
        )
        clients = @($manifestClients)
        processTimeline = @($processTimeline)
        faultTimeline = @($faultTimeline)
        assertions = @($assertionResults)
        firstDivergence = $firstDivergence
        healthSummary = @($convergenceSummaries | ForEach-Object { $_.health })
        observerSummary = [ordered]@{
            slowConsumer = [bool]$activePlan.slowConsumer
            bytesPerSecond = if ($activePlan.slowConsumer) { 256 } else { $null }
            burstBytes = if ($activePlan.slowConsumer) { 32768 } else { $null }
            maxQueueLength = if ($activePlan.slowConsumer) { 1 } else { $null }
            maxQueueAgeMs = if ($activePlan.slowConsumer) { 100 } else { $null }
            drainIntervalMs = if ($activePlan.slowConsumer) { 250 } else { $null }
            clients = @($convergenceSummaries)
        }
        artifacts = $artifacts
    }

    $temporaryPath = "$manifestPath.tmp"
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $temporaryPath -Encoding utf8
    Move-Item -LiteralPath $temporaryPath -Destination $manifestPath -Force
}

function Stop-StartedProcesses {
    param([System.Collections.Generic.List[System.Diagnostics.Process]]$Processes)

    for ($i = $Processes.Count - 1; $i -ge 0; $i--) {
        $process = $Processes[$i]
        if ($null -eq $process) {
            continue
        }

        try {
            $live = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($live) {
                Write-Host "Stopping spawned PID $($process.Id) ($($live.ProcessName))" -ForegroundColor Yellow
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Warning "Failed to stop spawned PID $($process.Id): $($_.Exception.Message)"
        }
    }
}

function ConvertTo-ProcessArgumentString {
    param([string[]]$Arguments)

    return ($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + (($_ -replace '"', '\"')) + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Start-DotnetProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$StdOut,
        [Parameter(Mandatory = $true)]
        [string]$StdErr
    )

    $workingDirectory = [System.IO.Path]::GetDirectoryName($project)
    $argumentString = ConvertTo-ProcessArgumentString -Arguments $Arguments
    $process = Start-Process -FilePath 'dotnet' `
        -ArgumentList $argumentString `
        -WorkingDirectory $workingDirectory `
        -RedirectStandardOutput $StdOut `
        -RedirectStandardError $StdErr `
        -PassThru

    return $process
}

function Get-BoundedTimeoutSeconds {
    param([int]$RequestedSeconds)

    if ($null -eq $scenarioDeadlineUtc) {
        return [Math]::Max(1, $RequestedSeconds)
    }

    $remaining = [int][Math]::Ceiling(($scenarioDeadlineUtc - [DateTime]::UtcNow).TotalSeconds)
    if ($remaining -le 0) {
        throw "Scenario '$Scenario' exceeded $timeoutPhase timeout of $timeoutBudgetSeconds seconds."
    }
    return [Math]::Max(1, [Math]::Min($RequestedSeconds, $remaining))
}

function Wait-ForPort {
    param(
        [int]$Port,
        [int]$TimeoutSeconds
    )

    $TimeoutSeconds = Get-BoundedTimeoutSeconds -RequestedSeconds $TimeoutSeconds
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $Port -TimeoutMilliseconds 500) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "TCP Gateway did not listen on 127.0.0.1:$Port within $TimeoutSeconds seconds."
}

function Wait-ForPortClosed {
    param(
        [int]$Port,
        [int]$TimeoutSeconds
    )

    $TimeoutSeconds = Get-BoundedTimeoutSeconds -RequestedSeconds $TimeoutSeconds
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $Port -TimeoutMilliseconds 250)) {
            return
        }

        Start-Sleep -Milliseconds 100
    }

    throw "TCP Gateway remained reachable on 127.0.0.1:$Port for $TimeoutSeconds seconds after the offline fault."
}

function Wait-ForResultLine {
    param(
        [string]$Path,
        [string]$Prefix,
        [int]$TimeoutSeconds
    )

    $TimeoutSeconds = Get-BoundedTimeoutSeconds -RequestedSeconds $TimeoutSeconds
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $Path) {
            $lines = @(Get-Content $Path -ErrorAction SilentlyContinue)
            $line = $lines | Where-Object { $_ -like "$Prefix*" } | Select-Object -Last 1
            if ($line) {
                return $line
            }

            $failure = $lines | Where-Object { $_ -like 'SHOOTER_MP_CLIENT_RESULT status=fail*' } | Select-Object -Last 1
            if ($failure) {
                throw "Client failed before '$Prefix': $failure"
            }
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for '$Prefix' in $Path."
}

function ConvertFrom-ClientResultLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Line
    )

    $fields = @{}
    $matches = [regex]::Matches($Line, '(?<name>[A-Za-z][A-Za-z0-9_]*)=(?<value>"(?:\\.|[^"])*"|[^\s]+)')
    foreach ($match in $matches) {
        $name = $match.Groups['name'].Value
        $value = $match.Groups['value'].Value
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2).Replace('\"', '"').Replace('\\', '\')
        }

        $fields[$name] = $value
    }

    return $fields
}

function Read-ResultValue {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Fields,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not $Fields.ContainsKey($Name)) {
        throw "Could not read $Name from result fields."
    }

    return [string]$Fields[$Name]
}

function Read-ResultInt {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Fields,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $value = Read-ResultValue -Fields $Fields -Name $Name
    return [int]::Parse($value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Read-ResultInt64 {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Fields,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $value = Read-ResultValue -Fields $Fields -Name $Name
    return [long]::Parse($value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Read-ResultBool {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Fields,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $value = Read-ResultValue -Fields $Fields -Name $Name
    return [bool]::Parse($value)
}

function Read-ResultDouble {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Fields,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $value = Read-ResultValue -Fields $Fields -Name $Name
    return [double]::Parse($value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function ConvertTo-RunRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $trimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $rootPath = [System.IO.Path]::GetFullPath($logDir).TrimEnd($trimChars) + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootUri = New-Object System.Uri($rootPath)
    $pathUri = New-Object System.Uri($fullPath)
    $relativeUri = $rootUri.MakeRelativeUri($pathUri)
    $relative = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    if ($relativeUri.IsAbsoluteUri -or $relative -eq '..' -or $relative.StartsWith('../')) {
        throw "Artifact path is outside the run root: $Path"
    }

    return $relative -replace '\\', '/'
}

function Get-ProcessExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [int]$RetryCount = 20,
        [int]$DelayMilliseconds = 100
    )

    for ($i = 0; $i -lt $RetryCount; $i++) {
        try {
            $null = $Process.Handle
            $Process.Refresh()
            return [int]$Process.ExitCode
        }
        catch {
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }

    throw "Could not read exit code for process $($Process.Id)."
}

function Add-AssertionResult {
    param([string]$Name, [bool]$Passed, [string]$Details = '')

    $script:assertionResults += [ordered]@{
        name = $Name
        passed = $Passed
        checkedAtUtc = [DateTime]::UtcNow.ToString('O')
        details = $Details
    }
    if (-not $Passed -and $null -eq $script:firstDivergence) {
        $script:firstDivergence = [ordered]@{
            assertion = $Name
            observedAtUtc = [DateTime]::UtcNow.ToString('O')
            details = $Details
        }
    }
}

function Invoke-GatewayFaultCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Action,
        [int]$TimeoutSeconds = 10
    )

    $commandId = [Guid]::NewGuid().ToString('N')
    $ackPath = "$faultControlPath.ack.json"
    Remove-Item -LiteralPath $ackPath -Force -ErrorAction SilentlyContinue
    $requestedAtUtc = [DateTime]::UtcNow
    [ordered]@{
        Id = $commandId
        Action = $Action
        RequestedAtUtc = $requestedAtUtc.ToString('O')
    } | ConvertTo-Json | Set-Content -LiteralPath $faultControlPath -Encoding utf8

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path $ackPath) {
            try {
                $ack = Get-Content -LiteralPath $ackPath -Raw | ConvertFrom-Json
                if ($ack.Id -eq $commandId) {
                    $script:faultTimeline += [ordered]@{
                        action = $Action
                        commandId = $commandId
                        requestedAtUtc = $requestedAtUtc.ToString('O')
                        receivedAtUtc = $ack.ReceivedAtUtc
                        completedAtUtc = $ack.CompletedAtUtc
                        status = $ack.Status
                        error = $ack.Error
                    }
                    if ($ack.Status -ne 'completed') {
                        throw "Gateway fault command '$Action' failed: $($ack.Error)"
                    }
                    return $ack
                }
            }
            catch [System.ArgumentException] {
            }
        }
        Start-Sleep -Milliseconds 50
    }

    throw "Timed out waiting for Gateway fault acknowledgement. Action=$Action"
}

function Get-FailureClassification {
    param([string]$Message, [bool]$Established)

    if (-not $Established -or $Message -match '(?i)(status|http|gatewaystatuscode)[^\r\n]*409|\b409\b|conflict|already exists|port.*(used|listen|bind)') {
        return 'PreconditionFailed'
    }
    return 'FaultRecoveryFailed'
}

function Assert-BoundedConvergence {
    param([Parameter(Mandatory = $true)][object[]]$ClientResults)

    $deadline = [DateTime]::UtcNow.AddSeconds($activePlan.convergenceTimeoutSeconds)
    $summaries = @()
    foreach ($clientResult in $ClientResults) {
        if ([DateTime]::UtcNow -ge $deadline) {
            throw "Convergence diagnostics exceeded timeout of $($activePlan.convergenceTimeoutSeconds) seconds."
        }

        $diagnosticPath = Read-ResultValue -Fields $clientResult.Fields -Name 'diagnosticArtifactPath'
        $resolvedDiagnosticPath = if ([System.IO.Path]::IsPathRooted($diagnosticPath)) {
            $diagnosticPath
        }
        else {
            Join-Path $logDir $diagnosticPath
        }
        if (-not (Test-Path -LiteralPath $resolvedDiagnosticPath)) {
            throw "Client diagnostic artifact was not created: $diagnosticPath"
        }
        $diagnostic = Get-Content -LiteralPath $resolvedDiagnosticPath -Raw | ConvertFrom-Json
        if (-not $diagnostic.diff.matched) {
            throw "Client authoritative diff did not converge. Client=$($clientResult.Client.ClientId), Status=$($diagnostic.diff.status), Reason=$($diagnostic.diff.reason)"
        }
        if ($diagnostic.reliableEvents.needsResync) {
            throw "Client reliable-event cursor still requires resync. Client=$($clientResult.Client.ClientId)"
        }
        if ((Read-ResultBool -Fields $clientResult.Fields -Name 'pureStateLastResyncNeeded')) {
            throw "Client baseline remained pending after recovery. Client=$($clientResult.Client.ClientId)"
        }

        if ($activePlan.slowConsumer -and
            ($null -eq $diagnostic.observer.serverDroppedItems -or
             $null -eq $diagnostic.observer.serverCoalescedItems -or
             $null -eq $diagnostic.observer.serverBaselineInvalidations)) {
            throw "Slow-consumer server delivery metrics were not captured. Client=$($clientResult.Client.ClientId)"
        }

        $summaries += [PSCustomObject][ordered]@{
            clientId = $clientResult.Client.ClientId
            stateHash = Read-ResultValue -Fields $clientResult.Fields -Name 'stateHash'
            baselineFrame = Read-ResultInt -Fields $clientResult.Fields -Name 'baselineFrame'
            reliableEventEpoch = [string]$diagnostic.reliableEvents.epoch
            reliableEventCursor = [long]$diagnostic.reliableEvents.lastAcknowledgedSequence
            reliableEventNeedsResync = [bool]$diagnostic.reliableEvents.needsResync
            diffStatus = [string]$diagnostic.diff.status
            serverQueueLength = [int]$diagnostic.observer.serverQueueLength
            serverDroppedItems = [long]$diagnostic.observer.serverDroppedItems
            serverCoalescedItems = [long]$diagnostic.observer.serverCoalescedItems
            serverBaselineInvalidations = [long]$diagnostic.observer.serverBaselineInvalidations
            fullBaselinesApplied = [int]$diagnostic.observer.pureStateFullBaselinesApplied
            health = [ordered]@{
                total = [long]$diagnostic.health.totalCount
                warnings = [long]$diagnostic.health.warningCount
                critical = [long]$diagnostic.health.criticalCount
                highestSeverity = [string]$diagnostic.health.highestSeverity
            }
        }
    }

    if ($activePlan.slowConsumer) {
        $pressureCount = [long](($summaries | Measure-Object -Property serverDroppedItems -Sum).Sum) +
            [long](($summaries | Measure-Object -Property serverCoalescedItems -Sum).Sum)
        $baselineCount = [long](($summaries | Measure-Object -Property fullBaselinesApplied -Sum).Sum)
        if ($pressureCount -le 0) {
            throw 'Slow-consumer scenario did not observe server queue drop or coalescing evidence.'
        }
        if ($baselineCount -lt $ClientResults.Count) {
            throw "Slow-consumer scenario did not restore a full baseline for every client. Expected=$($ClientResults.Count), Actual=$baselineCount."
        }
    }

    $script:convergenceSummaries = @($summaries)
    Add-AssertionResult -Name 'bounded-convergence' -Passed $true -Details ($summaries | ConvertTo-Json -Depth 4 -Compress)
}

function New-ClientArguments {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('create', 'join')]
        [string]$ClientMode,
        [Parameter(Mandatory = $true)]
        [string]$ClientId,
        [Parameter(Mandatory = $true)]
        [int]$PlayerId,
        [string]$RoomId,
        [int]$ClientReconnectCount = 0,
        [int]$ClientRecoverableFailureCount = 0,
        [int]$LatencyMs = 0,
        [int]$JitterMs = 0,
        [double]$PacketLossRate = 0,
        [int]$NetworkSeed = 0,
        [string]$ReplayOutputPath = '',
        [string]$ReconnectReleasePath = '',
        [string]$CompletionReleasePath = '',
        [Parameter(Mandatory = $true)]
        [string]$CorrelationId,
        [Parameter(Mandatory = $true)]
        [string]$DiagnosticOutputPath
    )

    $arguments = @($applicationDll)
    $arguments += @(
        '--client',
        '--state-sync-payload-mode', $PayloadMode,
        '--client-mode', $ClientMode,
        '--tcp-port', $TcpPort,
        '--client-id', $ClientId,
        '--player-id', $PlayerId,
        '--inputs', $Inputs,
        '--seed', $Seed,
        '--timeout-seconds', $TimeoutSeconds,
        '--run-id', $RunId,
        '--correlation-id', $CorrelationId,
        '--run-root', $logDir,
        '--diagnostic-output', $DiagnosticOutputPath)

    if ($WaitForMatchEnd) {
        $arguments += @('--wait-for-match-end')
    }

    if ($ClientReconnectCount -gt 0) {
        $clientReconnectDelayMs = if ($Scenario -eq 'gateway-offline') { [Math]::Max(1500, $ReconnectDelayMs) } else { $ReconnectDelayMs }
        $arguments += @(
            '--reconnect-count', $ClientReconnectCount,
            '--reconnect-delay-ms', $clientReconnectDelayMs,
            '--recoverable-failure-count', $ClientRecoverableFailureCount,
            '--retry-backoff-max-ms', $RetryBackoffMaxMs)
    }

    if ($LatencyMs -gt 0 -or $JitterMs -gt 0 -or $PacketLossRate -gt 0) {
        $arguments += @(
            '--condition-latency-ms', $LatencyMs,
            '--condition-jitter-ms', $JitterMs,
            '--condition-packet-loss-rate', ([string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0}', $PacketLossRate)),
            '--condition-seed', $NetworkSeed)
    }

    if (-not [string]::IsNullOrWhiteSpace($ReplayOutputPath)) {
        $arguments += @('--input-state-replay-output', $ReplayOutputPath)
    }

    if (-not [string]::IsNullOrWhiteSpace($ReconnectReleasePath)) {
        $arguments += @('--reconnect-release-path', $ReconnectReleasePath)
    }

    if (-not [string]::IsNullOrWhiteSpace($CompletionReleasePath)) {
        $arguments += @('--completion-release-path', $CompletionReleasePath)
    }

    if ($ClientMode -eq 'join') {
        if ([string]::IsNullOrWhiteSpace($RoomId)) {
            throw 'RoomId is required for join client mode.'
        }

        $arguments += @('--room-id', $RoomId)
    }

    return $arguments
}

function Start-SmokeClient {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('create', 'join')]
        [string]$ClientMode,
        [Parameter(Mandatory = $true)]
        [string]$ClientId,
        [Parameter(Mandatory = $true)]
        [int]$PlayerId,
        [string]$RoomId,
        [Parameter(Mandatory = $true)]
        [string]$LogPath,
        [Parameter(Mandatory = $true)]
        [string]$ErrLogPath,
        [int]$ClientReconnectCount = 0,
        [int]$ClientRecoverableFailureCount = 0,
        [int]$LatencyMs = 0,
        [int]$JitterMs = 0,
        [double]$PacketLossRate = 0,
        [int]$NetworkSeed = 0,
        [string]$ReplayOutputPath = '',
        [string]$ReconnectReleasePath = '',
        [string]$CompletionReleasePath = '',
        [Parameter(Mandatory = $true)]
        [string]$CorrelationId,
        [Parameter(Mandatory = $true)]
        [string]$DiagnosticOutputPath
    )

    $arguments = New-ClientArguments -ClientMode $ClientMode -ClientId $ClientId -PlayerId $PlayerId -RoomId $RoomId -ClientReconnectCount $ClientReconnectCount -ClientRecoverableFailureCount $ClientRecoverableFailureCount -LatencyMs $LatencyMs -JitterMs $JitterMs -PacketLossRate $PacketLossRate -NetworkSeed $NetworkSeed -ReplayOutputPath $ReplayOutputPath -ReconnectReleasePath $ReconnectReleasePath -CompletionReleasePath $CompletionReleasePath -CorrelationId $CorrelationId -DiagnosticOutputPath $DiagnosticOutputPath
    $startedAtUtc = [DateTime]::UtcNow
    $process = Start-DotnetProcess -Arguments $arguments -StdOut $LogPath -StdErr $ErrLogPath
    $startedProcesses.Add($process)

    return [pscustomobject]@{
        Mode = $ClientMode
        ClientId = $ClientId
        CorrelationId = $CorrelationId
        PlayerId = $PlayerId
        LogPath = $LogPath
        Process = $process
        StartedAtUtc = $startedAtUtc
        ReconnectCount = $ClientReconnectCount
        RecoverableFailureCount = $ClientRecoverableFailureCount
        LatencyMs = $LatencyMs
        JitterMs = $JitterMs
        PacketLossRate = $PacketLossRate
        ReplayOutputPath = $ReplayOutputPath
        DiagnosticOutputPath = $DiagnosticOutputPath
    }
}

function Wait-ForClientReconnectReady {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Client
    )

    $line = Wait-ForResultLine -Path $Client.LogPath -Prefix 'SHOOTER_MP_CLIENT_RECONNECT_READY' -TimeoutSeconds $TimeoutSeconds
    return [pscustomobject]@{
        Client = $Client
        Line = $line
        Fields = ConvertFrom-ClientResultLine -Line $line
    }
}

function Wait-ForClientReady {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Client
    )

    $line = Wait-ForResultLine -Path $Client.LogPath -Prefix 'SHOOTER_MP_CLIENT_READY' -TimeoutSeconds $SetupTimeoutSeconds
    return [pscustomobject]@{
        Client = $Client
        Line = $line
        Fields = ConvertFrom-ClientResultLine -Line $line
    }
}

function Wait-ForClientResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Client
    )

    $line = Wait-ForResultLine -Path $Client.LogPath -Prefix 'SHOOTER_MP_CLIENT_RESULT' -TimeoutSeconds $TimeoutSeconds
    return [pscustomobject]@{
        Client = $Client
        Line = $line
        Fields = ConvertFrom-ClientResultLine -Line $line
    }
}

function Assert-ClientResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$ClientResult
    )

    $line = $ClientResult.Line
    $fields = $ClientResult.Fields
    $client = $ClientResult.Client
    $expectedEntryKind = if ($client.Mode -eq 'create') { 'TeamLobby' } elseif ($client.ReconnectCount -gt 0) { 'Reconnect' } else { 'LateJoin' }

    if ((Read-ResultValue -Fields $fields -Name 'status') -ne 'pass') {
        throw "Client result did not pass: $line"
    }

    if ((Read-ResultValue -Fields $fields -Name 'mode') -ne $client.Mode) {
        throw "Client mode mismatch. Expected=$($client.Mode), Line=$line"
    }

    if ((Read-ResultValue -Fields $fields -Name 'clientId') -ne $client.ClientId) {
        throw "Client id mismatch. Expected=$($client.ClientId), Line=$line"
    }

    if ((Read-ResultInt -Fields $fields -Name 'playerId') -ne $client.PlayerId) {
        throw "Player id mismatch. Expected=$($client.PlayerId), Line=$line"
    }

    if ((Read-ResultValue -Fields $fields -Name 'entryKind') -ne $expectedEntryKind) {
        throw "Client entry kind mismatch. Expected=$expectedEntryKind, Line=$line"
    }

    if (-not $WaitForMatchEnd -and $PayloadMode -eq 'packed' -and -not (Read-ResultBool -Fields $fields -Name 'snapshotHashMatched')) {
        throw "Client snapshot hash validation failed: $line"
    }

    if ((Read-ResultValue -Fields $fields -Name 'payloadMode') -ne $PayloadMode) {
        throw "Client payload mode mismatch. Expected=$PayloadMode, Line=$line"
    }

    Assert-ClientPayloadResult -ClientResult $ClientResult
    Assert-ClientTimeAnchorResult -ClientResult $ClientResult
    Assert-ClientLagCompensationResult -ClientResult $ClientResult

    if (Read-ResultBool -Fields $fields -Name 'shouldResync') {
        throw "Client requested resync during multiprocess sync acceptance: $line"
    }

    if ((Read-ResultInt -Fields $fields -Name 'runtimeFrame') -le 0 -or (Read-ResultInt -Fields $fields -Name 'viewFrame') -le 0) {
        throw "Client runtime/presentation did not advance: $line"
    }

    if ((Read-ResultInt -Fields $fields -Name 'localRuntimeFrame') -ne (Read-ResultInt -Fields $fields -Name 'runtimeFrame') -or (Read-ResultInt -Fields $fields -Name 'localViewFrame') -ne (Read-ResultInt -Fields $fields -Name 'viewFrame')) {
        throw "Client local runtime/view frame aliases diverged from final frame fields: $line"
    }

    $actualInputs = Read-ResultInt -Fields $fields -Name 'inputs'
    if ($WaitForMatchEnd -and $client.Mode -eq 'create') {
        if ($actualInputs -lt $Inputs) {
            throw "Client input count is lower than expected. ExpectedAtLeast=$Inputs, Line=$line"
        }
    }
    elseif ($actualInputs -ne $Inputs) {
        throw "Client input count mismatch. Expected=$Inputs, Line=$line"
    }

    if ($Inputs -gt 0 -and -not (Read-ResultBool -Fields $fields -Name 'lastInputSuccess')) {
        throw "Client last input did not succeed: $line"
    }

    if ($Inputs -gt 0 -and (Read-ResultInt -Fields $fields -Name 'lastAcceptedFrame') -lt (Read-ResultInt -Fields $fields -Name 'lastRequestedFrame')) {
        throw "Client accepted frame regressed: $line"
    }

    if ($Inputs -gt 0 -and (Read-ResultInt64 -Fields $fields -Name 'lastServerTicks') -le 0) {
        throw "Client input response did not include positive server ticks: $line"
    }

    if ((Read-ResultInt -Fields $fields -Name 'entities') -lt $client.PlayerId) {
        throw "Client snapshot entity count is lower than expected player visibility. ExpectedAtLeast=$($client.PlayerId), Line=$line"
    }

    if ($client.ReconnectCount -gt 0) {
        if ((Read-ResultInt -Fields $fields -Name 'reconnectCount') -ne $client.ReconnectCount) {
            throw "Client reconnect count mismatch. Expected=$($client.ReconnectCount), Line=$line"
        }

        $expectedAttempts = $client.ReconnectCount + $client.RecoverableFailureCount
        if ((Read-ResultInt -Fields $fields -Name 'retryAttemptCount') -ne $expectedAttempts) {
            throw "Client retry attempt count mismatch. Expected=$expectedAttempts, Line=$line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'injectedFailureCount') -ne $client.RecoverableFailureCount) {
            throw "Client injected failure count mismatch. Expected=$($client.RecoverableFailureCount), Line=$line"
        }

        if ((Read-ResultValue -Fields $fields -Name 'reconnectEntryKind') -ne 'Reconnect') {
            throw "Client reconnect entry kind mismatch: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'reconnectPushesAfter') -le (Read-ResultInt -Fields $fields -Name 'reconnectPushesBefore')) {
            throw "Client did not receive a snapshot after reconnect: $line"
        }
    }
    else {
        if ((Read-ResultInt -Fields $fields -Name 'reconnectCount') -ne 0 -or
            (Read-ResultInt -Fields $fields -Name 'retryAttemptCount') -ne 0 -or
            (Read-ResultInt -Fields $fields -Name 'injectedFailureCount') -ne 0) {
            throw "Client unexpectedly entered reconnect/retry flow: $line"
        }
    }

    if ($client.LatencyMs -gt 0 -or $client.JitterMs -gt 0) {
        if ((Read-ResultInt -Fields $fields -Name 'conditionInboundDelayed') -le 0) {
            throw "Client network latency/jitter condition did not delay any inbound push: $line"
        }
    }

    if ($client.PacketLossRate -gt 0) {
        if ((Read-ResultInt -Fields $fields -Name 'conditionInboundDropped') -le 0) {
            throw "Client network packet loss condition did not drop any inbound push: $line"
        }
    }

    $replayPath = Read-ResultValue -Fields $fields -Name 'inputStateReplayPath'
    $minimizedReplayPath = Read-ResultValue -Fields $fields -Name 'minimizedInputStateReplayPath'
    if (-not $NoReplay) {
        if ([string]::IsNullOrWhiteSpace($replayPath)) {
            throw "Client did not report replay path: $line"
        }

        if ([string]::IsNullOrWhiteSpace($minimizedReplayPath)) {
            throw "Client did not report minimized replay path: $line"
        }

        if (-not (Test-Path $replayPath)) {
            throw "Client replay record file was not created: $replayPath"
        }

        if (-not (Test-Path $minimizedReplayPath)) {
            throw "Client minimized replay record file was not created: $minimizedReplayPath"
        }

        $replayFile = Get-Item -LiteralPath $replayPath
        if ($replayFile.Length -le 0) {
            throw "Client replay record file is empty: $replayPath"
        }

        $minimizedReplayFile = Get-Item -LiteralPath $minimizedReplayPath
        if ($minimizedReplayFile.Length -le 0) {
            throw "Client minimized replay record file is empty: $minimizedReplayPath"
        }

        if (-not (Read-ResultBool -Fields $fields -Name 'inputStateReplayConsumed')) {
            throw "Input-state replay record was not consumed by validation: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'inputStateReplaySnapshots') -le 0) {
            throw "Input-state replay record did not include snapshots: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'inputStateReplayHashes') -ne 0) {
            throw "Minimized input-state replay record should not include state hashes: $line"
        }
    }

    if ($WaitForMatchEnd) {
        if (-not (Read-ResultBool -Fields $fields -Name 'matchFinal')) {
            throw "Client did not observe final match state: $line"
        }

        $matchState = Read-ResultInt -Fields $fields -Name 'matchState'
        if ($matchState -ne 2 -and $matchState -ne 3 -and $matchState -ne 4) {
            throw "Client final match state is invalid: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'matchCompletedFrame') -le 0) {
            throw "Client final match completed frame is invalid: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'timeLimitFrames') -le 0) {
            throw "Client final match time limit is invalid: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'pushes') -le 1) {
            throw "Client did not receive continuous snapshot pushes before final state: $line"
        }
    }

    Write-Host $line -ForegroundColor Green
}

function Assert-ClientPayloadResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$ClientResult
    )

    $line = $ClientResult.Line
    $fields = $ClientResult.Fields
    $payloadOpCode = Read-ResultInt -Fields $fields -Name 'payloadOpCode'
    $payloadKind = Read-ResultInt -Fields $fields -Name 'payloadKind'
    $sourceFrame = Read-ResultInt -Fields $fields -Name 'sourceFrame'
    $baselineFrame = Read-ResultInt -Fields $fields -Name 'baselineFrame'
    $visibilityHints = Read-ResultInt -Fields $fields -Name 'visibilityHints'
    $entities = Read-ResultInt -Fields $fields -Name 'entities'
    $fullBaselinesApplied = Read-ResultInt -Fields $fields -Name 'pureStateFullBaselinesApplied'
    $deltasApplied = Read-ResultInt -Fields $fields -Name 'pureStateDeltasApplied'
    $resyncRequests = Read-ResultInt -Fields $fields -Name 'pureStateResyncRequests'
    $lastResyncNeeded = Read-ResultBool -Fields $fields -Name 'pureStateLastResyncNeeded'

    if ($PayloadMode -eq 'pure-state') {
        if ($payloadOpCode -ne 5207 -and $payloadOpCode -ne 5208) {
            throw "PureState payload op code mismatch: $line"
        }

        if ($payloadKind -ne 1 -and $payloadKind -ne 2 -and $payloadKind -ne 3) {
            throw "PureState payload kind is invalid: $line"
        }

        if ($payloadOpCode -eq 5207 -and $payloadKind -ne 1) {
            throw "PureState full payload did not report full baseline kind: $line"
        }

        if ($payloadOpCode -eq 5208 -and $payloadKind -ne 2 -and $payloadKind -ne 3) {
            throw "PureState delta payload did not report delta or low-frequency kind: $line"
        }

        if ($sourceFrame -le 0) {
            throw "PureState source frame is invalid: $line"
        }

        if ($baselineFrame -lt 0) {
            throw "PureState baseline frame is invalid: $line"
        }

        if (($payloadKind -eq 2 -or $payloadKind -eq 3) -and $baselineFrame -le 0) {
            throw "PureState delta baseline frame was not reported: $line"
        }

        if ($visibilityHints -lt 0) {
            throw "PureState visibility hint count is negative: $line"
        }

        if ($visibilityHints -ne $entities) {
            throw "PureState visibility hints should match exported entity count for current payload logic: $line"
        }

        if ($fullBaselinesApplied -lt 1) {
            throw "PureState full baseline was not applied: $line"
        }

        if (($deltasApplied + $resyncRequests + $fullBaselinesApplied) -lt 2) {
            throw "PureState did not apply a later delta, report a baseline resync request, or apply a repeated full baseline: $line"
        }

        if ($lastResyncNeeded -and $resyncRequests -le 0) {
            throw "PureState last resync state was reported without any resync request: $line"
        }
    }
    else {
        if ($payloadOpCode -ne 5204) {
            throw "Packed payload op code mismatch: $line"
        }

        if ($payloadKind -ne 0 -or $visibilityHints -ne 0) {
            throw "Packed payload should not report PureState metadata: $line"
        }
    }
}

function Assert-ClientTimeAnchorResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$ClientResult
    )

    $line = $ClientResult.Line
    $fields = $ClientResult.Fields
    $remoteAnchorValid = Read-ResultBool -Fields $fields -Name 'remoteAnchorValid'
    $targetFrame = Read-ResultInt -Fields $fields -Name 'targetFrame'
    $remoteTargetFrame = Read-ResultInt -Fields $fields -Name 'remoteTargetFrame'
    $remoteCatchUpFrames = Read-ResultInt -Fields $fields -Name 'remoteCatchUpFrames'
    $remoteElapsedSeconds = Read-ResultDouble -Fields $fields -Name 'remoteElapsedSeconds'
    $remoteServerTicks = Read-ResultInt64 -Fields $fields -Name 'remoteServerTicks'
    $snapshotServerTicks = Read-ResultInt64 -Fields $fields -Name 'snapshotServerTicks'
    $lastPushServerTicks = Read-ResultInt64 -Fields $fields -Name 'lastPushServerTicks'
    $lastPushPackedServerTick = Read-ResultInt64 -Fields $fields -Name 'lastPushPackedServerTick'
    $runtimeFrame = Read-ResultInt -Fields $fields -Name 'runtimeFrame'
    $viewFrame = Read-ResultInt -Fields $fields -Name 'viewFrame'
    $timeLimitFrames = Read-ResultInt -Fields $fields -Name 'timeLimitFrames'
    $reconnectCount = Read-ResultInt -Fields $fields -Name 'reconnectCount'
    $lastPushFrame = Read-ResultInt -Fields $fields -Name 'lastPushFrame'

    if ($remoteAnchorValid) {
        if ($remoteServerTicks -le 0) {
            throw "Remote time anchor did not include positive server ticks: $line"
        }

        if ($remoteTargetFrame -lt 0) {
            throw "Remote target frame is invalid: $line"
        }

        if ($remoteCatchUpFrames -lt 0) {
            throw "Remote catch-up frame count is invalid: $line"
        }

        if ($remoteElapsedSeconds -lt 0) {
            throw "Remote elapsed seconds is invalid: $line"
        }
    }

    if ($targetFrame -ne $remoteTargetFrame) {
        throw "Remote target frame diverged from launch target frame: $line"
    }

    if ($snapshotServerTicks -le 0 -or $lastPushServerTicks -le 0) {
        throw "Snapshot push server ticks were not reported: $line"
    }

    if ($lastPushServerTicks -lt $snapshotServerTicks) {
        throw "Last push server ticks regressed behind first applied snapshot: $line"
    }

    if ($lastPushPackedServerTick -le 0) {
        throw "Last push packed/server payload tick was not reported: $line"
    }

    $reachableTargetFrame = if ($reconnectCount -gt 0) {
        $lastPushFrame
    }
    else {
        $remoteTargetFrame
    }
    if ($timeLimitFrames -gt 0 -and $timeLimitFrames -lt $reachableTargetFrame) {
        $reachableTargetFrame = $timeLimitFrames
    }

    if ($runtimeFrame -lt $reachableTargetFrame) {
        throw "Final runtime frame did not catch up to reachable target frame: $line"
    }

    # Authoritative interpolation cannot present beyond the latest received snapshot.
    $reachableViewFrame = [Math]::Min($reachableTargetFrame, $lastPushFrame)
    if ($viewFrame -lt $reachableViewFrame) {
        throw "Final view frame did not consume the reachable authoritative snapshot frame: $line"
    }
}

function Assert-ClientLagCompensationResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$ClientResult
    )

    $line = $ClientResult.Line
    $fields = $ClientResult.Fields
    $accepted = Read-ResultBool -Fields $fields -Name 'lagCompAccepted'
    $reason = Read-ResultValue -Fields $fields -Name 'lagCompReason'
    $requestedFrame = Read-ResultInt -Fields $fields -Name 'lagCompRequestedFrame'
    $resolvedFrame = Read-ResultInt -Fields $fields -Name 'lagCompResolvedFrame'
    $hitEntityId = Read-ResultInt -Fields $fields -Name 'lagCompHitEntityId'
    $runtimeFrame = Read-ResultInt -Fields $fields -Name 'runtimeFrame'

    $acceptableReasons = @('Hit', 'HistoryUnavailable', 'RewindWindowExceeded')
    if (-not $accepted -and -not ($acceptableReasons -contains $reason)) {
        throw "Lag compensation result was not accepted and did not report an acceptable reason: $line"
    }

    if ($requestedFrame -lt 0) {
        throw "Lag compensation requested frame is invalid: $line"
    }

    if ($accepted) {
        if ($reason -ne 'Hit') {
            throw "Accepted lag compensation result did not report Hit: $line"
        }

        if ($hitEntityId -le 0) {
            throw "Accepted lag compensation result did not report a hit entity: $line"
        }

        if ($resolvedFrame -lt 0 -or $resolvedFrame -gt $runtimeFrame) {
            throw "Accepted lag compensation resolved frame is outside the runtime window: $line"
        }
    }
}

function Assert-ClientResultSet {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$ClientResults
    )

    if ($ClientResults.Count -ne ($JoinClients + 1)) {
        throw "Client result count mismatch. Expected=$($JoinClients + 1), Actual=$($ClientResults.Count)."
    }

    $roomId = Read-ResultValue -Fields $ClientResults[0].Fields -Name 'roomId'
    $battleId = Read-ResultValue -Fields $ClientResults[0].Fields -Name 'battleId'
    $worldId = Read-ResultValue -Fields $ClientResults[0].Fields -Name 'worldId'

    foreach ($clientResult in $ClientResults) {
        $line = $clientResult.Line
        if ((Read-ResultValue -Fields $clientResult.Fields -Name 'roomId') -ne $roomId) {
            throw "Client room id mismatch: $line"
        }

        if ((Read-ResultValue -Fields $clientResult.Fields -Name 'battleId') -ne $battleId) {
            throw "Client battle id mismatch: $line"
        }

        if ((Read-ResultValue -Fields $clientResult.Fields -Name 'worldId') -ne $worldId) {
            throw "Client world id mismatch: $line"
        }
    }

    if ($WaitForMatchEnd) {
        $stateHash = Read-ResultValue -Fields $ClientResults[0].Fields -Name 'stateHash'
        $runtimeFrame = Read-ResultInt -Fields $ClientResults[0].Fields -Name 'runtimeFrame'
        $matchState = Read-ResultInt -Fields $ClientResults[0].Fields -Name 'matchState'
        $matchCompletedFrame = Read-ResultInt -Fields $ClientResults[0].Fields -Name 'matchCompletedFrame'
        $matchVictory = Read-ResultBool -Fields $ClientResults[0].Fields -Name 'matchVictory'
 
        foreach ($clientResult in $ClientResults) {
            $line = $clientResult.Line
            if ((Read-ResultValue -Fields $clientResult.Fields -Name 'stateHash') -ne $stateHash) {
                throw "Client final state hash mismatch: $line"
            }

            if ((Read-ResultInt -Fields $clientResult.Fields -Name 'runtimeFrame') -ne $runtimeFrame) {
                throw "Client final runtime frame mismatch: $line"
            }

            if ((Read-ResultInt -Fields $clientResult.Fields -Name 'matchState') -ne $matchState) {
                throw "Client final match state mismatch: $line"
            }

            if ((Read-ResultInt -Fields $clientResult.Fields -Name 'matchCompletedFrame') -ne $matchCompletedFrame) {
                throw "Client final match completed frame mismatch: $line"
            }

            if ((Read-ResultBool -Fields $clientResult.Fields -Name 'matchVictory') -ne $matchVictory) {
                throw "Client final match victory mismatch: $line"
            }
        }
    }
}

function Assert-ClientExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Client
    )

    if (-not $Client.Process.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Client process $($Client.Process.Id) did not exit within $TimeoutSeconds seconds."
    }

    $exitCode = Get-ProcessExitCode -Process $Client.Process
    if ($exitCode -ne 0) {
        throw "Client process $($Client.Process.Id) exited with code $exitCode."
    }
}

if (-not (Test-Path $project)) {
    throw "Shooter smoke project was not found: $project"
}

if ($JoinClients -lt 0) {
    throw 'JoinClients must be >= 0.'
}

$ports = @($TcpPort, $SiloPort, $OrleansGatewayPort)
foreach ($port in $ports) {
    if ($port -le 0 -or $port -gt 65535) {
        throw "Smoke ports must be between 1 and 65535. Invalid port: $port"
    }
}
if (($ports | Sort-Object -Unique).Count -ne $ports.Count) {
    throw 'TcpPort, SiloPort, and OrleansGatewayPort must be distinct.'
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
New-Item -ItemType Directory -Force -Path $replayDir | Out-Null
New-Item -ItemType Directory -Force -Path $diagnosticDir | Out-Null
Write-RunManifest -Status 'running'

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

try {
    if (-not $NoCleanup) {
        Stop-AbilityKitServices `
            -Ports $ports `
            -GraceSeconds 2
    }

    if (-not $NoBuild) {
        Write-Host 'Building Shooter smoke project...' -ForegroundColor Cyan
        dotnet build $project -c $Configuration @commonArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Shooter smoke project build failed with exit code $LASTEXITCODE."
        }
    }

    if (-not (Test-Path -LiteralPath $applicationDll -PathType Leaf)) {
        throw "Shooter smoke application artifact was not found: $applicationDll"
    }

    $serverArgs = @($applicationDll)
    $serverArgs += @(
        '--server',
        '--tcp-port', $TcpPort,
        '--state-sync-payload-mode', $PayloadMode,
        '--fault-control-path', $faultControlPath,
        '--AbilityKit:Orleans:SiloPort', $SiloPort,
        '--AbilityKit:Orleans:GatewayPort', $OrleansGatewayPort,
        '--AbilityKit:Orleans:PrimarySiloPort', $SiloPort)
    if ($activePlan.slowConsumer) {
        $serverArgs += @(
            '--AbilityKit:StateSyncObserver:BytesPerSecond', 256,
            '--AbilityKit:StateSyncObserver:BurstBytes', 32768,
            '--AbilityKit:StateSyncObserver:MaxQueueLength', 1,
            '--AbilityKit:StateSyncObserver:MaxQueueAgeMs', 100,
            '--AbilityKit:StateSyncObserver:DrainIntervalMs', 250)
    }

    Write-Host "Starting Shooter state-sync server on 127.0.0.1:$TcpPort..." -ForegroundColor Cyan
    $serverStartedAtUtc = [DateTime]::UtcNow
    $server = Start-DotnetProcess -Arguments $serverArgs -StdOut $serverLog -StdErr (Join-Path $logDir 'server.err.log')
    $startedProcesses.Add($server)
    $processTimeline += [ordered]@{ role = 'server'; processId = $server.Id; startedAtUtc = $serverStartedAtUtc.ToString('O'); exitedAtUtc = $null; exitCode = $null }
    Wait-ForPort -Port $TcpPort -TimeoutSeconds $StartupTimeoutSeconds
    Add-AssertionResult -Name 'server-listening' -Passed $true -Details "127.0.0.1:$TcpPort"
    $timeoutPhase = 'setup'
    $timeoutBudgetSeconds = $SetupTimeoutSeconds
    $scenarioDeadlineUtc = [DateTime]::UtcNow.AddSeconds($SetupTimeoutSeconds)
    Write-RunManifest -Status 'running'

    Write-Host 'Starting primary create client...' -ForegroundColor Cyan
    $createReplayPath = if ($NoReplay) { '' } else { Join-Path $replayDir "input-state-create$ReplayExtension" }
    $createCorrelationId = "$RunId/shooter-mp-create"
    $createClient = Start-SmokeClient `
        -ClientMode 'create' `
        -ClientId 'shooter-mp-create' `
        -PlayerId 1 `
        -LogPath (Join-Path $logDir 'client-create.log') `
        -ErrLogPath (Join-Path $logDir 'client-create.err.log') `
        -ReplayOutputPath $createReplayPath `
        -CompletionReleasePath $(if ($Scenario -eq 'slow-consumer') { $completionReleasePath } else { '' }) `
        -CorrelationId $createCorrelationId `
        -DiagnosticOutputPath (Join-Path $diagnosticDir 'client-create.diagnostic.json')
    $clientLogs += $createClient.LogPath

    $createReady = Wait-ForClientReady -Client $createClient
    $roomId = Read-ResultValue -Fields $createReady.Fields -Name 'roomId'
    $scenarioEstablished = $true
    Add-AssertionResult -Name 'battle-established' -Passed $true -Details "RoomId=$roomId"
    Write-RunManifest -Status 'running'
    Write-Host "Primary client ready. RoomId=$roomId" -ForegroundColor Green

    $clients = New-Object System.Collections.Generic.List[object]
    $clientResults = New-Object System.Collections.Generic.List[object]
    $clients.Add($createClient)
    for ($i = 1; $i -le $JoinClients; $i++) {
        $playerId = $i + 1
        Write-Host "Starting join client $i as player $playerId..." -ForegroundColor Cyan
        $joinReplayPath = if ($NoReplay) { '' } else { Join-Path $replayDir "input-state-join-$i$ReplayExtension" }
        $joinCorrelationId = "$RunId/shooter-mp-join-$i"
        $joinClient = Start-SmokeClient `
            -ClientMode 'join' `
            -ClientId "shooter-mp-join-$i" `
            -PlayerId $playerId `
            -RoomId $roomId `
            -LogPath (Join-Path $logDir "client-join-$i.log") `
            -ErrLogPath (Join-Path $logDir "client-join-$i.err.log") `
            -ClientReconnectCount $(if ($i -eq 1) { $ReconnectCount } else { 0 }) `
            -ClientRecoverableFailureCount $(if ($i -eq 1) { $RecoverableFailureCount } else { 0 }) `
            -LatencyMs $ConditionLatencyMs `
            -JitterMs $ConditionJitterMs `
            -PacketLossRate $ConditionPacketLossRate `
            -NetworkSeed ($ConditionSeed + $i) `
            -ReplayOutputPath $joinReplayPath `
            -ReconnectReleasePath $(if ($Scenario -eq 'gateway-offline' -and $i -eq 1) { $reconnectReleasePath } else { '' }) `
            -CompletionReleasePath $(if ($Scenario -eq 'slow-consumer') { $completionReleasePath } else { '' }) `
            -CorrelationId $joinCorrelationId `
            -DiagnosticOutputPath (Join-Path $diagnosticDir "client-join-$i.diagnostic.json")
        $clientLogs += $joinClient.LogPath
        $clients.Add($joinClient)
    }

    for ($i = 1; $i -lt $clients.Count; $i++) {
        $joinReady = Wait-ForClientReady -Client $clients[$i]
        Add-AssertionResult -Name "join-$i-ready" -Passed $true -Details $joinReady.Line
    }
    $timeoutPhase = 'active scenario'
    $timeoutBudgetSeconds = $ScenarioTimeoutSeconds
    $scenarioDeadlineUtc = [DateTime]::UtcNow.AddSeconds($ScenarioTimeoutSeconds)
    Add-AssertionResult -Name 'scenario-active-budget-started' -Passed $true -Details "TimeoutSeconds=$ScenarioTimeoutSeconds"
    Write-RunManifest -Status 'running'

    if ($Scenario -eq 'slow-consumer') {
        Start-Sleep -Seconds 2
        New-Item -ItemType File -Path $completionReleasePath -Force | Out-Null
        Add-AssertionResult -Name 'slow-consumer-pressure-window-completed' -Passed $true -Details 'DurationSeconds=2'
        Write-RunManifest -Status 'running'
    }

    if ($Scenario -eq 'gateway-offline') {
        if ($JoinClients -lt 1) {
            throw 'Gateway offline scenario requires at least one join client.'
        }
        Add-AssertionResult -Name 'join-subscribed-before-fault' -Passed $true -Details 'join-1-ready'
        $reconnectReady = Wait-ForClientReconnectReady -Client $clients[1]
        Add-AssertionResult -Name 'join-inputs-completed-before-fault' -Passed $true -Details $reconnectReady.Line
        $null = Invoke-GatewayFaultCommand -Action 'gateway-offline'
        Add-AssertionResult -Name 'gateway-offline-acknowledged' -Passed $true
        Wait-ForPortClosed -Port $TcpPort -TimeoutSeconds 5
        Add-AssertionResult -Name 'gateway-offline-unreachable' -Passed $true -Details "127.0.0.1:$TcpPort"
        $null = Invoke-GatewayFaultCommand -Action 'gateway-online'
        Wait-ForPort -Port $TcpPort -TimeoutSeconds 10
        Add-AssertionResult -Name 'gateway-online-acknowledged' -Passed $true -Details "127.0.0.1:$TcpPort"
        New-Item -ItemType File -Path $reconnectReleasePath -Force | Out-Null
        Add-AssertionResult -Name 'join-reconnect-released-after-recovery' -Passed $true -Details $reconnectReleasePath
        Write-RunManifest -Status 'running'
    }

    foreach ($client in $clients) {
        $clientResult = Wait-ForClientResult -Client $client
        Assert-ClientResult -ClientResult $clientResult
        $clientResults.Add($clientResult)
        $fields = $clientResult.Fields
        $manifestClients += [ordered]@{
            clientId = $client.ClientId
            processId = $client.Process.Id
            correlationId = Read-ResultValue -Fields $fields -Name 'correlationId'
            accountId = Read-ResultValue -Fields $fields -Name 'accountId'
            playerId = Read-ResultInt -Fields $fields -Name 'playerId'
            roomId = Read-ResultValue -Fields $fields -Name 'roomId'
            battleId = Read-ResultValue -Fields $fields -Name 'battleId'
            worldId = Read-ResultValue -Fields $fields -Name 'worldId'
            recordPath = ConvertTo-RunRelativePath -Path (Read-ResultValue -Fields $fields -Name 'inputStateReplayPath')
            diagnosticPath = Read-ResultValue -Fields $fields -Name 'diagnosticArtifactPath'
            diagnosticSha256 = Read-ResultValue -Fields $fields -Name 'diagnosticArtifactSha256'
            diffPath = Read-ResultValue -Fields $fields -Name 'diffPath'
            diffSha256 = Read-ResultValue -Fields $fields -Name 'diffSha256'
            diffStatus = Read-ResultValue -Fields $fields -Name 'diffStatus'
        }
        Write-RunManifest -Status 'running'
    }

    Assert-ClientResultSet -ClientResults $clientResults.ToArray()
    Assert-BoundedConvergence -ClientResults $clientResults.ToArray()

    foreach ($client in $clients) {
        Assert-ClientExitCode -Client $client
        $exitedAtUtc = [DateTime]::UtcNow
        $processTimeline += [ordered]@{
            role = "client-$($client.ClientId)"
            processId = $client.Process.Id
            startedAtUtc = $client.StartedAtUtc.ToString('O')
            exitedAtUtc = $exitedAtUtc.ToString('O')
            exitCode = Get-ProcessExitCode -Process $client.Process
        }
    }

    $mode = if ($WaitForMatchEnd) { 'end-to-end' } elseif ($ReconnectCount -gt 0 -or $ConditionLatencyMs -gt 0 -or $ConditionJitterMs -gt 0 -or $ConditionPacketLossRate -gt 0) { 'resilience' } else { 'sync' }
    $replaySummary = if ($NoReplay) { 'Replay=disabled' } else { "Replay=$replayDir" }
    Add-AssertionResult -Name 'scenario-completed' -Passed $true -Details "Mode=$mode; Clients=$($clients.Count)"
    $manifestStatus = 'passed'
    Write-Host "Shooter multiprocess $mode smoke passed. PayloadMode=$PayloadMode, RoomId=$roomId, Clients=$($clients.Count), Logs=$logDir, Manifest=$manifestPath, $replaySummary" -ForegroundColor Green
}
catch {
    $manifestStatus = 'failed'
    $manifestError = $_.Exception.Message
    Write-Warning "Shooter smoke failure location: $($_.InvocationInfo.PositionMessage)"
    $manifestFailureCategory = Get-FailureClassification -Message $manifestError -Established $scenarioEstablished
    $manifestFailureStage = if ($manifestFailureCategory -eq 'PreconditionFailed') { 'setup' } else { 'fault-recovery' }
    Add-AssertionResult -Name 'scenario-completed' -Passed $false -Details $manifestError
}
finally {
    Stop-StartedProcesses -Processes $startedProcesses
    if ($null -ne $server) {
        $serverTimeline = @($processTimeline | Where-Object { $_.role -eq 'server' } | Select-Object -First 1)
        if ($serverTimeline.Count -gt 0) {
            $server.Refresh()
            if ($server.HasExited) {
                $serverExitCode = Get-ProcessExitCode -Process $server
                $serverTimeline[0].exitedAtUtc = [DateTime]::UtcNow.ToString('O')
                $serverTimeline[0].exitCode = $serverExitCode
            }
        }
    }

    if (-not $NoCleanup) {
        Stop-AbilityKitServices `
            -Ports $ports `
            -GraceSeconds 1
    }

    try {
        Write-RunManifest -Status $manifestStatus -ErrorMessage $manifestError
    }
    catch {
        Write-Warning "Failed to write final manifest without replacing the scenario result: $($_.Exception.Message)"
    }
}

if ($manifestStatus -eq 'failed') {
    [Console]::Error.WriteLine($manifestError)
    exit 1
}
