param(
    [string]$OutputDir = "Docs\ppt-assets\abilitykit"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

function New-Font([float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    $families = @("Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Arial")
    foreach ($family in $families) {
        try { return New-Object System.Drawing.Font($family, $size, $style, [System.Drawing.GraphicsUnit]::Pixel) } catch {}
    }
    return New-Object System.Drawing.Font([System.Drawing.FontFamily]::GenericSansSerif, $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
}

function New-Brush($hex) {
    return New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml($hex))
}

function New-Pen($hex, [float]$width = 2) {
    return New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml($hex), $width)
}

function New-StringFormat([string]$align = "Center", [string]$lineAlign = "Center") {
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::$align
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::$lineAlign
    $fmt.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
    $fmt.FormatFlags = [System.Drawing.StringFormatFlags]::LineLimit
    return $fmt
}

function Draw-RoundRect($g, [System.Drawing.RectangleF]$rect, [float]$radius, $fill, $stroke = $null) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    if ($fill) { $g.FillPath($fill, $path) }
    if ($stroke) { $g.DrawPath($stroke, $path) }
    $path.Dispose()
}

function Draw-Text($g, [string]$text, [System.Drawing.RectangleF]$rect, $font, $brush, [string]$align = "Center", [string]$lineAlign = "Center") {
    $fmt = New-StringFormat $align $lineAlign
    $g.DrawString($text, $font, $brush, $rect, $fmt)
    $fmt.Dispose()
}

function Draw-Arrow($g, [float]$x1, [float]$y1, [float]$x2, [float]$y2, $pen) {
    $cap = New-Object System.Drawing.Drawing2D.AdjustableArrowCap(5, 6, $true)
    $pen.CustomEndCap = $cap
    $g.DrawLine($pen, $x1, $y1, $x2, $y2)
    $pen.CustomEndCap = $null
    $cap.Dispose()
}

function Save-Canvas([string]$fileName, [scriptblock]$draw) {
    $bitmap = New-Object System.Drawing.Bitmap(1920, 1080)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.Clear([System.Drawing.ColorTranslator]::FromHtml("#F7F9FC"))
    & $draw $g
    $path = Join-Path $OutputDir $fileName
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bitmap.Dispose()
}

$titleFont = New-Font 42 ([System.Drawing.FontStyle]::Bold)
$subTitleFont = New-Font 28 ([System.Drawing.FontStyle]::Bold)
$bodyFont = New-Font 23
$smallFont = New-Font 19
$white = New-Brush "#FFFFFF"
$text = New-Brush "#172033"
$muted = New-Brush "#536070"
$blue = New-Brush "#2563EB"
$cyan = New-Brush "#0891B2"
$green = New-Brush "#059669"
$amber = New-Brush "#D97706"
$red = New-Brush "#DC2626"
$purple = New-Brush "#7C3AED"
$line = New-Pen "#8EA0B8" 3
$darkLine = New-Pen "#334155" 3

function Draw-Header($g, [string]$title, [string]$subtitle) {
    Draw-Text $g $title ([System.Drawing.RectangleF]::new(80, 42, 1760, 60)) $titleFont $text "Near" "Center"
    Draw-Text $g $subtitle ([System.Drawing.RectangleF]::new(82, 105, 1760, 38)) $smallFont $muted "Near" "Center"
    $g.DrawLine((New-Pen "#CBD5E1" 2), 80, 160, 1840, 160)
}

