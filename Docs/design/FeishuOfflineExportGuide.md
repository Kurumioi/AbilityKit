# Docs/design 飞书同步工具

本工具链用于把 `Docs/design` 下的 Markdown 设计文档预处理成飞书导入友好的结构，并通过一键同步脚本批量写入飞书。日常维护仍以 `Docs/design` 为源，修改文档后重新执行同步命令即可。

## 1. 推荐流程

第一次直接运行同步脚本的 dry-run：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -DryRun
```

脚本会自动完成这些准备动作：

- 如果 `tools/feishu-design-sync.local.json` 不存在，会从 `tools/feishu-design-sync.template.json` 复制一份本地配置。
- 如果 `artifacts/feishu-design-export/manifest.json` 不存在，会先扫描 `Docs/design` 并生成离线导出包。
- 默认只在终端输出摘要，完整文档列表写入 `artifacts/feishu-design-export/feishu-sync-plan.md`。
- 配置里仍是占位值时会强制 dry-run，不会调用飞书 API。

终端摘要类似：

```text
Feishu design sync
Documents: 75, CodeBlocks: 618, Mermaid: 474, TableRows: 3854
DryRun: True
Sync plan: artifacts/feishu-design-export/feishu-sync-plan.md
```

需要在终端打印每篇文档时追加 `-ListDocuments`：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -DryRun -ListDocuments
```

## 2. 填写飞书配置

编辑本地配置：

```text
tools/feishu-design-sync.local.json
```

最少需要填写：

```json
{
  "appId": "cli_xxxxxxxxxxxxxxxx",
  "appSecret": "your-app-secret",
  "target": {
    "rootType": "folder",
    "rootToken": "your-writable-folder-or-node-token"
  }
}
```

`tools/feishu-design-sync.local.json` 已被 `.gitignore` 忽略，不要提交真实 `appSecret` 和可写根节点 token。

飞书开放平台应用需要具备云文档/云空间相关读写权限，并确保目标目录或知识库节点对该应用可写。当前脚本默认走飞书云文档导入任务路线：先上传离线 Markdown，再创建导入任务，让飞书侧转换为文档，这比手工拼装 docx block 更适合保留代码块、表格和大批量文档。

## 3. 执行真实同步

配置确认无误后执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -RegenerateExport -Force
```

`-RegenerateExport` 会在同步前重新扫描 `Docs/design` 并刷新离线导出包。未传 `-Force` 时脚本永远按 dry-run 执行；配置仍包含占位值时，即使传了 `-Force` 也会强制 dry-run。

## 4. 单独生成离线导出包

如果只想刷新离线包，不调用同步逻辑，可以单独运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/export_design_docs_for_feishu.ps1 -Clean
```

默认输出结构：

```text
artifacts/feishu-design-export/
  README.md
  manifest.json
  manifest.md
  markdown/
  html/
```

- `markdown/`：飞书导入友好的 Markdown。每篇文档前会写入 source/title 元信息；Mermaid 代码块前会增加绘图小组件提示。
- `html/`：静态 HTML 预览。保留标题、段落、列表、表格、代码块和 Mermaid 源码块。
- `manifest.json`：机器可读同步清单。一键同步脚本以 `source` 作为稳定主键。
- `manifest.md`：人工审阅清单，包含标题、源路径、输出路径、代码块数量、Mermaid 数量和表格行数量。

## 5. 参数说明

| 参数 | 用途 |
|------|------|
| `-RegenerateExport` | 同步前重新扫描 `Docs/design` 并刷新离线导出包。 |
| `-DryRun` | 只生成同步计划，不调用飞书 API。 |
| `-Force` | 允许真实调用飞书 API；未传此参数时脚本默认 dry-run。 |
| `-ListDocuments` | 在终端打印每篇文档明细；默认只输出摘要。 |
| `-ExportDir` | 覆盖配置中的导出包目录。 |
| `-ConfigPath` | 指定本地配置文件。 |

脚本会生成：

```text
artifacts/feishu-design-export/feishu-sync-plan.md
artifacts/feishu-design-export/feishu-sync-state.local.json
```

`feishu-sync-plan.md` 用于检查本次准备同步的文档列表；`feishu-sync-state.local.json` 用于记录远端 token、URL、同步时间和导入任务 ticket。

## 6. 清单字段约定

`manifest.json` 预留以下字段：

| 字段 | 用途 |
|------|------|
| `source` | 本地 Markdown 源文件路径，作为稳定同步主键。 |
| `slug` | 输出文件路径和远端页面 slug 的建议值。 |
| `markdown` | 离线 Markdown 输出路径。 |
| `html` | 离线 HTML 输出路径。 |
| `stats` | 文档结构统计，用于发现代码块、表格和 Mermaid 风险。 |
| `feishuNodeToken` | API 同步后记录远端知识库节点或文档 token。 |
| `feishuDocumentUrl` | API 同步后记录可读写飞书文档地址。 |

首次运行会创建飞书文档并写入本地同步状态；后续运行继续以 `source` 匹配本地文档。真实飞书接口字段如有租户差异，优先调整 `tools/feishu-design-sync.local.json` 中的 `endpointPaths`，避免改动源文档结构。
