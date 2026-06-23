# AbilityKit Orleans Development Environment Restart Script
# Compatibility wrapper for start_abilitykit.ps1.

param(
    [string[]]$Profile,
    [switch]$NoCleanup,
    [switch]$NoBuild,
    [switch]$CleanAll,
    [string]$Configuration = 'Debug',
    [int]$GatewayPort = 5001,
    [int]$SiloPort = 11111,
    [int]$SiloGatewayPort = 30000,
    [int]$TcpPort = 4000
)

$ErrorActionPreference = 'Stop'

$launcherScript = Join-Path $PSScriptRoot 'start_abilitykit.ps1'
if (-not (Test-Path $launcherScript)) {
    throw "Launcher script was not found: $launcherScript"
}

$startParams = @{
    Configuration = $Configuration
    GatewayPort = $GatewayPort
    SiloPort = $SiloPort
    SiloGatewayPort = $SiloGatewayPort
    TcpPort = $TcpPort
}

if ($Profile -and $Profile.Count -gt 0) {
    $startParams.Profile = $Profile
}

if ($NoCleanup) {
    $startParams.NoCleanup = $true
}

if ($NoBuild) {
    $startParams.NoBuild = $true
}

if ($CleanAll) {
    $startParams.CleanAll = $true
}

& $launcherScript @startParams
