param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001,
    [switch]$NoCleanup
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'

if (-not (Test-Path $project)) {
    throw "Shooter smoke project was not found: $project"
}

if (-not $NoCleanup) {
    Stop-AbilityKitServices `
        -Ports @($TcpPort, 12111, 31001) `
        -CommandPatterns @('AbilityKit.Orleans.ShooterSmoke.csproj') `
        -GraceSeconds 2
}

$commonArgs = @(
    '-p:UseSharedCompilation=false',
    '-p:nodeReuse=false'
)

if (-not $NoBuild) {
    Write-Host 'Building Shooter state-sync server...' -ForegroundColor Cyan
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

$runArgs += $commonArgs
$runArgs += @('--', '--server', '--tcp-port', $TcpPort)

Write-Host "Starting Shooter state-sync server on 127.0.0.1:$TcpPort" -ForegroundColor Green
Write-Host "Unity endpoint: 127.0.0.1:$TcpPort" -ForegroundColor White
Push-Location (Split-Path -Parent $project)
try {
    & dotnet @runArgs
}
finally {
    Pop-Location
}
