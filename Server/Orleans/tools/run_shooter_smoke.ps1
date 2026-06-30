param(
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$TcpPort = 41001,
    [string]$ReplayExtension = '.record.bin',
    [switch]$NoCleanup
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..\..')
$project = Join-Path $repoRoot 'Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke\AbilityKit.Orleans.ShooterSmoke.csproj'
$logDir = Join-Path $repoRoot 'artifacts\shooter_smoke'
$replayDir = Join-Path $logDir 'records'
$inputLogicReplayPath = Join-Path $replayDir "input-logic$ReplayExtension"
$minimizedInputLogicReplayPath = Join-Path $replayDir "input-logic.min$ReplayExtension"

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
    Write-Host 'Building Shooter smoke project...' -ForegroundColor Cyan
    dotnet build $project -c $Configuration @commonArgs
}

New-Item -ItemType Directory -Force -Path $replayDir | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue $inputLogicReplayPath
Remove-Item -Force -ErrorAction SilentlyContinue $minimizedInputLogicReplayPath

Write-Host "Running Shooter TCP Gateway smoke on 127.0.0.1:$TcpPort..." -ForegroundColor Cyan
$runArgs = @(
    'run',
    '--project', $project,
    '-c', $Configuration
)

if ($NoBuild) {
    $runArgs += '--no-build'
}

$runArgs += $commonArgs
$runArgs += @('--', '--tcp-port', $TcpPort, '--input-logic-replay-output', $inputLogicReplayPath)
Push-Location (Split-Path -Parent $project)
try {
    & dotnet @runArgs
    if (-not (Test-Path $inputLogicReplayPath)) {
        throw "Input-logic replay file was not created: $inputLogicReplayPath"
    }

    $replayFile = Get-Item -LiteralPath $inputLogicReplayPath
    if ($replayFile.Length -le 0) {
        throw "Input-logic replay file is empty: $inputLogicReplayPath"
    }

    if (-not (Test-Path $minimizedInputLogicReplayPath)) {
        throw "Minimized input-logic replay file was not created: $minimizedInputLogicReplayPath"
    }

    $minimizedReplayFile = Get-Item -LiteralPath $minimizedInputLogicReplayPath
    if ($minimizedReplayFile.Length -le 0) {
        throw "Minimized input-logic replay file is empty: $minimizedInputLogicReplayPath"
    }
}
finally {
    Pop-Location
}
