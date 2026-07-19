# Shooter 网络同步示例测试覆盖分析

> 分析日期：2026-06-14
> 范围：`src/AbilityKit.Demo.Shooter.Runtime.Tests/` 全部 21 个测试类（~75 个 [Fact]）
> 对照基准：《网络同步抽象审计与能力矩阵》§10 能力矩阵验收快照
>
> **历史快照警告（2026-07-16）**：本文保留用于追溯 2026-06-14 时点，不再代表当前实现状态。当前源码与测试已经覆盖 Hybrid 本地预测/远端插值、FastReconnect、AOI enter/stay/leave、PriorityBudget、LodFrequency、PureState baseline/delta 消费、有限带宽档案及 lag compensation。后续规划以 `plans/shooter-multiplayer-sync-optimization-roadmap.md`、当前源码和最新测试结果为准；本文中的测试数量和“未实现”判定不得作为新开发依据。

---

## 1. 测试资产总览

### 1.1 测试文件清单（按层次分类）

| 层次 | 测试文件 | [Fact] 数 | 覆盖主题 |
| --- | --- | --- | --- |
| **验收基线** | [`ShooterAcceptanceLabTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterAcceptanceLabTests.cs) | 6 | Catalog 声明、Session 创建、矩阵运行 |
| | [`ShooterAcceptanceMatrixSnapshotTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterAcceptanceMatrixSnapshotTests.cs) | 4 | 四态快照基线 |
| | [`ShooterAcceptanceComparisonTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterAcceptanceComparisonTests.cs) | 7 | AuthoritativeWorld 对比、网络动态切换 |
| **Carrier/能力** | [`ShooterDemoHarnessCarrierTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterDemoHarnessCarrierTests.cs) | 4 | Carrier 能力声明、Unsupported 路径、指标暴露 |
| **同步控制器** | [`ShooterClientAuthoritativeInterpolationSyncControllerTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterClientAuthoritativeInterpolationSyncControllerTests.cs) | 6 | 插值缓冲、饥饿、快照消费 |
| | [`ShooterClientSyncStrategyContractTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterClientSyncStrategyContractTests.cs) | 2 | IClientSyncStrategy 契约 |
| | [`ShooterPackedSnapshotSyncControllerTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Synchronization/ShooterPackedSnapshotSyncControllerTests.cs) | 1 | Packed 快照导入覆写 |
| **会话/编排** | [`ShooterClientSessionTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterClientSessionTests.cs) | 9 | 会话生命周期、Gateway 输入、Resync、诊断 |
| | [`ShooterClientBattleHandleTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterClientBattleHandleTests.cs) | 3 | Battle handle、自动 resync、去重 |
| | [`ShooterPlaySessionRunnerTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterPlaySessionRunnerTests.cs) | 4 | Play mode 运行器、选项归一化 |
| **插值框架** | [`RemoteInterpolationPlaybackTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/RemoteInterpolationPlaybackTests.cs) | 6 | 播放管线、饥饿、诊断、Reset |
| | [`RemoteSnapshotInterpolationTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/RemoteSnapshotInterpolationTests.cs) | 11 | 缓冲重排、插值/外推、时间线、角度 lerp |
| **预测/对账** | [`ClientPredictionInputHistoryTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Networking/ClientPredictionInputHistoryTests.cs) | 2 | 输入记录/回放、trim |
| | [`ClientPredictionReconciliationCoordinatorTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Networking/ClientPredictionReconciliationCoordinatorTests.cs) | 1 | 完整对账流程 |
| | [`CommandRollbackLogTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Rollback/CommandRollbackLogTests.cs) | 2 | 回滚命令日志、RollbackCoordinator |
| **网络条件** | [`NetworkConditioningMiddlewareTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Networking/NetworkConditioningMiddlewareTests.cs) | 6 | 延迟/丢包/乱序/确定性 |
| **输入调度** | [`BattleInputFrameSchedulerTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Networking/BattleInputFrameSchedulerTests.cs) | 3 | 服务端输入重映射（late/early/future） |
| **时间/追帧** | [`WorldStartFrameCatchUpCalculatorTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/WorldStartFrameCatchUpCalculatorTests.cs) | 3 | Late join 追帧计算 |
| **确定性/序列化** | [`ShooterDeterministicReplayTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Application/Runtime/ShooterDeterministicReplayTests.cs) | 2 | 确定性回放、快照导入/导出 |
| | [`ShooterPackedSnapshotRuntimeTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Application/Runtime/ShooterPackedSnapshotRuntimeTests.cs) | 2 | Packed 快照序列化、Room 线路协议 |
| **世界模块** | [`ShooterWorldModuleTests.cs`](src/AbilityKit.Demo.Shooter.Runtime.Tests/Worlds/ShooterWorldModuleTests.cs) | 5 | DI 注册、Svelto 实体、World 生命周期 |

