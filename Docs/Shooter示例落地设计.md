# Shooter 示例落地设计

## 目标

Shooter 示例用于展示 AbilityKit 多玩法聚合能力。它应该比 MOBA 小很多，但链路要完整：公共房间协议、玩法协议、逻辑世界、输入提交、帧驱动、快照输出、视图消费、Orleans 多玩法 adapter。

## 从 MOBA 保留的关键链路

MOBA 中值得保留的是结构，不是复杂玩法内容：

- 世界蓝图：`IWorldBlueprint` + `WorldCreateOptions` + `IWorldModule`，用于声明玩法世界类型和安装运行时服务。
- 运行端口：类似 `IMobaBattleRuntimePort`，作为外部进入逻辑层的单一入口，承载 StartGame、SubmitInput、GetSnapshot。
- 会话驱动：类似 `MobaBattleDriverHost`，只驱动世界 Tick 和端口调用，不直接操作实体。
- 启动适配：类似 `MobaSessionCoordinatorHost`，负责 world type、world id、初始玩家数据和配置注入。
- 网关房间入口：统一使用 `com.abilitykit.protocol.room`，玩法只提供 roomType/gameplayId/worldType 和玩法启动参数。
- 快照分发：类似 `FrameSnapshotDispatcher` 的 opCode -> decoder -> handler 路由模型，可作为视图侧通用范式。

## 不作为 Shooter 模板复制的 MOBA 内容

以下内容是 MOBA 历史复杂度或特定玩法能力，Shooter 第一版不复制：

- Entitas 生成上下文和大量 ECS 系统安装链。
- 技能流水线、Buff、Trigger、Effect、DamagePipeline 的完整组合。
- 投射物、范围、召唤物、碰撞服务的复杂配置表驱动形态。
- 回滚模块、复杂预测/确认双世界表现。
- MOBA 专属 hero/loadout/team/spawn/skill 配置结构。
- 大量 Unity 表现绑定、VFX、浮字、区域表现和触发器表现事件。

## Shooter 最小完整闭环

Shooter 采用更直接的玩法模型：

- 玩家：id、位置、朝向、hp、score、alive。
- 输入：moveX、moveY、aimX、aimY、fire。
- 子弹：id、owner、position、velocity、ttl。
- 逻辑：固定帧推进，玩家移动，开火生成子弹，子弹命中扣血和加分。
- 快照：每帧输出玩家数组、子弹数组、事件数组。
- 视图：按快照创建/更新玩家和子弹表现，事件可先只打印或触发简单效果。

## 包边界

- `com.abilitykit.protocol.shooter`：Shooter wire 协议、opCode、MemoryPack codec。
- `com.abilitykit.demo.shooter.share`：玩法常量、通用描述、输入/快照领域模型。
- `com.abilitykit.demo.shooter.runtime`：逻辑世界蓝图、运行端口、最小战斗状态和 Tick。
- `com.abilitykit.demo.shooter.view.runtime`：Unity 表现入口、网关房间接入、快照应用。
- Orleans：新增 `ShooterRoomGameplayAdapter` 和 `ShooterBattleRuntimeAdapter`，注册 roomType=`shooter`。

## 第一版验收线

- Unity 包能被 asmdef 识别。
- .NET 能编译 Shooter protocol 桥接项目。
- Orleans 能创建 `roomType=shooter` 的房间并启动 battle。
- Shooter runtime 可接受输入并推进帧。
- Shooter snapshot 能被服务端推送、客户端解码。

## 网络协议与战斗时间锚点分析

Shooter 的同步方案不是单纯状态同步，也不是单纯帧同步，而是两条协议链路同时存在：

- 帧同步输入链路：客户端按目标帧提交输入，服务端按固定 Tick 消费输入并推进权威世界。
- 状态同步链路：开始、晚进、重连、校正时，服务端下发权威快照，客户端覆盖本地世界后追帧。

当前协议已经有基础雏形，但还不能完整支撑严格的双端同帧追赶。

### 当前已有能力

- Room Gateway 生命周期协议已经覆盖创建、加入、准备、启动、订阅、输入提交：`WireCreateRoomReq`、`WireJoinRoomReq`、`WireStartRoomBattleReq`、`WireSubscribeStateSyncReq`、`WireSubmitBattleInputReq`。
- 输入提交已经带 `BattleId`、`WorldId`、`Frame`、`PlayerId`、`InputOpCode`、玩法 payload，可承载 Shooter 的 `ShooterPlayerCommand`。
- 状态同步推送已经有 `WireStateSyncSnapshotPush`，包含 `WorldId`、`Frame`、`Timestamp`、`IsFullSnapshot`、`PayloadOpCode`、`Payload`。
- Shooter 协议已经区分输入、启动、普通快照、packed 快照、delta 快照和 state hash。
- packed 快照已支持 `Full`、`Delta`、`KeyFrame`、`AuthorityOverride` 标记，可用于开始/重连覆盖和普通增量同步。
- 时间同步协议已有 `WireTimeSyncReq` / `WireTimeSyncRes`，可让客户端估算服务端时间。
- Room 协议已经定义 `WireWorldStartAnchor`，包含 `StartServerTicks`、`ServerTickFrequency`、`StartFrame`、`FixedDeltaSeconds`。

