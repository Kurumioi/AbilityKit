# ET Logic Battle Integration Guide

本文档定义 `AbilityKit.Demo.ET.Logic` 中 ET Demo 接入 MOBA 战斗逻辑层的正式边界。它的目标不是把 ET Demo 做成另一套战斗框架，而是让 ET 侧成为 AbilityKit MOBA Runtime 的宿主、输入适配层和视图事件桥接层。

## 目标定位

`AbilityKit.Demo.ET.Logic` 负责：

- 在 ET Scene 生命周期内创建、启动、更新和销毁 MOBA 逻辑世界。
- 把 ET 或网络层输入转换为 MOBA Runtime 暴露的输入端口调用。
- 把 MOBA Runtime 产出的快照、出生、伤害、销毁等事件桥接回 ET 视图或 ET 事件系统。
- 提供 ET 平台相关服务，例如配置加载、日志桥接、同步适配器选择。

`AbilityKit.Demo.ET.Logic` 不负责：

- 重新实现 MOBA Runtime 的技能、Buff、属性、伤害、触发器或 ECS 逻辑。
- 直接替换 Runtime 内部服务，除非 Runtime 明确暴露端口或模块扩展点。
- 在 `ETMobaBattleDriver` 中继续堆叠具体战斗规则。
- 让 Hotfix 层绕过 Driver/Port 直接操作 MOBA Runtime 内部服务。

## 目录职责

| 目录 | 职责 | 约束 |
| --- | --- | --- |
| `Model/Battle` | ET 战斗组件数据、测试组件、视图快照缓存 | 只存 ET 组件状态，不放 MOBA Runtime 规则 |
| `Model/Driver` | 战斗逻辑世界宿主、输入路由、帧管线、生命周期处理器 | ET 与 MOBA Runtime 的主要接入层 |
| `Model/Driver/World` | World 创建、WorldFactory、World 模块和 Creator 注册 | 统一创建 `HostRuntime` / `IWorld` / `WorldCreateOptions` |
| `Model/Driver/EnterGame` | 进场 spec 构建、表现实体桥接、首帧快照派发 | Coordinator 只编排流程，具体步骤放入小组件 |
| `Model/Driver/Handlers` | 生命周期、输入、快照 handler | 每个 handler 只处理一个明确阶段或端口方向 |
| `Model/Driver/Pipeline` | ET 每帧推进管线 | 固定顺序推进输入、逻辑世界、快照、派发 |
| `Model/Services` | ET 平台服务，例如日志桥接、配置加载 | 不放战斗规则 |
| `Hotfix/Battle` | ET 组件生命周期入口和事件桥 | 调用 Driver，不直接承载 MOBA Runtime 规则 |
| `Hotfix/Driver` | 同步适配器与 Driver System | 只选择/驱动适配策略，不创建规则服务 |
| `Model/MobaCore` / `Hotfix/MobaCore` | 旧接入层预留目录 | 当前不作为新增代码入口，后续若重启需先迁移规范 |

## 正式启动链路

```text
DemoProcessComponentSystem
    -> ETBattleComponent.InitializeBattle
    -> ETMobaBattleDriver.Initialize
    -> InitializeHandler
        -> LogicWorldRegistry
        -> MobaLogicWorldCreator
            -> WorldManager
            -> HostRuntime
            -> WorldCreateOptions
            -> BattleServiceModule
            -> MobaWorldBootstrapModule
            -> HostRuntime.CreateWorld
    -> StartHandler
        -> resolve IMobaBattleInputPort
    -> ETMobaBattleDriver.OnAllPlayersReady
        -> ETBattleEnterGameCoordinator
            -> ETBattleEnterGameSpecBuilder
            -> MobaEnterGameFlowService.ApplyGameStartSpec
            -> MobaGamePhaseService.SetInGame
            -> ETBattlePresentationSpawnBridge
            -> ETBattleEnterGameSnapshotDispatcher
```

## 帧推进链路

```text
ETMobaBattleDriver.Tick
    -> ETBattleLifecycleDispatcher.Tick
    -> TickHandler
    -> BattleFramePipeline
        -> PreTickPhase
        -> ProcessETInputPhase
            -> IMobaBattleInputPort
        -> DriveWorldPhase
            -> IWorld.Tick
        -> CollectSnapshotPhase
            -> IMobaBattleOutputPort
            -> ETBattleWorldSnapshotAdapter
        -> DispatchSnapshotPhase
            -> ISnapshotHandler
            -> IBattleViewEventSink
        -> PostTickPhase
```

这条链路的核心原则是：ET 侧只提交输入和消费输出，真实战斗状态变化由 MOBA Runtime 世界推进。帧管线不能为了组装表现数据直接读取 Runtime ECS Entity，只能消费 Runtime 暴露的输出端口。

## 输入边界

推荐输入方向：

```text
ET input / network command
    -> ETBattleInputRouter
    -> IInputHandler
    -> IMobaBattleInputPort
    -> MOBA Runtime input systems
```

规则：

- 新输入命令优先新增 `IInputHandler` 实现。
- handler 中只做协议字段转换和端口调用。
- 不在 handler 内直接改 Entitas component，除非该行为已经被 Runtime 定义为公开端口。
- `IMobaBattleInputPort` 是 ET 侧进入战斗逻辑层的主要边界。

