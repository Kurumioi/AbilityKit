# Shooter 多人网络同步优化与扩展路线图

> 状态：P0/P1 核心闭环已完成（含 TEST-01B、PERF-01、分层 CI 与 Unity PlayMode）；长稳、完整故障矩阵和 Editor 分析工具继续迭代
>
> 基线日期：2026-07-16
>
> Source commit：`2e9f2d39`（基线运行时工作区包含用户既有未提交改动，任务不回退这些改动）
>
> 原则：以当前源码和测试为事实源。旧阶段文档只用于理解历史，不把已落地能力重新列为待实现。

## 1. 结论

Shooter 已具备预测回滚、权威插值、混合播放、Packed/PureState 快照、baseline/delta 恢复、AOI 生命周期、LOD/预算、快速重连、统一时间、延迟补偿、弱网模拟、DemoHarness 和多进程 smoke。下一阶段不应继续增加互斥“同步模式”，也不应新建一套平行网络框架。

优化重点应从“能力是否存在”转向四个生产化问题：

1. 稳定跨客户端、服务端和传输层的同步流契约。
2. 把静态带宽配置升级为有队列、背压、降级和指标反馈的发布闭环。
3. 把分散诊断升级为可聚合、可导出、可比较的同步健康工具链。
4. 把单元测试和 smoke 升级为协议兼容、性能预算、故障矩阵和 CI 门禁。

## 2. 当前真实基线

| 能力 | 当前状态 | 证据类型 | 后续定位 |
| --- | --- | --- | --- |
| PredictRollback | 已落地 | 客户端控制器、对账和输入历史测试 | 稳定契约，优化分配和诊断 |
| AuthoritativeInterpolation | 已落地 | 插值控制器、播放缓冲和饥饿测试 | 补自适应延迟与质量指标 |
| Hybrid 本地预测/远端插值 | 已落地 | 专用控制器、Carrier、Harness 场景 | 不再作为待实现模式 |
| Packed full/delta | 已落地 | 导入、despawn、metadata 测试 | 补版本兼容和基线生命周期契约 |
| PureState baseline/delta | 已落地 | 客户端应用、缺失/错配 resync 测试 | 抽取通用流状态机，避免 Shooter 私有编排扩散 |
| AOI enter/stay/leave | 已落地 | `AoiInterestSet`、运行时和 observer 发布测试 | 补索引实现、抖动指标和大规模压测 |
| LOD/优先级/实体预算 | 已落地基础 | PureState exporter 和有限带宽测试 | 升级为实时字节预算和反馈控制 |
| FastReconnect | 已落地 | 框架 session/driver 和 Shooter 集成测试 | 补多进程长断线、token 失效和重复恢复故障矩阵 |
| Lag compensation | 已落地基础 | rewind service、Shooter adapter 和健康事件测试 | 补服务端权威 E2E、历史容量和滥用边界 |
| 统一时间 | 已落地 | anchor、clock estimator、bridge | 增加漂移分布、重锚和异常时钟测试 |
| FrameRecord | 已具备记录、播放和首分歧分析 | smoke 产物、codec、diff library、CLI | 后续补二进制裁剪和 Unity Editor 对照视图 |
| 多进程 smoke | 已覆盖主链路、并行隔离与真实故障恢复矩阵 | late join、终局、重连、弱网、PureState、recoverable retry、Gateway offline、slow-consumer、三轮周期断线、回放/diff、SyncHealth、版本化 manifest、独立 run 目录与完整端口组 | 继续扩展 grain reactivation、长稳与容量矩阵 |

说明：`BatchStateSync`、`MassBattleLodSync` 和 `HybridHeroPrediction` 在当前 Harness 中已有专用 carrier 与 Completed 场景。它们是能力档案或组合载体，不应再被解释成需要独立底层架构的模式。

### 2.1 Phase 0 测试基线

| 项目 | 命令 | 结果 |
| --- | --- | --- |
| Network runtime | `dotnet test src\\AbilityKit.Network.Runtime.Tests\\AbilityKit.Network.Runtime.Tests.csproj --no-restore --nologo` | 94 passed，0 failed，0 skipped |
| Shooter runtime | `dotnet test src\\AbilityKit.Demo.Shooter.Runtime.Tests\\AbilityKit.Demo.Shooter.Runtime.Tests.csproj --no-restore --nologo` | 初始 327 passed、1 failed；失败已分类并修正测试口径 |
| High-density targeted regression | 同一 Shooter 项目，filter 为 `ExplicitHighDensityPlayModeScenarioCanDemonstrateThousandsOfEnemies` | 1 passed，0 failed |

