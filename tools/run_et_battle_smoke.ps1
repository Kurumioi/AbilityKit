param(
    [string]$ConfigPath = 'tools\et-battle-smoke.config.json',
    [int]$SmokeFrames,
    [string]$SmokeCasePath,
    [int]$MinBattleFrames,
    [int]$TimeoutMilliseconds,
    [int]$SleepMilliseconds,
    [int]$DrainFrames,
    [int]$ConsistencyRuns,
    [switch]$NoBuild,
    [switch]$SkipConfigValidation,
    [switch]$KeepOutput
)

$ErrorActionPreference = 'Stop'
$scriptBoundParameters = @{} + $PSBoundParameters

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    $resolvedConfigPath = $ConfigPath
}
else {
    $resolvedConfigPath = Join-Path $repoRoot $ConfigPath
}

if (-not (Test-Path $resolvedConfigPath)) {
    throw "Smoke config file not found: $resolvedConfigPath"
}

$smokeConfig = Get-Content -Path $resolvedConfigPath -Raw | ConvertFrom-Json

function Test-ConfigProperty {
    param(
        [object]$Config,
        [string]$Name
    )

    return $null -ne $Config.PSObject.Properties[$Name]
}

function Resolve-IntSetting {
    param(
        [string]$Name,
        [int]$CommandValue,
        [int]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return [int]$CommandValue
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [int]$smokeConfig.$Name
    }

    return $Fallback
}

function Resolve-BoolSetting {
    param(
        [string]$Name,
        [switch]$CommandValue,
        [bool]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return $CommandValue.IsPresent
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [bool]$smokeConfig.$Name
    }

    return $Fallback
}

function Resolve-StringSetting {
    param(
        [string]$Name,
        [string]$CommandValue,
        [string]$Fallback
    )

    if ($scriptBoundParameters.ContainsKey($Name)) {
        return $CommandValue
    }

    if (Test-ConfigProperty -Config $smokeConfig -Name $Name) {
        return [string]$smokeConfig.$Name
    }

    return $Fallback
}

$SmokeFrames = Resolve-IntSetting -Name 'SmokeFrames' -CommandValue $SmokeFrames -Fallback 600
$SmokeCasePath = Resolve-StringSetting -Name 'SmokeCasePath' -CommandValue $SmokeCasePath -Fallback 'tools\et-battle-smoke.case.damage.json'
$MinBattleFrames = Resolve-IntSetting -Name 'MinBattleFrames' -CommandValue $MinBattleFrames -Fallback 30
$TimeoutMilliseconds = Resolve-IntSetting -Name 'TimeoutMilliseconds' -CommandValue $TimeoutMilliseconds -Fallback 15000
$SleepMilliseconds = Resolve-IntSetting -Name 'SleepMilliseconds' -CommandValue $SleepMilliseconds -Fallback 16
$DrainFrames = Resolve-IntSetting -Name 'DrainFrames' -CommandValue $DrainFrames -Fallback 5
$ConsistencyRuns = Resolve-IntSetting -Name 'ConsistencyRuns' -CommandValue $ConsistencyRuns -Fallback 2
$NoBuild = Resolve-BoolSetting -Name 'NoBuild' -CommandValue $NoBuild -Fallback $false
$SkipConfigValidation = Resolve-BoolSetting -Name 'SkipConfigValidation' -CommandValue $SkipConfigValidation -Fallback $false
$KeepOutput = Resolve-BoolSetting -Name 'KeepOutput' -CommandValue $KeepOutput -Fallback $false

$smokeCasePaths = @($SmokeCasePath)
if (-not $scriptBoundParameters.ContainsKey('SmokeCasePath') -and (Test-ConfigProperty -Config $smokeConfig -Name 'SmokeCasePaths')) {
    $smokeCasePaths = @($smokeConfig.SmokeCasePaths)
}

