# AbilityKit 测试门禁与批量回归规范

## 1. 目标

AbilityKit 当前同时包含 Unity UPM 包源码、纯 C# runtime、MOBA/Shooter 示例、服务端实验工程和工具链。为了避免大型项目常见的“功能继续堆叠但基础链路已回归”的问题，测试流程必须从零散命令升级为统一门禁制度。

本规范的目标是：

1. 把单元测试、contract 测试、smoke 测试和批量回归测试分层管理。
2. 明确哪些门禁是继续开发前必须通过的阻断项。
3. 为后续 CI、批量回归、阶段性收口提供统一入口。
4. 让每个关键功能都有可追踪的测试标签和文档依据。

统一执行入口是 [`tools/run_test_gate.ps1`](../tools/run_test_gate.ps1)，门禁清单是 [`tools/test-gates.json`](../tools/test-gates.json)。

## 2. 门禁分层

| 层级 | 类型 | 典型门禁 | 触发时机 | 失败处理 |
| --- | --- | --- | --- | --- |
| P0 | Development Blocker | `precheck`、`moba-console-smoke` | 每次继续功能开发前、提交相关改动前 | 必须立即修复，不继续写新功能 |
| P1 | Contract Blocker | `runtime-contracts` | 网络、DI、表现运行时、跨模块契约变化后 | 修复契约破坏，或同步更新设计文档和测试预期 |
| P2 | Regression Baseline | `regression` | 大范围重构、合并前、阶段性收口、候选发布前 | 作为合并/发布阻断项处理 |

### 2.1 P0：继续开发前置门禁

P0 关注最短反馈链路。任何影响 MOBA console、战斗入口、表现层可测试性、runtime 输入链路的改动，都必须至少通过 `moba-console-smoke`。

推荐命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate moba-console-smoke
```

如果只是确认本地继续开发状态，运行默认门禁：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1
```

### 2.2 P1：运行时契约门禁

P1 关注跨模块契约是否被破坏，例如：

- 网络 runtime 协议行为。
- World DI 生命周期和服务解析。
- 通用表现运行时 contract。

推荐命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate runtime-contracts
```

### 2.3 P2：批量回归门禁

P2 是阶段性收口标准，用于大范围重构或合并前。它不替代 P0/P1，而是在 P0/P1 已经稳定后，用来确认主要纯 C# 测试面没有明显回归。

推荐命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate regression
```

## 3. 当前门禁清单

门禁定义以 [`tools/test-gates.json`](../tools/test-gates.json) 为准。当前门禁包括：

| 门禁 | 层级 | 负责人域 | 作用域 | 阻断点 |
| --- | --- | --- | --- | --- |
| `precheck` | P0 | Core/MOBA Runtime | 本地开发、MOBA console、战斗入口 | 继续功能开发、提交 runtime/console 相关改动前 |
| `moba-console-smoke` | P0 | MOBA Runtime/Presentation | MOBA console、表现层可测试性、战斗 smoke、技能 trace | 继续 MOBA 表现层/runtime 后续开发前 |
| `runtime-contracts` | P1 | Runtime Platform | 网络 runtime、World DI、Game View Runtime | 合并 runtime contract 变化前 |
| `regression` | P2 | AbilityKit Engineering | MOBA、Shooter、runtime contracts 批量回归 | 大范围重构、候选发布、批量合并前 |

查看完整门禁：

```powershell
powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -List
```

## 4. 配置规范

### 4.1 顶层字段

[`tools/test-gates.json`](../tools/test-gates.json) 的顶层字段用于描述门禁体系本身：

| 字段 | 含义 |
| --- | --- |
| `schemaVersion` | 配置结构版本。脚本和文档演进时递增。 |
| `owner` | 门禁体系维护责任域。 |
| `defaultGate` | 不传 `-Gate` 时默认执行的门禁。 |
| `documentation` | 对应规范文档路径。 |
| `policies` | P0/P1/P2 的统一规则说明。 |
| `gates` | 实际门禁列表。 |

### 4.2 单个门禁字段

每个门禁必须包含以下信息：

| 字段 | 必填 | 说明 |
| --- | --- | --- |
| `name` | 是 | 命令行使用的稳定门禁名，使用 kebab-case。 |
| `level` | 是 | P0/P1/P2。 |
| `owner` | 是 | 维护责任域，不要求是个人。 |
| `description` | 是 | 一句话说明门禁目标。 |
| `scope` | 是 | 该门禁覆盖的功能域。 |
| `requiredBefore` | 是 | 哪些行为前必须通过该门禁。 |
| `failurePolicy` | 是 | 失败后的阻断和处理策略。 |
| `steps` | 是 | 实际执行步骤。 |

### 4.3 Step 类型

当前支持三类 step：

| kind | 用途 | 必要字段 |
| --- | --- | --- |
| `dotnet-build` | 构建指定项目 | `name`、`project` |
| `dotnet-test` | 测试指定项目 | `name`、`project`，可选 `filter` |
| `gate` | 嵌套执行另一个门禁 | `name`、`gate` |

## 5. xUnit 标签规范

门禁过滤依赖 xUnit `Trait`，规则如下：

| Trait | 用途 | 示例 |
| --- | --- | --- |
| `Gate` | 硬门禁标签，用于 `dotnet test --filter` | `MobaConsoleSmoke` |
| `Category` | 功能域或测试类型 | `Smoke`、`MobaConsole`、`Presentation`、`RuntimeContract` |
| `Feature` | 可选，具体功能名 | `ViewTimeline`、`SkillCastTrace` |

