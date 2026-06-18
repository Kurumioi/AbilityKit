function Stop-AbilityKitPortOwners {
    param(
        [Parameter(Mandatory = $true)]
        [int[]]$Ports
    )

    $owners = foreach ($port in $Ports) {
        Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique |
            Where-Object { $_ -and $_ -gt 0 } |
            ForEach-Object {
                [pscustomobject]@{
                    Port = $port
                    ProcessId = $_
                }
            }
    }

    foreach ($ownerGroup in ($owners | Group-Object ProcessId)) {
        $processId = [int]$ownerGroup.Name
        $portsText = ($ownerGroup.Group.Port | Sort-Object -Unique) -join ', '
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if (-not $process) {
            continue
        }

        Write-Host "Stopping PID $processId ($($process.ProcessName)) listening on port(s): $portsText" -ForegroundColor Yellow
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-AbilityKitDotnetCommands {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$CommandPatterns
    )

    $escapedPatterns = $CommandPatterns | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    if (-not $escapedPatterns -or $escapedPatterns.Count -eq 0) {
        return
    }

    $processes = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $commandLine = $_.CommandLine
            if ([string]::IsNullOrWhiteSpace($commandLine)) {
                return $false
            }

            foreach ($pattern in $escapedPatterns) {
                if ($commandLine -like "*$pattern*") {
                    return $true
                }
            }

            return $false
        }

    foreach ($process in $processes) {
        Write-Host "Stopping dotnet PID $($process.ProcessId): $($process.CommandLine)" -ForegroundColor Yellow
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-AbilityKitServices {
    param(
        [int[]]$Ports = @(),
        [string[]]$CommandPatterns = @(),
        [int]$GraceSeconds = 1
    )

    Write-Host ""
    Write-Host "=== Stopping Existing AbilityKit Services ===" -ForegroundColor Yellow

    if ($Ports -and $Ports.Count -gt 0) {
        Stop-AbilityKitPortOwners -Ports $Ports
    }

    if ($CommandPatterns -and $CommandPatterns.Count -gt 0) {
        Stop-AbilityKitDotnetCommands -CommandPatterns $CommandPatterns
    }

    if ($GraceSeconds -gt 0) {
        Start-Sleep -Seconds $GraceSeconds
    }

    Write-Host "Cleanup complete." -ForegroundColor Green
    Write-Host ""
}

function Wait-AbilityKitHttpEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [int]$TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return $true
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }

    return $false
}

function Test-AbilityKitTcpPort {
    param(
        [string]$HostName = '127.0.0.1',
        [Parameter(Mandatory = $true)]
        [int]$Port,
        [int]$TimeoutMilliseconds = 1000
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connectTask = $client.ConnectAsync($HostName, $Port)
        if (-not $connectTask.Wait($TimeoutMilliseconds)) {
            return $false
        }

        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}