初始失败不是敌人生成或同步能力退化。8192-enemy 场景会让权威世界在三秒内达到数千 active enemies，但高密度 PlayMode 表现链路按设计切换到 PureState，并受默认 512 active sync budget 限制；旧测试错误地用客户端投影工作集证明权威世界规模。修正后的测试分别断言权威运行时达到至少 2048 enemies，以及客户端投影不超过 active sync budget。

Shooter 构建仍有既有 NuGet compatibility、XML documentation 和 nullable warning。它们不影响本次基线通过，也不在同步契约工作包中顺带清理。

### 2.2 SYNC-01 完成快照

- 新增玩法无关 snapshot stream envelope/state machine，结构化处理 full baseline、delta、duplicate、stale、gap、missing baseline、baseline mismatch、world changed 和 unsupported version。
- 保持两阶段 validate/commit；payload 应用成功前不会推进流状态。gap 作为质量信号返回，不强制 full baseline，保持现有 PureState 跨丢包应用语义。
- ignored 元数据与 recovery 元数据独立维护，后续 stale/duplicate 不会覆盖此前 baseline mismatch/world change 的恢复帧和 hash。
- Shooter PureState controller 已迁移为 wire adapter；现有 payload 字段、MemoryPack 顺序、payload version 和 opcode 均未修改。当前 wire 无独立 sequence，adapter 暂以 frame 同时提供 sequence/frame。
- unsupported version 返回结构化 Shooter 结果，记录 `SnapshotDropped`，不触发无意义的 full-baseline resync，也不被 facade/聚合结果误判为已应用。
- 完整回归：Network runtime 102 passed、0 failed、0 skipped；Shooter runtime 330 passed、0 failed、0 skipped（包含 PureState wire round-trip）。

### 2.3 SYNC-02 完成快照

- 新增通用 `ISyncHealthEventSink`、固定容量 `SyncHealthEventBuffer`、`SyncHealthEventAggregator` 与可由 `System.Text.Json` 直接序列化的 `SyncHealthReport`。
- ring buffer 仅保留最新事件并统计覆盖次数，累计汇总不因覆盖丢失；`SyncHealthEvent.None` 不进入保留区，并计入 ignored 数量。
- 报告统一提供 event 总数、severity 计数、按 kind 汇总和 retained event snapshot；发布路径保持有界，仅显式导出报告时创建数组快照。
- DemoHarness 复用统一聚合器，同时保留原有三个 health 计数字段；FastReconnect 临时事件收集改为固定容量缓冲。
- Shooter 核心复用稳定的动态组合视图，重复读取最近 health events 不再创建合并数组；最近一次操作批次与会话级累计报告继续保持不同语义。
- 保持现有 `SyncHealthEvent` envelope 兼容，未在本工作包加入 world/session/observer/ticks/correlation id；这些上下文字段需结合跨层协议和 artifact 关联契约单独推进。
- 完整回归：Network runtime 105 passed、0 failed、0 skipped；Shooter runtime 330 passed、0 failed、0 skipped。

### 2.4 SYNC-03 完成快照

- Shooter protocol 新增统一 schema compatibility policy：Packed 支持 previous/current 版本 2..3，PureState 支持当前版本 1..1，并结构化区分 compatible、unsupported old 和 unsupported future。
- Packed 客户端在 stale 检查、importer 和 runtime/presentation mutation 前执行版本门禁；PureState 复用通用 snapshot stream 版本范围。拒绝结果统一映射为 `UnsupportedVersion`，不误判为 `ImportFailed`，也不触发无意义 full snapshot resync。
- Packed/PureState codec 对非空 wire payload 保留原始版本，确保 old/future 门禁不可被默认版本归一化绕过；空 payload 继续使用当前版本的 `Empty()` 语义。
- 保存 Packed/PureState 两份完整 `WireStateSyncSnapshotPush` Base64 golden fixtures；测试同时验证当前编码逐字节匹配和固定字节反向解码，覆盖 Gateway 外层字段、opcode 与内层 schema。
- 兼容矩阵覆盖 Packed previous/current/old/future 与 PureState current/old/future；pipeline 测试证明 Packed previous 可应用，old/future 在运行时状态修改前拒绝。
- 未改变 snapshot opcode、MemoryPack 字段顺序或 Gateway subscription wire。
- 完整回归：Network runtime 105 passed、0 failed、0 skipped；Shooter runtime 344 passed、0 failed、0 skipped。

### 2.5 TOOL-01 完成快照