Save-Canvas "01-abilitykit-architecture-layers.png" {
    param($g)
    Draw-Header $g "AbilityKit 四层架构" "从底层能力到游戏示例：依赖方向向下，业务按需组合"
    $layers = @(
        @{Name="应用与示例层"; Color="#2563EB"; Items=@("MOBA 示例", "Shooter 示例", "Unity 表现", "ET / Orleans 集成")},
        @{Name="技能与战斗层"; Color="#0891B2"; Items=@("Pipeline", "Triggering", "Ability Runtime", "Targeting / Projectile / Damage")},
        @{Name="世界与同步层"; Color="#059669"; Items=@("World.DI", "ECS", "FrameSync", "Snapshot / StateSync / Record")},
        @{Name="核心基础层"; Color="#7C3AED"; Items=@("Core", "Math", "Event", "Attributes / Effects")}
    )
    $y = 210
    foreach ($layer in $layers) {
        $fill = New-Brush $layer.Color
        $rect = [System.Drawing.RectangleF]::new(150, $y, 1620, 150)
        Draw-RoundRect $g $rect 18 $fill (New-Pen "#FFFFFF" 2)
        Draw-Text $g $layer.Name ([System.Drawing.RectangleF]::new(190, $y + 38, 260, 70)) $subTitleFont $white "Near" "Center"
        $x = 500
        foreach ($item in $layer.Items) {
            Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x, $y + 40, 280, 70)) 10 (New-Brush "#FFFFFF") $null
            Draw-Text $g $item ([System.Drawing.RectangleF]::new($x + 10, $y + 48, 260, 54)) $smallFont $text
            $x += 305
        }
        $y += 180
    }
    Draw-Arrow $g 960 900 960 840 $darkLine
    Draw-Text $g "上层组合业务，下层提供稳定能力" ([System.Drawing.RectangleF]::new(690, 925, 540, 45)) $bodyFont $muted
}

Save-Canvas "02-abilitykit-capability-map.png" {
    param($g)
    Draw-Header $g "AbilityKit 能力地图" "按问题域理解模块，而不是按包名机械记忆"
    $groups = @(
        @{Title="技能编排"; Color="#2563EB"; Items=@("Pipeline", "Phase", "Sequence / Parallel", "暂停 / 恢复 / 中断")},
        @{Title="事件触发"; Color="#0891B2"; Items=@("Triggering", "强类型事件", "条件表达式", "ExecCtx 注入")},
        @{Title="数值效果"; Color="#059669"; Items=@("Attributes", "Effects", "Buff / Debuff", "Trace")},
        @{Title="战斗查询"; Color="#D97706"; Items=@("Targeting", "Projectile", "Damage", "Entity Index")},
        @{Title="同步回放"; Color="#7C3AED"; Items=@("FrameSync", "Snapshot", "StateSync", "Record")},
        @{Title="承载验证"; Color="#DC2626"; Items=@("Host", "DemoHarness", "Smoke", "CI Gate")}
    )
    $positions = @(@(130,230),@(700,230),@(1270,230),@(130,610),@(700,610),@(1270,610))
    for ($i = 0; $i -lt $groups.Count; $i++) {
        $group = $groups[$i]
        $x = $positions[$i][0]; $y = $positions[$i][1]
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x, $y, 500, 300)) 16 (New-Brush "#FFFFFF") (New-Pen "#CBD5E1" 2)
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x, $y, 500, 68)) 16 (New-Brush $group.Color) $null
        Draw-Text $g $group.Title ([System.Drawing.RectangleF]::new($x + 24, $y + 12, 452, 44)) $subTitleFont $white "Near" "Center"
        $yy = $y + 95
        foreach ($item in $group.Items) {
            Draw-Text $g ("• " + $item) ([System.Drawing.RectangleF]::new($x + 40, $yy, 420, 34)) $bodyFont $text "Near" "Center"
            $yy += 48
        }
    }
}