**总计：21 个测试类，~84 个 [Fact]**

### 1.2 Sync Mode × Network Matrix 当前矩阵

Shooter Acceptance Catalog 声明了 **2 个同步模式 × 5 个网络环境** = 10 个场景：

| | Ideal (0ms) | LAN (5ms) | Mobile4G (60ms) | CrossRegion (150ms) | PoorWifi (80ms+loss) |
| --- | --- | --- | --- | --- | --- |
| `PredictRollback` | ✅ Completed | ✅ Completed | ✅ Completed | ✅ Completed | ✅ Completed |
| `AuthoritativeInterpolation` | ⚠️ Unsupported | ⚠️ Unsupported | ⚠️ Unsupported | ⚠️ Unsupported | ⚠️ Unsupported |

> `AuthoritativeInterpolation` 的 5 个网络环境全部被 `ShooterDemoHarnessCarrier.Supports()` 正确拒绝，返回 `Unsupported` 而非静默通过。

---

## 2. 按能力维度的覆盖分析

以下对照《网络同步抽象审计与能力矩阵》§3 的 7 个能力维度逐一审计。

### 2.1 ClientPlaybackPolicy（客户端播放策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `PredictRollback` | ✅ 已实现 | 验收矩阵、Carrier、Session、Comparison、对账、回滚 → 最深覆盖 | ✅ 充分 |
| `AuthoritativeInterpolation` | ✅ 已实现 | 控制器、Session、插值框架 → 层内覆盖充分，但 Harness 矩阵不支持 | 🟡 充分但矩阵缺 |
| `HoldLatest` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `ExtrapolateThenCorrect` | ⚪ 仅声明 | 插值播放的饥饿外推是 fallback，非独立策略 | ❌ 未覆盖 |
| `HybridLocalPredictRemoteInterpolate` | ⚪ 仅声明 | 无（组合档案，非底层模式） | ❌ 未覆盖 |
| `None` | ⚪ 仅声明 | 隐式出现（插值控制器的本地实体不模拟），无独立测试 | ⚪ 非重点 |

**结论**：两个已实现档案覆盖充分。但 `AuthoritativeInterpolation` 在 Harness 矩阵中被标记为 Unsupported 是正确的——因为 `ShooterDemoHarnessCarrier` 只绑定 `PredictRollback` 控制器。后续若新增支持插值的 Carrier，矩阵会自然扩列。

### 2.2 InputPolicy（输入策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `ImmediateSubmit` | ✅ 已实现 | Session 输入提交、BattleHandle gateway 输入 | ✅ 充分 |
| `ServerRemapAcceptedFrame` | ✅ 已实现 | Session gateway resync、BattleHandle resync、BattleInputFrameScheduler | ✅ 充分 |
| `NoClientInput` | ✅ 已实现 | 插值控制器不提交输入（远端实体） | ✅ 充分 |
| `InputDelayBuffer` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `DeterministicBroadcast` | ⚪ 仅声明 | 无（Lockstep 未实现） | ❌ 未覆盖 |

**结论**：已实现的三种输入策略覆盖充分。`InputDelayBuffer` 和 `DeterministicBroadcast` 依赖 Lockstep 模式，暂不需要覆盖。

### 2.3 SnapshotPolicy（快照发布策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `FullSnapshot` | ✅ 已实现 | Packed snapshot 往返、网关推送、对账导入 | ✅ 充分 |
| `AuthorityOverride` | ✅ 已实现 | Packed snapshot flags、对账覆写 | ✅ 充分 |
| `FixedRateStateStream` | ✅ 已实现 | 插值时间线、远程快照缓冲 | ✅ 充分 |
| `KeyFrameSnapshot` | ⚪ 仅声明 | 协议有概念，delta 恢复/关键帧请求链未测试 | 🟡 协议存在但无测试 |
| `DeltaSnapshot` | ⚪ 仅声明 | 协议有概念，import 语义未补全 | ❌ 未覆盖 |
| `BatchSnapshot` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `EventStream` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |

