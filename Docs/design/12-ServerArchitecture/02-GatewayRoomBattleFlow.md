# 12.2 Gateway、Room 与 Battle 主链路设计

## 1. 能力定位

本篇解释一次服务器玩法流程如何从 Gateway 进入 Orleans，再落到 RoomGrain 和 BattleLogicHostGrain。重点不是协议字段罗列，而是状态归属和职责分界：

1. Gateway 负责接入、路由、会话上下文和错误映射。
2. RoomDirectoryGrain 负责房间目录和房间摘要。
3. RoomGrain 负责成员、准备、玩法房间状态、战斗启动、late join 和关闭。
4. BattleFrameSyncGrain 负责帧同步 relay 型战斗的时钟与 observer。
5. BattleLogicHostGrain 负责权威 BattleWorld、输入缓冲、Tick、状态同步推送和诊断。
6. ServerGameplayModuleCatalog 决定不同 RoomType 走哪种同步模板和运行时。

## 2. 源码入口

| 主题 | 源码入口 | 说明 |
|------|----------|------|
| Gateway 路由 | `Server/Orleans/src/AbilityKit.Orleans.Gateway/Gateway/Core/GatewayRequestRouter.cs` | opCode 到 handler，超时和异常映射 |
| Handler 注册 | `Server/Orleans/src/AbilityKit.Server.Analyzers/Generators/Gateway/GatewayHandlerRegistrationGenerator.cs` | GatewayHandlerAttribute 生成注册代码 |
| 房间目录 | `Server/Orleans/src/AbilityKit.Orleans.Grains/Rooms/RoomDirectoryGrain.cs` | Create/List/Notify/Remove Room |
| 房间 Grain | `Server/Orleans/src/AbilityKit.Orleans.Grains/Rooms/RoomGrain.cs` | Join/Ready/Command/StartBattle/LateJoin/Close |
| 房间生命周期 | `Server/Orleans/src/AbilityKit.Orleans.Grains/Rooms/RoomLifecyclePolicy.cs` | Lobby/Starting/InBattle/Closing/Closed |
| 战斗路线 | `Server/Orleans/src/AbilityKit.Orleans.Grains/Rooms/RoomFrameSyncRoute.cs` | FrameSync 与 BattleRuntime 启动决策 |
| 战斗主机 | `Server/Orleans/src/AbilityKit.Orleans.Grains/Battle/BattleLogicHostGrain.cs` | 权威战斗世界和状态推送 |
| 玩法模块 | `Server/Orleans/src/AbilityKit.Orleans.Grains/Gameplay/ServerGameplayModuleCatalog.cs` | RoomAdapter、BattleRuntimeAdapter、WorldBlueprint 注册 |

## 3. 请求主链路

```mermaid
sequenceDiagram
    participant Client as Client or Smoke
    participant Gateway as Gateway Pipeline
    participant Router as GatewayRequestRouter
    participant Handler as Gateway Handler
    participant Directory as RoomDirectoryGrain
    participant Room as RoomGrain
    participant Frame as BattleFrameSyncGrain
    participant Battle as BattleLogicHostGrain
    participant Runtime as BattleRuntimeAdapter

    Client->>Gateway: HTTP or TCP request
    Gateway->>Router: opCode, seq, payload, session context
    Router->>Handler: HandleAsync
    Handler->>Directory: CreateRoom or ListRooms
    Directory->>Room: InitializeAsync
    Handler->>Room: Join, Ready, GameplayCommand
    Handler->>Room: StartBattle
    Room->>Room: BuildBattleInitParams
    Room->>Room: Resolve sync template route
    alt Frame sync relay template
        Room->>Frame: InitializeAsync
        Room-->>Handler: StartRoomBattleResponse
    else State sync or authoritative runtime template
        Room->>Battle: InitializeBattleAsync
        Battle->>Runtime: Start
        Battle-->>Room: WorldStartAnchor
        Room-->>Handler: StartRoomBattleResponse
    end
    Handler-->>Router: GatewayResponse
    Router-->>Gateway: response
    Gateway-->>Client: response packet or HTTP response
```

## 4. Gateway 路由设计

GatewayRequestRouter 的职责很克制：