$resolvedSmokeCasePaths = @()
foreach ($casePath in $smokeCasePaths) {
    if ([string]::IsNullOrWhiteSpace($casePath)) {
        continue
    }

    if ([System.IO.Path]::IsPathRooted($casePath)) {
        $resolvedCasePath = $casePath
    }
    else {
        $resolvedCasePath = Join-Path $repoRoot $casePath
    }

    if (-not (Test-Path $resolvedCasePath)) {
        throw "Smoke case file not found: $resolvedCasePath"
    }

    $resolvedSmokeCasePaths += $resolvedCasePath
}

if ($resolvedSmokeCasePaths.Count -eq 0) {
    throw 'At least one smoke case file is required.'
}

$project = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App\AbilityKit.Demo.ET.App.csproj'
$outputDirectory = Join-Path $repoRoot 'src\AbilityKit.Demo.ET.App'
$output = Join-Path $outputDirectory 'smoke-output.txt'

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
        'src\AbilityKit.Demo.ET.App\smoke-output.txt',
        'src\AbilityKit.Demo.ET.App\smoke-output-run-*.txt'
    )

    foreach ($file in $diagnosticFiles) {
        $path = Join-Path $repoRoot $file
        foreach ($matchedPath in Get-ChildItem -Path $path -ErrorAction SilentlyContinue) {
            Remove-Item $matchedPath.FullName -Force
            Write-Host ("Removed diagnostic output {0}" -f $matchedPath.Name) -ForegroundColor DarkGray
        }
    }
}

function Invoke-ConfigValidation {
    $arguments = @(
        'run',
        '--no-build',
        '--project',
        $project,
        '--',
        '--validate-config-only'
    )

    Write-Host '=== Config Validation ===' -ForegroundColor Cyan
    & dotnet @arguments
    $validateExitCode = $LASTEXITCODE
    if ($validateExitCode -ne 0) {
        throw "Config validation failed with exit code $validateExitCode"
    }
}

function Invoke-SmokeRun {
    param(
        [int]$CaseIndex,
        [int]$CaseCount,
        [string]$ResolvedSmokeCasePath,
        [int]$RunIndex
    )

    $caseName = [System.IO.Path]::GetFileNameWithoutExtension($ResolvedSmokeCasePath)
    $runOutput = Join-Path $outputDirectory ("smoke-output-case-{0}-run-{1}.txt" -f $CaseIndex, $RunIndex)
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
        "--smoke-drain-frames=$DrainFrames",
        "--smoke-case=$ResolvedSmokeCasePath"
    )

    Write-Host ("=== Smoke Case {0}/{1}: {2}, Run {3}/{4} ===" -f $CaseIndex, $CaseCount, $caseName, $RunIndex, $ConsistencyRuns) -ForegroundColor Cyan
    $null = & dotnet @arguments 2>&1 | Tee-Object -FilePath $runOutput
    $runExitCode = $LASTEXITCODE

    Stop-SmokeProcesses

    $passed = Select-String -Path $runOutput -Pattern '=== ET Battle Smoke Passed ===' -Quiet
    $resultLine = Select-String -Path $runOutput -Pattern '^\[ETBattleSmoke\]' | Select-Object -Last 1
    $signature = $null

    if ($resultLine -and $resultLine.Line -match 'DeterminismSignature=(.+)$') {
        $signature = $Matches[1]
    }

    [PSCustomObject]@{
        CaseIndex = $CaseIndex
        CaseName = $caseName
        Index = $RunIndex
        Output = $runOutput
        ExitCode = $runExitCode
        Passed = ($runExitCode -eq 0 -and $passed)
        ResultLine = $(if ($resultLine) { $resultLine.Line } else { '' })
        Signature = $signature
    }
}

$ConsistencyRuns = [Math]::Max(1, $ConsistencyRuns)

Write-Host '=== AbilityKit ET Battle Smoke ===' -ForegroundColor Cyan
Write-Host ("Project: {0}" -f $project)
Write-Host ("Config: {0}" -f $resolvedConfigPath)
Write-Host ("SmokeCases: {0}" -f ($resolvedSmokeCasePaths -join ', '))
Write-Host ("Frames: {0}, MinBattleFrames: {1}, TimeoutMs: {2}, SleepMs: {3}, DrainFrames: {4}, ConsistencyRuns: {5}, SkipConfigValidation: {6}" -f $SmokeFrames, $MinBattleFrames, $TimeoutMilliseconds, $SleepMilliseconds, $DrainFrames, $ConsistencyRuns, $SkipConfigValidation)