Save-Canvas "03-skill-cast-main-flow.png" {
    param($g)
    Draw-Header $g "技能释放主链路" "从输入到表现事件：技能系统不是一个 Cast 函数"
    $steps = @(
        @{T="输入"; D="玩家 / AI / 脚本 / 网络"; C="#2563EB"},
        @{T="校验"; D="冷却 / 资源 / 目标 / 状态"; C="#0891B2"},
        @{T="管线编排"; D="阶段 / 延迟 / 并行 / 中断"; C="#059669"},
        @{T="效果执行"; D="伤害 / Buff / 位移 / 投射物"; C="#D97706"},
        @{T="事件触发"; D="Hit / Damage / Death / BuffChanged"; C="#7C3AED"},
        @{T="输出"; D="表现事件 / Trace / Snapshot / 断言"; C="#DC2626"}
    )
    $x = 105; $y = 360
    for ($i = 0; $i -lt $steps.Count; $i++) {
        $s = $steps[$i]
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x, $y, 260, 150)) 14 (New-Brush $s.C) $null
        Draw-Text $g $s.T ([System.Drawing.RectangleF]::new($x + 20, $y + 24, 220, 40)) $subTitleFont $white
        Draw-Text $g $s.D ([System.Drawing.RectangleF]::new($x + 20, $y + 75, 220, 54)) $smallFont $white
        if ($i -lt $steps.Count - 1) {
            Draw-Arrow $g ($x + 270) ($y + 75) ($x + 335) ($y + 75) $darkLine
        }
        $x += 305
    }
    Draw-RoundRect $g ([System.Drawing.RectangleF]::new(230, 670, 1460, 120)) 16 (New-Brush "#FFFFFF") (New-Pen "#CBD5E1" 2)
    Draw-Text $g "教学重点：AbilityKit 把技能释放拆成可配置、可追踪、可测试的运行时链路；每个环节都可以沉淀测试。" ([System.Drawing.RectangleF]::new(270, 692, 1380, 76)) $bodyFont $text
}

Save-Canvas "04-moba-runtime-and-dsl-flow.png" {
    param($g)
    Draw-Header $g "MOBA 示例：运行时启动链与 DSL 场景" "复杂战斗业务的治理方式：启动可验证，技能可追踪，场景可复用"
    $left = @("WorldTypeRegistry", "Blueprint / Module", "WorldInitData", "EntitasWorld", "System Install", "Tick Execute")
    $right = @("BattleTestScript", "Move / Skill / Wait", "Console Driver", "View Runtime Driver", "Trace / Snapshot", "Smoke Assertion")
    $x1 = 260; $x2 = 1110; $y = 220
    Draw-Text $g "运行时启动链" ([System.Drawing.RectangleF]::new($x1, 185, 420, 45)) $subTitleFont $text
    Draw-Text $g "DSL / 脚本场景" ([System.Drawing.RectangleF]::new($x2, 185, 420, 45)) $subTitleFont $text
    for ($i = 0; $i -lt $left.Count; $i++) {
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x1, $y, 420, 70)) 10 (New-Brush "#E0F2FE") (New-Pen "#38BDF8" 2)
        Draw-Text $g $left[$i] ([System.Drawing.RectangleF]::new($x1 + 20, $y + 15, 380, 40)) $bodyFont $text
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x2, $y, 420, 70)) 10 (New-Brush "#ECFDF5") (New-Pen "#34D399" 2)
        Draw-Text $g $right[$i] ([System.Drawing.RectangleF]::new($x2 + 20, $y + 15, 380, 40)) $bodyFont $text
        if ($i -lt $left.Count - 1) {
            Draw-Arrow $g ($x1 + 210) ($y + 75) ($x1 + 210) ($y + 118) $line
            Draw-Arrow $g ($x2 + 210) ($y + 75) ($x2 + 210) ($y + 118) $line
        }
        $y += 115
    }
    Draw-Arrow $g 690 505 1100 505 $darkLine
    Draw-Text $g "同一脚本意图可驱动不同运行环境" ([System.Drawing.RectangleF]::new(705, 455, 380, 46)) $smallFont $muted
}

