# Shooter 多进程 Smoke 推进计划

> 状态：历史验收事实已整理，后续工作并入 `plans/shooter-multiplayer-sync-optimization-roadmap.md`
>
> 更新日期：2026-07-16
>
> 原则：当前脚本和测试是事实源；本文不固定长期测试数量、瞬时 hash 或易变化的日志计数。

## 1. 背景

Shooter 多进程 smoke 已从“进程可启动、客户端可连接”推进到同步链路验收。当前脚本能够统一启动 server、create client 和多个 join client，采集机器可读结果并在结束后清理进程。

现有验收不只检查退出码，还检查客户端身份、房间/战斗/世界一致性、快照应用、输入接受、late join、终局收敛、重连、弱网扰动、PureState baseline/delta 以及 FrameRecord 产物。

## 2. 已落地能力

- 修复了进程启动、输出采集、退出码读取和客户端异常结果输出。
- client 失败时仍输出 `SHOOTER_MP_CLIENT_RESULT status=fail`，脚本可结构化定位失败字段。
- `dotnet run` 使用前置构建产物，避免重复构建占用端口等待窗口。
- `lastServerTicks` 使用 64 位整数解析。
- packed snapshot 验收使用导入时 reconciliation/authoritative hash，避免 replay/catch-up 后 runtime hash 引起误判。
- create、late join 和 reconnect 客户端都校验 room、battle、world、frame、输入结果与实体可见性。
- `-WaitForMatchEnd` 校验客户端最终 frame、state hash 和 match metadata 收敛。
- `-ReconnectJoinClient` 使用同一 session token 和 room id 重连，并确认重连后继续收到 snapshot push。
- `-ConditionLatencyMs`、`-ConditionJitterMs`、`-ConditionPacketLossRate` 和 `-ConditionSeed` 提供可复现的入站弱网扰动。
- `-PayloadMode packed|pure-state` 覆盖两类快照链路。
- PureState 路径检查 full/delta kind、baseline frame、visibility hint、resync 和 lag compensation 信号。
- 默认生成 `.record.bin`，并断言 replay 与 minimized replay 产物存在；`-NoReplay` 可关闭记录。
- `-TcpPort` 已参数化，不再属于待实现项。

## 3. 已验证场景

### 3.1 最小闭环

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 0 -TimeoutSeconds 45
```

验收重点：

- create client 创建房间、启动战斗并订阅同步。
- 快照可以应用，输入可以提交。
- 客户端输出机器可读 pass 结果。

### 3.2 Late Join

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -TimeoutSeconds 45
```

验收重点：

- create 和 join client 共享同一 room、battle、world。
- join client 的 entry kind 为 `LateJoin`。
- 快照 hash、resync、runtime/view frame 和输入接受结果满足脚本约束。

通过提高 `-JoinClients` 可扩展多客户端覆盖。该路径已验证多个 late join client 共享同一战斗并持续接收权威状态。

### 3.3 终局收敛

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -TimeoutSeconds 75 -WaitForMatchEnd
```

验收重点：

- create client 持续输入直到终局。
- late join client 持续接收权威快照。
- 所有客户端最终 runtime frame、state hash 和 match result 一致。
- 终局模式以最终收敛为核心，不要求 late join 首个快照 hash 等于最终状态。

### 3.4 断线重连

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -Inputs 3 -TimeoutSeconds 45 -ReconnectJoinClient -ReconnectDelayMs 500
```

验收重点：

- join client 使用原 session token 和 room id 恢复。
- entry kind 进入 `Reconnect`。
- 重连前后均收到 snapshot push。
- 恢复后继续满足同步、输入和房间一致性约束。

### 3.5 弱网

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -Inputs 3 -TimeoutSeconds 60 -ConditionLatencyMs 25 -ConditionJitterMs 10 -ConditionPacketLossRate 0.25 -ConditionSeed 20260610
```

验收重点：

- 延迟、抖动和丢包计数证明扰动实际生效。
- 固定 seed 下结果可复现。
- 扰动下仍满足快照应用、resync、输入和 room/battle/world 一致性约束。

### 3.6 PureState

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -TimeoutSeconds 60 -PayloadMode pure-state
```

验收重点：

- 首个可用状态建立 full baseline。
- 后续 delta 携带正确 baseline identity。
- visibility hint、resync 和最终状态满足脚本约束。
- 回放及最小化回放产物均可加载。

### 3.7 FrameRecord 产物

默认输出位于 `artifacts/shooter_multiprocess_smoke/`，包括客户端日志和 `records/*.record.bin`。记录包含 meta、inputs、state hashes 和 snapshot payload，可供运行时 reader 与 Unity 调试入口加载。

具体记录数量取决于运行时序和场景，不作为稳定验收值。稳定条件是产物存在、codec 可读、meta 完整，并且失败最小化记录可被再次加载。

## 4. 当前结论

- 多进程主链路已经覆盖 create、late join、终局、重连、弱网、Packed、PureState 和 FrameRecord。
- 当前剩余工作不是重复增加同类 smoke，而是提升 CI 隔离、artifact 结构、诊断关联和失败可复现性。
- 脚本失败时优先读取 `SHOOTER_MP_CLIENT_RESULT`；帧级排查使用对应 FrameRecord，而不是依赖人工拼接多份日志。
- 大量既有 build warning 会放大输出，应单独治理，不与同步行为修复混在同一工作包。

## 5. 后续执行顺序

1. 参数化 artifact root，避免并行任务写入同一目录。
2. 将单一 `TcpPort` 扩展为完整端口组或基于 run id 的自动端口分配，覆盖 Gateway、Silo 和相关宿主端口。
3. 每次运行生成 manifest 与 summary JSON，记录 commit、命令、seed、端口、客户端、日志、records 和 correlation id。
4. 将 smoke 分为快速、终局、resilience 和完整矩阵，分别接入 PR、主分支和定时任务。
5. 接入 FrameRecord first-divergence 分析；失败 summary 直接指向首分歧帧和最小化记录。
6. 增加并行运行测试，证明两个 smoke 实例不会发生端口、进程名或 artifact 冲突。

## 6. 非目标

- 不重复实现已经存在的 `TcpPort` 参数。
- 不重复添加 minimized replay 存在性断言。
- 不把 Unity 大型调试窗口作为 CI 事实源；CLI 与 JSON 报告优先。
- 不把某次运行的固定 hash、push 数或 snapshot 数写成长期基线。

## 7. 相关文件

- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Program.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Networking/SmokeTcpGameFrameworkNetworkChannel.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Runner/ShooterSmokeClientProcessRunner.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Runner/ShooterSmokeReplayRecordScope.cs`
- `Server/Orleans/tools/run_shooter_multiprocess_smoke.ps1`
- `Unity/Packages/com.abilitykit.record/Runtime/Record/FrameRecord/`
- `artifacts/shooter_multiprocess_smoke/`
- `plans/shooter-multiplayer-sync-optimization-roadmap.md`
