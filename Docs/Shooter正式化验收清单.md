# Shooter 正式化验收清单

本文用于承接本轮 Shooter 正式化 P0-P4 工作，作为后续回归、交付与优先级推进的统一检查入口。

## 验收范围

- PlayMode 运行视图可编译、可启动、可重建，并具备最低运行诊断信息。
- Shooter 显示层避免按帧重复创建 GameObject 与材质，支持对象池复用和材质属性复用。
- Shooter AI 与敌人攻击使用统一空间目标索引，避免重复线性扫描入口分叉。
- Orleans Battle Grain、Shooter runtime adapter、HTTP Gateway、TCP Gateway 使用收敛后的错误/状态映射。
- Shooter Orleans Smoke 覆盖登录、房间、战斗启动、快照推送、输入提交、过期快照拒绝、晚加入、重连与清理闭环。

## P0：PlayMode 与测试编译

验收目标：PlayMode HUD 与 Shooter runtime 测试在当前代码状态下不引入编译错误。

已完成项：

- 修复 Shooter PlaySession 测试中不存在的快照应用结果枚举引用，改为当前协议内有效的忽略结果。
- 验证 PlayMode HUD 依赖的运行诊断数据结构与视图批次统计入口可编译。
- 保持 PlayMode Host、Remote StateSync Host、Editor Window 的既有启动路径不变。

验收命令：

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter ShooterPlaySessionRunnerTests
```

## P1：显示层对象池与 HUD 诊断

验收目标：运行视图不再依赖按帧销毁/重建所有实体对象，HUD 能展示关键运行状态。

已完成项：

- PlayMode GameObject 视图 sink 为玩家、子弹、敌人分别维护视图字典和对象池。
- 删除实体时回收到对象池，重建或清空时统一隐藏并复用实例。
- 使用隐藏 prefab 作为克隆模板，避免反复创建基础 primitive。
- 使用 MaterialPropertyBlock 写入颜色，避免按实体重复实例化材质。
- HUD 展示帧号、玩家/敌人/子弹数量、受控玩家血量、authority/client batch 来源等运行信息。

验收入口：

- `Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Unity/PlayMode/UnityShooterPlayAdapters.cs`
- `Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Hosting/ShooterHostDiagnostics.cs`
- `Unity/Packages/com.abilitykit.demo.shooter.editor/Editor/Diagnostics/ShooterDemoDiagnostics.cs`

## P2：AI 感知与敌人攻击寻敌统一

验收目标：Shooter Bot AI 与敌人攻击共享同一个空间目标查询能力，避免同类查询逻辑分叉。

已完成项：

- `ShooterSpatialTargetIndex` 作为玩家目标索引的统一入口，按帧重建并按网格邻域搜索最近目标。
- Bot AI 黑板刷新继续通过共享索引查询目标。
- 敌人攻击系统接入共享索引，攻击寻敌不再自行扫描全部玩家。
- 保持现有敌人伤害、波次生成、玩家存活判断语义不变。

验收命令：

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter ShooterWorldModuleTests
```

## P3：Orleans 错误映射与 Shooter Smoke

验收目标：Battle 结果状态、HTTP Gateway 错误、TCP Gateway 错误具备共享映射来源，并提供 Shooter 远程闭环自动化。

已完成项：

- 新增 Battle 结果状态常量，集中维护 Grain/runtime adapter 返回的拒绝状态字符串。
- BattleLogicHostGrain 与 ShooterBattleRuntimeAdapter 使用共享状态常量，避免协议状态字符串散落。
- 新增 RoomOperationErrorClassifier，HTTP Room mapper 与 TCP Room mapper 共享异常到状态码的分类逻辑。
- 新增 Gateway 测试覆盖房间已满、房间关闭、未在房间、非房主、非法玩法命令、参数错误、未知异常。
- ShooterSmoke 工程纳入 Orleans solution，提供 PowerShell 与 BAT 包装脚本。
- Smoke 支持 TCP 端口参数，便于本地并行或规避端口占用。

验收命令：

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.Grains.Tests\AbilityKit.Orleans.Grains.Tests.csproj --filter ShooterBattleRuntimeAdapterTests
```

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.Gateway.Tests\AbilityKit.Orleans.Gateway.Tests.csproj
```

```powershell
.\Server\Orleans\tools\run_shooter_smoke.ps1 -Configuration Debug -TcpPort 41001
```

```cmd
Server\Orleans\tools\run_shooter_smoke.bat -Configuration Debug -TcpPort 41001
```

## P4：文档与交付检查

验收目标：Shooter 正式化状态可被单文档追踪，后续推进不再依赖分散上下文。

交付检查：

- 本文作为 P0-P4 的统一验收入口。
- Orleans 侧运行、Smoke 与测试命令已补入 `Server/Orleans/README.md`。
- 既有阶段性文档仍保留，用于解释 Shooter 示例定位、远程闭环背景与后续专题计划。
- 后续新增验收项应优先追加到本文，再同步到更细的专题设计文档。