- record 包新增玩法无关的 FrameRecord diff API，按 `world` 和 `(frame, ordinal)` 对齐 state-hash track；同帧多 hash 不会被覆盖，同时比较 version 与 hash，并结构化报告 unequal、left missing 和 right missing。
- 首分歧报告提供前后 N 帧有界上下文，汇总两侧 input、state hash、snapshot metadata、payload bytes 和小写 SHA-256；不复制 Base64 payload。上下文边界使用饱和运算，覆盖 `int.MaxValue` 窗口。
- 新增独立 CLI：`AbilityKit.Record.Tools diff <left.record.bin> <right.record.bin> [--context N] [--output report.json] [--indented]`。stdout 仅输出 schema version 1 的 camelCase JSON；退出码 0 表示一致，1 表示有效比较但不一致，2 表示参数或加载/执行错误。
- library 和 CLI 只消费 `FrameRecordFile` 与现有 optimized binary codec，不解析 Shooter payload，不修改 FrameRecord 容器格式，也不引入 Unity 大型调试窗口。
- 当前最小复现交付为首分歧附近的有界 JSON 上下文；裁剪并写出新的 `.record.bin` 尚未实现，留作后续独立能力，路线图不将其误记为已完成。
- 完整回归：Record 9 passed、0 failed；Network runtime 110 passed、0 failed；Shooter runtime 344 passed、0 failed；CLI build 0 warnings、0 errors。

### 2.6 P0/P1 收口快照（2026-07-18）

- 输入链路已有双层防护：Gateway 校验会话、battle/player 所有权、payload/opcode、未来帧、sequence replay window、token bucket 和跟踪容量；Battle Grain 再执行玩家级 sequence、速率和容量 admission。Gateway 9/9、Grain 相关聚焦回归 34/34 通过。
- 可靠事件已与可替换 snapshot delta 分离，提供 epoch/sequence/event id、observer cursor、ack、retention trim、replay 和 retention gap；observer 使用独立有界可靠事件队列。客户端 authoritative reconciliation、pending input replay 与 fast reconnect 聚焦回归 25/25 通过。
- observer send budget 已通过 `AbilityKit:StateSyncObserver` 配置绑定并在启动时校验。Gateway 与 Battle Grain 输入安全阈值已统一绑定 `AbilityKit:BattleInputSecurity`，共享 payload/opcode、replay window、token bucket、跟踪容量和 Gateway idle TTL 默认值；非法非正值在组合根启动/解析 options 时拒绝。Gateway guard 保持 singleton 以维持跨请求 admission 状态，Battle Grain activation 保存配置快照以避免 battle 生命周期中策略漂移。
- 输入安全配置化聚焦回归：Gateway 14/14、Grains 18/18 通过；Orleans Host 与 ShooterSmoke 均构建 0 error，三份基础 appsettings JSON 解析通过。ShooterSmoke 自有 Gateway 组合根从共享 options 注册 singleton guard，确保 source-generated `SubmitBattleInputHandler` 可完整解析；部署 profile 与环境变量可覆盖基础默认值。
- TEST-01B minimal `recoverable-retry` 通过（run id：`test01b-20260718-input-security-config-retry`）：真实 ShooterSmoke Silo、TCP Gateway、generated handler registry 与 Battle Grain activation 均完成启动和调用；3 次注入失败、4 次 retry、1 次 reconnect，两个客户端最终收敛；两份 FrameRecord diff 均为 `Identical`，SyncHealth 无 warning/critical，三个专用端口均完成清理。
- TEST-01B `gateway-offline` 通过（run id：`test01b-20260719-gateway-offline-gated-r2`）：首个 join 客户端在 initial sync 和 3 次输入确认后进入有界 reconnect gate，runner 再停止 TCP transport、等待 offline ack 并硬探测 `127.0.0.1:42451` 不可达；Gateway 恢复监听后释放客户端执行 1 次真实 reconnect。offline/online fault timeline 均为 completed，两客户端 FrameRecord diff 均为 `Identical`，bounded convergence 通过，critical 为 0；join 的 2 个 warning 是预期断线/恢复事件。server/create/join 均以 0 退出，TCP/Silo/Orleans Gateway 三个专用端口均确认释放。
- TEST-01B `slow-consumer + pure-state + replay` 通过（run id：`test01b-20260719-slow-consumer-pure-state-replay-r12`）：保持 operation 30 秒、startup/setup 60 秒、scenario 45 秒、convergence 20 秒，以及 256 B/s、32768 burst、queue length 1、queue age 100 ms、drain 250 ms 的压力配置。r11 暴露的 `BothStateHashesMissing` 根因是结束时把最后收到的 stale/ignored push 当作 pure-state 收敛证据；修复仅在 callback 内保存最近一次成功应用、同帧、双方非零的 comparable hash，未修改 wire、codec、diff 规则、timeout 或安全边界，聚焦回归 9/9 通过。r12 create/join authoritative diff 均为 `Identical`；服务端结构化指标记录 dropped 255890/65424、coalesced 180875/84506、baseline invalidation 100/24，客户端应用 full baseline 8/6 次；两端 reliable cursor 均为 433、gap 为 0、`needsResync=false`，完整与 minimized replay 均被消费，health 无 warning/critical，所有进程以 0 退出且三个端口释放。
- TEST-01B `reconnect-cycles + pure-state + replay` 通过（run id：`test01b-20260719-reconnect-cycles-pure-state-replay-r3`）：join 客户端连续执行 3 次真实连接关闭，并逐轮通过正式 join/ready/start/subscribe 流程恢复；每轮入口均为 `Reconnect`，且都有新的成功应用 snapshot push，最终 push 从 5 前进到 9。r2 证明重复 full baseline 在非 slow-consumer 场景同样是合法 PureState 推进：hash 样本持续匹配、pending resync 已清除且 authoritative diff 已为 `Identical`；runner 因场景特判误拒绝该结果。修复将推进证据统一为 delta、resync 或重复 full baseline，仍独立要求首个 full baseline、`needsResync=false`、pending baseline 清除和 authoritative diff 收敛，未增加 timeout、sleep 或降低压力。r3 create/join diff 均为 `Identical`，两端 reliable epoch 一致、cursor 有效且无需 resync，四份完整/minimized replay 均存在并被消费；server/create/join 均以 0 退出，三个专用端口释放，聚焦契约回归 11/11 与 PowerShell AST 校验通过。
- 多进程 runner 的冷构建、setup、场景执行和 matrix global timeout 已拆分；父级超时按本场景三个端口清理派生服务，并将 running 子 manifest 与 matrix manifest 原子收口为 failed。Gateway offline 使用客户端进度行和文件式 release gate 确定故障时序，不依赖固定 sleep；脚本契约增量编译后 11/11 通过。
- PERF-01 AOI/LOD smoke 阈值门禁通过。Network snapshot queue/stream/baseline validator 聚焦回归 23/23，FrameRecord diff/tool 10/10 通过。
- 分层门禁契约 79/79 通过；workflow 已包含 fast、integration、Unity PlayMode、multiprocess、performance smoke/full。Unity 2022.3.62f1 PlayMode 实际门禁 1/1 通过。
- 仍保留的验证边界：父 global timeout 在子 manifest 已进入 running 后的动态强杀路径目前由源码契约覆盖；单 Observer slow-consumer 和三轮周期断线已完成真实验证，grain reactivation、多 observer 长稳和完整性能矩阵仍需独立持续验证。

