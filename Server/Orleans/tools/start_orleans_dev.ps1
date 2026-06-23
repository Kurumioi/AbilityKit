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
    [switch]$CleanAll
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

if (-not $NoBuild) {
    Write-Host 'Building Orleans Host and Gateway...' -ForegroundColor Cyan
    dotnet build $hostProj -c $Configuration @commonArgs
    dotnet build $gatewayProj -c $Configuration @commonArgs
}

$runBuildArgs = $commonArgs -join ' '
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
$noBuildArg = if ($NoBuild) { '--no-build' } else { '' }

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
$siloGatewayReady = $false
$siloGatewayDeadline = (Get-Date).AddSeconds(60)
while ((Get-Date) -lt $siloGatewayDeadline) {
    if (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $SiloGatewayPort -TimeoutMilliseconds 1000) {
        $siloGatewayReady = $true
        break
    }

    Start-Sleep -Seconds 1
}

if (-not $siloGatewayReady) {
    Write-Host "Orleans Silo Gateway: not listening yet (127.0.0.1:$SiloGatewayPort)" -ForegroundColor Yellow
    Write-Host "  Gateway will still be started, but Orleans client startup may fail until the silo binds this port." -ForegroundColor Yellow
    Write-Host "  Please inspect host log: $hostLog" -ForegroundColor Yellow
}
else {
    Write-Host "Orleans Silo Gateway: OK (127.0.0.1:$SiloGatewayPort)" -ForegroundColor Green
}

Write-Host 'Starting Orleans Gateway...' -ForegroundColor Cyan
$gatewayLog = Join-Path $instanceLogs 'gateway.log'
$gatewayCommand = "`$Host.UI.RawUI.WindowTitle = 'AbilityKit $InstanceName Gateway'; dotnet run --project `"$gatewayProj`" -c $Configuration $noBuildArg $runBuildArgs -- $gatewayConfigArgs 2>&1 | Tee-Object -FilePath `"$gatewayLog`" -Append"
$gatewayArgs = @('-NoExit', '-NoProfile', '-Command', $gatewayCommand)
$gatewayWindow = Start-Process powershell -ArgumentList $gatewayArgs -PassThru -WindowStyle Normal
Write-Host "  Gateway window PID: $($gatewayWindow.Id)" -ForegroundColor Gray

if (Wait-AbilityKitHttpEndpoint -Uri $healthUri -TimeoutSeconds 30) {
    Write-Host "Gateway Health: OK ($healthUri)" -ForegroundColor Green
}
elseif (Wait-AbilityKitHttpEndpoint -Uri $healthLiveUri -TimeoutSeconds 5) {
    Write-Host "Gateway Health: live but not ready ($healthLiveUri)" -ForegroundColor Yellow
}
else {
    Write-Host "Gateway Health: not ready yet ($healthUri)" -ForegroundColor Yellow
    if (-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $SiloGatewayPort -TimeoutMilliseconds 1000)) {
        Write-Host "  Orleans Silo Gateway is not reachable: 127.0.0.1:$SiloGatewayPort" -ForegroundColor Yellow
    }
    if (-not (Test-AbilityKitTcpPort -HostName '127.0.0.1' -Port $GatewayPort -TimeoutMilliseconds 1000)) {
        Write-Host "  HTTP Gateway port is not reachable: 127.0.0.1:$GatewayPort" -ForegroundColor Yellow
    }
    Write-Host "  Please inspect logs: $hostLog and $gatewayLog" -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Gateway:' -ForegroundColor Green
Write-Host "  Admin: $adminUri"
Write-Host "  HTTP:  $healthUri"
Write-Host "  TCP:   127.0.0.1:$TcpPort"
Write-Host ''
Write-Host 'Use a different profile or explicit ports to run multiple local environments in parallel.' -ForegroundColor Gray
Write-Host "Stop this instance with: powershell -ExecutionPolicy Bypass -File `"$PSScriptRoot\stop_abilitykit.ps1`" -Profile $InstanceName" -ForegroundColor Gray
Write-Host 'Close the spawned PowerShell windows to stop services manually, or run the stop script for a clean full exit.' -ForegroundColor Gray