**结论**：已实现的三种策略覆盖充分。`KeyFrameSnapshot`/`DeltaSnapshot` 在协议层已有概念，但缺乏端到端恢复链路测试。`BatchSnapshot` 和 `EventStream` 是大规模场景需求，当前小规模 demo 无需覆盖。

### 2.4 InterestPolicy（兴趣管理策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `AllEntities` | ✅ 已实现 | 所有测试默认（小规模 2 玩家） | ✅ 充分 |
| `OwnerRelevant` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `DistanceAoi` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `TeamOrFactionAoi` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `PriorityBudget` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `LodFrequency` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |

**结论**：整个 `InterestPolicy` 维度除 `AllEntities` 外全为空白。这是 **已知的最大覆盖率缺口**。但这不意味着当前有问题——AOI/LOD/优先级/带宽预算都需要服务端发布侧配合和更大的实体规模才变得有意义。Shooter 2 玩家 demo 规模下 `AllEntities` 是正确的选择。

### 2.5 RecoveryPolicy（恢复策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `CatchUpToServerFrame` | ✅ 已实现 | WorldStartFrameCatchUpCalculator | ✅ 充分 |
| `RequestFullSnapshot` | ✅ 已实现 | Session resync、BattleHandle auto-resync | ✅ 充分 |
| `ReconnectResume` | ✅ 已实现（框架） | 无 Shooter 测试（框架层 FastReconnectSession 存在） | 🟡 框架有但 Shooter 未测 |
| `RequestKeyFrame` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `RequestAoiSlice` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `None` | ⚪ 仅声明 | 无独立测试 | ⚪ 非重点 |

**结论**：两个核心恢复路径（catch-up + full-snapshot）覆盖充分。`ReconnectResume` 框架层已实现但 Shooter demo 未接入，等 FastReconnect 改造完成后自然会有覆盖。

### 2.6 ServerValidationPolicy（服务器验证策略）

| Flag | 框架声明 | Shooter 测试覆盖 | 判定 |
| --- | --- | --- | --- |
| `AuthoritativeOnly` | ✅ 已实现 | AuthoritativeWorld 对比测试 | 🟡 间接覆盖 |
| `InputValidation` | ⚪ 仅声明 | 无（BattleInputFrameScheduler 只有调度，无校验） | ❌ 未覆盖 |
| `LagCompensatedHitValidation` | ⚪ 仅声明 | 无（框架 helper 存在） | ❌ 未覆盖 |
| `ClientHashAudit` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |
| `AntiCheatEnvelope` | ⚪ 仅声明 | 无 | ❌ 未覆盖 |

**结论**：`AuthoritativeOnly` 通过权威世界对比间接验证。其余 flag 属于服务端职责，客户端测试不覆盖是合理的（框架文档 §10.2 🟡 标记确认此分层结论）。

### 2.7 TransportConditionPolicy（传输条件策略）

| 环境 | 参数 | Harness 矩阵 | 独立测试 | 判定 |
| --- | --- | --- | --- | --- |
| Ideal (0ms/0%) | latency=0, jitter=0, loss=0, reorder=0 | ✅ Completion | ✅ NetworkConditioningMiddleware | ✅ 充分 |
| LAN (5ms/0%) | latency=5, jitter=0, loss=0, reorder=0 | ✅ Completion | — | ✅ 充分 |
| Mobile4G (60ms/0.5%) | latency=60, jitter=10, loss=0.005, reorder=0.01 | ✅ Completion | — | ✅ 充分 |
| CrossRegion (150ms/1%) | latency=150, jitter=20, loss=0.01, reorder=0.02 | ✅ Completion | — | ✅ 充分 |
| PoorWifi (80ms/5%) | latency=80, jitter=30, loss=0.05, reorder=0.05 | ✅ Completion | — | ✅ 充分 |
| BandwidthBudget | bandwidthKbps 字段存在但=0(无限) | ❌ 未启用 | ❌ 无 | ❌ 未覆盖 |