### 2.7 基线治理规则

- 测试数量只记录在带日期和 source commit 的基线快照中，不作为长期能力说明。
- 能力存在性以源码、可执行测试和最近 smoke artifact 为准。
- 旧分析必须显式标记历史时点；后续不按旧缺口重复实现 Hybrid、FastReconnect、AOI、PureState 或 lag compensation。
- 高密度验收区分权威模拟规模与客户端同步工作集，避免把 AOI/预算限流误判为实体生成失败。

## 3. 基础模块边界

### 3.1 下沉到通用网络运行时

以下能力应由 `com.abilitykit.network.runtime` 持有：

- 同步档案、policy、兼容性检查和 controller registry。
- 远端样本缓冲、插值时间线、时钟估计和时间锚。
- baseline/delta 流验证、恢复请求和流状态机，但不感知 Shooter payload。
- 发布预算、队列水位、丢弃/合并决策和背压结果模型。
- `SyncHealthEvent` 的聚合、窗口统计和统一导出模型。
- 网络条件模拟及可复现随机种子。
- 可复用的 lag compensation 历史窗口与判定原语。

通用层不应依赖 Shooter entity kind、opcode、Svelto 查询或表现层对象。

### 3.2 保留在协议与记录包

- Shooter wire schema、opcode 和 payload codec 留在 Shooter protocol 包。
- FrameRecord 容器、codec、索引、diff 基础算法和最小化接口留在 record 包。
- 协议包显式维护 schema/version、兼容窗口和未知字段策略。

### 3.3 保留在 Shooter 层

