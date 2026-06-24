param(
    [string]$Gate,
    [string]$ConfigPath = 'tools\test-gates.json',
    [string]$Configuration = 'Debug',
    [string]$ResultsDirectory = 'artifacts\test-gates',
    [switch]$List,
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$CI
)

$ErrorActionPreference = 'Stop'

if ($CI) {
    $ProgressPreference = 'SilentlyContinue'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    $resolvedConfigPath = $ConfigPath
}
else {
    $resolvedConfigPath = Join-Path $repoRoot $ConfigPath
}

if (-not (Test-Path $resolvedConfigPath)) {
    throw "Test gate config not found: $resolvedConfigPath"
}

$config = Get-Content -Path $resolvedConfigPath -Raw | ConvertFrom-Json
$gatesByName = @{}
foreach ($gateDef in $config.gates) {
    $gatesByName[[string]$gateDef.name] = $gateDef
}

function ConvertTo-SafeName {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return 'unnamed'
    }

    $safe = [string]$Value
    foreach ($invalidChar in [System.IO.Path]::GetInvalidFileNameChars()) {
        $safe = $safe.Replace($invalidChar, '_')
    }

    $safe = $safe -replace '\s+', '_'
    return $safe
}

function Format-StringList {
    param([object]$Value)

    if ($null -eq $Value) {
        return ''
    }

    $items = @($Value) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    if ($items.Count -eq 0) {
        return ''
    }

    return ($items -join ', ')
}

function Get-OptionalStringProperty {
    param(
        [object]$Value,
        [string]$PropertyName,
        [string]$Fallback = 'Unspecified'
    )

    if ($null -eq $Value -or -not $Value.PSObject.Properties[$PropertyName]) {
        return $Fallback
    }

    return [string]$Value.PSObject.Properties[$PropertyName].Value
}

function Show-GateList {
    Write-Host 'Available test gates:' -ForegroundColor Cyan
    foreach ($gateDef in $config.gates) {
        $level = if ($gateDef.PSObject.Properties['level']) { [string]$gateDef.level } else { 'Unspecified' }
        $owner = if ($gateDef.PSObject.Properties['owner']) { [string]$gateDef.owner } else { 'Unspecified' }
        $scope = Format-StringList $gateDef.scope
        $requiredBefore = Format-StringList $gateDef.requiredBefore
        $failurePolicy = if ($gateDef.PSObject.Properties['failurePolicy']) { [string]$gateDef.failurePolicy } else { 'Unspecified' }

        Write-Host ("- {0} [{1}]" -f $gateDef.name, $level)
        Write-Host ("  Owner: {0}" -f $owner)
        Write-Host ("  Scope: {0}" -f $(if ([string]::IsNullOrWhiteSpace($scope)) { 'Unspecified' } else { $scope }))
        Write-Host ("  Required before: {0}" -f $(if ([string]::IsNullOrWhiteSpace($requiredBefore)) { 'Unspecified' } else { $requiredBefore }))
        Write-Host ("  Failure policy: {0}" -f $failurePolicy)
        Write-Host ("  Description: {0}" -f $gateDef.description)
    }
}

if ($List) {
    Show-GateList
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Gate)) {
    $Gate = [string]$config.defaultGate
}

if (-not $gatesByName.ContainsKey($Gate)) {
    throw "Unknown test gate '$Gate'. Use -List to inspect available gates."
}

$resultsRoot = Resolve-RepoPath $ResultsDirectory
$runId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), (ConvertTo-SafeName $Gate)
$runRoot = Join-Path $resultsRoot $runId
$null = New-Item -ItemType Directory -Force -Path $runRoot

function Write-GateHeader {
    param($GateDef, [string]$GateOutputDirectory)

    $level = Get-OptionalStringProperty -Value $GateDef -PropertyName 'level'
    $owner = Get-OptionalStringProperty -Value $GateDef -PropertyName 'owner'
    Write-Host ("`n>>> Gate: {0} [{1}]" -f $GateDef.name, $level) -ForegroundColor Green
    Write-Host ("Owner: {0}" -f $owner) -ForegroundColor DarkGray
    Write-Host $GateDef.description -ForegroundColor DarkGray
    if ($GateDef.PSObject.Properties['scope']) {
        Write-Host ("Scope: {0}" -f (Format-StringList $GateDef.scope)) -ForegroundColor DarkGray
    }
    if ($GateDef.PSObject.Properties['requiredBefore']) {
        Write-Host ("Required before: {0}" -f (Format-StringList $GateDef.requiredBefore)) -ForegroundColor DarkGray
    }
    if ($GateDef.PSObject.Properties['failurePolicy']) {
        Write-Host ("Failure policy: {0}" -f [string]$GateDef.failurePolicy) -ForegroundColor DarkGray
    }
    Write-Host ("Output directory: {0}" -f $GateOutputDirectory) -ForegroundColor DarkGray
}