**结论**：5 种预设网络环境全部通过 Harness 矩阵覆盖，且 `NetworkConditioningMiddleware` 有独立单元测试验证延迟/丢包/乱序/确定性的底层实现。**带宽预算限制**是唯一缺口，但它是优先级/兴趣管理的前置条件，当前 demo 规模不需要。

---

## 3. 同步模型档案覆盖总览

| 档案 | 框架运行时 | Shooter 测试 | Catalog 声明 | Harness 矩阵 |
| --- | --- | --- | --- | --- |
| `PredictRollback` | ✅ | ✅ 最深覆盖 | ✅ implemented | ✅ 5/5 Completed |
| `AuthoritativeInterpolation` | ✅ | ✅ 控制器+插值框架 | ✅ implemented | ⚠️ 5/5 Unsupported |
| `FastReconnect` | ✅ | ❌ 无 Shooter 测试 | ❌ 未声明 | ❌ |
| `Lockstep` | ❌ | ❌ | ❌ | ❌ |
| `BatchStateSync` | ❌ | ❌ | ❌ | ❌ |
| `MassBattleLodSync` | ❌ | ❌ | ❌ | ❌ |
| `HybridHeroPrediction` | ❌ | ❌ | ❌ | ❌ |
| `ServerRewindLagCompensation` | ❌ | ❌ | ❌ | ❌ |

---

## 4. 框架扩展性评估

### 4.1 能否支持更复杂的真实场景？

基于对当前框架架构的分析，逐场景评估：

#### 场景 A：混合同步（本地预测 + 远端插值，即 HybridHeroPrediction）

- **框架能力**：`ClientPlaybackPolicy` 已区分 `PredictRollback` 与 `AuthoritativeInterpolation`，两者可在同一 session 共存
- **当前缺口**：`ShooterDemoHarnessCarrier.Supports()` 硬绑定单一控制器类型；`ShooterClientSession` 构造时选择一种 controller
- **可行性**：✅ 架构可支持，需要：
  1. 将 session/controller 从"单选同步模型"改为"多实体分层策略"
  2. Carrier 扩展为支持组合能力声明
  3. 本地实体走 `PredictRollback`，远端实体走 `AuthoritativeInterpolation`

#### 场景 B：大规模同步（MassBattleLodSync，AOI + LOD + 批量快照）

- **框架能力**：`InterestPolicy`/`SnapshotPolicy` 已声明所有必要 flag，`NetworkConditionProfile` 有 bandwidth 字段
- **当前缺口**：客户端的 snapshot consumer 假设全量实体；服务端无 AOI 裁剪；无 LOD 频率控制
- **可行性**：🟡 框架维度已划分，但 runtime 全部缺失。需要：
  1. 服务端 AOI 管理器
  2. BatchSnapshot 编码/解码
  3. 客户端按 LOD 频率消费
  4. 带宽预算联动优先级选择

#### 场景 C：快速重连（FastReconnect）

- **框架能力**：`FastReconnectSession` + `SyncTimeAnchor` 已在框架层实现
- **当前缺口**：Shooter demo 未接入；恢复路径只有 full-snapshot，缺 keyframe
- **可行性**：✅ 框架层已就绪，Shooter 接入成本低。已有专门设计文档《Shooter重连接入FastReconnect改造设计》

#### 场景 D：延迟补偿（ServerRewindLagCompensation）

- **框架能力**：lag compensation helper 已存在
- **当前缺口**：服务端实现属于 Orleans 链路，客户端包内仅声明
- **可行性**：✅ 框架设计已预留，属于服务端职责

#### 场景 E：确定性锁步（Lockstep）

- **框架能力**：枚举已声明，工厂抛 NotSupportedException
- **当前缺口**：需要确定性模拟、输入广播、帧等待、hash 校验等全套基础设施
- **可行性**：🟡 需要大幅改造 runtime 为确定性模式，当前 Shooter 物理不是确定性的

### 4.2 架构约束与瓶颈

1. **Carrier 单控制器绑定**：`ShooterDemoHarnessCarrier` 构造函数接收单一 `IShooterClientSyncController`，无法在同一 carrier 内混合 PredictRollback 和 AuthoritativeInterpolation。这是 Harness 矩阵中 `AuthoritativeInterpolation` → `Unsupported` 的根因，也是混合同步的首要架构约束。