- 玩家、投射物、敌人等实体到快照字段的映射。
- 本地受控实体与远端实体的播放分流。
- PureState 优先级、实体层级和 gameplay-specific AOI scope。
- Shooter state hash、snapshot exporter/importer 和表现投影。
- Shooter 的恢复原因映射、HUD 和验收模板。

### 3.4 保留在 Orleans 层

- Battle tick、observer 生命周期、按 observer 发布和 Gateway 投递。
- 每个 observer 的 baseline/AOI 会话状态。
- 服务端发送队列、重试/清理策略和生产指标。
- Shooter adapter 负责构造 payload；通用 Battle host 不解析 Shooter 数据。

## 4. 真实缺口与风险

### 4.1 P0：同步流契约仍分散

Packed、PureState、插值和恢复链路已有实现，但 baseline 身份、序列、世界标识、快照种类、应用结果和恢复原因仍分散在 Shooter controller、Gateway push 和 Orleans observer 状态中。需要统一“快照流 envelope + 消费状态机 + 恢复动作”契约，并允许玩法提供 payload codec。

验收标准：

- stale、duplicate、gap、baseline mismatch、world changed 和 unsupported version 都返回结构化结果。
- full/keyframe/delta 的状态转换由框架测试覆盖。
- Shooter Packed/PureState 只实现 adapter，不重复实现流顺序规则。
- 不破坏现有 opcode 和兼容入口。

### 4.2 P0：实际发送预算闭环已落地，容量阈值仍待验证

SYNC-04 已在每个 Orleans observer 的实际序列化字节边界引入 token-bucket、bounded queue、最大队列年龄、优先级、delta 替换/丢弃和 baseline 恢复。通用策略归属 Network Runtime，Orleans observer 只负责 wire 序列化、队列所有权、Gateway 投递和恢复编排。

后续验收标准：

- 通过部署配置覆盖 bytes/sec、burst、最大队列年龄、最大队列长度和 drain interval；当前先使用内部默认 policy。
- 用独立可靠事件流验证 despawn/关键事件语义，不把它们伪装成可替换状态 delta。
- 在 128 Kbps profile 下证明预算上限、本地玩家/关键对象可见性和最终状态收敛。
- 建立多 observer 容量测试，报告 queue age、baseline age、resync count 和 recovery latency 分布。

### 4.3 P0：统一诊断基础已落地，跨层上下文与 artifact 仍待闭环

SYNC-02 已建立统一 sink、有界 ring buffer、按 kind/severity 聚合、JSON 可序列化报告和 DemoHarness 接入，并清理 Shooter getter 的重复数组合并。剩余缺口集中在跨 client、transport、server push 的上下文关联、窗口分位数和 smoke artifact 输出。

后续验收标准：

- client、transport、snapshot consumer、recovery 和 server push 使用兼容的事件 envelope 或明确的 adapter。
- 在不破坏现有 `SyncHealthEvent` API 的前提下定义 world/session/observer/ticks/correlation id 上下文契约。
- 扩展 JSON 汇总，提供 p50/p95 frame gap、starvation、rollback、resync、drop 和 queue age。
- Unity HUD 和 smoke 消费与 DemoHarness 相同的聚合报告，不各自拼字段。

### 4.4 P0：协议同步兼容门禁已落地，订阅协商仍待跨层设计

SYNC-03 已为 Packed/PureState 建立首包兼容门禁、明确拒绝原因、向后兼容窗口和完整 Gateway push golden fixtures。Gateway subscription wire 暂未扩展版本协商；如后续需要订阅阶段拒绝，应结合客户端、Gateway 和 Orleans 的跨层协商契约单独推进。

验收标准：

- 建立 Shooter sync schema compatibility policy。
- 覆盖 current/current、current/previous、unknown future 和 unsupported old 四类测试。
- 保存 Packed/PureState golden payload fixture，CI 检测无意 wire 变化。
- 不兼容必须在订阅或首包阶段结构化失败，不进入无限 resync。

### 4.5 P1：FrameRecord 分析基础已落地，二进制裁剪与 Editor 对照仍待补齐

TOOL-01 已补齐玩法无关 diff library、首分歧定位、有界上下文、payload 摘要和机器可读 JSON CLI。两份记录按 world 与 `(frame, ordinal)` 对齐 state hash，不依赖 Shooter payload 解析。

后续验收标准：

- 从失败记录裁剪并写出最小 `.record.bin`，保留原始 meta/schema 和首分歧所需输入/快照。
- Unity Editor 消费同一 JSON/report API，提供时间线、跳帧和双记录对照，不另建平行分析逻辑。
- smoke manifest 可直接关联左右记录、diff report 和日志 correlation id。

