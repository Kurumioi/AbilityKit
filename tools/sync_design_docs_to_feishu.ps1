param(
    [string]$ConfigPath = "tools\feishu-design-sync.local.json",
    [string]$ExportDir = "",
    [switch]$RegenerateExport,
    [switch]$DryRun,
    [switch]$Force,
    [switch]$ListDocuments
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = [Console]::OutputEncoding

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Read-JsonFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    return Get-Content -Raw -LiteralPath $Path -Encoding UTF8 | ConvertFrom-Json
}

function Write-JsonFile([string]$Path, [object]$Value) {
    $parent = [System.IO.Path]::GetDirectoryName((Resolve-RepoPath $Path))
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $json = $Value | ConvertTo-Json -Depth 20
    Set-Content -LiteralPath $Path -Value $json -Encoding UTF8
}

function Read-JsonArrayFile([string]$Path) {
    $value = Read-JsonFile $Path
    if ($value -is [System.Array]) { return @($value) }
    if ($null -ne $value.PSObject.Properties["Count"] -and $null -ne $value.PSObject.Properties["Length"]) {
        return @($value)
    }

    return @($value)
}

function Test-Placeholder([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return $true }
    return $Value -like "replace-with-*" -or $Value -like "cli_xxxxxxxxx*" -or $Value -like "optional-*"
}

function Get-ConfigValue([object]$Config, [string]$Name, [object]$DefaultValue) {
    $property = $Config.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        return $DefaultValue
    }

    return $property.Value
}

function Join-FeishuUrl([string]$BaseUrl, [string]$Path) {
    return $BaseUrl.TrimEnd('/') + '/' + $Path.TrimStart('/')
}

function Initialize-LocalConfig([string]$ConfigPath) {
    $fullPath = Resolve-RepoPath $ConfigPath
    if (Test-Path -LiteralPath $fullPath) { return $false }

    $templatePath = Resolve-RepoPath "tools\feishu-design-sync.template.json"
    if (-not (Test-Path -LiteralPath $templatePath)) {
        throw "Config not found: $ConfigPath, and template is missing: tools\feishu-design-sync.template.json"
    }

    $parent = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Copy-Item -LiteralPath $templatePath -Destination $fullPath -Force
    return $true
}

function Invoke-ExportPackage([object]$Config) {
    $exportScript = Resolve-RepoPath "tools\export_design_docs_for_feishu.ps1"
    if (-not (Test-Path -LiteralPath $exportScript)) {
        throw "Export script not found: tools\export_design_docs_for_feishu.ps1"
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File $exportScript -SourceDir $Config.sourceDir -OutputDir $Config.exportDir -Clean
}

function Get-ManifestSummary([object[]]$Entries) {
    $codeBlocks = 0
    $mermaidBlocks = 0
    $tableRows = 0
    foreach ($entry in $Entries) {
        if ($null -ne $entry.stats) {
            $codeBlocks += [int]$entry.stats.codeBlocks
            $mermaidBlocks += [int]$entry.stats.mermaidBlocks
            $tableRows += [int]$entry.stats.tableRows
        }
    }

    return [pscustomobject]@{
        codeBlocks = $codeBlocks
        mermaidBlocks = $mermaidBlocks
        tableRows = $tableRows
    }
}

function Invoke-FeishuJson([string]$Method, [string]$Url, [object]$Body, [hashtable]$Headers, [int]$TimeoutSeconds) {
    $params = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
        TimeoutSec = $TimeoutSeconds
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json; charset=utf-8"
        $params.Body = ($Body | ConvertTo-Json -Depth 20)
    }

    $response = Invoke-RestMethod @params
    if ($null -ne $response.code -and [int]$response.code -ne 0) {
        $message = $response.msg
        if ([string]::IsNullOrWhiteSpace($message)) { $message = ($response | ConvertTo-Json -Depth 10) }
        throw "Feishu API error $($response.code): $message"
    }

    return $response
}