2. **Session 的"模式选择"构造**：`ShooterClientSession` 构造函数通过 `syncModel` 参数选择一种 controller，不支持同一 session 内多种 controller 共存。

3. **小规模假设**：所有测试的 `InterestPolicy` 都是 `AllEntities`，没有裁剪逻辑。大规模场景需要补全 AOI 管理器。

4. **`BandwidthKbps=0`**：所有 `NetworkConditionProfile` 预设的带宽字段都是 0（无限），优先级预算从未被触发。

---

## 5. 测试流程合理性与覆盖率评价

### 5.1 当前做得好的方面

1. **分层清晰**：测试分为框架层（`RemoteInterpolationPlayback`、`RemoteSnapshotBuffer`、`NetworkConditioningMiddleware`）和 Shooter 适配层（`*SyncController`、`*Session`），各层独立可测
2. **Harness 矩阵正确实现四态**：`Completed`/`Unsupported`/`Degraded`/`Failed` 四种状态均有对应的测试路径
3. **网络条件覆盖全面**：5 种环境 + 独立 middleware 测试覆盖了延迟/丢包/乱序的核心行为
4. **确定性验证**：replay 测试 + network conditioning 确定性测试保证了可复现性
5. **恢复路径覆盖**：resync、catch-up、full-snapshot 请求都有对应测试
6. **Gateway 协议集成**：输入提交、接受帧反馈、resync 触发、去重都覆盖

### 5.2 已知覆盖缺口（按优先级排序）

| 优先级 | 缺口 | 影响 |
| --- | --- | --- |
| 🔴 高 | `AuthoritativeInterpolation` 在 Harness 矩阵中全部 Unsupported | 插值策略无法通过矩阵回归验证 |
| 🔴 高 | 无 `FastReconnect` Shooter 测试 | 快速重连是最常见的生产需求，demo 未覆盖 |
| 🟡 中 | 无带宽预算测试 | 弱网下的优先级裁剪无法验证 |
| 🟡 中 | 无 `DeltaSnapshot` / `KeyFrameSnapshot` 恢复链路测试 | delta 链断裂时无法覆盖 |
| 🟢 低 | 全部 `InterestPolicy` 除 `AllEntities` 外无覆盖 | 大规模场景的必要基础设施缺失 |
| 🟢 低 | `InputDelayBuffer` / `DeterministicBroadcast` 无覆盖 | Lockstep 模式依赖，暂不需要 |
| 🟢 低 | `ServerValidationPolicy` 无客户端测试 | 属于服务端职责，分层合理 |

### 5.3 各测试流程合理性判断

| 测试类 | 是否合理 | 说明 |
| --- | --- | --- |
| `ShooterAcceptanceLabTests` | ✅ 合理 | Lab 工厂 + Catalog + 矩阵运行三者覆盖完整 |
| `ShooterAcceptanceMatrixSnapshotTests` | ✅ 合理 | 四态快照基线，回归价值高 |
| `ShooterAcceptanceComparisonTests` | ✅ 合理 | 权威世界对比是预测质量的核心验证手段 |
| `ShooterDemoHarnessCarrierTests` | ✅ 合理 | 能力声明 + Unsupported 路径 + 指标暴露 |
| `ShooterClientAuthoritativeInterpolationSyncControllerTests` | ✅ 合理 | 缓冲/插值/饥饿/实体保持覆盖全面 |
| `ShooterClientSyncStrategyContractTests` | ✅ 合理 | 验证框架契约缝 |
| `ShooterClientSessionTests` | ✅ 合理 | 会话生命周期 + Gateway 集成 + 诊断 |
| `ShooterClientBattleHandleTests` | ✅ 合理 | Battle handle 编排 + 自动 resync + 去重 |
| `ShooterPlaySessionRunnerTests` | ✅ 合理 | Play mode 运行器 + 选项归一化 |
| `RemoteInterpolationPlaybackTests` | ✅ 合理 | 框架层插值管线独立验证 |
| `RemoteSnapshotInterpolationTests` | ✅ 合理 | 缓冲/时间线/角度插值全面 |
| `ClientPredictionInputHistoryTests` | ✅ 合理 | 预测回滚的前置输入记录 |
| `ClientPredictionReconciliationCoordinatorTests` | ✅ 合理 | 完整对账流程的单测 |
| `CommandRollbackLogTests` | ✅ 合理 | 回滚日志 + RollbackCoordinator 集成 |
| `NetworkConditioningMiddlewareTests` | ✅ 合理 | 网络模拟底层验证 |
| `BattleInputFrameSchedulerTests` | ✅ 合理 | 服务端输入调度 |
| `WorldStartFrameCatchUpCalculatorTests` | ✅ 合理 | Late join 追帧计算 |
| `ShooterDeterministicReplayTests` | ✅ 合理 | 确定性基线 |
| `ShooterPackedSnapshotRuntimeTests` | ✅ 合理 | 序列化往返 + 线路协议 |
| `ShooterPackedSnapshotSyncControllerTests` | ✅ 合理 | 快照导入覆写 |
| `ShooterWorldModuleTests` | ✅ 合理 | World DI 注册验证 |