Save-Canvas "05-shooter-sync-matrix.png" {
    param($g)
    Draw-Header $g "Shooter 示例：同步能力矩阵" "同步能力必须用矩阵验收，而不是只靠手动体验"
    $rows = @("PredictRollback", "AuthoritativeInterpolation", "BatchStateSync", "MassBattleLodSync", "HybridHeroPrediction")
    $cols = @("启动", "收敛", "Snapshot", "协议", "回滚", "重连")
    $x0 = 260; $y0 = 250; $cw = 210; $ch = 92
    Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x0, $y0 - 85, $cw * ($cols.Count + 1), 72)) 12 (New-Brush "#334155") $null
    Draw-Text $g "Sync Model × 验收维度" ([System.Drawing.RectangleF]::new($x0 + 25, $y0 - 72, 520, 46)) $subTitleFont $white "Near" "Center"
    for ($c = 0; $c -lt $cols.Count; $c++) {
        Draw-Text $g $cols[$c] ([System.Drawing.RectangleF]::new($x0 + $cw * ($c + 1), $y0 - 68, $cw, 40)) $smallFont $white
    }
    for ($r = 0; $r -lt $rows.Count; $r++) {
        $y = $y0 + $r * $ch
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x0, $y, $cw, $ch - 8)) 8 (New-Brush "#E2E8F0") (New-Pen "#CBD5E1" 1)
        Draw-Text $g $rows[$r] ([System.Drawing.RectangleF]::new($x0 + 10, $y + 12, $cw - 20, $ch - 32)) $smallFont $text
        for ($c = 0; $c -lt $cols.Count; $c++) {
            $color = if (($r + $c) % 4 -eq 0) { "#DBEAFE" } elseif (($r + $c) % 4 -eq 1) { "#D1FAE5" } elseif (($r + $c) % 4 -eq 2) { "#FEF3C7" } else { "#EDE9FE" }
            Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x0 + $cw * ($c + 1), $y, $cw - 8, $ch - 8)) 8 (New-Brush $color) (New-Pen "#CBD5E1" 1)
            Draw-Text $g "✓" ([System.Drawing.RectangleF]::new($x0 + $cw * ($c + 1), $y + 8, $cw - 8, $ch - 24)) (New-Font 36 ([System.Drawing.FontStyle]::Bold)) $text
        }
    }
    Draw-Text $g "DemoHarness 将 sync model、carrier、network profile、scenario 组合成可自动回归的验收矩阵。" ([System.Drawing.RectangleF]::new(320, 790, 1280, 64)) $bodyFont $muted
}

Save-Canvas "06-test-gates-ci-pyramid.png" {
    param($g)
    Draw-Header $g "测试门禁与 CI 分层" "P0 快速阻断，P1 保护契约，P2 承担批量回归"
    $cx = 960
    $levels = @(
        @{Name="P2 Regression Baseline"; Desc="nightly / 候选发布 / 大范围重构"; W=1280; Y=690; C="#7C3AED"},
        @{Name="P1 Contract Blocker"; Desc="runtime contracts / Unity EditMode / 同步专项"; W=980; Y=520; C="#0891B2"},
        @{Name="P0 Development Blocker"; Desc="precheck / moba-console-smoke / 主链路 smoke"; W=680; Y=350; C="#059669"}
    )
    foreach ($lv in $levels) {
        $x = $cx - $lv.W / 2
        Draw-RoundRect $g ([System.Drawing.RectangleF]::new($x, $lv.Y, $lv.W, 135)) 14 (New-Brush $lv.C) $null
        Draw-Text $g $lv.Name ([System.Drawing.RectangleF]::new($x + 35, $lv.Y + 25, $lv.W - 70, 40)) $subTitleFont $white
        Draw-Text $g $lv.Desc ([System.Drawing.RectangleF]::new($x + 35, $lv.Y + 75, $lv.W - 70, 35)) $smallFont $white
    }
    Draw-Arrow $g 960 300 960 338 $darkLine
    Draw-Text $g "本地开发 / PR / CI" ([System.Drawing.RectangleF]::new(760, 250, 400, 40)) $bodyFont $text
    Draw-Text $g "原则：不是所有测试都进 PR；测试越重，越靠近 nightly 或阶段性收口。" ([System.Drawing.RectangleF]::new(360, 875, 1200, 60)) $bodyFont $muted
}