function Get-TenantAccessToken([object]$Config) {
    $endpointPath = $Config.import.endpointPaths.tenantAccessToken
    $url = Join-FeishuUrl $Config.apiBaseUrl $endpointPath
    $body = @{
        app_id = $Config.appId
        app_secret = $Config.appSecret
    }

    $response = Invoke-FeishuJson "POST" $url $body @{} ([int]$Config.requestTimeoutSeconds)
    if ([string]::IsNullOrWhiteSpace($response.tenant_access_token)) {
        throw "tenant_access_token missing in Feishu auth response."
    }

    return $response.tenant_access_token
}

function Send-FeishuMultipartFile([object]$Config, [string]$Token, [string]$FilePath, [object]$Entry) {
    $endpointPath = $Config.import.endpointPaths.uploadAll
    $url = Join-FeishuUrl $Config.apiBaseUrl $endpointPath
    $timeout = [int]$Config.requestTimeoutSeconds
    $rootToken = $Config.target.rootToken
    $fileName = [System.IO.Path]::GetFileName($FilePath)

    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds($timeout)
    $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $Token)

    $content = New-Object System.Net.Http.MultipartFormDataContent
    $content.Add((New-Object System.Net.Http.StringContent($fileName)), "file_name")
    $content.Add((New-Object System.Net.Http.StringContent($rootToken)), "parent_node")
    $content.Add((New-Object System.Net.Http.StringContent("docx")), "parent_type")
    $content.Add((New-Object System.Net.Http.StringContent(([System.IO.FileInfo]$FilePath).Length.ToString())), "size")

    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $fileContent = New-Object System.Net.Http.StreamContent($stream)
        $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("text/markdown")
        $content.Add($fileContent, "file", $fileName)
        $result = $client.PostAsync($url, $content).GetAwaiter().GetResult()
        $raw = $result.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $result.IsSuccessStatusCode) {
            throw "Feishu upload failed: HTTP $([int]$result.StatusCode) $raw"
        }

        $response = $raw | ConvertFrom-Json
        if ($null -ne $response.code -and [int]$response.code -ne 0) {
            throw "Feishu upload error $($response.code): $($response.msg)"
        }

        $fileToken = $response.data.file_token
        if ([string]::IsNullOrWhiteSpace($fileToken)) {
            $fileToken = $response.data.fileToken
        }
        if ([string]::IsNullOrWhiteSpace($fileToken)) {
            throw "file_token missing in Feishu upload response for $($Entry.source)."
        }

        return $fileToken
    }
    finally {
        $stream.Dispose()
        $content.Dispose()
        $client.Dispose()
    }
}

function Start-FeishuImportTask([object]$Config, [string]$Token, [string]$FileToken, [object]$Entry) {
    $endpointPath = $Config.import.endpointPaths.createImportTask
    $url = Join-FeishuUrl $Config.apiBaseUrl $endpointPath
    $headers = @{ Authorization = "Bearer $Token" }
    $body = @{
        file_extension = $Config.import.fileExtension
        file_token = $FileToken
        type = $Config.import.targetFormat
        file_name = $Entry.title
        point = @{
            mount_type = $Config.target.rootType
            mount_key = $Config.target.rootToken
        }
    }

    $response = Invoke-FeishuJson "POST" $url $body $headers ([int]$Config.requestTimeoutSeconds)
    $ticket = $response.data.ticket
    if ([string]::IsNullOrWhiteSpace($ticket)) {
        $ticket = $response.data.job_ticket
    }
    if ([string]::IsNullOrWhiteSpace($ticket)) {
        throw "Import task ticket missing for $($Entry.source)."
    }

    return $ticket
}