### 4.6 P1：AOI/LOD 需要规模和稳定性验证

现有功能测试证明语义正确，但尚不足以证明大量实体和大量 observer 下的复杂度、内存、边界抖动与公平性。

验收标准：

- 建立 100/1K/10K entity 与 1/16/64 observer benchmark profile。
- 比较全扫描与空间索引的 CPU、allocation、payload bytes。
- 统计 enter/leave churn、预算饿死时间和高优先级实体漏发。
- 只有 benchmark 证明必要时再抽通用空间索引，不提前泛化 Shooter scope。

### 4.7 P1：observer 投递语义已落地，真实生命周期矩阵仍待补齐

SYNC-04 已提供 accepted、queued、dropped-stale、backpressured、offline 和 failed 结构化结果；Gateway offline 会清理队列并取消订阅，临时异常会丢弃已取出的快照、计入 resync 并请求新 baseline，不重放可能失效的 delta。重复订阅、切换 battle、取消订阅和 deactivate 都会清理 live queue/baseline 状态。

后续验收标准：

- 使用 Orleans test cluster 验证 deactivate/reactivate、timer interleaving、Gateway offline 和 battle switch。
- 验证持续临时失败下 baseline 请求被合并，恢复后不会形成请求风暴。
- 验证 observer 移除同步释放 AOI、baseline 和队列状态。
- 将慢消费者、周期断线和重连加入多进程 smoke 故障矩阵。

### 4.8 P2：高级扩展

- 自适应插值延迟：根据 jitter、starvation 和 buffer occupancy 调节，不默认开启外推。
- 客户端 hash audit：抽样上报并由服务端关联 FrameRecord，不把客户端 hash 当权威。
- 反作弊 envelope：输入速率、帧窗口、速度和技能约束属于玩法/服务端规则。
- AOI slice recovery：仅在 full baseline 体积和恢复时延数据证明必要后实现。
- 事件流与状态流分层：命中、死亡等可靠事件是否独立发布，需要先定义幂等和顺序语义。
- 长稳和容量测试：30 分钟、多 observer、周期断线、网络 profile 动态切换。

Lockstep 不进入本路线图。Shooter 当前模拟并未以跨平台确定性锁步为目标，不能因为兼容枚举存在就扩张范围。

## 5. 分阶段执行

### Phase 0：基线冻结与文档治理（已完成）

1. [x] 生成当前能力/测试清单，标记 source commit、测试项目和日期。
2. [x] 修复多进程 smoke 计划的编码损坏。
3. [x] 给旧测试覆盖分析加“历史快照”标记，并列出已经关闭的缺口。
4. [x] 规定路线图只引用源码、测试和最新验收结果，不手写长期测试数量。

完成结果：当前基线、测试口径与历史文档边界已经明确，不再按旧文档重复实现 Hybrid、FastReconnect、AOI 或 PureState client consumption。

### Phase 1：生产化基础契约

1. [x] 设计并落地通用 snapshot stream envelope/state machine，并迁移 Shooter PureState adapter。
2. [x] 建立协议版本兼容 policy 和 golden fixtures。
3. [x] 建立统一 sync health sink、ring buffer、aggregator 和 JSON report。
4. [x] 清理 Shooter controller 内重复的 health-event merge 与 last-state 投影。

SYNC-01 完成结果：通用流顺序、baseline identity、world/version rejection 和两阶段提交已有框架测试；Shooter 只负责 payload-to-envelope 与结果映射。

SYNC-02 完成结果：通用层已有有界事件保留、完整历史聚合和标准 JSON 报告模型；DemoHarness 与 FastReconnect 已接入，Shooter 最近事件 getter 不再重复分配合并数组。

SYNC-03 完成结果：Shooter Packed/PureState 已有明确版本窗口、首包结构化拒绝、完整 Gateway push golden fixtures 和 current/previous/future/old 兼容矩阵；不兼容 payload 不进入 importer 或运行时状态修改。

完成定义：Packed/PureState 在同一顺序、版本、恢复和诊断框架下运行，现有行为保持兼容。

### Phase 2：服务端发布闭环（SYNC-04 已完成，集成验收待推进）

1. [x] 引入 observer send budget 和 bounded queue。
2. [x] 定义 snapshot priority、merge/drop、baseline invalidation 和 retry policy。
3. [x] 输出 Orleans/Gateway observer 指标。
4. [-] 扩展有限带宽、慢消费者、断线和 grain reactivation 集成测试（单 Observer slow-consumer 与三轮周期断线已完成，grain reactivation 待补）。