$mermaid = @{
    "01-abilitykit-architecture-layers.mmd" = @'
flowchart TB
    A[应用与示例层<br/>MOBA / Shooter / Unity 表现 / ET-Orleans] --> B[技能与战斗层<br/>Pipeline / Triggering / Ability / Targeting / Projectile / Damage]
    B --> C[世界与同步层<br/>World.DI / ECS / FrameSync / Snapshot / StateSync / Record]
    C --> D[核心基础层<br/>Core / Math / Event / Attributes / Effects]
'@;
    "02-abilitykit-capability-map.mmd" = @'
mindmap
  root((AbilityKit 能力地图))
    技能编排
      Pipeline
      Phase
      Sequence / Parallel
    事件触发
      Triggering
      强类型事件
      ExecCtx
    数值效果
      Attributes
      Effects
      Buff / Debuff
    战斗查询
      Targeting
      Projectile
      Damage
    同步回放
      FrameSync
      Snapshot
      StateSync
      Record
    承载验证
      Host
      DemoHarness
      Test Gates
'@;
    "03-skill-cast-main-flow.mmd" = @'
flowchart LR
    Input[输入<br/>玩家 / AI / 脚本 / 网络] --> Validate[校验<br/>冷却 / 资源 / 目标 / 状态]
    Validate --> Pipeline[管线编排<br/>阶段 / 延迟 / 并行 / 中断]
    Pipeline --> Effect[效果执行<br/>伤害 / Buff / 位移 / 投射物]
    Effect --> Trigger[事件触发<br/>Hit / Damage / Death / BuffChanged]
    Trigger --> Output[输出<br/>表现事件 / Trace / Snapshot / 断言]
'@;
    "04-moba-runtime-and-dsl-flow.mmd" = @'
flowchart LR
    subgraph Runtime[MOBA 运行时启动链]
      A[WorldTypeRegistry] --> B[Blueprint / Module] --> C[WorldInitData] --> D[EntitasWorld] --> E[System Install] --> F[Tick Execute]
    end
    subgraph DSL[DSL / 脚本场景]
      S[BattleTestScript] --> T[Move / Skill / Wait] --> U[Console Driver]
      T --> V[View Runtime Driver]
      U --> W[Trace / Snapshot]
      V --> W
      W --> X[Smoke Assertion]
    end
    DSL --> Runtime
'@;
    "05-shooter-sync-matrix.mmd" = @'
flowchart TB
    H[DemoHarness Matrix] --> A[PredictRollback]
    H --> B[AuthoritativeInterpolation]
    H --> C[BatchStateSync]
    H --> D[MassBattleLodSync]
    H --> E[HybridHeroPrediction]
    A --> V[启动 / 收敛 / Snapshot / 协议 / 回滚 / 重连]
    B --> V
    C --> V
    D --> V
    E --> V
'@;
    "06-test-gates-ci-pyramid.mmd" = @'
flowchart TB
    Dev[本地开发 / PR / CI] --> P0[P0 Development Blocker<br/>precheck / smoke]
    P0 --> P1[P1 Contract Blocker<br/>runtime contracts / Unity EditMode / 同步专项]
    P1 --> P2[P2 Regression Baseline<br/>nightly / 候选发布 / 大范围重构]
'@
}

foreach ($entry in $mermaid.GetEnumerator()) {
    Set-Content -Path (Join-Path $OutputDir $entry.Key) -Value $entry.Value -Encoding UTF8
}

$index = @"
# AbilityKit PPT 图表资产

本目录由 `tools/generate_abilitykit_ppt_assets.ps1` 生成。

## PNG

1. `01-abilitykit-architecture-layers.png`：四层架构图。
2. `02-abilitykit-capability-map.png`：能力地图。
3. `03-skill-cast-main-flow.png`：技能释放主链路。
4. `04-moba-runtime-and-dsl-flow.png`：MOBA 启动链与 DSL 场景。
5. `05-shooter-sync-matrix.png`：Shooter 同步能力矩阵。
6. `06-test-gates-ci-pyramid.png`：测试门禁与 CI 分层。

## Mermaid 源码

同名 `.mmd` 文件用于后续在 PPT、Markdown 或 Mermaid Live Editor 中继续调整流程图结构。
"@
Set-Content -Path (Join-Path $OutputDir "README.md") -Value $index -Encoding UTF8

Write-Host "Generated AbilityKit PPT assets in $OutputDir"