function Invoke-DotNetStep {
    param(
        [string]$DisplayName,
        [string]$Kind,
        [string[]]$Arguments,
        [string]$LogFilePath,
        [string]$ProjectPath,
        [string]$Filter,
        [string]$ResultsDirectory,
        [string]$TrxFilePath
    )

    $startedAt = Get-Date
    Write-Host ("=== {0} ===" -f $DisplayName) -ForegroundColor Cyan
    Write-Host ("dotnet {0}" -f ($Arguments -join ' ')) -ForegroundColor DarkGray

    if (-not [string]::IsNullOrWhiteSpace($LogFilePath)) {
        $null = New-Item -ItemType Directory -Force -Path (Split-Path $LogFilePath)
        & dotnet @Arguments 2>&1 | Tee-Object -FilePath $LogFilePath | ForEach-Object { Write-Host $_ }
    }
    else {
        & dotnet @Arguments
    }

    $exitCode = $LASTEXITCODE
    $endedAt = Get-Date

    $record = [ordered]@{
        name           = $DisplayName
        kind           = $Kind
        project        = $ProjectPath
        filter         = $Filter
        command        = ('dotnet {0}' -f ($Arguments -join ' '))
        status         = $(if ($exitCode -eq 0) { 'Passed' } else { 'Failed' })
        exitCode       = $exitCode
        startedAt      = $startedAt.ToString('o')
        endedAt        = $endedAt.ToString('o')
        elapsedSeconds = [math]::Round(($endedAt - $startedAt).TotalSeconds, 3)
        logFile        = $LogFilePath
        resultsDirectory = $ResultsDirectory
        trxFile        = $TrxFilePath
    }

    return [pscustomobject]$record
}

