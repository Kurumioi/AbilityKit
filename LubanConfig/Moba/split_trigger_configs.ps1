param(
    [Parameter(Mandatory = $false)]
    [string]$InputFile = "..\\..\\Unity\\Assets\\Resources\\ability\\ability_triggers.json",

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "..\\..\\Unity\\Assets\\Resources\\ability\\triggers",

    [Parameter(Mandatory = $false)]
    [string]$ManifestFile = "..\\..\\Unity\\Assets\\Resources\\ability\\trigger_manifest.json",

    [Parameter(Mandatory = $false)]
    [switch]$UseSourceFormat
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$absInputFile = [System.IO.Path]::GetFullPath((Join-Path $root $InputFile))
$absOutputDir = [System.IO.Path]::GetFullPath((Join-Path $root $OutputDir))
$absManifestFile = [System.IO.Path]::GetFullPath((Join-Path $root $ManifestFile))

if (!(Test-Path $absInputFile)) {
    Write-Host "[SplitTriggerConfigs] Input file not found: $absInputFile"
    exit 1
}

Write-Host "[SplitTriggerConfigs] Input: $absInputFile"
Write-Host "[SplitTriggerConfigs] Output: $absOutputDir"
Write-Host "[SplitTriggerConfigs] Manifest: $absManifestFile"
Write-Host "[SplitTriggerConfigs] UseSourceFormat: $UseSourceFormat"

# Create output directory
New-Item -ItemType Directory -Force -Path $absOutputDir | Out-Null

# Read source JSON
$sourceJson = Get-Content -Path $absInputFile -Raw -Encoding UTF8
$sourceData = ConvertFrom-Json $sourceJson

if ($sourceData -eq $null -or $sourceData.Triggers -eq $null) {
    Write-Host "[SplitTriggerConfigs] No triggers found in source file"
    exit 1
}

$triggers = $sourceData.Triggers
Write-Host "[SplitTriggerConfigs] Found $($triggers.Count) triggers"

# Create manifest
$manifest = @{
    entries = @()
}

# Category mapping based on trigger ID ranges
# 10000-19999: skills
# 20000-20999: buffs
# 21000-21999: buffs (AOE)
# 22000-22999: passives
$categoryMap = @{
    10000 = "skills"
    20000 = "buffs"
    21000 = "buffs"
    22000 = "passives"
}

function Get-CategoryFromId {
    param([int]$triggerId)

    # Find the best matching category
    $bestMatch = "common"
    $bestKey = -1

    foreach ($key in $categoryMap.Keys) {
        if ($triggerId -ge $key -and $key -gt $bestKey) {
            $bestKey = $key
            $bestMatch = $categoryMap[$key]
        }
    }

    return $bestMatch
}

function Get-StableStringHash {
    param([string]$str)
    $hash = 5381
    foreach ($c in $str.ToCharArray()) {
        $hash = (($hash -shl 5) + $hash) + [int]$c
    }
    return [math]::Abs($hash)
}

function ConvertActionToSource {
    param($action)

    $result = @{
        type = $action.Type.ToLower()
    }

    if ($action.Args -ne $null) {
        foreach ($prop in $action.Args.PSObject.Properties) {
            $result[$prop.Name] = $prop.Value
        }
    }

    if ($action.Items -ne $null -and $action.Items.Count -gt 0) {
        $result.items = @()
        foreach ($item in $action.Items) {
            $result.items += (ConvertActionToSource $item)
        }
    }

    return $result
}

function ConvertTriggerToSource {
    param($trigger)

    $eventName = ""
    if ($trigger.EventId -is [string] -and $trigger.EventId.Length -gt 0) {
        $eventName = $trigger.EventId
    }

    $result = @{
        id = $trigger.TriggerId
        name = "Trigger_$($trigger.TriggerId)"
        event = $eventName
        priority = 50
        phase = "immediate"
        enabled = $true
        allowExternal = $trigger.AllowExternal
        conditions = @()
        actions = @()
    }

    if ($trigger.Conditions -ne $null) {
        foreach ($cond in $trigger.Conditions) {
            $result.conditions += $cond
        }
    }

    if ($trigger.Actions -ne $null) {
        foreach ($action in $trigger.Actions) {
            $result.actions += (ConvertActionToSource $action)
        }
    }

    return $result
}

function ConvertActionToRuntime {
    param($action)

    $result = @{
        ActionId = 0
        Arity = 0
        Args = @{}
    }

    $typeStr = $action.Type.ToLower()
    $result.ActionId = Get-StableStringHash "action:$typeStr"

    if ($action.Args -ne $null) {
        foreach ($prop in $action.Args.PSObject.Properties) {
            $value = $prop.Value
            if ($value -is [System.Collections.ArrayList] -or $value -is [array]) {
                $result.Args[$prop.Name] = @{
                    Kind = "Const"
                    ConstValue = 0
                }
            }
            elseif ($value -is [double] -or $value -is [int]) {
                $result.Args[$prop.Name] = @{
                    Kind = "Const"
                    ConstValue = $value
                }
            }
            elseif ($value -is [bool]) {
                $result.Args[$prop.Name] = @{
                    Kind = "Const"
                    ConstValue = if ($value) { 1.0 } else { 0.0 }
                }
            }
            else {
                $result.Args[$prop.Name] = @{
                    Kind = "Const"
                    ConstValue = 0
                }
            }
        }
    }

    return $result
}

function ConvertTriggerToRuntime {
    param($trigger)

    $result = @{
        TriggerId = $trigger.TriggerId
        EventId = 0
        EventName = ""
        AllowExternal = $trigger.AllowExternal
        Phase = 0
        Priority = 50
        Predicate = @{
            Kind = "none"
            Nodes = $null
        }
        Actions = @()
    }

    if ($trigger.EventId -is [string] -and $trigger.EventId.Length -gt 0) {
        $result.EventName = $trigger.EventId
    }

    if ($trigger.Actions -ne $null) {
        foreach ($action in $trigger.Actions) {
            $result.Actions += (ConvertActionToRuntime $action)
        }
    }

    return $result
}

# Process each trigger
$processedCount = 0
foreach ($trigger in $triggers) {
    $triggerId = $trigger.TriggerId
    $category = Get-CategoryFromId $triggerId

    $categoryDir = Join-Path $absOutputDir $category
    New-Item -ItemType Directory -Force -Path $categoryDir | Out-Null

    $filename = "trigger_$($triggerId).json"
    $filepath = Join-Path $categoryDir $filename

    if ($UseSourceFormat) {
        $outputTrigger = ConvertTriggerToSource $trigger
    }
    else {
        $outputTrigger = ConvertTriggerToRuntime $trigger
    }

    $outputJson = ConvertTo-Json $outputTrigger -Depth 10
    Set-Content -Path $filepath -Value $outputJson -Encoding UTF8

    $relativePath = "$category\$filename"
    $manifest.entries += @{
        trigger_id = $triggerId
        path = $relativePath
        category = $category
    }

    $processedCount++
    Write-Host "[SplitTriggerConfigs] Processed trigger $triggerId -> $relativePath"
}

$manifestJson = ConvertTo-Json $manifest -Depth 10
Set-Content -Path $absManifestFile -Value $manifestJson -Encoding UTF8

Write-Host ""
Write-Host "[SplitTriggerConfigs] Done! Processed $processedCount triggers"
Write-Host "[SplitTriggerConfigs] Manifest written to: $absManifestFile"