Stop-SmokeProcesses
Remove-SmokeDiagnostics

if (-not $NoBuild) {
    Write-Host '=== Build ===' -ForegroundColor Cyan
    dotnet build $project
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipConfigValidation) {
    Invoke-ConfigValidation
}

$results = @()
for ($caseIndex = 1; $caseIndex -le $resolvedSmokeCasePaths.Count; $caseIndex++) {
    $casePath = $resolvedSmokeCasePaths[$caseIndex - 1]
    for ($runIndex = 1; $runIndex -le $ConsistencyRuns; $runIndex++) {
        $results += Invoke-SmokeRun -CaseIndex $caseIndex -CaseCount $resolvedSmokeCasePaths.Count -ResolvedSmokeCasePath $casePath -RunIndex $runIndex
    }
}

Write-Host '=== Smoke Summary ===' -ForegroundColor Cyan
foreach ($result in $results) {
    if ($result.ResultLine) {
        Write-Host ("Case {0} ({1}) Run {2}: {3}" -f $result.CaseIndex, $result.CaseName, $result.Index, $result.ResultLine)
    }
    else {
        Write-Host ("Case {0} ({1}) Run {2}: missing ETBattleSmoke result line" -f $result.CaseIndex, $result.CaseName, $result.Index) -ForegroundColor Yellow
    }
}

$failedRuns = @($results | Where-Object { -not $_.Passed -or [string]::IsNullOrWhiteSpace($_.Signature) })
if ($failedRuns.Count -gt 0) {
    foreach ($result in $failedRuns) {
        Write-Host ("Case {0} ({1}) Run {2} failed, exit code {3}, output kept at {4}" -f $result.CaseIndex, $result.CaseName, $result.Index, $result.ExitCode, $result.Output) -ForegroundColor Red
    }

    exit $(if ($failedRuns[0].ExitCode -ne 0) { $failedRuns[0].ExitCode } else { 2 })
}

foreach ($caseGroup in ($results | Group-Object CaseIndex)) {
    $caseResults = @($caseGroup.Group)
    $baselineSignature = $caseResults[0].Signature
    $mismatchedRuns = @($caseResults | Where-Object { $_.Signature -ne $baselineSignature })
    if ($mismatchedRuns.Count -gt 0) {
        Write-Host ("Consistency: Failed for case {0} ({1})" -f $caseResults[0].CaseIndex, $caseResults[0].CaseName) -ForegroundColor Red
        foreach ($result in $caseResults) {
            Write-Host ("Case {0} Run {1} Signature: {2}" -f $result.CaseIndex, $result.Index, $result.Signature)
            Write-Host ("Case {0} Run {1} Output: {2}" -f $result.CaseIndex, $result.Index, $result.Output) -ForegroundColor Yellow
        }

        exit 3
    }
}

Write-Host 'Result: Passed' -ForegroundColor Green
Write-Host 'Consistency: Passed' -ForegroundColor Green
foreach ($caseGroup in ($results | Group-Object CaseIndex)) {
    $caseResults = @($caseGroup.Group)
    Write-Host ("Case {0} ({1}) DeterminismSignature: {2}" -f $caseResults[0].CaseIndex, $caseResults[0].CaseName, $caseResults[0].Signature)
}

if (-not $KeepOutput) {
    Remove-Item $output -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path (Join-Path $outputDirectory 'smoke-output-run-*.txt') -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Write-Host ("Removed ET smoke output {0} after successful run" -f $_.Name) -ForegroundColor DarkGray
        }
    Get-ChildItem -Path (Join-Path $outputDirectory 'smoke-output-case-*-run-*.txt') -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Write-Host ("Removed ET smoke output {0} after successful run" -f $_.Name) -ForegroundColor DarkGray
        }
}

exit 0
