# AbilityKit Orleans Development Environment Restart Script
# Compatibility wrapper for start_orleans_dev.ps1.

param(
    [switch]$NoCleanup,
    [switch]$NoBuild,
    [string]$Configuration = 'Debug',
    [int]$GatewayPort = 5001,
    [int]$SiloPort = 11111,
    [int]$SiloGatewayPort = 30000,
    [int]$TcpPort = 4000
)

$ErrorActionPreference = 'Stop'

$startScript = Join-Path $PSScriptRoot 'start_orleans_dev.ps1'
if (-not (Test-Path $startScript)) {
    throw "Start script was not found: $startScript"
}

$startParams = @{
    GatewayPort = $GatewayPort
    SiloPort = $SiloPort
    SiloGatewayPort = $SiloGatewayPort
    TcpPort = $TcpPort
}

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