SYNC-04 完成结果：Network Runtime 持有玩法无关的 token-bucket 与 bounded queue；Orleans observer 按实际 MemoryPack payload bytes 排队并定时投递，暴露 produced/sent/dropped/merged bytes、queue/baseline age 和 resync count。Battle host 观察所有 enqueue task，Gateway offline、临时失败和 baseline 失效都有明确清理或恢复语义。

剩余完成定义：在真实弱网和多 observer 条件下证明实际字节预算、队列年龄、恢复时延和最终收敛满足阈值。

### Phase 3：调试与验收工具

1. [x] 实现 FrameRecord diff、first-divergence 和 JSON CLI；二进制记录裁剪后续独立补齐。
2. [ ] Unity Editor 接入分析结果，提供时间线、跳帧和双记录对照。
3. [x] 多进程 smoke 写入版本化运行 manifest，关联场景、网络条件、端口、room、records、diff、同步健康 summary、correlation id 和日志 artifact。
4. [x] 参数化 artifact root、run id 与 TCP/Silo/Orleans Gateway 完整端口组；run 目录拒绝复用，清理仅作用于本轮端口和已记录 PID。

TEST-01A 完成结果：脚本契约与 replay summary 测试通过；使用独立端口组和 artifact root 的真实最小多进程 smoke 通过，终态 manifest 正确记录 `passed`、room id 和日志 inventory。

TEST-01B 完成结果：minimal `recoverable-retry`、真实 `gateway-offline`、`slow-consumer + pure-state + replay` 与三轮 `reconnect-cycles + pure-state + replay` 故障场景通过；注入失败、retry、transport stop/start acknowledgement、离线端口硬探测、有界阶段 gate、逐轮正式 Reconnect 入口与 snapshot push 前进、结构化 observer delivery metrics、baseline 恢复、reliable cursor、最终收敛、SyncHealth、FrameRecord replay/diff、进程退出和端口清理均进入 manifest。slow-consumer 首个真实失败已通过 callback 时点的 comparable hash 证据修复关闭；周期断线移除了重复 full baseline 仅限 slow-consumer 的错误场景耦合，同时保留独立 hash、pending baseline、reliable 和 authoritative diff 门禁。runner timeout 失败也统一产出 failed scenario/matrix manifest。后续继续扩展 grain reactivation 和 running-manifest 动态强杀矩阵。

### Phase 4：性能与 CI 门禁

1. [x] 建立 fast、integration、Unity PlayMode、multiprocess、performance 分层门禁。
2. [x] PR 运行 fast、integration、Unity PlayMode 与 performance smoke，并保留 always-upload artifact。
3. [x] 主分支具备多进程 TEST-01B 和性能门禁入口，故障运行使用 non-cancelling concurrency group。
4. [ ] 定时任务继续扩展 AOI/LOD full、长稳、多 observer 和 allocation 趋势预算。
5. [x] 性能 smoke 使用明确阈值与机器可读报告，不使用无意义 allocation 非负断言。

完成定义：功能、协议、恢复、带宽和性能回归都有明确 owner、命令、artifact 和失败阈值。

## 6. 推荐工作包

| ID | 工作包 | 优先级 | 主要归属 | 前置 | 关键产物 |
| --- | --- | --- | --- | --- | --- |
| SYNC-01（已完成） | Snapshot stream contract/state machine | P0 | network.runtime | 无 | API、迁移 adapter、状态转换测试 |
| SYNC-02（已完成） | Sync health aggregation/report | P0 | network.runtime + Shooter view | SYNC-01 可并行 | ring buffer、summary JSON、Harness adapter |
| SYNC-03（已完成） | Shooter protocol compatibility gate | P0 | protocol + Gateway | 无 | policy、golden fixtures、兼容测试 |
| SYNC-04（已完成） | Observer send budget/backpressure | P0 | network.runtime + Orleans battle/Gateway | SYNC-01 | bounded queue、投递结果、metrics、恢复编排测试 |
| TOOL-01（已完成） | FrameRecord diff/first divergence | P1 | record | 无 | library + CLI + JSON schema |
| TOOL-02 | Shooter replay inspector integration | P1 | Shooter editor/view | TOOL-01 | 双记录时间线和跳帧 |
| TEST-01A（已完成） | Multiprocess manifest and artifact isolation | P1 | ShooterSmoke/tools | SYNC-02 | 版本化 manifest、独立 run 目录、完整端口组、脚本契约测试 |
| TEST-01B（minimal + Gateway offline + slow-consumer + reconnect-cycles 已完成） | Multiprocess fault matrix | P1 | ShooterSmoke/tools | TEST-01A、SYNC-04 | recoverable retry、真实 transport outage、PureState 背压/baseline/reliable/replay、三轮周期断线、恢复收敛、manifest/diff/health；后续扩展 grain reactivation 与运行中强杀 |
| PERF-01（smoke 已完成） | AOI/LOD scale benchmark | P1 | Shooter runtime + Orleans tests | SYNC-04 | benchmark profiles、阈值与 JSON report；后续扩展 full/长稳 |
| TEST-02（已完成） | CI layered gates | P1 | workflows | TEST-01 | PR/main/scheduled jobs、Unity PlayMode、always-upload artifacts |
| SYNC-05 | Adaptive interpolation delay | P2 | network.runtime | SYNC-02 | 可选策略、质量对照报告 |

