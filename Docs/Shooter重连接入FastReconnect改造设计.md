# Shooter 重连接入 FastReconnect 改造设计

## 1. 背景与目标

网络同步框架（migration roadmap §1–§6）已阶段性收口，《网络同步抽象审计与能力矩阵》§10 形成验收基线。基线中明确指出一条已知缺口：

> `FastReconnectSession` 已实现并经测试覆盖，但尚未被任何真实业务消费者使用。

与此同时，Shooter 示例工程自身已有一套完整、可工作的重连/漂移恢复链路（见《Shooter 客户端漂移检测与强同步恢复方案》），它通过 `ShooterClientRecoveryState` 状态机 + `ShooterClientDriftRecoveryPolicy` 阈值 + 服务端 `StateSyncObserverGrain` 定向全量快照实现恢复。

这两件事放在一起，构成一个理想的“第二真实消费者”验证点：

- 框架侧：`FastReconnectSession` 是与玩法无关的重连状态机（Connected→Disconnected→{Resuming | AwaitingFullSnapshot}→Recovered），需要被真实业务驱动以证明其抽象正确。
- 业务侧：Shooter 已有等价但私有的状态机，重复实现了“按 gap 判定短追帧 vs 全量覆盖”的决策逻辑。

本改造的目标：**让 Shooter 的重连决策层驱动（wrap）框架的 `FastReconnectSession`，把恢复阶段判定与健康事件统一收敛到框架，关闭 §10.4 缺口，同时不改变 Shooter 现有可观测行为。**

非目标（明确排除，避免 speculative generality）：

- 不向 Shooter 这个小型 FPS 叠加 AOI / LOD / BatchSnapshot 等其用不到的能力。
- 不重写 Shooter 的快照导入/回放协议（`ImportPackedSnapshot` / `ClientPredictionReconciliationCoordinator` 维持现状）。
- 不引入新的 wire opcode（沿用现有 `SubscribeStateSync` 复用路径，专用 resync opcode 仍属第二阶段）。

## 2. 现状

### 2.1 Shooter 现有重连链路

涉及类型：

- `IShooterClientSyncController`：客户端同步控制器接口，暴露 `RecoveryState`、`NeedsFullSnapshotResync`、`LastResyncReason`、`LastResync{Client,Authoritative}Frame`、`LastResync{Client,Authoritative}StateHash`、`TryEnterCatchUp(int)`、`CatchUpToFrame(int)`、`ApplyGatewayPush(uint, ArraySegment<byte>)`、`RequestFullSnapshotResyncAsync(...)`。
- `ShooterClientSession`：上述控制器的门面（facade），逐项委托。
- `ShooterClientDriftRecoveryPolicy`（struct，Default）：
  - `smallCatchUpThreshold = 8`
  - `replayThreshold = 120`
  - `maxCatchUpTicksPerUpdate = 4`
  - `snapshotTimeoutTicks`（约 2s 对应帧数）
- `ShooterClientRecoveryState`（enum）：`Normal=0, CatchUp=1, AwaitingFullSnapshot=2, ApplyingFullSnapshot=3, Recovered=4`。

恢复决策逻辑（简化）：

1. 收到权威 packed 快照 → `ApplyGatewayPush` 导入 + reconciliation。
2. 客户端落后服务器 gap：
   - `gap ≤ smallCatchUpThreshold` 或 `≤ replayThreshold`：进入 `CatchUp`，按 `maxCatchUpTicksPerUpdate` 本地追帧。
   - `gap > replayThreshold` 或导入哈希不一致：进入 `AwaitingFullSnapshot` → 请求全量 → `ApplyingFullSnapshot` → `Recovered`。
3. 异常原因：`ImportFailed / AuthoritativeHashMismatch / FrameTooFarBehind / FrameTooFarAhead / SnapshotTimeout / WorldMismatch` 等。

### 2.2 框架 FastReconnectSession

