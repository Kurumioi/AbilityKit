# AbilityKit Orleans Development Environment Start Script
# Startup order: Host -> Gateway
# Auto cleanup old processes, avoid port conflicts

param(
    [switch]$NoCleanup
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$src = Join-Path $root "src"

$hostProj = Join-Path $src "AbilityKit.Orleans.Host\AbilityKit.Orleans.Host.csproj"
$gatewayProj = Join-Path $src "AbilityKit.Orleans.Gateway\AbilityKit.Orleans.Gateway.csproj"

# Default port configuration
$GatewayHttpPort = 5001
$SiloPort = 11111
$SiloGatewayPort = 30000
$TcpPort = 4000

# ==================== Cleanup Functions ====================

function Cleanup-OrleansProcesses {
    Write-Host ""
    Write-Host "=== Cleaning Orleans Processes ===" -ForegroundColor Yellow

    # 1. Clean processes on ports
    $portList = @($GatewayHttpPort, $SiloPort, $SiloGatewayPort, $TcpPort)
    foreach ($portNum in $portList) {
        $conns = Get-NetTCPConnection -LocalPort $portNum -ErrorAction SilentlyContinue |
                Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($procId in $conns) {
            if ($procId -and $procId -gt 0) {
                Write-Host "  Kill PID $procId (port $portNum)" -ForegroundColor Gray
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # 2. Clean AbilityKit dotnet processes
    $abilityKitProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
                       Where-Object { $_.Path -like "*AbilityKit*" }
    foreach ($proc in $abilityKitProcs) {
        Write-Host "  Kill AbilityKit dotnet PID $($proc.Id)" -ForegroundColor Gray
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 2
    Write-Host "  Cleanup done" -ForegroundColor Green
    Write-Host ""
}

# ==================== Main Flow ====================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AbilityKit Orleans Dev Start Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Cleanup old processes
if (-not $NoCleanup) {
    Cleanup-OrleansProcesses
}

# Step 2: Check project paths
if (!(Test-Path $hostProj)) { throw "Host csproj not found: $hostProj" }
if (!(Test-Path $gatewayProj)) { throw "Gateway csproj not found: $gatewayProj" }

# Step 3: Start Orleans Host
Write-Host "=== Starting Orleans Host ===" -ForegroundColor Cyan
$hostCmd = "dotnet run --project '$hostProj'"
$hostWindow = Start-Process powershell -ArgumentList @('-NoExit', '-Command', $hostCmd) -PassThru -WindowStyle Normal
Write-Host "  Host PID: $($hostWindow.Id)" -ForegroundColor Gray

# Step 4: Wait for Host
Start-Sleep -Seconds 5
Write-Host "  Host started (waiting for Orleans init...)" -ForegroundColor Green

# Step 5: Start Orleans Gateway
Write-Host ""
Write-Host "=== Starting Orleans Gateway ===" -ForegroundColor Cyan
$gatewayCmd = "dotnet run --project '$gatewayProj'"
$gatewayWindow = Start-Process powershell -ArgumentList @('-NoExit', '-Command', $gatewayCmd) -PassThru -WindowStyle Normal
Write-Host "  Gateway PID: $($gatewayWindow.Id)" -ForegroundColor Gray

# Wait for Gateway HTTP port
Start-Sleep -Seconds 5

# Step 6: Verify startup
Write-Host ""
Write-Host "=== Verifying Services ===" -ForegroundColor Cyan
$gatewayHealth = "http://localhost:$GatewayHttpPort/health"
try {
    $response = Invoke-WebRequest -Uri $gatewayHealth -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "  Gateway Health: OK" -ForegroundColor Green
    } else {
        Write-Host "  Gateway Health: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  Gateway Health: Not accessible (may still starting)" -ForegroundColor Yellow
}

# Completion summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Startup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Gateway HTTP:  http://localhost:$GatewayHttpPort/health" -ForegroundColor White
Write-Host "Gateway TCP:   localhost:$TcpPort" -ForegroundColor White
Write-Host "Silo Port:     $SiloPort" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop all services, or close PowerShell windows" -ForegroundColor Gray
Write-Host ""

# Check if services exited
if ($hostWindow.HasExited -or $gatewayWindow.HasExited) {
    Write-Host "Warning: A service has exited" -Foreground Red
}