function Invoke-Gate {
    param(
        [string]$GateName,
        [System.Collections.Generic.HashSet[string]]$Visiting,
        [string[]]$GatePath,
        [string]$RunRoot
    )

    if (-not $gatesByName.ContainsKey($GateName)) {
        throw "Nested gate '$GateName' is not defined."
    }

    if ($Visiting.Contains($GateName)) {
        throw "Circular gate dependency detected at '$GateName'."
    }

    $null = $Visiting.Add($GateName)
    $gateDef = $gatesByName[$GateName]
    $pathSegments = @($GatePath)
    if ($pathSegments.Count -eq 0) {
        $pathSegments = @($GateName)
    }

    $safePathSegments = foreach ($segment in $pathSegments) {
        ConvertTo-SafeName ([string]$segment)
    }
    $gateOutputDirectory = Join-Path $RunRoot (($safePathSegments -join [System.IO.Path]::DirectorySeparatorChar))
    $null = New-Item -ItemType Directory -Force -Path $gateOutputDirectory

    $startedAt = Get-Date
    $stepResults = New-Object System.Collections.Generic.List[object]
    $status = 'Passed'
    $failureMessage = $null

    try {
        Write-GateHeader -GateDef $gateDef -GateOutputDirectory $gateOutputDirectory

        $stepIndex = 0
        foreach ($step in $gateDef.steps) {
            $stepIndex++
            $stepName = [string]$step.name
            switch ([string]$step.kind) {
                'gate' {
                    $nestedGateName = [string]$step.gate
                    $nestedGatePath = @($pathSegments + $nestedGateName)
                    $nestedResult = Invoke-Gate -GateName $nestedGateName -Visiting $Visiting -GatePath $nestedGatePath -RunRoot $RunRoot
                    $stepResults.Add([pscustomobject]@{
                        name             = $stepName
                        kind             = 'gate'
                        gate             = $nestedGateName
                        status           = $nestedResult.status
                        outputDirectory  = $nestedResult.outputDirectory
                        summaryPath      = $nestedResult.summaryPath
                        startedAt        = $nestedResult.startedAt
                        endedAt          = $nestedResult.endedAt
                        elapsedSeconds   = $nestedResult.elapsedSeconds
                    })
                }
                'dotnet-build' {
                    $project = Resolve-RepoPath ([string]$step.project)
                    $arguments = @('build', $project, '-c', $Configuration)
                    if ($NoRestore) { $arguments += '--no-restore' }
                    if ($CI) { $arguments += '--nologo' }
                    $logFile = Join-Path $gateOutputDirectory (('{0:00}-{1}.log' -f $stepIndex, (ConvertTo-SafeName $stepName)))
                    $record = Invoke-DotNetStep -DisplayName $stepName -Kind 'dotnet-build' -Arguments $arguments -LogFilePath $logFile -ProjectPath $project -Filter $null -ResultsDirectory $null -TrxFilePath $null
                    $stepResults.Add($record)
                    if ($record.status -ne 'Passed') {
                        throw "Step '$stepName' failed with exit code $($record.exitCode)."
                    }
                }
                'dotnet-test' {
                    $project = Resolve-RepoPath ([string]$step.project)
                    $testResultsDirectory = Join-Path $gateOutputDirectory 'test-results'
                    $null = New-Item -ItemType Directory -Force -Path $testResultsDirectory
                    $trxFileName = ('{0:00}-{1}.trx' -f $stepIndex, (ConvertTo-SafeName $stepName))
                    $trxFilePath = Join-Path $testResultsDirectory $trxFileName
                    $logFile = Join-Path $gateOutputDirectory (('{0:00}-{1}.log' -f $stepIndex, (ConvertTo-SafeName $stepName)))
                    $arguments = @('test', $project, '-c', $Configuration, '-v', 'minimal', '--logger', ("trx;LogFileName=$trxFileName"), '--results-directory', $testResultsDirectory)
                    if ($NoRestore) { $arguments += '--no-restore' }
                    if ($NoBuild) { $arguments += '--no-build' }
                    if ($CI) { $arguments += '--nologo' }
                    if ($step.PSObject.Properties['filter'] -and -not [string]::IsNullOrWhiteSpace([string]$step.filter)) {
                        $arguments += @('--filter', [string]$step.filter)
                    }
                    $record = Invoke-DotNetStep -DisplayName $stepName -Kind 'dotnet-test' -Arguments $arguments -LogFilePath $logFile -ProjectPath $project -Filter ([string]$step.filter) -ResultsDirectory $testResultsDirectory -TrxFilePath $trxFilePath
                    $stepResults.Add($record)
                    if ($record.status -ne 'Passed') {
                        throw "Step '$stepName' failed with exit code $($record.exitCode)."
                    }
                }
                default {
                    throw "Unsupported gate step kind '$($step.kind)' in '$GateName'."
                }
            }
        }
    }
    catch {
        $status = 'Failed'
        $failureMessage = $_.Exception.Message
        throw
    }
    finally {
        $endedAt = Get-Date
        $gateLevel = Get-OptionalStringProperty -Value $gateDef -PropertyName 'level'
        $gateOwner = Get-OptionalStringProperty -Value $gateDef -PropertyName 'owner'
        $summaryPath = Join-Path $gateOutputDirectory 'gate-summary.json'
        $summary = New-Object psobject
        $summary | Add-Member -MemberType NoteProperty -Name gate -Value $GateName
        $summary | Add-Member -MemberType NoteProperty -Name gatePath -Value ([object[]]@($pathSegments))
        $summary | Add-Member -MemberType NoteProperty -Name level -Value $gateLevel
        $summary | Add-Member -MemberType NoteProperty -Name owner -Value $gateOwner
        $summary | Add-Member -MemberType NoteProperty -Name status -Value $status
        $summary | Add-Member -MemberType NoteProperty -Name startedAt -Value ($startedAt.ToString('o'))
        $summary | Add-Member -MemberType NoteProperty -Name endedAt -Value ($endedAt.ToString('o'))
        $summary | Add-Member -MemberType NoteProperty -Name elapsedSeconds -Value ([math]::Round(($endedAt - $startedAt).TotalSeconds, 3))
        $summary | Add-Member -MemberType NoteProperty -Name outputDirectory -Value $gateOutputDirectory
        $summary | Add-Member -MemberType NoteProperty -Name summaryPath -Value $summaryPath
        $summary | Add-Member -MemberType NoteProperty -Name failureMessage -Value $failureMessage
        $summary | Add-Member -MemberType NoteProperty -Name steps -Value ([object[]]$stepResults.ToArray())

        $summary | ConvertTo-Json -Depth 12 | Set-Content -Path $summaryPath -Encoding UTF8
        $Visiting.Remove($GateName) | Out-Null
    }

    return $summary
}

$startedAt = Get-Date
try {
    $gateResult = Invoke-Gate -GateName $Gate -Visiting ([System.Collections.Generic.HashSet[string]]::new()) -GatePath @($Gate) -RunRoot $runRoot
    $elapsed = (Get-Date) - $startedAt
    Write-Host ("`nGate '{0}' passed in {1:n1}s." -f $Gate, $elapsed.TotalSeconds) -ForegroundColor Green
    Write-Host ("Summary: {0}" -f $gateResult.summaryPath) -ForegroundColor DarkGray
    exit 0
}
catch {
    Write-Host ("`nGate '{0}' failed. See summary under {1}." -f $Gate, $runRoot) -ForegroundColor Red
    throw
}