- `FastReconnectPhase`：`Connected=0, Disconnected=1, Resuming=2, AwaitingFullSnapshot=3, Recovered=4`。
- 构造：`FastReconnectSession(int resumeWindowFrames = 32)`。
- 关键方法：
  - `ObserveServerFrame(int serverFrame)`：Connected/Recovered 心跳，刷新 `LastAckedServerFrame`，发 `SnapshotReceived`。
  - `Disconnect()`：→Disconnected，发 `InterpolationStarved`，reason `SnapshotTimeout`。
  - `Reconnect(int currentServerFrame)`：`gap = current - lastAcked`；`gap ≤ resumeWindowFrames` → `Resuming`（reconciliation reason `FrameTooFarBehind/CatchUp`，`replayTicks = gap`）；否则 → `AwaitingFullSnapshot`（`needsFullSnapshot = true`，发 `FullSnapshotRequested`）；两路都发 `SnapshotGap`。
  - `CompleteRecovery()`：→Recovered；全量路径先发 `FullSnapshotApplied` 再发 `InterpolationRecovered`。
- 输出：`FastReconnectStepReport { Phase, SyncReconciliationReport Reconciliation, IReadOnlyList<SyncHealthEvent> HealthEvents }`。

## 3. 接缝差距分析

| 维度 | Shooter 现状 | FastReconnectSession | 差距/处理 |
|------|-------------|----------------------|-----------|
| 状态机 | `ShooterClientRecoveryState`（5 态，含 `ApplyingFullSnapshot`） | `FastReconnectPhase`（5 态，无独立 Applying） | Shooter 多出 `ApplyingFullSnapshot` 中间态——映射为框架 `AwaitingFullSnapshot` 的尾段，`CompleteRecovery()` 之前 |
| 短追帧判定 | 双阈值 `smallCatchUpThreshold=8` / `replayThreshold=120` | 单窗口 `resumeWindowFrames` | 用 `resumeWindowFrames = replayThreshold(120)` 作为“短追帧 vs 全量”的总分界；`smallCatchUpThreshold` 留在 Shooter 侧作为追帧节奏细分，框架不需要感知 |
| 断线触发 | `SnapshotTimeout`（snapshotTimeoutTicks）+ 显式重连 | `Disconnect()` 后 `Reconnect(frame)` | Shooter 的超时检测驱动 `Disconnect()`；收到首个权威帧时驱动 `Reconnect()` |
| 心跳 | 每次 `ApplyGatewayPush` 隐式推进帧 | `ObserveServerFrame(frame)` | Shooter 每次收到权威帧调用 `ObserveServerFrame` |
| 健康事件 | 私有 `LastResyncReason` 等字段 | 统一 `SyncHealthEvent` 流 | Shooter 把框架产出的 `HealthEvents` 转发进现有 DemoHarness 遥测 |
| 异常原因枚举 | Shooter 私有 7 项 | 框架 `SyncReconciliationReport` reason | 两者并存：Shooter 原因（`ImportFailed/WorldMismatch` 等业务语义）保留；帧距类原因（`FrameTooFarBehind`）以框架为准 |

关键结论：两套状态机在“按 gap 判定短追帧 vs 全量覆盖”这一核心决策上语义同构，差异仅在阈值粒度与业务专属异常原因。因此可由框架承担**阶段判定**，Shooter 保留**导入/回放执行**与**业务异常分类**。

## 4. 改造方案

### 4.1 总体策略：Wrap 而非 Replace

`IShooterClientSyncController` 的实现内部持有一个 `FastReconnectSession` 实例，把现有重连决策点改为“先驱动 session、再据 session 的 `FastReconnectStepReport` 执行 Shooter 侧导入/追帧动作”。`ShooterClientRecoveryState` 退化为对 `FastReconnectPhase` 的投影（或直接复用框架枚举），不再独立持有判定逻辑。

选择 wrap 的理由：

- 保持 Shooter 现有公开 API（`RecoveryState`、`NeedsFullSnapshotResync` 等）不变，零破坏面。
- 框架只负责“阶段判定 + 健康事件”，不接触 wire/导入，符合“frame/tick/policy/health → 框架；position/world/wire → 业务”的接缝准则。

### 4.2 状态映射

```
FastReconnectPhase            ShooterClientRecoveryState
-----------------------------------------------------------
Connected                  →  Normal
Disconnected               →  (内部，超时态)
Resuming                   →  CatchUp
AwaitingFullSnapshot       →  AwaitingFullSnapshot
  (执行导入中)             →  ApplyingFullSnapshot   ← Shooter 私有细分
Recovered                  →  Recovered
```

