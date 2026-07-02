param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001,
    [string]$ReplayExtension = '.record.bin',
    [int]$JoinClients = 1,
    [int]$Inputs = 3,
    [int]$Seed = 20260610,
    [int]$TimeoutSeconds = 30,
    [switch]$WaitForMatchEnd,
    [switch]$ReconnectJoinClient,
    [int]$ReconnectDelayMs = 500,
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

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'
$logDir = Join-Path $repoRoot 'artifacts\shooter_multiprocess_smoke'
$serverLog = Join-Path $logDir 'server.log'
$replayDir = Join-Path $logDir 'records'
$clientLogs = @()
$startedProcesses = New-Object System.Collections.Generic.List[System.Diagnostics.Process]

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

function Wait-ForPort {
    param(
        [int]$Port,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $Port -TimeoutMilliseconds 500) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "TCP Gateway did not listen on 127.0.0.1:$Port within $TimeoutSeconds seconds."
}

function Wait-ForResultLine {
    param(
        [string]$Path,
        [string]$Prefix,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $Path) {
            $line = Get-Content $Path -ErrorAction SilentlyContinue | Where-Object { $_ -like "$Prefix*" } | Select-Object -Last 1
            if ($line) {
                return $line
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
        [switch]$EnableReconnect,
        [int]$LatencyMs = 0,
        [int]$JitterMs = 0,
        [double]$PacketLossRate = 0,
        [int]$NetworkSeed = 0,
        [string]$ReplayOutputPath = ''
    )

    $arguments = @('run', '--project', $project, '-c', $Configuration, '--no-build')
    $arguments += $commonArgs
    $arguments += @(
        '--',
        '--client',
        '--state-sync-payload-mode', $PayloadMode,
        '--client-mode', $ClientMode,
        '--tcp-port', $TcpPort,
        '--client-id', $ClientId,
        '--player-id', $PlayerId,
        '--inputs', $Inputs,
        '--seed', $Seed,
        '--timeout-seconds', $TimeoutSeconds)

    if ($WaitForMatchEnd) {
        $arguments += @('--wait-for-match-end')
    }

    if ($EnableReconnect) {
        $arguments += @('--reconnect-once', '--reconnect-delay-ms', $ReconnectDelayMs)
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
        [switch]$EnableReconnect,
        [int]$LatencyMs = 0,
        [int]$JitterMs = 0,
        [double]$PacketLossRate = 0,
        [int]$NetworkSeed = 0,
        [string]$ReplayOutputPath = ''
    )

    $arguments = New-ClientArguments -ClientMode $ClientMode -ClientId $ClientId -PlayerId $PlayerId -RoomId $RoomId -EnableReconnect:$EnableReconnect -LatencyMs $LatencyMs -JitterMs $JitterMs -PacketLossRate $PacketLossRate -NetworkSeed $NetworkSeed -ReplayOutputPath $ReplayOutputPath
    $process = Start-DotnetProcess -Arguments $arguments -StdOut $LogPath -StdErr $ErrLogPath
    $startedProcesses.Add($process)

    return [pscustomobject]@{
        Mode = $ClientMode
        ClientId = $ClientId
        PlayerId = $PlayerId
        LogPath = $LogPath
        Process = $process
        EnableReconnect = [bool]$EnableReconnect
        LatencyMs = $LatencyMs
        JitterMs = $JitterMs
        PacketLossRate = $PacketLossRate
        ReplayOutputPath = $ReplayOutputPath
    }
}

function Wait-ForClientReady {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Client
    )

    $line = Wait-ForResultLine -Path $Client.LogPath -Prefix 'SHOOTER_MP_CLIENT_READY' -TimeoutSeconds $TimeoutSeconds
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
    $expectedEntryKind = if ($client.Mode -eq 'create') { 'TeamLobby' } elseif ($client.EnableReconnect) { 'Reconnect' } else { 'LateJoin' }

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

    if ($client.EnableReconnect) {
        if ((Read-ResultInt -Fields $fields -Name 'reconnectCount') -ne 1) {
            throw "Client reconnect count mismatch: $line"
        }

        if ((Read-ResultValue -Fields $fields -Name 'reconnectEntryKind') -ne 'Reconnect') {
            throw "Client reconnect entry kind mismatch: $line"
        }

        if ((Read-ResultInt -Fields $fields -Name 'reconnectPushesAfter') -le (Read-ResultInt -Fields $fields -Name 'reconnectPushesBefore')) {
            throw "Client did not receive a snapshot after reconnect: $line"
        }
    }
    else {
        if ((Read-ResultInt -Fields $fields -Name 'reconnectCount') -ne 0) {
            throw "Client unexpectedly reconnected: $line"
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

        if (($deltasApplied + $resyncRequests) -lt 1) {
            throw "PureState did not apply a later delta or report a baseline resync request: $line"
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

    $reachableTargetFrame = $remoteTargetFrame
    if ($timeLimitFrames -gt 0 -and $timeLimitFrames -lt $reachableTargetFrame) {
        $reachableTargetFrame = $timeLimitFrames
    }

    if ($runtimeFrame -lt $reachableTargetFrame -or $viewFrame -lt $reachableTargetFrame) {
        throw "Final runtime/view frame did not catch up to reachable target frame: $line"
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

    $acceptableReasons = @('Hit', 'HistoryUnavailable')
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

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
New-Item -ItemType Directory -Force -Path $replayDir | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $logDir '*.log')
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $replayDir '*.record.json')
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $replayDir '*.record.bin')

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

try {
    if (-not $NoCleanup) {
        Stop-AbilityKitServices `
            -Ports @($TcpPort, 12111, 31001) `
            -CommandPatterns @('AbilityKit.Orleans.ShooterSmoke.csproj', '--client-id shooter-mp-', '--server --tcp-port') `
            -GraceSeconds 2
    }

    if (-not $NoBuild) {
        Write-Host 'Building Shooter smoke project...' -ForegroundColor Cyan
        dotnet build $project -c $Configuration @commonArgs
    }

    $serverArgs = @('run', '--project', $project, '-c', $Configuration, '--no-build')
    $serverArgs += $commonArgs
    $serverArgs += @('--', '--server', '--tcp-port', $TcpPort, '--state-sync-payload-mode', $PayloadMode)

    Write-Host "Starting Shooter state-sync server on 127.0.0.1:$TcpPort..." -ForegroundColor Cyan
    $server = Start-DotnetProcess -Arguments $serverArgs -StdOut $serverLog -StdErr (Join-Path $logDir 'server.err.log')
    $startedProcesses.Add($server)
    Wait-ForPort -Port $TcpPort -TimeoutSeconds $TimeoutSeconds

    Write-Host 'Starting primary create client...' -ForegroundColor Cyan
    $createReplayPath = if ($NoReplay) { '' } else { Join-Path $replayDir "input-state-create$ReplayExtension" }
    $createClient = Start-SmokeClient `
        -ClientMode 'create' `
        -ClientId 'shooter-mp-create' `
        -PlayerId 1 `
        -LogPath (Join-Path $logDir 'client-create.log') `
        -ErrLogPath (Join-Path $logDir 'client-create.err.log') `
        -ReplayOutputPath $createReplayPath
    $clientLogs += $createClient.LogPath

    $createReady = Wait-ForClientReady -Client $createClient
    $roomId = Read-ResultValue -Fields $createReady.Fields -Name 'roomId'
    Write-Host "Primary client ready. RoomId=$roomId" -ForegroundColor Green

    $clients = New-Object System.Collections.Generic.List[object]
    $clientResults = New-Object System.Collections.Generic.List[object]
    $clients.Add($createClient)
    for ($i = 1; $i -le $JoinClients; $i++) {
        $playerId = $i + 1
        Write-Host "Starting join client $i as player $playerId..." -ForegroundColor Cyan
        $joinReplayPath = if ($NoReplay) { '' } else { Join-Path $replayDir "input-state-join-$i$ReplayExtension" }
        $joinClient = Start-SmokeClient `
            -ClientMode 'join' `
            -ClientId "shooter-mp-join-$i" `
            -PlayerId $playerId `
            -RoomId $roomId `
            -LogPath (Join-Path $logDir "client-join-$i.log") `
            -ErrLogPath (Join-Path $logDir "client-join-$i.err.log") `
            -EnableReconnect:($ReconnectJoinClient -and $i -eq 1) `
            -LatencyMs $ConditionLatencyMs `
            -JitterMs $ConditionJitterMs `
            -PacketLossRate $ConditionPacketLossRate `
            -NetworkSeed ($ConditionSeed + $i) `
            -ReplayOutputPath $joinReplayPath
        $clientLogs += $joinClient.LogPath
        $clients.Add($joinClient)
    }

    foreach ($client in $clients) {
        $clientResult = Wait-ForClientResult -Client $client
        Assert-ClientResult -ClientResult $clientResult
        $clientResults.Add($clientResult)
    }

    Assert-ClientResultSet -ClientResults $clientResults.ToArray()

    foreach ($client in $clients) {
        Assert-ClientExitCode -Client $client
    }

    $mode = if ($WaitForMatchEnd) { 'end-to-end' } elseif ($ReconnectJoinClient -or $ConditionLatencyMs -gt 0 -or $ConditionJitterMs -gt 0 -or $ConditionPacketLossRate -gt 0) { 'resilience' } else { 'sync' }
    $replaySummary = if ($NoReplay) { 'Replay=disabled' } else { "Replay=$replayDir" }
    Write-Host "Shooter multiprocess $mode smoke passed. PayloadMode=$PayloadMode, RoomId=$roomId, Clients=$($clients.Count), Logs=$logDir, $replaySummary" -ForegroundColor Green
}
finally {
    Stop-StartedProcesses -Processes $startedProcesses

    if (-not $NoCleanup) {
        Stop-AbilityKitServices `
            -Ports @($TcpPort, 12111, 31001) `
            -CommandPatterns @('AbilityKit.Orleans.ShooterSmoke.csproj', '--client-id shooter-mp-', '--server --tcp-port') `
            -GraceSeconds 1
    }
}
