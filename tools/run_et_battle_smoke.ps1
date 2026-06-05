param(
    [int]$SmokeFrames = 600,
    [int]$MinBattleFrames = 30,
    [int]$TimeoutMilliseconds = 15000,
    [int]$SleepMilliseconds = 16,
    [int]$DrainFrames = 5,
    [switch]$NoBuild,
    [switch]$KeepOutput
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App\AbilityKit.Demo.ET.App.csproj'
$output = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App\smoke-output.txt'

function Stop-SmokeProcesses {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq 'dotnet.exe' -and
            $_.CommandLine -like '*AbilityKit.Demo.ET.App*--smoke*'
        } |
        ForEach-Object {
            Write-Host ("Stopping lingering smoke process {0}" -f $_.ProcessId) -ForegroundColor Yellow
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
}

function Remove-SmokeDiagnostics {
    $diagnosticFiles = @(
        'smoke-runtime-ascii.txt',
        'smoke-runtime-clean.txt',
        'smoke-runtime-output.txt',
        'smoke-output.txt'
    )

    foreach ($file in $diagnosticFiles) {
        $path = Join-Path $repoRoot $file
        if (Test-Path $path) {
            Remove-Item $path -Force
            Write-Host ("Removed diagnostic output {0}" -f $file) -ForegroundColor DarkGray
        }
    }

    if (-not $KeepOutput -and (Test-Path $output)) {
        Remove-Item $output -Force
        Write-Host 'Removed previous ET smoke output' -ForegroundColor DarkGray
    }
}

Write-Host '=== AbilityKit ET Battle Smoke ===' -ForegroundColor Cyan
Write-Host ("Project: {0}" -f $project)
Write-Host ("Frames: {0}, MinBattleFrames: {1}, TimeoutMs: {2}, SleepMs: {3}, DrainFrames: {4}" -f $SmokeFrames, $MinBattleFrames, $TimeoutMilliseconds, $SleepMilliseconds, $DrainFrames)

Stop-SmokeProcesses
Remove-SmokeDiagnostics

if (-not $NoBuild) {
    Write-Host '=== Build ===' -ForegroundColor Cyan
    dotnet build $project
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

Write-Host '=== Smoke Run ===' -ForegroundColor Cyan
$arguments = @(
    'run',
    '--no-build',
    '--project',
    $project,
    '--',
    '--smoke',
    "--smoke-frames=$SmokeFrames",
    "--smoke-min-battle-frames=$MinBattleFrames",
    "--smoke-timeout-ms=$TimeoutMilliseconds",
    "--smoke-sleep-ms=$SleepMilliseconds",
    "--smoke-drain-frames=$DrainFrames"
)

& dotnet @arguments 2>&1 | Tee-Object -FilePath $output
$runExitCode = $LASTEXITCODE

Stop-SmokeProcesses

$passed = Select-String -Path $output -Pattern '=== ET Battle Smoke Passed ===' -Quiet
$resultLine = Select-String -Path $output -Pattern '^\[ETBattleSmoke\]' | Select-Object -Last 1

Write-Host '=== Smoke Summary ===' -ForegroundColor Cyan
if ($resultLine) {
    Write-Host $resultLine.Line
}

if ($runExitCode -eq 0 -and $passed) {
    Write-Host 'Result: Passed' -ForegroundColor Green
    if (-not $KeepOutput) {
        Remove-Item $output -Force -ErrorAction SilentlyContinue
        Write-Host 'Removed ET smoke output after successful run' -ForegroundColor DarkGray
    }
    exit 0
}

Write-Host ("Result: Failed, exit code {0}" -f $runExitCode) -ForegroundColor Red
Write-Host ("Smoke output kept at {0}" -f $output) -ForegroundColor Yellow
exit $(if ($runExitCode -ne 0) { $runExitCode } else { 2 })
