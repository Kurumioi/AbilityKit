param(
    [int[]]$Ports = @(4000, 5001)
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'abilitykit_process_utils.ps1')

Stop-AbilityKitServices -Ports $Ports -GraceSeconds 1
