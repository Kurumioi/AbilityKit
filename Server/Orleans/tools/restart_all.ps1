# AbilityKit Orleans Development Environment Restart Script
# Compatibility wrapper for start_orleans_dev.ps1.

param(
    [switch]$NoCleanup,
    [switch]$NoBuild,
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$startScript = Join-Path $PSScriptRoot 'start_orleans_dev.ps1'
if (-not (Test-Path $startScript)) {
    throw "Start script was not found: $startScript"
}

$startParams = @{}

if ($NoCleanup) {
    $startParams.NoCleanup = $true
}

if ($NoBuild) {
    $startParams.NoBuild = $true
}

if ($Configuration -ne 'Debug') {
    $startParams.Configuration = $Configuration
}

& $startScript @startParams