| 行为 | 说明 |
|------|------|
| 根据 opCode 取 handler | 未注册返回 UnhandledOpCode |
| 为请求创建 timeout token | RequestTimeoutMs 大于 0 时自动 CancelAfter |
| 调用 handler.HandleAsync | 传入 GatewayRequest 和 GatewaySessionContext |
| 捕获超时 | 转换为 GatewayStatusCode.Timeout |
| 捕获异常 | 转换为 GatewayStatusCode.Exception |

这意味着业务错误不应依赖异常穿透到 transport 层。Room/Gateway 层需要把可预期失败映射成明确状态码或 HTTP error response。

## 5. RoomGrain 状态机

RoomGrain 内部状态由几个关键字段构成：

| 字段 | 含义 |
|------|------|
| `_summary` | 房间摘要，包含 RoomId、RoomType、Owner、MaxPlayers 等 |
| `_directoryKey` | RoomDirectoryGrain 的 key，用于通知房间列表变化 |
| `_gameplay` | 当前 RoomType 对应的 IRoomGameplayAdapter |
| `_gameplayState` | 玩法房间阶段状态，例如英雄选择、准备、loadout |
| `_members` | 房间成员、在线状态、bot 状态 |
| `_closed` | 房间是否对大厅动作关闭 |
| `_battleId` | 已启动战斗 ID，当前用 RoomId 作为 battleId |
| `_worldId` | 战斗世界 ID |
| `_worldStartAnchor` | 客户端对齐服务器世界时间的锚点 |

生命周期由 RoomLifecyclePolicy 计算，而不是散落在各个方法中：

```mermaid
stateDiagram-v2
    [*] --> Lobby
    Lobby --> Starting: StartBattle requested
    Starting --> InBattle: Battle route initialized
    InBattle --> Closing: Close or cleanup
    Closing --> Closed: Members cleared
    Lobby --> Closed: Owner closes empty room
    InBattle --> InBattle: Reconnect or LateJoin
    Closed --> [*]
```

## 6. 房间启动战斗路线

RoomGrain.StartBattleAsync 的核心决策是：当前 RoomType 和 SyncTemplate 到底需要启动什么。

| 路线 | 条件 | 启动对象 | 说明 |
|------|------|----------|------|
| Frame relay | sync template supports frame sync and runtime mode is FrameRelayOnly | BattleFrameSyncGrain | 服务端负责帧时钟和帧包 relay，不创建完整权威 BattleWorld |
| State sync runtime | sync template requires BattleWorld | BattleLogicHostGrain | 服务端创建权威世界，接收输入，Tick 后推送状态 |
| Unsupported template | sync template 不在 profile 中 | 不启动 | 返回/记录不支持的模板，避免隐式 fallback 到错误同步模型 |

路线由 RoomFrameSyncRoute 计算，输入来自 ServerGameplayModuleCatalog 的 sync profile。

## 7. BattleLogicHostGrain 运行流程

BattleLogicHostGrain 是权威战斗主机。它包含四个关键子系统：

| 子系统 | 职责 |
|--------|------|
| BattleHostState | 当前 worldId、battleId、frame、tickRate |
| BattleInputBuffer | 按 accepted frame 缓冲输入 |
| BattleTickDriver | 每 Tick 提交输入并推进运行时世界 |
| BattleSnapshotPublisher | 给 StateSync observer 推送 full/delta snapshot |

```mermaid
flowchart TB
    Init[InitializeBattleAsync]
    Resolve[Resolve ServerGameplayModule and SyncTemplate]
    Adapter[Create IBattleRuntimeSession]
    Start[RuntimeSession.Start]
    Timer[Register Orleans Timer]
    Input[SubmitInputAsync]
    Schedule[BattleInputFrameScheduler]
    Buffer[BattleInputBuffer]
    Tick[OnTickAsync]
    RuntimeInput[RuntimeSession.SubmitInputs]
    RuntimeTick[RuntimeSession.Tick]
    Snapshot[CreateStateSyncPush]
    Observers[StateSyncObserverGrain]
    Destroy[DestroyAsync]

    Init --> Resolve --> Adapter --> Start --> Timer
    Input --> Schedule --> Buffer
    Timer --> Tick
    Tick --> RuntimeInput
    Buffer --> RuntimeInput
    RuntimeInput --> RuntimeTick
    RuntimeTick --> Snapshot --> Observers
    Destroy --> Adapter
```

