# Docs/design 飞书一键同步工具

本工具链以 `Docs/design` 为唯一文档源，一次执行完成 Mermaid 校验、飞书友好格式导出、内容摘要比对和增量发布。默认通过用户 OAuth 以浏览器中登录的个人身份写入个人云空间，应用只承担 OAuth 客户端角色。

## 1. 最少配置

首次执行安全预览：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -Preview
```

脚本会自动从 `tools/feishu-design-sync.template.json` 创建被 Git 忽略的本地配置：

```text
tools/feishu-design-sync.local.json
```

只需填写 OAuth 应用凭据和个人云空间目标文件夹：

```json
{
  "authMode": "user-oauth",
  "appId": "cli_xxxxxxxxxxxxxxxx",
  "appSecret": "your-app-secret",
  "target": {
    "rootToken": "your-personal-folder-token"
  },
  "changedDocumentMode": "versioned-reimport"
}
```

`rootToken` 是个人云空间文件夹 URL 中 `/folder/` 后面的部分，不是完整 URL。也可以不把凭据写入文件，改用环境变量：

```powershell
$env:FEISHU_APP_ID = "cli_xxxxxxxxxxxxxxxx"
$env:FEISHU_APP_SECRET = "your-app-secret"
$env:FEISHU_ROOT_TOKEN = "your-writable-feishu-folder-token"
```

环境变量优先于本地配置。`tools/feishu-design-sync.local.json` 已被 `.gitignore` 忽略，不要提交真实密钥或目录 token。

在飞书开放平台为该应用开通用户身份权限 `drive:drive`（云空间）、`docs:document.media:upload`（上传素材或文件）和 `docx:document`（读取与编辑文档块），并开通导入任务接口要求的文档导入权限。`docx:document` 用于在 Markdown 导入完成后，把 Mermaid 占位段落替换为图片块。错误码 `99991679` 表示当前用户令牌缺少接口权限；错误码 `1061004` 表示当前调用身份对请求中的目标资源没有编辑权限。后台新增权限并发布配置后，必须使用 `-Login` 重新授权，旧令牌不会自动继承新增权限。

导入源上传遵循飞书 `ccm_import_open` 协议：不发送 `parent_node`，并通过 `extra` 声明目标文档类型和源文件扩展名。目标文件夹 token 仅用于后续导入任务的 `mount_key`。

把以下地址登记为 OAuth 重定向 URL：

```text
http://127.0.0.1:8765/feishu/oauth/callback
```

应用不需要公开上架，但 OAuth 应用配置和用户身份权限必须在当前可用版本中生效。实际文件访问权限来自浏览器中完成授权的个人账号，因此目标文件夹应位于该账号自己的云空间中。

## 2. 一键同步

配置完成后，无参数执行即可同步：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1
```

Windows 下也可以直接运行：

```text
tools\sync_design_docs_to_feishu.cmd
```

首次正式运行会打开默认浏览器。使用目标个人飞书账号登录并同意 `drive:drive`、`docs:document.media:upload` 和 `docx:document` 授权后，浏览器会回调本机端口，脚本保存用户访问令牌和刷新令牌并继续上传。后续运行会复用或自动刷新令牌；若缓存缺少当前所需权限，脚本会自动要求重新登录授权。

每次执行会自动完成：

1. 检查 Mermaid 校验依赖，首次缺失时安装到被忽略的 `artifacts/mermaid-validation`。
2. 使用 Mermaid 官方解析器校验 `Docs/design` 下的全部图表，坏图会阻断发布并生成报告。
3. 重新生成 `artifacts/feishu-design-export`，把 Mermaid 源码替换为稳定占位符并提取独立 `.mmd` 资产。
4. 根据导出 Markdown 的 SHA-256 摘要生成增量计划。
5. 创建新增文档、跳过未变文档，并按配置处理已变文档。
6. 导入成功后用 Mermaid 官方 CLI 渲染 PNG，通过 Docx Block API 把占位段落替换为飞书图片块；最终页面不显示 Mermaid 源码或辅助说明。
7. 每创建一个导入任务和每完成一篇文档都立即更新本地检查点，异常退出后可继续轮询既有 ticket，不重复创建页面。

凭据未配置完整时，脚本会自动退化为预览，不会打开登录页面或调用飞书写接口。

## 3. 安全预览

只生成完整计划、不写飞书也不改同步状态：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -Preview
```

需要同时在终端列出每篇文档时：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/sync_design_docs_to_feishu.ps1 -Preview -ListDocuments
```

计划输出到：

```text
artifacts/feishu-design-export/feishu-sync-plan.md
```

## 4. 增量策略

