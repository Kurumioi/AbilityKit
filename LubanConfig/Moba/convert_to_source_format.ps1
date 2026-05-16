param(
    [Parameter(Mandatory = $false)]
    [string]$InputFile = "..\\..\\Unity\\Assets\\Resources\\ability\\ability_triggers.json",

    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "..\\..\\Unity\\Assets\\Resources\\ability\\trigger_sources\\trigger_sources.json",

    [Parameter(Mandatory = $false)]
    [string]$ManifestFile = "..\\..\\Unity\\Assets\\Resources\\ability\\trigger_source_manifest.json"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$absInputFile = [System.IO.Path]::GetFullPath((Join-Path $root $InputFile))
$absOutputFile = [System.IO.Path]::GetFullPath((Join-Path $root $OutputFile))
$absManifestFile = [System.IO.Path]::GetFullPath((Join-Path $root $ManifestFile))
$absOutputDir = [System.IO.Path]::GetDirectoryName($absOutputFile)

if (!(Test-Path $absInputFile)) {
    Write-Host "[ConvertToSourceFormat] Input file not found: $absInputFile"
    exit 1
}

Write-Host "[ConvertToSourceFormat] Input: $absInputFile"
Write-Host "[ConvertToSourceFormat] Output: $absOutputFile"

# Create output directory
New-Item -ItemType Directory -Force -Path $absOutputDir | Out-Null

# Read source JSON
$sourceJson = Get-Content -Path $absInputFile -Raw -Encoding UTF8
$sourceData = ConvertFrom-Json $sourceJson

if ($sourceData -eq $null -or $sourceData.Triggers -eq $null) {
    Write-Host "[ConvertToSourceFormat] No triggers found in source file"
    exit 1
}

$triggers = $sourceData.Triggers
Write-Host "[ConvertToSourceFormat] Found $($triggers.Count) triggers"

# Create manifest
$manifest = @{
    entries = @()
}

# Category mapping
$categoryMap = @{
    10000 = "skills"
    20000 = "buffs"
    21000 = "buffs"
    22000 = "passives"
}

function Get-CategoryFromId {
    param([int]$triggerId)

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

function Get-EventName {
    param([string]$eventId)

    if ([string]::IsNullOrEmpty($eventId)) {
        return ""
    }

    $eventMap = @{
        "attack.hit" = "Attack Hit"
        "skill.cast.complete" = "Skill Cast Complete"
        "skill.cast.start" = "Skill Cast Start"
        "buff.applied" = "Buff Applied"
        "buff.removed" = "Buff Removed"
        "damage.dealt" = "Damage Dealt"
        "damage.taken" = "Damage Taken"
        "collision.enter" = "Collision Enter"
        "collision.exit" = "Collision Exit"
    }

    if ($eventMap.ContainsKey($eventId)) {
        return $eventMap[$eventId]
    }

    return $eventId
}

function Get-Comment {
    param($trigger)

    $typeStr = ""
    if ($trigger.Actions -ne $null -and $trigger.Actions.Count -gt 0) {
        $typeStr = $trigger.Actions[0].Type
    }

    $commentMap = @{
        "debug_log" = "Debug log output"
        "shoot_projectile" = "Shoot projectile"
        "emit" = "Emit effect"
        "add_buff" = "Add buff"
        "give_damage" = "Deal damage"
        "aoe_burst" = "AOE burst"
        "knock" = "Knock back"
    }

    if ($commentMap.ContainsKey($typeStr)) {
        return $commentMap[$typeStr]
    }

    return ""
}

function ConvertAction {
    param($action)

    $result = @{
        type = $action.Type.ToLower()
    }

    if ($action.Args -ne $null) {
        foreach ($prop in $action.Args.PSObject.Properties) {
            $value = $prop.Value
            $name = $prop.Name

            switch ($name) {
                "message" { $result["message"] = $value }
                "dump_args" { $result["dump_args"] = $value }
                "launcherId" { $result["launcher"] = $value }
                "projectileId" { $result["projectile_id"] = $value }
                "emitterId" { $result["emitter_id"] = $value }
                "buffIds" {
                    if ($value -is [array] -and $value.Count -gt 0) {
                        $result["buff_id"] = $value[0]
                    } else {
                        $result["buff_id"] = $value
                    }
                }
                "value" { $result["amount"] = "=$value" }
                "damageType" { $result["damage_type"] = $value }
                "perTargetTriggerId" { $result["per_target_trigger_id"] = $value }
                "horizontalSpeed" { $result["horizontal_speed"] = $value }
                "verticalSpeed" { $result["vertical_speed"] = $value }
                "durationMs" { $result["duration_ms"] = $value }
                "gravity" { $result["gravity"] = $value }
                "priority" { $result["knock_priority"] = $value }
                "directionMode" { $result["direction_mode"] = $value }
                "targetMode" { $result["target_mode"] = $value }
                "crit" { $result["crit"] = $value }
                "reasonKind" { $result["reason_kind"] = $value }
                "reasonParam" { $result["reason_param"] = $value }
                "queryTemplateId" { $result["query_template_id"] = $value }
                "log" { $result["log"] = $value }
                default { $result[$name] = $value }
            }
        }
    }

    if ($action.Items -ne $null -and $action.Items.Count -gt 0) {
        $result.items = @()
        foreach ($item in $action.Items) {
            $result.items += (ConvertAction $item)
        }
    }

    return $result
}

function ConvertTrigger {
    param($trigger)

    $eventName = ""
    if ($trigger.EventId -is [string] -and $trigger.EventId.Length -gt 0) {
        $eventName = $trigger.EventId
    }

    $name = Get-EventName $eventName
    $comment = Get-Comment $trigger

    $result = @{
        id = $trigger.TriggerId
        name = if ([string]::IsNullOrEmpty($name)) { "Trigger_$($trigger.TriggerId)" } else { $name }
        event = $eventName
        priority = 50
        phase = "immediate"
        enabled = $true
        allowExternal = $trigger.AllowExternal
        comment = $comment
        conditions = @()
        actions = @()
    }

    if ($trigger.Conditions -ne $null -and $trigger.Conditions.Count -gt 0) {
        foreach ($cond in $trigger.Conditions) {
            $result.conditions += $cond
        }
    }

    if ($trigger.Actions -ne $null) {
        foreach ($action in $trigger.Actions) {
            $result.actions += (ConvertAction $action)
        }
    }

    return $result
}

# Process each trigger
$processedCount = 0
$convertedTriggers = @()

foreach ($trigger in $triggers) {
    $triggerId = $trigger.TriggerId
    $category = Get-CategoryFromId $triggerId

    $outputTrigger = ConvertTrigger $trigger
    $convertedTriggers += $outputTrigger

    $manifest.entries += @{
        trigger_id = $triggerId
        category = $category
    }

    $processedCount++
    Write-Host "[ConvertToSourceFormat] Processed trigger $triggerId ($category)"
}

# Build source format
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

$sourceFormat = @{
    '$schema' = "abilitykit-trigger-source-v1"
    version = "1.0"
    metadata = @{
        author = "moba-config-generator"
        created_at = $timestamp
        last_modified = $timestamp
        description = "Auto-generated from ability_triggers.json"
    }
    variables = @(
        @{ name = '$caster'; description = 'Skill caster' }
        @{ name = '$target'; description = 'Target entity' }
        @{ name = '$self'; description = 'Self entity' }
    )
    actions = @{
        "debug_log" = @{
            type = "debug_log"
            displayName = "Debug Log"
            description = "Output debug message"
            category = "Debug"
            params = @(
                @{ name = "message"; type = "string"; required = $true }
                @{ name = "dump_args"; type = "bool"; required = $false; defaultValue = $false }
            )
        }
        "shoot_projectile" = @{
            type = "shoot_projectile"
            displayName = "Shoot Projectile"
            description = "Shoot projectile to target"
            category = "Combat"
            params = @(
                @{ name = "launcher"; type = "entity"; required = $true }
                @{ name = "target"; type = "entity"; required = $true }
                @{ name = "projectile_id"; type = "int"; required = $true }
                @{ name = "speed"; type = "float"; required = $false; defaultValue = 300.0 }
            )
        }
        "give_damage" = @{
            type = "give_damage"
            displayName = "Deal Damage"
            description = "Deal damage to target"
            category = "Combat"
            params = @(
                @{ name = "from"; type = "entity"; required = $true }
                @{ name = "to"; type = "entity"; required = $true }
                @{ name = "amount"; type = "expr"; required = $true }
                @{ name = "damage_type"; type = "int"; required = $false; defaultValue = 0 }
            )
        }
        "add_buff" = @{
            type = "add_buff"
            displayName = "Add Buff"
            description = "Add buff to target"
            category = "Buff"
            params = @(
                @{ name = "target"; type = "entity"; required = $true }
                @{ name = "buff_id"; type = "int"; required = $true }
                @{ name = "duration"; type = "float"; required = $false; defaultValue = -1.0 }
            )
        }
        "aoe_burst" = @{
            type = "aoe_burst"
            displayName = "AOE Burst"
            description = "AOE burst effect"
            category = "Combat"
            params = @(
                @{ name = "per_target_trigger_id"; type = "int"; required = $true }
            )
        }
        "knock" = @{
            type = "knock"
            displayName = "Knock Back"
            description = "Knock back target"
            category = "Combat"
            params = @(
                @{ name = "horizontal_speed"; type = "float"; required = $true }
                @{ name = "vertical_speed"; type = "float"; required = $true }
                @{ name = "duration_ms"; type = "int"; required = $true }
                @{ name = "gravity"; type = "float"; required = $false; defaultValue = 9.8 }
                @{ name = "knock_priority"; type = "int"; required = $false; defaultValue = 0 }
                @{ name = "direction_mode"; type = "string"; required = $false; defaultValue = "FromAreaCenterToTarget" }
            )
        }
        "emit" = @{
            type = "emit"
            displayName = "Emit Effect"
            description = "Emit visual or audio effect"
            category = "VFX"
            params = @(
                @{ name = "emitter_id"; type = "int"; required = $true }
            )
        }
    }
    conditions = @{
        "arg_eq" = @{
            type = "arg_eq"
            displayName = "Arg Equal"
            description = "Check if argument equals value"
            category = "Parameter"
            params = @(
                @{ name = "arg_name"; type = "string"; required = $true }
                @{ name = "value"; type = "number"; required = $true }
            )
        }
        "arg_gt" = @{
            type = "arg_gt"
            displayName = "Arg Greater Than"
            description = "Check if argument greater than value"
            category = "Parameter"
            params = @(
                @{ name = "arg_name"; type = "string"; required = $true }
                @{ name = "value"; type = "number"; required = $true }
            )
        }
    }
    triggers = $convertedTriggers
}

# Write source format file
$outputJson = ConvertTo-Json $sourceFormat -Depth 20
Set-Content -Path $absOutputFile -Value $outputJson -Encoding UTF8

# Write manifest
$manifestJson = ConvertTo-Json $manifest -Depth 10
Set-Content -Path $absManifestFile -Value $manifestJson -Encoding UTF8

Write-Host ""
Write-Host "[ConvertToSourceFormat] Done! Processed $processedCount triggers"
Write-Host "[ConvertToSourceFormat] Source file: $absOutputFile"
Write-Host "[ConvertToSourceFormat] Manifest: $absManifestFile"