`ApplyingFullSnapshot` 不进入框架，作为 Shooter 在 `AwaitingFullSnapshot` 阶段调用 `ApplyGatewayPush` 期间的本地子态，导入完成后调用 `session.CompleteRecovery()` → `Recovered`。

### 4.3 阈值映射

- `FastReconnectSession(resumeWindowFrames: ShooterClientDriftRecoveryPolicy.Default.replayThreshold /* 120 */)`。
- `smallCatchUpThreshold(8)` 与 `maxCatchUpTicksPerUpdate(4)` 保留在 Shooter 追帧执行层，决定 `CatchUpToFrame` 的步进节奏，不上送框架。

### 4.4 驱动时序

1. 收到权威帧（`ApplyGatewayPush` / 状态同步链路）：调用 `session.ObserveServerFrame(authoritativeFrame)` 刷新 ack。
2. 超时未收到权威帧（`snapshotTimeoutTicks` 触发）：调用 `session.Disconnect()`。
3. 重新收到权威帧且当前处于 Disconnected：调用 `report = session.Reconnect(authoritativeFrame)`：
   - `report.Phase == Resuming` → 走现有 `TryEnterCatchUp` / `CatchUpToFrame(report.Reconciliation.replayTicks)` 路径。
   - `report.Phase == AwaitingFullSnapshot` → 设 `NeedsFullSnapshotResync = true`，走 `RequestFullSnapshotResyncAsync(...)`（沿用 `SubscribeStateSync` 复用路径）。
4. 全量快照导入完成（`ApplyingFullSnapshot` 子态结束）：调用 `session.CompleteRecovery()`。
5. 每次拿到 `report` 后，把 `report.HealthEvents` 转发进现有 DemoHarness 遥测；`report.Reconciliation` 的帧距原因覆盖/补充 `LastResyncReason`。

### 4.5 受影响类型清单（实现阶段）

| 类型 | 改动 |
|------|------|
| `IShooterClientSyncController` 实现类 | 持有 `FastReconnectSession`；重连决策点改为驱动 session |
| `ShooterClientRecoveryState` | 退化为对 `FastReconnectPhase` 的投影；保留 `ApplyingFullSnapshot` 子态 |
| `ShooterClientDriftRecoveryPolicy` | `replayThreshold` 成为 `resumeWindowFrames` 的来源；其余阈值留作追帧节奏 |
| DemoHarness 遥测接入点 | 接收并转发 `FastReconnectStepReport.HealthEvents` |

## 5. 验证策略

- 等价性（characterization）：保留 Shooter 现有重连用例，断言改造前后 `RecoveryState` 投影、`NeedsFullSnapshotResync`、`LastResyncReason` 与公开行为不变。
- 框架消费证明：新增用例断言 Shooter 重连过程中 `FastReconnectSession` 经历预期相位序列（Connected→Disconnected→Resuming/AwaitingFullSnapshot→Recovered），且产出的 `SyncHealthEvent` 被 DemoHarness 捕获。
- 回归：现有 87/87 测试维持绿色；本改造不应改变框架侧任何既有断言。

## 6. 风险与回滚

- 风险：阈值语义迁移（双阈值→单窗口）可能改变边界帧（gap 落在 8–120 之间）的追帧节奏。处理：`smallCatchUpThreshold` 仍在执行层生效，框架只决定“是否全量”，边界行为不变。
- 风险：`ApplyingFullSnapshot` 子态不在框架内，状态一致性靠调用方纪律。处理：在实现类内用单一方法封装“导入→CompleteRecovery”，避免漏调。
- 回滚：wrap 方案为叠加式，移除 `FastReconnectSession` 字段并恢复私有判定即可回退，公开 API 不受影响。

## 7. 与基线文档的衔接

本改造落地后：

- 关闭《网络同步抽象审计与能力矩阵》§10.4「FastReconnectSession 尚未被真实消费者使用」缺口。
- FastReconnect 档案从“已实现、仅测试覆盖”升级为“已实现 + 真实业务消费”，可在 §10.1 落地状态总览中标注消费者为 Shooter。
- 为后续“第二真实消费者”验证框架接缝正确性提供首个端到端样例。

> 本文件为设计/方案文档，不含代码改动。实现需在确认本方案后另行启动。
