param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001,
    [switch]$NoCleanup
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'

if (-not (Test-Path $project)) {
    throw "Shooter smoke project was not found: $project"
}

function Stop-PortOwners {
    param([int[]]$Ports)

    foreach ($port in $Ports) {
        $owners = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($owner in $owners) {
            if ($owner -and $owner -gt 0) {
                Write-Host "Stopping process $owner on port $port" -ForegroundColor Yellow
                Stop-Process -Id $owner -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

if (-not $NoCleanup) {
    Stop-PortOwners -Ports @($TcpPort, 12111, 31001)
    Start-Sleep -Seconds 1
}

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

if (-not $NoBuild) {
    Write-Host "Building Shooter state-sync server..." -ForegroundColor Cyan
    dotnet build $project -c $Configuration @commonArgs
}

$runArgs = @(
    'run',
    '--project', $project,
    '-c', $Configuration
)

if ($NoBuild) {
    $runArgs += '--no-build'
}

$runArgs += @('--')
$runArgs += @('--server')
$runArgs += @('--tcp-port', $TcpPort)
$runArgs += $commonArgs

Write-Host "Starting Shooter state-sync server on 127.0.0.1:$TcpPort" -ForegroundColor Green
Write-Host "Unity endpoint: 127.0.0.1:$TcpPort" -ForegroundColor White
& dotnet @runArgs
