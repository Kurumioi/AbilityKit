param(
    [string[]]$Profile,
    [string]$ProfileConfig = (Join-Path $PSScriptRoot 'abilitykit_launch_profiles.json'),
    [switch]$ListProfiles,
    [switch]$All,
    [int]$GatewayPort,
    [int]$SiloPort,
    [int]$SiloGatewayPort,
    [int]$TcpPort,
    [int]$GraceSeconds = 2
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

function Get-ProfileValue($profile, [string]$name, $fallback) {
    if ($profile.PSObject.Properties.Name -contains $name -and $null -ne $profile.$name) {
        return $profile.$name
    }

    return $fallback
}

function Get-StopTarget($profileName, $profileData) {
    $effectiveGatewayPort = if ($PSBoundParameters.ContainsKey('GatewayPort')) { $GatewayPort } else { Get-ProfileValue $profileData 'gatewayPort' 5001 }
    $effectiveSiloPort = if ($PSBoundParameters.ContainsKey('SiloPort')) { $SiloPort } else { Get-ProfileValue $profileData 'siloPort' 11111 }
    $effectiveSiloGatewayPort = if ($PSBoundParameters.ContainsKey('SiloGatewayPort')) { $SiloGatewayPort } else { Get-ProfileValue $profileData 'siloGatewayPort' 30000 }
    $effectiveTcpPort = if ($PSBoundParameters.ContainsKey('TcpPort')) { $TcpPort } else { Get-ProfileValue $profileData 'tcpPort' 4000 }

    [pscustomobject]@{
        ProfileName = $profileName
        InstanceName = Get-ProfileValue $profileData 'instanceName' $profileName
        GatewayPort = $effectiveGatewayPort
        SiloPort = $effectiveSiloPort
        SiloGatewayPort = $effectiveSiloGatewayPort
        TcpPort = $effectiveTcpPort
        Ports = @($effectiveGatewayPort, $effectiveSiloPort, $effectiveSiloGatewayPort, $effectiveTcpPort)
    }
}

if (-not (Test-Path $ProfileConfig)) {
    throw "Launch profile config was not found: $ProfileConfig"
}

$config = Get-Content $ProfileConfig -Raw | ConvertFrom-Json
$profileNames = @($config.profiles.PSObject.Properties.Name)

if ($ListProfiles) {
    Write-Host 'Available AbilityKit stop profiles:' -ForegroundColor Cyan
    foreach ($profileName in $profileNames) {
        $profileData = $config.profiles.$profileName
        $target = Get-StopTarget $profileName $profileData
        Write-Host "  $profileName" -ForegroundColor Green
        Write-Host "    $($profileData.description)" -ForegroundColor Gray
        Write-Host "    HTTP: $($target.GatewayPort), TCP: $($target.TcpPort), Silo: $($target.SiloPort), Orleans Gateway: $($target.SiloGatewayPort)" -ForegroundColor Gray
    }

    return
}

if ($All) {
    $Profile = $profileNames
}
elseif (-not $Profile -or $Profile.Count -eq 0) {
    $Profile = @($config.defaultProfile)
}

$targets = foreach ($profileName in $Profile) {
    if ($profileNames -notcontains $profileName) {
        throw "Unknown launch profile '$profileName'. Use -ListProfiles to see available profiles."
    }

    Get-StopTarget $profileName $config.profiles.$profileName
}

$ports = @($targets | ForEach-Object { $_.Ports } | Sort-Object -Unique)
$commandPatterns = @(
    'AbilityKit.Orleans.Host.csproj',
    'AbilityKit.Orleans.Gateway.csproj'
)

Write-Host "Stopping AbilityKit profile(s): $($targets.ProfileName -join ', ')" -ForegroundColor Cyan
foreach ($target in $targets) {
    Write-Host "  $($target.ProfileName): HTTP $($target.GatewayPort), TCP $($target.TcpPort), Silo $($target.SiloPort), Orleans Gateway $($target.SiloGatewayPort)" -ForegroundColor Gray
}

Stop-AbilityKitServices -Ports $ports -CommandPatterns $commandPatterns -GraceSeconds $GraceSeconds

Write-Host 'AbilityKit local environment stopped.' -ForegroundColor Green
