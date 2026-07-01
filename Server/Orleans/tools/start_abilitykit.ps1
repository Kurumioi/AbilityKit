param(
    [string[]]$Profile,
    [string]$ProfileConfig = (Join-Path $PSScriptRoot 'abilitykit_launch_profiles.json'),
    [switch]$ListProfiles,
    [switch]$NoBuild,
    [switch]$NoCleanup,
    [switch]$CleanAll,
    [string]$Configuration,
    [int]$GatewayPort,
    [int]$SiloPort,
    [int]$SiloGatewayPort,
    [int]$TcpPort,
    [int]$PrimarySiloPort
)

$ErrorActionPreference = 'Stop'

function Get-ProfileValue($profile, [string]$name, $fallback) {
    if ($profile.PSObject.Properties.Name -contains $name -and $null -ne $profile.$name) {
        return $profile.$name
    }

    return $fallback
}

function Get-ProfileSilos($profileData, [int]$effectiveSiloPort, [int]$effectiveSiloGatewayPort, [int]$effectivePrimarySiloPort) {
    if ($profileData.PSObject.Properties.Name -contains 'silos' -and $null -ne $profileData.silos) {
        return @($profileData.silos)
    }

    return @(
        [pscustomobject]@{
            role = Get-ProfileValue $profileData 'role' 'Shared'
            siloPort = $effectiveSiloPort
            siloGatewayPort = $effectiveSiloGatewayPort
            primarySiloPort = $effectivePrimarySiloPort
            logicalGroups = @('session', 'room', 'battle')
            maxRooms = 500
            maxBattles = 200
            maxSessions = 5000
        }
    )
}

if (-not (Test-Path $ProfileConfig)) {
    throw "Launch profile config was not found: $ProfileConfig"
}

$config = Get-Content $ProfileConfig -Raw | ConvertFrom-Json
$profileNames = @($config.profiles.PSObject.Properties.Name)

if ($ListProfiles) {
    Write-Host 'Available AbilityKit launch profiles:' -ForegroundColor Cyan
    foreach ($profileName in $profileNames) {
        $profileData = $config.profiles.$profileName
        $profileSilos = Get-ProfileSilos $profileData (Get-ProfileValue $profileData 'siloPort' 11111) (Get-ProfileValue $profileData 'siloGatewayPort' 30000) (Get-ProfileValue $profileData 'primarySiloPort' (Get-ProfileValue $profileData 'siloPort' 11111))
        Write-Host "  $profileName" -ForegroundColor Green
        Write-Host "    $($profileData.description)" -ForegroundColor Gray
        Write-Host "    HTTP: $($profileData.gatewayPort), TCP: $($profileData.tcpPort), Silo: $($profileData.siloPort), Orleans Gateway: $($profileData.siloGatewayPort), Primary: $(Get-ProfileValue $profileData 'primarySiloPort' (Get-ProfileValue $profileData 'siloPort' 11111))" -ForegroundColor Gray
        Write-Host "    Roles: $((@($profileSilos | ForEach-Object { $_.role }) | Where-Object { $_ }) -join ', ')" -ForegroundColor Gray
    }

    return
}

if (-not $Profile -or $Profile.Count -eq 0) {
    $Profile = @($config.defaultProfile)
}

$startScript = Join-Path $PSScriptRoot 'start_orleans_dev.ps1'
if (-not (Test-Path $startScript)) {
    throw "Start script was not found: $startScript"
}

foreach ($profileName in $Profile) {
    if ($profileNames -notcontains $profileName) {
        throw "Unknown launch profile '$profileName'. Use -ListProfiles to see available profiles."
    }

    $profileData = $config.profiles.$profileName
    $effectiveConfiguration = if ([string]::IsNullOrWhiteSpace($Configuration)) { Get-ProfileValue $profileData 'configuration' 'Debug' } else { $Configuration }
    $effectiveGatewayPort = if ($PSBoundParameters.ContainsKey('GatewayPort')) { $GatewayPort } else { Get-ProfileValue $profileData 'gatewayPort' 5001 }
    $effectiveSiloPort = if ($PSBoundParameters.ContainsKey('SiloPort')) { $SiloPort } else { Get-ProfileValue $profileData 'siloPort' 11111 }
    $effectiveSiloGatewayPort = if ($PSBoundParameters.ContainsKey('SiloGatewayPort')) { $SiloGatewayPort } else { Get-ProfileValue $profileData 'siloGatewayPort' 30000 }
    $effectivePrimarySiloPort = if ($PSBoundParameters.ContainsKey('PrimarySiloPort')) { $PrimarySiloPort } else { Get-ProfileValue $profileData 'primarySiloPort' $effectiveSiloPort }
    $effectiveTcpPort = if ($PSBoundParameters.ContainsKey('TcpPort')) { $TcpPort } else { Get-ProfileValue $profileData 'tcpPort' 4000 }
    $profileSilos = Get-ProfileSilos $profileData $effectiveSiloPort $effectiveSiloGatewayPort $effectivePrimarySiloPort

    $startParams = @{
        InstanceName = Get-ProfileValue $profileData 'instanceName' $profileName
        ClusterId = Get-ProfileValue $profileData 'clusterId' "abilitykit-$profileName"
        ServiceId = Get-ProfileValue $profileData 'serviceId' "abilitykit-orleans-$profileName"
        Configuration = $effectiveConfiguration
        GatewayPort = $effectiveGatewayPort
        SiloPort = $effectiveSiloPort
        SiloGatewayPort = $effectiveSiloGatewayPort
        PrimarySiloPort = $effectivePrimarySiloPort
        TcpPort = $effectiveTcpPort
        Silos = $profileSilos
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

    Write-Host "Launching AbilityKit profile '$profileName'..." -ForegroundColor Cyan
    & $startScript @startParams
}