## 第三轮：规模预算、Replay 排障与回归入口

验收目标：Shooter 示例在远程链路可观测基础上，补齐长期可回归的规模预算、FrameRecord replay 排障摘要与最小 CI/脚本入口。

已完成项：

- Svelto gameplay benchmark 增加 small/medium/mass 三档实体预算 profile，保留 large-scale 兼容入口。
- Runtime 测试覆盖三档预算的 MaxEntityCount、ActiveSyncBudget、deterministic、tick/allocation diagnostics，以及 pure-state payload entity count 与 visibility hint count 对齐。
- ShooterSmoke replay validation 增加 summary/diagnostic，汇总 meta、inputs、snapshots、state hashes、first/last frame、input/snapshot op code 分布、packed/pure-state/server snapshot 计数。
- Smoke 文本输出追加 replay first/last frame、op code 分布、pure-state/packed snapshot 计数，便于定位 artifact 问题。
- 新增 ShooterSmoke replay summary 单元测试工程，避免依赖完整 Orleans smoke 服务即可验证 `.record.json/.record.bin` 汇总逻辑。

回归入口：

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter "EntityBudgetProfilesRunSmallMediumAndMassAcceptanceMatrix|PureStateSnapshotBudgetProfilesKeepPayloadAndVisibilityHintsAligned|ShooterSnapshotAllocationDiagnosticsTests"
```

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke.Tests\AbilityKit.Orleans.ShooterSmoke.Tests.csproj --filter ShooterSmokeReplaySummaryTests
```

```powershell
.\Server\Orleans\tools\run_shooter_smoke.ps1 -Configuration Debug -TcpPort 41001
```

```powershell
.\Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -Configuration Debug -TcpPort 41001 -PayloadMode packed
```

```powershell
.\Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -Configuration Debug -TcpPort 41001 -PayloadMode pure-state
```

```powershell
.\Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -Configuration Debug -TcpPort 41001 -PayloadMode pure-state -ConditionLatencyMs 120 -ConditionJitterMs 30 -ConditionPacketLossRate 0.05
```

Replay 排障说明：

- packed/pure-state multiprocess smoke 会输出 input-state replay 与 minimized replay 路径，并在 result 行暴露 `inputStateReplayFirstFrame`、`inputStateReplayLastFrame`、`inputStateReplaySnapshotOpCodes`、`inputStateReplayPureStateSnapshots`、`inputStateReplayPackedStateSnapshots`。
- 单进程 smoke 会输出 input-logic replay 与 server-frame 兼容字段，并暴露 `InputLogicReplayFirstFrame`、`InputLogicReplayLastFrame`、`InputLogicReplayOpCodes`、`InputLogicReplaySnapshotOpCodes` 等摘要字段。
- 若需要在测试中直接读取 artifact，可调用 ShooterSmoke replay summary 入口汇总 `.record.bin` 或 `.record.json`。

## 当前剩余缺口

以下内容未在本轮 P0-P4 内完全关闭，建议作为下一轮正式化优先级：

1. Unity 侧自动化：补充可在 CI 或批处理环境执行的 PlayMode/Editor 验收入口。
2. 长时压测：当前 small/medium/mass 预算入口偏快速回归，仍需独立的夜间长时压测策略。
3. Replay CLI：当前 replay summary 已可被测试和 smoke 输出复用，后续可按需补独立命令行入口。
4. 真实网络条件：继续在更复杂网络抖动、丢包与多客户端条件下扩大 resilience smoke 覆盖。

## 最小回归清单

合入 Shooter 正式化相关改动前，建议至少执行：

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter ShooterPlaySessionRunnerTests
```

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter ShooterWorldModuleTests
```

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.Grains.Tests\AbilityKit.Orleans.Grains.Tests.csproj --filter ShooterBattleRuntimeAdapterTests
```

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.Gateway.Tests\AbilityKit.Orleans.Gateway.Tests.csproj
```

```powershell
.\Server\Orleans\tools\run_shooter_smoke.ps1 -Configuration Debug -TcpPort 41001
```

```cmd
dotnet test src\AbilityKit.Demo.Shooter.Runtime.Tests\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --filter "EntityBudgetProfilesRunSmallMediumAndMassAcceptanceMatrix|PureStateSnapshotBudgetProfilesKeepPayloadAndVisibilityHintsAligned|ShooterSnapshotAllocationDiagnosticsTests"
```

```cmd
dotnet test Server\Orleans\src\AbilityKit.Orleans.ShooterSmoke.Tests\AbilityKit.Orleans.ShooterSmoke.Tests.csproj --filter ShooterSmokeReplaySummaryTests
```

```powershell
.\Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -Configuration Debug -TcpPort 41001 -PayloadMode packed
```

```powershell
.\Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -Configuration Debug -TcpPort 41001 -PayloadMode pure-state
```