---

## 6. 与框架能力矩阵文档的对齐程度

比对 §10 验收快照的五问清单：

| 问题 | PredictRollback | AuthoritativeInterpolation | 评估 |
| --- | --- | --- | --- |
| 属于哪类策略？ | ✅ ClientPlayback | ✅ ClientPlayback | 正确分层 |
| 是否与已有能力互斥？ | ✅ 非互斥（与插值可共存） | ✅ 非互斥 | 正确建模 |
| 是否需要 C/S 同时实现？ | ✅ 已识别 | ✅ 已识别 | ServerValidation 留给 Orleans |
| 是否依赖统一时间锚点？ | ✅ SyncTimeAnchor | ✅ InterpolationTimeline | 时间族统一 |
| 失败/降级路径？ | ✅ 四态 + Reconciliation | ✅ 饥饿诊断 | 可观测 |

**结论**：Shooter 测试与框架能力矩阵的 §10 验收快照完全对齐，两个已实现档案均通过五问审查。

---

## 7. 改进建议（按优先级）

### 7.1 短期（低成本高收益）

1. **为 `AuthoritativeInterpolation` 新增独立 Harness Carrier**：新建一个只绑定 `ShooterClientAuthoritativeInterpolationSyncController` 的 Carrier，使插值策略也能走 Harness 矩阵验证，当前 5×Unsupported 变成 5×Completed
2. **接入 `FastReconnectSession`**：按《Shooter重连接入FastReconnect改造设计》将框架层已有的快速重连能力接入 Shooter demo，并补测试

### 7.2 中期（需要一定工程量）

3. **`DeltaSnapshot` 导入语义补全**：协议层已有 delta snapshot 概念，补全 import 语义和恢复链路测试（delta → keyframe 恢复）
4. **带宽预算测试**：给至少一个 `NetworkConditionProfile` 设置非零 `BandwidthKbps`，验证弱网下的行为
5. **混合同步 Carrier**：扩展 Carrier 支持"本地实体 PredictRollback + 远端实体 AuthoritativeInterpolation"的组合能力声明

### 7.3 长期（新能力实现后）

6. **`BatchStateSync` / `MassBattleLodSync` 的客户端消费端**
7. **`InterestPolicy` AOI 管理器**
8. **`Lockstep` 确定性模拟**

---

## 8. 总结

**当前 Shooter 网络同步测试流程总体合理且充分**。21 个测试类覆盖了从框架底层（插值管线、网络条件模拟、回滚日志）到 Shooter 适配层（控制器、会话、Gateway 集成、验收矩阵）的完整链路。测试在 PredictRollback 和 AuthoritativeInterpolation 两个已实现档案上覆盖深度足够。

**核心缺口不是测试数量不足，而是框架已声明的能力未被 Shooter demo 接入**：
- `FastReconnect` 框架 runtime 已有，Shooter 未接入
- `AuthoritativeInterpolation` 控制器测试充分，但 Harness 矩阵因 Carrier 单绑定而全部 Unsupported
- `DeltaSnapshot`/`KeyFrameSnapshot` 协议已有概念，恢复链路未补全
- `InterestPolicy`/`BandwidthBudget` 基础结构已声明，无消费方

框架本身的设计**可以支撑真实复杂场景**：Policy 维度正交、时间锚点统一、四态诊断完善、能力声明机制到位。后续工作应聚焦于"把已声明的能力接入 Shooter demo 并在矩阵中验证"，而非重新设计框架。
