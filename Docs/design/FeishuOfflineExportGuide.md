# Docs/design 飞书同步工具

本工具链用于把 `Docs/design` 下的 Markdown 设计文档预处理成飞书导入友好的结构，并通过一键发布脚本批量写入飞书。日常维护仍以 `Docs/design` 为唯一源；脚本会记录每篇导出 Markdown 的 SHA-256 摘要，用于区分新增、未变、已变和本地删除的文档。

## 1. 推荐流程

第一次直接运行同步脚本的 dry-run：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -DryRun
```

脚本会自动完成这些准备动作：

- 如果 `tools/feishu-design-sync.local.json` 不存在，会从 `tools/feishu-design-sync.template.json` 复制一份本地配置。
- 如果 `artifacts/feishu-design-export/manifest.json` 不存在，会先扫描 `Docs/design` 并生成离线导出包。
- 默认只在终端输出摘要，完整文档列表及 `create`、`skip-unchanged`、`changed-needs-update`、`local-deleted` 分类写入 `artifacts/feishu-design-export/feishu-sync-plan.md`。
- 配置里仍是占位值时会强制 dry-run，不会调用飞书 API。
- 状态写入 `artifacts/feishu-design-export/feishu-sync-state.local.json`，且该文件受忽略规则保护。

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

## 3. 执行首次真实发布

配置确认无误后执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -RegenerateExport -Force
```

`-RegenerateExport` 会在发布前重新扫描 `Docs/design` 并刷新离线导出包。未传 `-Force` 时脚本永远按 dry-run 执行；配置仍包含占位值时，即使传了 `-Force` 也会强制 dry-run。首次发布时，所有文档均为 `create`，脚本会为每篇文档创建一次飞书导入任务，并保存远端 token、URL、导入 ticket 和内容摘要。

## 4. 后续发布与更新边界

后续执行同一条命令时，脚本会先比对本地摘要：

| 同步计划动作 | 含义 | 脚本行为 |
|------|------|----------|
| `create` | 本地文档从未发布过 | `-Force` 时创建飞书文档 |
| `skip-unchanged` | 内容未变化 | 不调用飞书 API |
| `changed-needs-update` | 已发布文档内容变化 | 默认失败退出，避免导入 API 重新创建重复页面 |
| `local-deleted` | 状态中存在但本地文档已删除 | 只在计划中标记，不删除远端页面 |

当前飞书导入任务 API 只负责“文件导入并创建文档”，不能凭已有 document token 原位覆盖正文。因此现有脚本已经具备可靠的首次批量发布、未变跳过和变更拦截能力，但**尚未实现真正的原位更新**。要完成真正的一键同步，需要新增基于飞书 Docx Block API 的 Markdown 到 Block 转换、替换正文、标题更新以及删除策略。

仅在已人工确认允许创建新版本页面时，可显式使用以下参数：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -RegenerateExport -Force -AllowDuplicateReimport
```

该命令会为 `changed-needs-update` 文档创建新的飞书页面，并更新本地状态到新页面；它不是远端原位更新。

## 5. 单独生成离线导出包

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
- `manifest.json`：机器可读同步清单。一键同步脚本以 `source` 作为稳定主键，以 `feishuTitle` 作为远端中文显示名。
- `manifest.md`：人工审阅清单，包含标题、源路径、输出路径、代码块数量、Mermaid 数量和表格行数量。

## 6. 参数说明

| 参数 | 用途 |
|------|------|
| `-RegenerateExport` | 同步前重新扫描 `Docs/design` 并刷新离线导出包。 |
| `-DryRun` | 只生成同步计划，不调用飞书 API。 |
| `-Force` | 允许真实调用飞书 API；未传此参数时脚本默认 dry-run。 |
| `-ListDocuments` | 在终端打印每篇文档及其同步计划动作；默认只输出摘要。 |
| `-AllowDuplicateReimport` | 允许将已变文档重新导入为新飞书页面；仅用于人工确认的版本化发布。 |
| `-ExportDir` | 覆盖配置中的导出包目录。 |
| `-ConfigPath` | 指定本地配置文件。 |

脚本会生成：

```text
artifacts/feishu-design-export/feishu-sync-plan.md
artifacts/feishu-design-export/feishu-sync-state.local.json
```

`feishu-sync-plan.md` 用于检查本次准备同步的文档列表；`feishu-sync-state.local.json` 用于记录远端 token、URL、同步时间和导入任务 ticket。

## 7. 清单字段约定

`manifest.json` 预留以下字段：

| 字段 | 用途 |
|------|------|
| `source` | 本地 Markdown 源文件路径，作为稳定同步主键。 |
| `slug` | 仅用于稳定的本地导出路径，不作为飞书文档显示名。 |
| `feishuTitle` | 从 Markdown 一级标题提取的飞书中文显示名，同时用于上传文件名和导入任务名称。 |
| `markdown` | 离线 Markdown 输出路径。 |
| `html` | 离线 HTML 输出路径。 |
| `stats` | 文档结构统计，用于发现代码块、表格和 Mermaid 风险。 |
| `feishuNodeToken` | API 发布后记录远端知识库节点或文档 token。 |
| `feishuDocumentUrl` | API 发布后记录可读写飞书文档地址。 |
| `contentSha256` | 已发布 Markdown 内容摘要，用于增量计划和未变跳过。 |
| `syncedAt` | 上一次成功创建或版本化重导入的时间。 |
| `lastImportTicket` | 飞书导入任务 ticket，用于审计和故障排查。 |

首次运行会创建飞书文档并写入本地同步状态；后续运行以 `source` 关联状态并以 `contentSha256` 生成增量计划。英文目录名和 Markdown 文件名可以继续保持稳定，不会影响飞书页面名称。需要调整某篇飞书文档名称时，修改该文档的一级标题即可；AbilityKit、MOBA、ECS 等技术专名可以保留，标题的说明部分使用中文。

真实飞书接口字段如有租户差异，优先调整 `tools/feishu-design-sync.local.json` 中的 `endpointPaths`，避免改动源文档结构。