function Wait-FeishuImportTask([object]$Config, [string]$Token, [string]$Ticket) {
    $headers = @{ Authorization = "Bearer $Token" }
    $pollInterval = [int]$Config.pollIntervalSeconds
    $maxPoll = [int]$Config.maxPollSeconds
    $deadline = (Get-Date).AddSeconds($maxPoll)

    while ((Get-Date) -lt $deadline) {
        $endpointPath = $Config.import.endpointPaths.getImportTask.Replace('{ticket}', [Uri]::EscapeDataString($Ticket))
        $url = Join-FeishuUrl $Config.apiBaseUrl $endpointPath
        $response = Invoke-FeishuJson "GET" $url $null $headers ([int]$Config.requestTimeoutSeconds)
        $data = $response.data
        $status = [string]$data.status
        if ($status -eq "success" -or $status -eq "succeeded" -or $status -eq "done") {
            return $data
        }
        if ($status -eq "failed" -or $status -eq "error") {
            throw "Feishu import task failed: $($data | ConvertTo-Json -Depth 10)"
        }

        Start-Sleep -Seconds $pollInterval
    }

    throw "Feishu import task timed out: $Ticket"
}

function Get-RemoteDocInfo([object]$TaskData) {
    $token = $TaskData.token
    if ([string]::IsNullOrWhiteSpace($token)) { $token = $TaskData.obj_token }
    if ([string]::IsNullOrWhiteSpace($token)) { $token = $TaskData.doc_token }
    if ([string]::IsNullOrWhiteSpace($token)) { $token = $TaskData.document_token }

    $url = $TaskData.url
    if ([string]::IsNullOrWhiteSpace($url)) { $url = $TaskData.document_url }
    if ([string]::IsNullOrWhiteSpace($url)) { $url = $TaskData.obj_url }

    return [pscustomobject]@{
        token = $token
        url = $url
    }
}

function New-SyncState([object[]]$Entries) {
    $documents = [ordered]@{}
    foreach ($entry in $Entries) {
        $documents[$entry.source] = [ordered]@{
            source = $entry.source
            title = $entry.title
            slug = $entry.slug
            feishuNodeToken = $entry.feishuNodeToken
            feishuDocumentUrl = $entry.feishuDocumentUrl
            syncedAt = $null
            lastImportTicket = $null
        }
    }

    return [ordered]@{
        generatedAt = (Get-Date).ToString("o")
        documents = $documents
    }
}

$configFullPath = Resolve-RepoPath $ConfigPath
$configInitialized = Initialize-LocalConfig $ConfigPath
if ($configInitialized) {
    Write-Host "Created local config from template: $ConfigPath"
    Write-Host "Fill appId, appSecret, and target.rootToken before real sync."
}

$config = Read-JsonFile $configFullPath
$config.apiBaseUrl = [string](Get-ConfigValue $config "apiBaseUrl" "https://open.feishu.cn/open-apis")
$config.exportDir = [string](Get-ConfigValue $config "exportDir" "artifacts/feishu-design-export")
$config.sourceDir = [string](Get-ConfigValue $config "sourceDir" "Docs/design")
$config.syncStatePath = [string](Get-ConfigValue $config "syncStatePath" "artifacts/feishu-design-export/feishu-sync-state.local.json")
$config.requestTimeoutSeconds = [int](Get-ConfigValue $config "requestTimeoutSeconds" 120)
$config.pollIntervalSeconds = [int](Get-ConfigValue $config "pollIntervalSeconds" 3)
$config.maxPollSeconds = [int](Get-ConfigValue $config "maxPollSeconds" 180)

if (-not [string]::IsNullOrWhiteSpace($ExportDir)) {
    $config.exportDir = $ExportDir
}

$exportFull = Resolve-RepoPath $config.exportDir
$manifestPath = Join-Path $exportFull "manifest.json"
if ($RegenerateExport -or -not (Test-Path -LiteralPath $manifestPath)) {
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        Write-Host "Manifest not found; generating export package first."
    }
    Invoke-ExportPackage $config
}

$exportFull = Resolve-RepoPath $config.exportDir
$manifestPath = Join-Path $exportFull "manifest.json"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Manifest not found after export: $manifestPath"
}

