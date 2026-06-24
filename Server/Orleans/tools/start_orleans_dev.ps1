param(
    [int]$GatewayPort = 5001,
    [int]$SiloPort = 11111,
    [int]$SiloGatewayPort = 30000,
    [int]$TcpPort = 4000,
    [string]$ClusterId = 'abilitykit-dev',
    [string]$ServiceId = 'abilitykit-orleans',
    [string]$InstanceName = 'dev',
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [switch]$NoCleanup,
    [switch]$CleanAll,
    [switch]$ForceStartGateway,
    [int]$SiloGatewayWaitSeconds = 120
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$src = Join-Path $root 'src'

$hostProj = Join-Path $src 'AbilityKit.Orleans.Host\AbilityKit.Orleans.Host.csproj'
$gatewayProj = Join-Path $src 'AbilityKit.Orleans.Gateway\AbilityKit.Orleans.Gateway.csproj'
$healthUri = "http://localhost:$GatewayPort/health/ready"
$healthLiveUri = "http://localhost:$GatewayPort/health/live"
$adminUri = "http://localhost:$GatewayPort/admin"
$instanceLogs = Join-Path $root (Join-Path 'logs' $InstanceName)

if (!(Test-Path $hostProj)) { throw "Host csproj not found: $hostProj" }
if (!(Test-Path $gatewayProj)) { throw "Gateway csproj not found: $gatewayProj" }

New-Item -ItemType Directory -Force -Path $instanceLogs | Out-Null

$managedPorts = @($GatewayPort, $SiloPort, $SiloGatewayPort, $TcpPort)
$managedProjects = if ($CleanAll) {
    @(
        'AbilityKit.Orleans.Host.csproj',
        'AbilityKit.Orleans.Gateway.csproj'
    )
}
else {
    @()
}

if (-not $NoCleanup) {
    Stop-AbilityKitServices -Ports $managedPorts -CommandPatterns $managedProjects -GraceSeconds 2
}

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

$startupStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$runBuildArgs = ''
$noBuildArg = '--no-build'

if (-not $NoBuild) {
    Write-Host 'Building Orleans Host and Gateway...' -ForegroundColor Cyan
    $buildStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build $hostProj -c $Configuration @commonArgs
    dotnet build $gatewayProj -c $Configuration @commonArgs
    $buildStopwatch.Stop()
    Write-Host ("Build completed in {0:n1}s. Runtime windows will use dotnet run --no-build to avoid rebuilding." -f $buildStopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
}
else {
    Write-Host 'Skipping build because -NoBuild was specified. Runtime windows will use dotnet run --no-build.' -ForegroundColor Yellow
}

$hostConfigArgs = @(
    '--AbilityKit:Orleans:ClusterId', $ClusterId,
    '--AbilityKit:Orleans:ServiceId', $ServiceId,
    '--AbilityKit:Orleans:SiloPort', $SiloPort,
    '--AbilityKit:Orleans:GatewayPort', $SiloGatewayPort
) -join ' '
$gatewayConfigArgs = @(
    '--AbilityKit:Orleans:ClusterId', $ClusterId,
    '--AbilityKit:Orleans:ServiceId', $ServiceId,
    '--AbilityKit:Orleans:GatewayPort', $SiloGatewayPort,
    '--AbilityKit:Gateway:Http:Port', $GatewayPort,
    '--AbilityKit:Gateway:Tcp:Port', $TcpPort,
    '--TcpGateway:Port', $TcpPort
) -join ' '
Write-Host "Starting AbilityKit instance '$InstanceName'..." -ForegroundColor Green
Write-Host "  ClusterId:        $ClusterId" -ForegroundColor Gray
Write-Host "  ServiceId:        $ServiceId" -ForegroundColor Gray
Write-Host "  HTTP Gateway:     $GatewayPort" -ForegroundColor Gray
Write-Host "  Orleans Silo:     $SiloPort" -ForegroundColor Gray
Write-Host "  Orleans Gateway:  $SiloGatewayPort" -ForegroundColor Gray
Write-Host "  TCP Gateway:      $TcpPort" -ForegroundColor Gray
Write-Host "  Logs:             $instanceLogs" -ForegroundColor Gray
Write-Host ''

Write-Host 'Starting Orleans Silo Host...' -ForegroundColor Cyan
$hostLog = Join-Path $instanceLogs 'host.log'
$hostCommand = "`$Host.UI.RawUI.WindowTitle = 'AbilityKit $InstanceName Host'; dotnet run --project `"$hostProj`" -c $Configuration $noBuildArg $runBuildArgs -- $hostConfigArgs 2>&1 | Tee-Object -FilePath `"$hostLog`" -Append"
$hostArgs = @('-NoExit', '-NoProfile', '-Command', $hostCommand)
$hostWindow = Start-Process powershell -ArgumentList $hostArgs -PassThru -WindowStyle Normal
Write-Host "  Host window PID: $($hostWindow.Id)" -ForegroundColor Gray

Write-Host "Waiting for Orleans Silo Gateway TCP endpoint 127.0.0.1:$SiloGatewayPort ..." -ForegroundColor Cyan
$siloWaitStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$siloGatewayReady = $false
$siloGatewayDeadline = (Get-Date).AddSeconds($SiloGatewayWaitSeconds)
while ((Get-Date) -lt $siloGatewayDeadline) {
    if (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $SiloGatewayPort -TimeoutMilliseconds 1000) {
        $siloGatewayReady = $true
        break
    }

    Start-Sleep -Seconds 1
}

$siloWaitStopwatch.Stop()

if (-not $siloGatewayReady) {
    Write-Host ("Orleans Silo Gateway: not listening after {0:n1}s (127.0.0.1:$SiloGatewayPort)" -f $siloWaitStopwatch.Elapsed.TotalSeconds) -ForegroundColor Yellow
    Write-Host "  Host may still be building/starting, may have exited, or may be blocked by another process/port." -ForegroundColor Yellow
    Write-Host "  Please inspect host log: $hostLog" -ForegroundColor Yellow
    if (-not $ForceStartGateway) {
        Write-Host "  Gateway startup skipped to avoid an immediate Orleans client ConnectionRefused failure." -ForegroundColor Yellow
        Write-Host "  Re-run with -ForceStartGateway only if you intentionally want to start the HTTP Gateway before the Silo Gateway port is ready." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "  -ForceStartGateway specified; Gateway will still be started, but Orleans client startup may fail until the silo binds this port." -ForegroundColor Yellow
}
else {
    Write-Host ("Orleans Silo Gateway: OK after {0:n1}s (127.0.0.1:$SiloGatewayPort)" -f $siloWaitStopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
}

Write-Host 'Starting Orleans Gateway...' -ForegroundColor Cyan
$gatewayLog = Join-Path $instanceLogs 'gateway.log'
$gatewayCommand = "`$Host.UI.RawUI.WindowTitle = 'AbilityKit $InstanceName Gateway'; dotnet run --project `"$gatewayProj`" -c $Configuration $noBuildArg $runBuildArgs -- $gatewayConfigArgs 2>&1 | Tee-Object -FilePath `"$gatewayLog`" -Append"
$gatewayArgs = @('-NoExit', '-NoProfile', '-Command', $gatewayCommand)
$gatewayWindow = Start-Process powershell -ArgumentList $gatewayArgs -PassThru -WindowStyle Normal
Write-Host "  Gateway window PID: $($gatewayWindow.Id)" -ForegroundColor Gray

$gatewayHealthStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
if (Wait-AbilityKitHttpEndpoint -Uri $healthUri -TimeoutSeconds 30) {
    $gatewayHealthStopwatch.Stop()
    Write-Host ("Gateway Health: OK after {0:n1}s ($healthUri)" -f $gatewayHealthStopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
}
elseif (Wait-AbilityKitHttpEndpoint -Uri $healthLiveUri -TimeoutSeconds 5) {
    $gatewayHealthStopwatch.Stop()
    Write-Host "Gateway Health: live but not ready ($healthLiveUri)" -ForegroundColor Yellow
}
else {
    $gatewayHealthStopwatch.Stop()
    Write-Host ("Gateway Health: not ready after {0:n1}s ($healthUri)" -f $gatewayHealthStopwatch.Elapsed.TotalSeconds) -ForegroundColor Yellow
    if (-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $SiloGatewayPort -TimeoutMilliseconds 1000)) {
        Write-Host "  Orleans Silo Gateway is not reachable: 127.0.0.1:$SiloGatewayPort" -ForegroundColor Yellow
    }
    if (-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $GatewayPort -TimeoutMilliseconds 1000)) {
        Write-Host "  HTTP Gateway port is not reachable: 127.0.0.1:$GatewayPort" -ForegroundColor Yellow
    }
    Write-Host "  Please inspect logs: $hostLog and $gatewayLog" -ForegroundColor Yellow
}

$startupStopwatch.Stop()
Write-Host ("Startup script orchestration completed in {0:n1}s." -f $startupStopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
Write-Host ''
Write-Host 'Gateway:' -ForegroundColor Green
Write-Host "  Admin: $adminUri"
Write-Host "  HTTP:  $healthUri"
Write-Host "  TCP:   127.0.0.1:$TcpPort"
Write-Host ''
Write-Host 'Use a different profile or explicit ports to run multiple local environments in parallel.' -ForegroundColor Gray
Write-Host "Stop this instance with: powershell -ExecutionPolicy Bypass -File `"$PSScriptRoot\stop_abilitykit.ps1`" -Profile $InstanceName" -ForegroundColor Gray
Write-Host 'Close the spawned PowerShell windows to stop services manually, or run the stop script for a clean full exit.' -ForegroundColor Gray