SYNC-01、SYNC-02、SYNC-03、SYNC-04、TOOL-01、TEST-01A、TEST-01B minimal + Gateway offline + slow-consumer + reconnect-cycles、PERF-01 smoke、TEST-02 与输入安全阈值部署配置化已完成。下一步按风险推进 grain reactivation/运行中强杀故障矩阵和多 observer 长稳。Unity Editor 后续消费 TOOL-01 的 report API 和 JSON，不另建事实源。

## 7. 验收矩阵

每个基础模块至少覆盖以下层级：

| 层级 | 必须证明 |
| --- | --- |
| 纯 C# 单测 | 状态转换、边界、确定性、无效输入和 reset |
| 协议测试 | wire round-trip、golden fixture、版本窗口 |
| Shooter 集成 | Packed/PureState/Hybrid 与恢复路径实际消费 |
| Orleans 集成 | observer-specific baseline、AOI、队列和投递结果 |
| 多进程 smoke | create/join/late join/reconnect/weak network/final convergence |
| 工具验证 | 失败 artifact 可加载、可 diff、可定位首分歧 |
| 性能验证 | CPU、allocation、bytes/sec、queue age、payload size |

建议固定两类核心结果：

- 正确性：最终 frame/hash/match result 一致，且不存在未处理 resync、错误 baseline 或版本降级。
- 质量：rollback、starvation、drop、queue age、payload bytes 和 recovery latency 在 profile 阈值内。

## 8. 文档一致性处理

- `plans/shooter-sync-test-coverage-analysis.md` 保留为 2026-06-14 历史快照，但需在顶部标明 Hybrid、FastReconnect、AOI、PureState、带宽和 lag compensation 结论已过期。
- `plans/shooter-multiprocess-smoke-followup-plan.md` 先修复编码；artifact root、run id、完整端口组和并行隔离已由 TEST-01A 落地，后续只保留故障矩阵、CI 分层与长稳验收。
- `Docs/网络同步抽象审计与能力矩阵.md` 的能力维度和包边界继续有效，但 §10 的落地快照应更新，不再把 Hybrid、Batch、MassBattleLod 和 LagCompensation 标为无 runtime。
- Shooter 深潜文档作为实现说明保留；路线图不复制类清单，只维护方向、边界和验收标准。

## 9. 明确不做

- 不新建第二个 multiplayer sync foundation 包。
- 不继续向 `NetworkSyncModel` 添加组合型枚举来表达每种玩法搭配。
- 不把 Shooter payload、entity kind 或 Svelto 查询下沉到通用网络层。
- 不以 HUD 字段数量代替结构化诊断和 artifact。
- 不在缺少真实 profile 数据时默认启用外推、自适应算法或复杂空间索引。
- 不把单元测试通过等同于真实网络、带宽和 observer 生命周期闭环。

## 10. 第一批落地建议

第一批迭代控制在四个可独立验收的交付物：

1. Snapshot stream contract 设计与 PureState adapter 迁移，不改变 wire。
2. Sync health aggregator + smoke summary JSON，消除客户端多处事件数组 merge。
3. Packed/PureState golden fixtures + 版本不兼容测试。
4. FrameRecord first-divergence CLI，输入为两份 `.record.bin`，输出 summary JSON。

以上四项、SYNC-04 observer send budget、TEST-01A artifact 隔离、TEST-01B minimal、Gateway offline、slow-consumer 与三轮 reconnect-cycles 故障恢复、PERF-01 smoke、分层 CI、Unity PlayMode 与输入安全阈值部署配置化均已完成。后续工作聚焦 grain reactivation、运行中强杀、多 observer 长稳和容量覆盖，而不是继续扩张同步模式。