新增关键功能时必须遵守：

1. 至少补一个主链路测试。
2. 如果该功能会阻断后续开发，必须标记 `Gate`。
3. `Gate` 名使用 PascalCase，门禁名使用 kebab-case。
4. 测试加入门禁后，同步更新 [`tools/test-gates.json`](../tools/test-gates.json) 和本文档。

当前 MOBA console smoke 使用：

```csharp
[Trait("Gate", "MobaConsoleSmoke")]
[Trait("Category", "Smoke")]
[Trait("Category", "MobaConsole")]
```

## 6. 测试优先开发流程

### 6.1 改动前

1. 判断影响域：console、runtime、view runtime、network、DI、Shooter、Unity package。
2. 选择最小必要门禁。
3. 如果没有可覆盖该影响域的门禁，先补测试或把新测试纳入已有门禁。

### 6.2 开发中

1. 优先抽出可测试 seam，避免逻辑只能在 Unity 场景中验证。
2. 主链路优先接入 smoke 或 contract 测试。
3. 每完成一个小功能点，先跑相关 P0/P1 门禁，再继续下一批功能。
4. 新增功能默认遵循“先补测试、再扩实现、再跑门禁”的顺序。
5. 如果一项改动会影响后续多人协作或 CI 定时检查，必须同步补充门禁标签或门禁配置。

### 6.3 提交、合并或继续开发前

1. 普通小改动：至少运行 `precheck`。
2. MOBA 表现层或 console 链路改动：运行 `moba-console-smoke`。
3. runtime contract 改动：运行 `runtime-contracts`。
4. 大范围重构或阶段收口：运行 `regression`。
5. CI 定时检查失败后，必须先修复失败门禁，再继续合并或继续开发。

## 7. 失败处理流程

门禁失败时按以下流程处理：

1. 停止继续写新功能。
2. 确认失败类型：编译失败、测试失败、测试过滤无命中、配置错误、环境问题。
3. 如果是编译失败，先修复最小项目构建。
4. 如果是测试失败，优先判断是功能回归还是测试预期需要更新。
5. 如果是契约变化，必须同步更新设计文档、门禁配置和测试预期。
6. 修复后重新运行同一个门禁，直到通过。

不能用以下方式绕过门禁：

- 删除 `Trait` 让测试不被过滤命中。
- 把失败测试移出门禁但不补等价覆盖。
- 只跑单个测试方法后宣称门禁通过。
- 将 P0 失败延后到 P2 批量回归再处理。

## 8. CI 接入与定时检测

当前脚本已经可直接用于 CI，且会统一输出到 [`artifacts/test-gates`](../artifacts/test-gates) 下的门禁运行目录。每次运行都会生成：

- 门禁输出目录。
- 每个 `dotnet test` 的日志文件。
- 每个测试步的 TRX 结果文件。
- `gate-summary.json` 汇总文件。

推荐阶段如下：

| CI 阶段 | 命令 | 目的 |
| --- | --- | --- |
| Pull Request quick check | `powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate precheck -CI -ResultsDirectory artifacts\test-gates\precheck` | 快速阻断明显回归 |
| Runtime contract check | `powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate runtime-contracts -CI -ResultsDirectory artifacts\test-gates\runtime-contracts` | 校验核心运行时契约 |
| Nightly regression | `powershell -ExecutionPolicy Bypass -File tools\run_test_gate.ps1 -Gate regression -CI -ResultsDirectory artifacts\test-gates\regression` | 批量回归 |

对应的 GitHub Actions 工作流位于 [`/.github/workflows/abilitykit-test-gates.yml`](../.github/workflows/abilitykit-test-gates.yml)。

### 8.1 定时策略

当前定时策略以 `workflow schedule` 为准：

- `0 18 * * *` UTC，每天触发一次。
- 对应北京时间约为每天 02:00。
- 定时任务默认执行 `regression` 门禁，适合做夜间批量回归和失效检测。

### 8.2 CI 产物约定

每次 CI 运行建议上传整个门禁目录作为 artifact，便于排查：

- `gate-summary.json`：门禁状态、耗时、失败步骤。
- `*.log`：每个 step 的原始命令输出。
- `*.trx`：Test Explorer / CI 可直接读取的测试结果。

如果后续要引入覆盖率或 Unity batchmode 门禁，只需要继续沿用同一目录结构即可。

## 9. 维护规则

1. 新增门禁必须同时更新 [`tools/test-gates.json`](../tools/test-gates.json) 和本文档。
2. 删除门禁必须说明替代门禁或删除原因。
3. 修改 P0 门禁前必须确认不会降低继续开发前置保障。
4. `regression` 可以变大，但 P0 应保持快速。
5. 文档中的命令必须与脚本实际参数保持一致。

## 10. 当前 MOBA console smoke 覆盖范围

`moba-console-smoke` 当前覆盖：

1. 完整战斗 smoke 场景。
2. Console 表现层时间线对齐决策。
3. 真实 tick 链路中的表现层时间线对齐。
4. 技能释放、runtime 输入端口、技能效果 trace、导出 artifact 主链路。

该门禁是后续 MOBA 表现层可测试性优化的默认前置条件。