$entries = Read-JsonArrayFile $manifestPath
if ($entries.Count -eq 0) {
    throw "Manifest contains no documents: $manifestPath"
}

$configLooksSecret = -not (Test-Placeholder ([string]$config.appId)) -and -not (Test-Placeholder ([string]$config.appSecret)) -and -not (Test-Placeholder ([string]$config.target.rootToken))
$effectiveDryRun = $DryRun -or -not $Force
if (-not $configLooksSecret) {
    $effectiveDryRun = $true
}

$summary = Get-ManifestSummary $entries
Write-Host "Feishu design sync"
Write-Host "Config: $ConfigPath"
Write-Host "ExportDir: $($config.exportDir)"
Write-Host "Documents: $($entries.Count), CodeBlocks: $($summary.codeBlocks), Mermaid: $($summary.mermaidBlocks), TableRows: $($summary.tableRows)"
Write-Host "DryRun: $effectiveDryRun"

$state = New-SyncState $entries
$planLines = New-Object System.Collections.Generic.List[string]
$planLines.Add("# Feishu design sync plan")
$planLines.Add("")
$planLines.Add("GeneratedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
$planLines.Add("DryRun: $effectiveDryRun")
$planLines.Add("")
$planLines.Add("| Order | Title | Source | Markdown | RemoteToken | RemoteUrl |")
$planLines.Add("|-------|-------|--------|----------|-------------|-----------|")

$token = $null
if (-not $effectiveDryRun) {
    $token = Get-TenantAccessToken $config
}

foreach ($entry in $entries) {
    $markdownPath = Join-Path $exportFull $entry.markdown
    if (-not (Test-Path -LiteralPath $markdownPath)) {
        throw "Markdown export missing for $($entry.source): $markdownPath"
    }

    $remoteToken = $entry.feishuNodeToken
    $remoteUrl = $entry.feishuDocumentUrl
    if ($effectiveDryRun) {
        if ($ListDocuments) {
            Write-Host ("[dry-run] {0}. {1} <= {2}" -f $entry.order, $entry.title, $entry.markdown)
        }
    }
    else {
        Write-Host ("[sync] {0}. {1}" -f $entry.order, $entry.title)
        $fileToken = Send-FeishuMultipartFile $config $token $markdownPath $entry
        $ticket = Start-FeishuImportTask $config $token $fileToken $entry
        $taskData = Wait-FeishuImportTask $config $token $ticket
        $remote = Get-RemoteDocInfo $taskData
        $remoteToken = $remote.token
        $remoteUrl = $remote.url
        $state.documents[$entry.source].lastImportTicket = $ticket
        $state.documents[$entry.source].syncedAt = (Get-Date).ToString("o")
        $state.documents[$entry.source].feishuNodeToken = $remoteToken
        $state.documents[$entry.source].feishuDocumentUrl = $remoteUrl
    }

    $state.documents[$entry.source].title = $entry.title
    $state.documents[$entry.source].slug = $entry.slug
    $planLines.Add("| $($entry.order) | $($entry.title) | $($entry.source) | $($entry.markdown) | $remoteToken | $remoteUrl |")
}

$planPath = Join-Path $exportFull "feishu-sync-plan.md"
Set-Content -LiteralPath $planPath -Value ($planLines -join [Environment]::NewLine) -Encoding UTF8
Write-JsonFile (Resolve-RepoPath $config.syncStatePath) $state

Write-Host "Sync plan: $planPath"
Write-Host "Sync state: $($config.syncStatePath)"
if ($effectiveDryRun -and -not $ListDocuments) {
    Write-Host "Document details are in the sync plan. Pass -ListDocuments to print every document in the terminal."
}
if ($effectiveDryRun -and -not $configLooksSecret) {
    Write-Host "Config still contains placeholders; dry-run was forced."
}
if ($effectiveDryRun -and -not $Force) {
    Write-Host "Pass -Force without -DryRun to call Feishu APIs after filling local config."
}