| 动作 | 含义 | 默认行为 |
|------|------|----------|
| `create` | 本地文档从未发布 | 创建飞书文档并保存 token、URL 和摘要 |
| `skip-unchanged` | 导出内容摘要未变化 | 不调用飞书 API |
| `duplicate-reimport` | 已发布文档内容变化 | 创建替代页面并更新本地源文件到新页面的映射 |
| `local-deleted` | 状态中存在但本地源已删除 | 只写入计划，不自动删除远端页面 |

默认 `changedDocumentMode` 为 `versioned-reimport`。飞书导入任务 API 只能创建文档，不能凭已有 document token 原位覆盖正文，因此已变文档的 URL 可能变化。工具会把新 token 和 URL 写回本地状态，但不会自动删除旧页面。导入任务超时或进程中断时，本地检查点保留 ticket；下次执行先恢复轮询该任务，不会再次上传并创建同一页面。

飞书公开 API 没有把 Mermaid 节点和连线写入可编辑“画板/绘图”组件的接口。本工具采用飞书原生图片块作为可自动化实现：页面只展示渲染后的流程图，不展示 Mermaid 源码，但图片内部元素不能在飞书中逐个编辑。

若团队要求 URL 永久稳定，需要后续实现基于飞书 Docx Block API 的正文替换与标题更新；这与当前基于 Markdown 导入任务的一键版本化同步是两种更新模型。

## 5. 常用参数

| 参数 | 用途 |
|------|------|
| `-Preview` | 校验、导出并生成计划，但不调用飞书写接口、不更新同步状态。旧参数 `-DryRun` 仍作为别名兼容。 |
| `-ListDocuments` | 在终端打印每篇文档及其计划动作。 |
| `-ConfigPath` | 指定另一份本地配置。 |
| `-ExportDir` | 临时覆盖导出目录。 |
| `-SkipMermaidValidation` | 跳过 Mermaid 校验，仅用于已由上游门禁完成校验的场景。 |
| `-SkipExport` | 复用已有导出包；若清单不存在则失败。 |
| `-AllowDuplicateReimport` | 兼容旧调用，显式允许已变文档执行版本化重导入。 |
| `-Login` | 忽略已有用户令牌缓存，强制重新打开浏览器授权。 |

`-RegenerateExport` 和 `-Force` 为旧命令兼容参数。当前默认行为已经是自动刷新导出；凭据完整且未传 `-Preview` 时即执行真实同步。

## 6. 输出与状态

默认输出结构：

```text
artifacts/feishu-design-export/
  README.md
  manifest.json
  manifest.md
  feishu-sync-plan.md
  feishu-sync-state.local.json
  markdown/
  mermaid/
  feishu-user-token.local.json
  html/
```

- `markdown/`：飞书导入友好的 Markdown；Mermaid 源码已替换为图片占位符。
- `mermaid/`：提取的 `.mmd` 源码和同步时按需生成的 PNG。
- `html/`：用于人工检查的静态预览。
- `manifest.json`：以 `source` 为稳定主键的机器可读清单。
- `manifest.md`：文档标题、顺序和结构统计。
- `feishu-sync-plan.md`：本次增量动作和远端映射。
- `feishu-sync-state.local.json`：逐文档检查点，保存内容摘要、远端 token、URL、时间和导入 ticket；重新导出不会删除此文件。
- `feishu-user-token.local.json`：个人 OAuth 访问令牌与刷新令牌，仅保存在被 Git 忽略的本地输出目录。

预览模式不会发起 OAuth 登录，也不会写 `feishu-sync-state.local.json`，避免预览后把未发布内容错误标记为已同步。

## 7. 高级默认值

模板只暴露日常需要配置的字段。以下值由脚本提供默认值，通常无需写入本地配置：

- 认证模式：`user-oauth`
- OAuth 回调：`http://127.0.0.1:8765/feishu/oauth/callback`
- 用户令牌缓存：`artifacts/feishu-design-export/feishu-user-token.local.json`
- 源目录：`Docs/design`
- 导出目录：`artifacts/feishu-design-export`
- 飞书 API：`https://open.feishu.cn/open-apis`
- 目标挂载类型：文件夹 `1`
- 导入格式：Markdown 到 Docx
- 请求超时：120 秒
- 导入轮询：每 3 秒一次，最长 180 秒；超时后保留 ticket 供下次恢复
- Mermaid 展示：官方 CLI 渲染 PNG，导入后替换为飞书图片块

租户接口路径确有差异时，仍可在本地配置中增加 `apiBaseUrl`、`sourceDir`、`exportDir`、`syncStatePath`、`oauth`、超时字段，以及 `import.endpointPaths` 覆盖默认值。旧的企业应用身份模式仍可通过 `"authMode": "tenant-app"` 启用。
