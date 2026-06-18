param(
    [int]$GatewayPort = 5001,
    [int]$SiloPort = 11111,
    [int]$SiloGatewayPort = 30000,
    [int]$TcpPort = 4000,
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [switch]$NoCleanup
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$src = Join-Path $root 'src'

$hostProj = Join-Path $src 'AbilityKit.Orleans.Host\AbilityKit.Orleans.Host.csproj'
$gatewayProj = Join-Path $src 'AbilityKit.Orleans.Gateway\AbilityKit.Orleans.Gateway.csproj'

if (!(Test-Path $hostProj)) { throw "Host csproj not found: $hostProj" }
if (!(Test-Path $gatewayProj)) { throw "Gateway csproj not found: $gatewayProj" }

$managedPorts = @($GatewayPort, $SiloPort, $SiloGatewayPort, $TcpPort)
$managedProjects = @(
    'AbilityKit.Orleans.Host.csproj',
    'AbilityKit.Orleans.Gateway.csproj'
)

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

$noBuildArg = if ($NoBuild) { '--no-build' } else { '' }
$runBuildArgs = $commonArgs -join ' '

Write-Host 'Starting Orleans Silo Host...' -ForegroundColor Cyan
$hostArgs = @(
    '-NoExit',
    '-NoProfile',
    '-Command',
    "dotnet run --project `"$hostProj`" -c $Configuration $noBuildArg $runBuildArgs"
)
$hostWindow = Start-Process powershell -ArgumentList $hostArgs -PassThru -WindowStyle Normal
Write-Host "  Host window PID: $($hostWindow.Id)" -ForegroundColor Gray

Start-Sleep -Seconds 5

Write-Host 'Starting Orleans Gateway...' -ForegroundColor Cyan
$gatewayArgs = @(
    '-NoExit',
    '-NoProfile',
    '-Command',
    "dotnet run --project `"$gatewayProj`" -c $Configuration $noBuildArg $runBuildArgs"
)
$gatewayWindow = Start-Process powershell -ArgumentList $gatewayArgs -PassThru -WindowStyle Normal
Write-Host "  Gateway window PID: $($gatewayWindow.Id)" -ForegroundColor Gray

$gatewayHealth = "http://localhost:$GatewayPort/health"
if (Wait-AbilityKitHttpEndpoint -Uri $gatewayHealth -TimeoutSeconds 20) {
    Write-Host "Gateway Health: OK ($gatewayHealth)" -ForegroundColor Green
}
else {
    Write-Host "Gateway Health: not ready yet ($gatewayHealth)" -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Gateway:' -ForegroundColor Green
Write-Host "  HTTP: $gatewayHealth"
Write-Host "  TCP:  127.0.0.1:$TcpPort"
Write-Host ''
Write-Host 'Close the spawned PowerShell windows to stop services, or run this script again to restart cleanly.' -ForegroundColor Gray
