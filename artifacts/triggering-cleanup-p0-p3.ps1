$ErrorActionPreference = 'Stop'

$workspace = Get-Location
$runtimeRoot = Join-Path $workspace 'Unity/Packages/com.abilitykit.triggering/Runtime'
$projectFiles = @(
    (Join-Path $workspace 'Unity/AbilityKit.Triggering.csproj'),
    (Join-Path $workspace 'Unity/AbilityKit.Triggering.Tests.csproj')
)

$script:RemovedCompileIncludes = @()
$script:RemovedAssets = 0
$script:RemovedDirectories = 0

function Remove-TriggeringAsset {
    param(
        [Parameter(Mandatory=$true)][string]$RelativePath,
        [bool]$CompileItem = $true
    )

    $path = Join-Path $runtimeRoot $RelativePath
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
        $script:RemovedAssets++
    }

    $meta = "$path.meta"
    if (Test-Path -LiteralPath $meta) {
        Remove-Item -LiteralPath $meta -Force
        $script:RemovedAssets++
    }

    if ($CompileItem -and $RelativePath.EndsWith('.cs')) {
        $script:RemovedCompileIncludes += "Packages\com.abilitykit.triggering\Runtime\" + ($RelativePath -replace '/', '\')
    }
}

function Remove-TriggeringDirectoryTree {
    param([Parameter(Mandatory=$true)][string]$RelativePath)

    $path = Join-Path $runtimeRoot $RelativePath
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
        $script:RemovedDirectories++
    }

    $meta = "$path.meta"
    if (Test-Path -LiteralPath $meta) {
        Remove-Item -LiteralPath $meta -Force
        $script:RemovedAssets++
    }
}

function Remove-CompileIncludes {
    foreach ($projectFile in $projectFiles) {
        if (-not (Test-Path -LiteralPath $projectFile)) { continue }

        $lines = [System.IO.File]::ReadAllLines($projectFile)
        $filtered = New-Object System.Collections.Generic.List[string]
        foreach ($line in $lines) {
            $remove = $false
            foreach ($include in $script:RemovedCompileIncludes) {
                if ($line.Contains('<Compile Include="' + $include + '" />')) {
                    $remove = $true
                    break
                }
            }

            if (-not $remove) {
                $filtered.Add($line)
            }
        }

        [System.IO.File]::WriteAllLines($projectFile, $filtered, [System.Text.UTF8Encoding]::new($false))
    }
}

# P0：删除迁移后遗留的旧空壳目录、占位目录与对应目录 .meta。
$obsoleteShellDirectories = @(
    'Runtime',
    'Plan',
    'Schedule',
    'ActionScheduler',
    'RuleScheduler',
    'Eventing',
    'Registry',
    'Executable',
    'Behavior',
    'Data',
    'Example',
    'TriggerScheduler',
    'Continuous',
    'Diagnostics',
    'Dispatcher',
    'Extensions',
    'Factory',
    'Instance',
    'Random',
    'Sync',
    'Time'
)

foreach ($dir in $obsoleteShellDirectories) {
    Remove-TriggeringDirectoryTree $dir
}

# P1：移除可编译 Experimental TODO。保留 Runtime/Experimental 目录边界，删除 Todo 子树。
Remove-TriggeringAsset 'Experimental/Todo/TriggerScheduler/TriggerExecutorTodo.cs'
Remove-TriggeringDirectoryTree 'Experimental/Todo'

# P2：删减明确低价值的 Legacy 显式失败/旧入口。
Remove-TriggeringAsset 'Legacy/Executable/ExecutableDsl.cs'
Remove-TriggeringAsset 'Legacy/TriggerScheduler/TriggerExecutor.cs'
Remove-TriggeringAsset 'Legacy/Schedule/DefaultScheduleManager.cs'
Remove-TriggeringAsset 'Scheduling/Effects/EffectExecutorAdapter.cs'
Remove-TriggeringDirectoryTree 'Legacy/TriggerScheduler'
Remove-TriggeringDirectoryTree 'Legacy/Schedule'

Remove-CompileIncludes

Write-Host "Removed $($script:RemovedAssets) files/meta assets, $($script:RemovedDirectories) directories, and $($script:RemovedCompileIncludes.Count) compile includes."