### 关键缺口

- `WireWorldStartAnchor` 目前只是 wire 类型，服务端 `RoomGatewayWireMapper.ToJoinRoomRes` 返回的是 `default`，真实开始时间没有从 Orleans 战斗生命周期传回客户端。
- `WireStartRoomBattleRes` 目前只返回 `BattleId`、`WorldId`、`Started`、`Message`，没有返回开始锚点。对创建者来说，启动战斗响应本身就应该携带第一份时间基准。
- `StartRoomBattleResponse` 服务端 contract 也没有 `WorldStartAnchor`，所以 handler 即使想填 wire 字段，也没有来源。
- `BattleLogicHostGrain` 的战斗 timer 是初始化后直接 `RegisterTimer`，但没有记录“服务端计划开始 ticks / 实际开始 ticks / start frame”。这会让客户端无法根据服务端时间换算目标追赶帧。
- `WireStateSyncSnapshotPush.Timestamp` 是 `double`，语义不够明确：有的地方填 `DateTime.UtcNow.Ticks`，测试里填小数秒。后续应统一为 server ticks 或 unix ms，并在字段命名/文档里明确。
- `ShooterStartGamePayload` 只包含 `MatchId`、`TickRate`、`RandomSeed`、`Players`，不包含 `WorldId`、`StartFrame`、`StartServerTicks`。如果它作为玩法启动 payload 发送给客户端，需要与 Room Gateway 的 start anchor 对齐。
- 输入响应 `WireSubmitBattleInputRes` 只有 `AcceptedFrame`，缺少服务器当前帧/拒绝原因枚举/建议重同步标记。第一版可以先保留，但后续纠偏会需要更明确的反馈。

### 推荐协议形态

开始战斗时，服务端应该返回一个明确的世界时间锚点：

- `WorldId`：本局逻辑世界标识。
- `BattleId`：输入和订阅绑定的战斗标识。
- `StartServerTicks`：服务端时间域中的战斗起始 ticks。
- `ServerTickFrequency`：服务端 ticks 频率。
- `StartFrame`：通常为 0，也允许恢复/迁移场景从非 0 开始。
- `FixedDeltaSeconds`：固定帧间隔，例如 `1.0 / 30.0`。
- `ServerNowTicks`：响应发出时服务端 ticks，便于客户端立即估算应该追到哪一帧。

重连/晚进时，客户端应先通过 TimeSync 估算服务端时间偏移，再 Join/Subscribe，然后接收带 `AuthorityOverride` 的 full/keyframe packed snapshot。客户端导入快照后，根据：

```text
elapsedSeconds = (estimatedServerNowTicks - StartServerTicks) / ServerTickFrequency
targetFrame = StartFrame + floor(elapsedSeconds / FixedDeltaSeconds)
catchUpFrames = targetFrame - snapshot.Frame
```

将本地世界追到目标帧。追赶期间可以只消费本地已知输入/空输入，直到下一次权威快照校正。

### 推荐落地顺序

1. 在 Room/Orleans contract 中新增统一的 `WorldStartAnchor` 模型，并让 `StartRoomBattleResponse` 携带它。
2. 在 `BattleLogicHostGrain.InitializeBattleAsync` 记录战斗开始 server ticks、tick frequency、start frame、fixed delta。
3. 扩展 `WireStartRoomBattleRes`，追加 `WireWorldStartAnchor WorldStartAnchor` 和 `long ServerNowTicks`，保持 MemoryPack 字段追加以降低破坏性。
4. 让 `WireJoinRoomRes` 在战斗已存在时返回真实 `WorldStartAnchor`，用于重连/晚进。
5. 明确 `WireStateSyncSnapshotPush.Timestamp` 语义，优先改为或补充 `ServerTicks`，避免 double timestamp 混用。
6. Shooter 客户端在 start/join/subscribe 后保存 anchor，并用 TimeSync + anchor 计算目标追赶帧。
7. 为开始、晚进、重连三类路径分别加协议测试，确保 wire 层字段完整且客户端能算出正确 catch-up frame。
