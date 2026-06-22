param(
    [Parameter(Mandatory = $false)]
    [string]$SourceRoot = "src\AbilityKit.Demo.Moba.Console\Configs",

    [Parameter(Mandatory = $false)]
    [string]$UnityResourcesRoot = "Unity\Assets\Resources",

    [Parameter(Mandatory = $false)]
    [string[]]$ConfigDirs = @("moba", "ability"),

    [Parameter(Mandatory = $false)]
    [switch]$Check,

    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [switch]$DeleteExtra
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$absSourceRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $SourceRoot))
$absUnityResourcesRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $UnityResourcesRoot))

function Write-Info {
    param([string]$Message)
    Write-Host "[sync_moba_json_configs] $Message"
}

function Resolve-RelativePath {
    param(
        [string]$BasePath,
        [string]$FullPath
    )

    $baseUri = [System.Uri](([System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)) + [System.IO.Path]::DirectorySeparatorChar)
    $fullUri = [System.Uri]([System.IO.Path]::GetFullPath($FullPath))
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($fullUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Get-NormalizedJson {
    param([string]$Path)

    $text = Get-Content -Path $Path -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($text)) {
        return ""
    }

    try {
        # Windows PowerShell 5.1 does not support ConvertFrom-Json -Depth.
        # Keep the depth limit on ConvertTo-Json so nested gameplay configs are preserved when normalized.
        $json = ConvertFrom-Json -InputObject $text
        return ($json | ConvertTo-Json -Depth 100 -Compress)
    }
    catch {
        throw "Invalid JSON: $Path. $($_.Exception.Message)"
    }
}

function Test-SemanticJsonEqual {
    param(
        [string]$LeftPath,
        [string]$RightPath
    )

    return (Get-NormalizedJson $LeftPath) -eq (Get-NormalizedJson $RightPath)
}

function Ensure-Directory {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        if ($DryRun -or $Check) {
            Write-Info "Would create directory: $Path"
        }
        else {
            New-Item -ItemType Directory -Force -Path $Path | Out-Null
        }
    }
}

if (!(Test-Path $absSourceRoot)) {
    Write-Error "Source config root not found: $absSourceRoot"
    exit 1
}

Write-Info "SourceRoot: $absSourceRoot"
Write-Info "UnityResourcesRoot: $absUnityResourcesRoot"
Write-Info "ConfigDirs: $($ConfigDirs -join ', ')"
Write-Info "Mode: $(if ($Check) { 'Check' } elseif ($DryRun) { 'DryRun' } else { 'Sync' })"
Write-Info "DeleteExtra: $DeleteExtra"

$totalCopied = 0
$totalChanged = 0
$totalMissing = 0
$totalExtra = 0
$totalInvalid = 0

foreach ($dir in $ConfigDirs) {
    if ([string]::IsNullOrWhiteSpace($dir)) {
        continue
    }

    $sourceDir = Join-Path $absSourceRoot $dir
    $targetDir = Join-Path $absUnityResourcesRoot $dir

    if (!(Test-Path $sourceDir)) {
        Write-Info "Skip missing source dir: $sourceDir"
        continue
    }

    Ensure-Directory $targetDir

    $sourceFiles = Get-ChildItem -Path $sourceDir -Filter "*.json" -File -Recurse | Sort-Object FullName
    foreach ($sourceFile in $sourceFiles) {
        $relative = Resolve-RelativePath $sourceDir $sourceFile.FullName
        $targetFile = Join-Path $targetDir $relative
        $targetFileDir = Split-Path -Parent $targetFile

        try {
            [void](Get-NormalizedJson $sourceFile.FullName)
        }
        catch {
            Write-Info "Invalid source JSON: $($sourceFile.FullName)"
            Write-Info $_.Exception.Message
            $totalInvalid++
            continue
        }

        if (!(Test-Path $targetFile)) {
            Write-Info "Missing target: $dir\$relative"
            $totalMissing++
            if (!$Check) {
                Ensure-Directory $targetFileDir
                if ($DryRun) {
                    Write-Info "Would copy: $($sourceFile.FullName) -> $targetFile"
                }
                else {
                    Copy-Item -Path $sourceFile.FullName -Destination $targetFile -Force
                    $totalCopied++
                }
            }
            continue
        }

        try {
            $equal = Test-SemanticJsonEqual $sourceFile.FullName $targetFile
        }
        catch {
            Write-Info "Invalid target JSON: $targetFile"
            Write-Info $_.Exception.Message
            $totalInvalid++
            $equal = $false
        }

        if (!$equal) {
            Write-Info "Changed target: $dir\$relative"
            $totalChanged++
            if (!$Check) {
                if ($DryRun) {
                    Write-Info "Would copy: $($sourceFile.FullName) -> $targetFile"
                }
                else {
                    Copy-Item -Path $sourceFile.FullName -Destination $targetFile -Force
                    $totalCopied++
                }
            }
        }
    }

    if ($DeleteExtra -or $Check) {
        if (Test-Path $targetDir) {
            $targetFiles = Get-ChildItem -Path $targetDir -Filter "*.json" -File -Recurse | Sort-Object FullName
            foreach ($targetFile in $targetFiles) {
                $relative = Resolve-RelativePath $targetDir $targetFile.FullName
                $sourceFile = Join-Path $sourceDir $relative
                if (!(Test-Path $sourceFile)) {
                    Write-Info "Extra target: $dir\$relative"
                    $totalExtra++
                    if ($DeleteExtra -and !$Check) {
                        if ($DryRun) {
                            Write-Info "Would delete: $($targetFile.FullName)"
                        }
                        else {
                            Remove-Item -Path $targetFile.FullName -Force
                        }
                    }
                }
            }
        }
    }
}

Write-Info "Summary: copied=$totalCopied, changed=$totalChanged, missing=$totalMissing, extra=$totalExtra, invalid=$totalInvalid"

if ($Check -and ($totalChanged -gt 0 -or $totalMissing -gt 0 -or $totalExtra -gt 0 -or $totalInvalid -gt 0)) {
    Write-Info "Check failed. Run tools\sync_moba_json_configs.ps1 to sync Unity Resources from Console JSON configs."
    exit 2
}

if ($totalInvalid -gt 0) {
    exit 3
}

Write-Info "Done."