## 8. Late Join 与 Reconnect

RoomGrain 对进行中战斗的 Join 做了专门处理：

1. 如果玩家已经在房间里，返回 Reconnect。
2. 如果战斗已经开始且不是成员，先检查容量，再加入房间成员集合。
3. 通过 IRoomGameplayAdapter.BuildLateJoinPlayer 创建 PlayerInitInfo。
4. 调用 BattleLogicHostGrain.JoinPlayerAsync 增量加入运行中的 BattleWorld。
5. 如果 Battle 拒绝加入，则回滚 Room 成员和玩法状态。

这个设计避免了 Room 与 Battle 状态单边成功导致的幽灵成员。

## 9. StateSync Observer

StateSync 的订阅目标是 IStateSyncObserverGrain。BattleLogicHostGrain 订阅 observer 后会：

| 场景 | 行为 |
|------|------|
| Subscribe | 保存 observer context，必要时推 full snapshot |
| RequestFullSnapshot | 针对指定 observer 生成 full snapshot |
| Tick publish | 根据 BattleSnapshotSyncPolicy 判断是否推送 |
| Observer-aware runtime | 允许 runtime 按 observer context 生成兴趣范围或个性化快照 |
| Unsubscribe/Destroy | 清理 observer registry 和 context |

这为 Shooter 这类纯状态同步玩法提供了兴趣范围、重连补帧、full snapshot 恢复的扩展点。

## 10. 多玩法接入模型

ServerGameplayModuleCatalog 让 MOBA 和 Shooter 共享服务器主链路，但保留玩法差异：

| 模块字段 | MOBA | Shooter |
|----------|------|---------|
| RoomType | `moba` | `shooter` |
| RoomAdapter | MobaRoomGameplayAdapter | ShooterRoomGameplayAdapter |
| BattleRuntimeAdapter | MobaBattleRuntimeAdapter | ShooterBattleRuntimeAdapter |
| WorldBlueprint | MobaLobbyWorldBlueprint、MobaBattleWorldBlueprint | ShooterBattleWorldBlueprint |
| 默认同步 | FrameSync relay | StateSync authoritative runtime |
| 可选同步 | state-sync-authority | runtime-snapshot-interpolation、state-sync-authority、pure-state-authority |

新增玩法不应该复制 Gateway/Room/Battle 主链路，而应该新增：

1. GameplayRoomDescriptor。
2. IRoomGameplayAdapter。
3. IBattleRuntimeAdapter。
4. IWorldBlueprint。
5. SyncProfile 模板。
6. 对应 Gateway/Admin/Snapshot 验收用例。

## 11. 失败处理原则

| 层级 | 失败处理 |
|------|----------|
| Gateway Router | 未注册 opCode、超时、异常转为统一 GatewayStatusCode |
| Gateway Handler | 业务失败转为协议响应或 HTTP error mapper |
| RoomGrain | 校验 owner/member/open/capacity，不满足时拒绝动作 |
| Room to Battle | Late join 失败时回滚 Room 成员状态 |
| BattleLogicHost | 未初始化、world mismatch、input frame invalid、runtime start fail 都返回明确 result |
| Runtime Adapter | Start/Join/MountBotAI 失败通过 Result 返回，不让异常成为常规控制流 |

## 12. 验收路径

| 验收 | 覆盖内容 |
|------|----------|
| Gateway tests | Handler 注册、错误映射、HTTP endpoint、Admin API 边界 |
| Grains tests | Room lifecycle、Room to Battle、ServerGameplayModuleCatalog、State storage provider plan |
| ShooterSmoke | Guest login、room launch、snapshot push、input submit、late join、reconnect、cleanup |
| Replay validation | Smoke 记录与回放一致性 |
| Mermaid/docs validation | 服务端设计文档中的流程图可解析 |

服务端专题后续应继续把 Smoke 输出和测试门禁绑定，让“服务器是正式运行面”不仅体现在文档，也体现在 CI 和验收报告中。
