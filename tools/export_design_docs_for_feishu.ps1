param(
    [string]$SourceDir = "Docs\design",
    [string]$OutputDir = "artifacts\feishu-design-export",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    $baseFull = [System.IO.Path]::GetFullPath($BasePath)
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    if (-not $baseFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFull += [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = [Uri]$baseFull
    $targetUri = [Uri]$targetFull
    $relative = $baseUri.MakeRelativeUri($targetUri).ToString()
    return [Uri]::UnescapeDataString($relative).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function ConvertTo-Slug([string]$relativePath) {
    $withoutExtension = [System.IO.Path]::ChangeExtension($relativePath, $null)
    $normalized = $withoutExtension -replace '\\', '/'
    $normalized = $normalized -replace '[^0-9A-Za-z\u4e00-\u9fa5/_-]+', '-'
    $normalized = $normalized -replace '/+', '/'
    $normalized = $normalized.Trim('/','-')
    if ([string]::IsNullOrWhiteSpace($normalized)) { return "document" }
    return $normalized
}

function Get-TitleFromMarkdown([string]$content, [string]$fallback) {
    $match = [regex]::Match($content, '(?m)^#\s+(.+?)\s*$')
    if ($match.Success) { return $match.Groups[1].Value.Trim() }
    return [System.IO.Path]::GetFileNameWithoutExtension($fallback)
}

function Get-NewLine() {
    return [Environment]::NewLine
}

function Get-MarkdownFence() {
    return ([string][char]96) * 3
}

function Get-MarkdownStats([string]$content) {
    $fenceEscaped = [regex]::Escape((Get-MarkdownFence))
    $codePattern = '(?ms)^' + $fenceEscaped + '([^\r\n]*)[\r\n].*?^' + $fenceEscaped
    $mermaidPattern = '(?ms)^' + $fenceEscaped + 'mermaid[\r\n].*?^' + $fenceEscaped
    $codeBlocks = [regex]::Matches($content, $codePattern).Count
    $mermaidBlocks = [regex]::Matches($content, $mermaidPattern).Count
    $tables = [regex]::Matches($content, '(?m)^\|.*\|\s*$').Count
    $headings = [regex]::Matches($content, '(?m)^#{1,6}\s+').Count
    $links = [regex]::Matches($content, '\[[^\]]+\]\([^)]+\)').Count
    return [ordered]@{
        headings = $headings
        codeBlocks = $codeBlocks
        mermaidBlocks = $mermaidBlocks
        tableRows = $tables
        links = $links
    }
}

function ConvertTo-FeishuMarkdown([string]$content, [string]$relativePath, [string]$title) {
    $lf = [string][char]10
    $nl = Get-NewLine
    $normalized = $content.Replace([string][char]13 + [string][char]10, $lf)
    $lines = $normalized.Split([char]10)
    $output = New-Object System.Collections.Generic.List[string]
    $output.Add('<!--')
    $output.Add('source: ' + $relativePath)
    $output.Add('title: ' + $title)
    $output.Add('generated_for: feishu_manual_import')
    $output.Add('-->')
    $output.Add('')

    $inFence = $false
    $fenceLang = ''
    $fencePattern = '^' + [regex]::Escape((Get-MarkdownFence)) + '(.*)\s*$'
    foreach ($line in $lines) {
        if ($line -match $fencePattern) {
            if (-not $inFence) {
                $fenceLang = $matches[1].Trim()
                if ($fenceLang -eq 'mermaid') {
                    $output.Add('> Diagram widget: the following block is Mermaid source. After import, paste it into a Feishu diagram/code component or let a later API sync tool convert it.')
                    $output.Add('')
                }
                $inFence = $true
            }
            else {
                $inFence = $false
                $fenceLang = ''
            }

            $output.Add($line)
            continue
        }

        $output.Add($line)
    }

    return ($output -join $nl)
}

function HtmlEncode([string]$value) {
    if ($null -eq $value) { return '' }
    return [System.Net.WebUtility]::HtmlEncode($value)
}

function Convert-InlineMarkdownToHtml([string]$line) {
    $encoded = HtmlEncode $line
    $encoded = [regex]::Replace($encoded, '\*\*(.+?)\*\*', '<strong>$1</strong>')
    $tick = [regex]::Escape([string][char]96)
    $inlineCodePattern = $tick + '([^' + $tick + ']+)' + $tick
    $encoded = [regex]::Replace($encoded, $inlineCodePattern, '<code>$1</code>')
    $encoded = [regex]::Replace($encoded, '\[([^\]]+)\]\(([^)]+)\)', '<a href="$2">$1</a>')
    return $encoded
}

function Close-List([System.Collections.Generic.List[string]]$body, [ref]$inList) {
    if ($inList.Value) {
        $body.Add('</ul>')
        $inList.Value = $false
    }
}

function Close-Table([System.Collections.Generic.List[string]]$body, [ref]$inTable) {
    if ($inTable.Value) {
        $body.Add('</tbody></table>')
        $inTable.Value = $false
    }
}

function Convert-MarkdownToHtml([string]$content, [string]$title, [string]$relativePath) {
    $lf = [string][char]10
    $normalized = $content.Replace([string][char]13 + [string][char]10, $lf)
    $lines = $normalized.Split([char]10)
    $body = New-Object System.Collections.Generic.List[string]
    $inFence = $false
    $fenceLang = ''
    $codeBuffer = New-Object System.Collections.Generic.List[string]
    $inList = $false
    $inTable = $false

    $fencePattern = '^' + [regex]::Escape((Get-MarkdownFence)) + '(.*)\s*$'
    foreach ($line in $lines) {
        if ($line -match $fencePattern) {
            if (-not $inFence) {
                Close-List $body ([ref]$inList)
                Close-Table $body ([ref]$inTable)
                $inFence = $true
                $fenceLang = $matches[1].Trim()
                $codeBuffer.Clear()
            }
            else {
                $class = ''
                if (-not [string]::IsNullOrWhiteSpace($fenceLang)) {
                    $class = ' class="language-' + (HtmlEncode $fenceLang) + '"'
                }
                if ($fenceLang -eq 'mermaid') {
                    $body.Add('<div class="diagram-note">Diagram widget: Mermaid source</div>')
                }
                $body.Add('<pre><code' + $class + '>' + (HtmlEncode (($codeBuffer.ToArray()) -join $lf)) + '</code></pre>')
                $inFence = $false
                $fenceLang = ''
            }
            continue
        }

        if ($inFence) {
            $codeBuffer.Add($line)
            continue
        }

        if ($line -match '^\s*$') {
            Close-List $body ([ref]$inList)
            Close-Table $body ([ref]$inTable)
            continue
        }

        if ($line -match '^(#{1,6})\s+(.+)$') {
            Close-List $body ([ref]$inList)
            Close-Table $body ([ref]$inTable)
            $level = $matches[1].Length
            $text = Convert-InlineMarkdownToHtml $matches[2].Trim()
            $body.Add('<h' + $level + '>' + $text + '</h' + $level + '>')
            continue
        }

        if ($line -match '^\s*[-*]\s+(.+)$') {
            Close-Table $body ([ref]$inTable)
            if (-not $inList) {
                $body.Add('<ul>')
                $inList = $true
            }
            $body.Add('<li>' + (Convert-InlineMarkdownToHtml $matches[1].Trim()) + '</li>')
            continue
        }

        if ($line -match '^\|.*\|\s*$') {
            Close-List $body ([ref]$inList)
            $cells = $line.Trim().Trim('|') -split '\|'
            $isDivider = $true
            foreach ($cell in $cells) {
                if ($cell.Trim() -notmatch '^:?-{3,}:?$') {
                    $isDivider = $false
                    break
                }
            }
            if ($isDivider) { continue }
            if (-not $inTable) {
                $body.Add('<table><tbody>')
                $inTable = $true
            }
            $htmlCells = $cells | ForEach-Object { '<td>' + (Convert-InlineMarkdownToHtml $_.Trim()) + '</td>' }
            $body.Add('<tr>' + ($htmlCells -join '') + '</tr>')
            continue
        }

        Close-List $body ([ref]$inList)
        Close-Table $body ([ref]$inTable)
        if ($line -match '^>\s*(.+)$') {
            $body.Add('<blockquote>' + (Convert-InlineMarkdownToHtml $matches[1].Trim()) + '</blockquote>')
        }
        else {
            $body.Add('<p>' + (Convert-InlineMarkdownToHtml $line.Trim()) + '</p>')
        }
    }

    Close-List $body ([ref]$inList)
    Close-Table $body ([ref]$inTable)

    $encodedTitle = HtmlEncode $title
    $encodedSource = HtmlEncode $relativePath
    $bodyHtml = $body -join $lf
    return @"
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>$encodedTitle</title>
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Microsoft YaHei", sans-serif; line-height: 1.72; max-width: 1040px; margin: 32px auto; padding: 0 24px; color: #1f2937; }
    h1, h2, h3, h4, h5, h6 { line-height: 1.35; margin-top: 1.5em; }
    code { background: #f3f4f6; border-radius: 4px; padding: 0.1em 0.35em; }
    pre { background: #111827; color: #e5e7eb; border-radius: 8px; padding: 16px; overflow: auto; }
    pre code { background: transparent; padding: 0; }
    table { width: 100%; border-collapse: collapse; margin: 16px 0; }
    td, th { border: 1px solid #d1d5db; padding: 8px 10px; vertical-align: top; }
    blockquote { margin: 16px 0; padding: 8px 16px; border-left: 4px solid #9ca3af; background: #f9fafb; color: #4b5563; }
    .source { color: #6b7280; font-size: 13px; border-bottom: 1px solid #e5e7eb; padding-bottom: 12px; }
    .diagram-note { margin: 12px 0 6px; padding: 8px 12px; background: #eff6ff; border-left: 4px solid #2563eb; color: #1d4ed8; }
  </style>
</head>
<body>
  <div class="source">Source: $encodedSource</div>
$bodyHtml
</body>
</html>
"@
}

$sourceFull = [System.IO.Path]::GetFullPath($SourceDir)
$outputFull = [System.IO.Path]::GetFullPath($OutputDir)
$markdownOut = Join-Path $outputFull "markdown"
$htmlOut = Join-Path $outputFull "html"

if (-not (Test-Path $sourceFull)) {
    throw "SourceDir not found: $SourceDir"
}

if ($Clean -and (Test-Path $outputFull)) {
    Remove-Item -Recurse -Force $outputFull
}

New-Item -ItemType Directory -Force -Path $markdownOut | Out-Null
New-Item -ItemType Directory -Force -Path $htmlOut | Out-Null

$files = Get-ChildItem -Path $sourceFull -Filter "*.md" -Recurse -File | Sort-Object FullName
$manifest = New-Object System.Collections.Generic.List[object]
$indexLines = New-Object System.Collections.Generic.List[string]
$indexLines.Add("# AbilityKit Design Feishu Offline Import Manifest")
$indexLines.Add("")
$indexLines.Add("GeneratedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
$indexLines.Add("")
$indexLines.Add("| Order | Title | Source | Markdown | HTML | CodeBlocks | Mermaid | TableRows |")
$indexLines.Add("|-------|-------|--------|----------|------|------------|---------|-----------|")

$order = 0
foreach ($file in $files) {
    $order++
    $relative = Get-RelativePathCompat $sourceFull $file.FullName
    $relativeUnix = $relative.Replace('\','/')
    $slug = ConvertTo-Slug $relative
    $content = Get-Content -Raw -LiteralPath $file.FullName -Encoding UTF8
    $title = Get-TitleFromMarkdown $content $file.Name
    $stats = Get-MarkdownStats $content

    $mdTarget = Join-Path $markdownOut ($slug + ".md")
    $htmlTarget = Join-Path $htmlOut ($slug + ".html")
    New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($mdTarget)) | Out-Null
    New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($htmlTarget)) | Out-Null

    $feishuMd = ConvertTo-FeishuMarkdown $content $relativeUnix $title
    $html = Convert-MarkdownToHtml $content $title $relativeUnix
    Set-Content -LiteralPath $mdTarget -Value $feishuMd -Encoding UTF8
    Set-Content -LiteralPath $htmlTarget -Value $html -Encoding UTF8

    $mdRel = Get-RelativePathCompat $outputFull $mdTarget
    $htmlRel = Get-RelativePathCompat $outputFull $htmlTarget
    $entry = [ordered]@{
        order = $order
        title = $title
        source = $relativeUnix
        slug = $slug
        markdown = $mdRel.Replace('\','/')
        html = $htmlRel.Replace('\','/')
        stats = $stats
        suggestedFeishuTitle = $title
        feishuNodeToken = $null
        feishuDocumentUrl = $null
    }
    $manifest.Add([pscustomobject]$entry)

    $indexLines.Add("| $order | $title | $relativeUnix | $($entry.markdown) | $($entry.html) | $($stats.codeBlocks) | $($stats.mermaidBlocks) | $($stats.tableRows) |")
}

$nl = Get-NewLine
$manifestJson = $manifest | ConvertTo-Json -Depth 8
Set-Content -LiteralPath (Join-Path $outputFull "manifest.json") -Value $manifestJson -Encoding UTF8
Set-Content -LiteralPath (Join-Path $outputFull "manifest.md") -Value ($indexLines -join $nl) -Encoding UTF8

$readmeLines = @(
    '# Feishu offline import package',
    '',
    ('This directory is generated by tools/export_design_docs_for_feishu.ps1 from source directory: {0}.' -f $SourceDir),
    '',
    '## Contents',
    '',
    '- markdown/: Feishu-import-friendly Markdown. Mermaid blocks get a diagram-widget note for manual handling or later API conversion.',
    '- html/: Static HTML previews for headings, paragraphs, lists, tables, code blocks, and Mermaid source blocks.',
    '- manifest.json: Machine-readable sync manifest. Later Feishu API sync can write back feishuNodeToken and feishuDocumentUrl.',
    '- manifest.md: Human-readable review manifest.',
    '',
    '## Suggested import flow',
    '',
    '1. Open manifest.md and review document order, titles, and stats.',
    '2. Create a root node in Feishu Wiki, for example AbilityKit Design Docs.',
    '3. Create page hierarchy according to each source path in manifest.json.',
    '4. Import the matching file under markdown/; open html/ when visual preview is needed.',
    '5. A later Feishu API tool should use source as the stable key, slug as the output path, and feishuNodeToken as the remote update target.'
)
Set-Content -LiteralPath (Join-Path $outputFull "README.md") -Value ($readmeLines -join $nl) -Encoding UTF8

Write-Host "Exported $($files.Count) documents to $OutputDir"
Write-Host "Manifest: $(Join-Path $OutputDir 'manifest.md')"