## 输出边界

推荐输出方向：

```text
MOBA Runtime world
    -> IMobaBattleOutputPort / WorldStateSnapshot
    -> ETBattleWorldSnapshotAdapter
    -> FrameSnapshotData
    -> ISnapshotHandler
    -> IBattleViewEventSink
    -> ET view event system / ET cache
```

规则：

- 新表现事件优先新增 Runtime snapshot emitter、`ETBattleWorldSnapshotAdapter` 转换分支、`ISnapshotHandler` 或视图事件 sink。
- ET Unit 只作为表现实体和视图缓存，不作为权威战斗状态。
- `ETBattleEnterGameCoordinator` 只协调进场流程，不承载 spec 构建、表现实体创建、快照组装、技能或伤害规则。
- `CollectSnapshotPhase` 只能依赖 `IMobaBattleOutputPort`，不能解析 `MobaEntityManager` 或直接读取 Runtime ECS Entity。
- ViewSink 和 cache 只消费 `FrameSnapshotData`，不反向查询 Runtime 内部状态。

## 最小接入实现

新的逻辑层表现/缓存接入应尽量只实现以下小面：

- 输入缓存或输入路由：把 ET、网络或测试命令缓存为帧输入，再提交给 `IMobaBattleInputPort`。
- 表现实体桥接：把进场或出生快照映射成宿主引擎的表现实体，例如 ET Unit。
- 快照适配：把 Runtime 输出端口的 `WorldStateSnapshot` 转换成项目共享的 `FrameSnapshotData` 或宿主 DTO。
- View event sink：把 `FrameSnapshotData` 写入表现缓存，并派发 UI、动画、音效或 ET 事件。
- 平台服务模块：提供配置加载、日志、时钟、网络会话等平台能力，通过 World Module/Service 注册。

不应由表现/缓存层实现：

- 技能、Buff、伤害、属性、死亡、复活、投射物命中等战斗规则。
- Runtime ECS Entity 的直接查询或写入。
- 绕过 `IMobaBattleInputPort` 的输入注入。
- 绕过 `IMobaBattleOutputPort` 的状态采样。
- 在 ViewSink/cache 中根据表现状态反推权威逻辑状态。

## World 接入边界

`MobaLogicWorldCreator` 是 ET Demo 创建 MOBA Battle 世界的唯一推荐入口。

它负责：

- 创建 `WorldManager` 和 `HostRuntime`。
- 填充 `WorldCreateOptions`。
- 添加 ET 侧模块和 MOBA Runtime Bootstrap 模块。
- 设置 Entitas contexts factory。
- 校验关键服务是否可解析。

新增平台服务时优先放入 `BattleServiceModule` 或独立 `IWorldModule`，并从 `MobaLogicWorldCreator` 统一加入 `WorldCreateOptions.Modules`。

## Driver 职责边界

`ETMobaBattleDriver` 是 ET Scene 上的战斗宿主组件和门面，不是规则实现类。

允许它持有：

- 生命周期状态，例如 frame、running、tick rate。
- World/HostRuntime/WorldManager 引用。
- 输入、快照、生命周期 handler 列表。
- ET 表现实体映射和 ViewSink。

不建议继续加入：

- 具体技能释放逻辑。
- 属性、Buff、伤害、死亡规则。
- 具体协议解析逻辑。
- 直接写 Runtime 内部 ECS 的临时代码。

这类逻辑应进入 MOBA Runtime、World Module、Input Handler、Snapshot Handler 或专门 Coordinator。

## 临时路径治理

当前需要逐步收敛的临时路径：

- `ETMobaBattleDriver` 仍保留大量 `IBattleDriver` 空实现，仅作为兼容门面，新增功能不要沿这个方向扩张。
- `Model/MobaCore` 和 `Hotfix/MobaCore` 属于早期 MobaCore 直连概念；新增接入应走 Driver/World/Port，后续逐步迁移或删除旧入口。
- `ETBattleEnterGameCoordinator` 已拆出 EnterGameSpecBuilder、PresentationSpawnBridge、EnterGameSnapshotDispatcher；后续保持 Coordinator 只做流程编排。
- `ETMobaInputSink` 仍保留直接写 Runtime Entity 的旧 `IWorldInputSink` 路径；正式输入路径应收敛到 `IMobaBattleInputPort`。
- `ETBattleViewEventSink` 当前还承担 auto-test/skill-test 初始化，这是 Demo 测试钩子，不应成为正式表现缓存层的默认职责。
- `HandlerRegistry` 以反射发现 handler，适合 Demo 扩展；正式项目可迁移为显式注册或模块化 registry。

## 后续优化顺序

1. 收敛 Driver：把 `IBattleDriver` 空实现标记为兼容层，并逐步改为端口转发或删除依赖。
2. 强化 World 模块：把 ET 平台服务统一通过 `IWorldModule` 注册。
3. 明确同步模式：让 Local / Remote / Hybrid adapter 只负责输入帧和快照同步策略。
4. 增加构建验证：保证每轮结构调整后 `AbilityKit.Demo.ET.Logic` 可编译。
