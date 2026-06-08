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
