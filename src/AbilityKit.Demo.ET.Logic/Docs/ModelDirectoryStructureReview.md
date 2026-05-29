# ET Logic Model Directory Structure Review

本文档审计 `src/AbilityKit.Demo.ET.Logic/Model` 的目录职责、当前实现中的不合理点，并给出后续整理建议。目标不是一次性大搬家，而是把示例代码从“能跑的临时分组”收敛到“新逻辑层表现/缓存接入可以照着复用”的结构。

## 当前目录现状

当前 `Model` 目录大致分为以下几类：

| 当前目录 | 实际内容 | 判断 |
| --- | --- | --- |
| `Battle` | 战斗根组件、实体缓存、测试组件、旧输入 sink、视图快照 provider | 混合了运行态组件、表现缓存、测试工具和遗留 Runtime 直连路径 |
| `Driver` | 战斗宿主、生命周期、输入路由、世界创建、帧管线、进场流程、同步接口 | 职责偏大，但它是当前最接近正式接入层的主干 |
| `Driver/World` | World 创建、Creator 注册、World Module | 方向正确，应继续保留并强化 |
| `Driver/Pipeline` | 每帧输入、世界推进、快照收集、快照分发 | 方向正确，已开始收敛到端口边界 |
| `Driver/Handlers` | 输入、生命周期、快照 handler | 方向正确，但可拆成 Input/Lifecycle/Snapshot 子目录 |
| `Driver/EnterGame` | 进场 spec、表现实体桥接、首帧快照派发 | 方向正确 |
| `Input` | ET 输入缓存和命令 DTO | 方向正确，但命令类型目前用 `object` 缓冲，类型边界偏弱 |
| `Unit` | ET 表现实体和表现实体集合 | 方向正确，应明确为 Presentation Entity，不是逻辑实体 |
| `Services` | 配置加载、日志 sink、临时 JSON DTO、玩家出生数据 | 混杂平台服务、配置 DTO、临时转换逻辑 |
| `Flow` | ET 流程组件和枚举 | 可以保留，但要明确它是 ET Demo 流程状态，不是 AbilityKit Flow 抽象本体 |
| `Session` | ET 会话组件 | 可以保留，后续和房间/登录流程统一考虑 |
| `Login` | 登录状态和登录事件 | 可以保留在 Demo App 流程区 |
| `Process` | Demo 进程组件和 Fiber 初始化 | 偏 Demo Application Bootstrap，不是战斗模型 |
| `MobaCore` | 空目录或旧概念残留 | 不应继续作为新增入口 |
| 根目录散文件 | 房间组件、事件、ViewSubFeature、TextAssetLoader、PlayerRegistration | 根目录职责不清，需要归档 |

## 主要问题

### 1. 根目录散文件太多，入口意图不清

`BattleEvent`、`BattleEventTypes`、`ETMobaRoomComponent`、`ETTextAssetLoader`、`IViewSubFeature`、`PlayerRegistration` 都在 `Model` 根目录。根目录应只放极少数全局模型或入口说明，否则新开发者无法判断这些类型属于房间、表现、配置还是流程。

建议：

- `ETMobaRoomComponent` 移到 `Model/Room`。
- `ETTextAssetLoader` 移到 `Model/Platform/Config` 或 `Model/Services/Config`。
- `BattleEvent`、`BattleEventTypes`、`IViewSubFeature` 移到 `Model/Presentation/Events` 或 `Model/View`。
- `PlayerRegistration` 移到 `Model/Room` 或删除，若已被房间快照替代则标记遗留。

### 2. `Battle` 目录混合了组件、测试和遗留接入

`Battle` 目录里既有 `ETBattleComponent`、`ETBattleEntityCacheComponent` 这类运行态组件，也有 `ETBattleAutoTestComponent`、`ETBattleSkillTestComponent` 这类 Demo 测试组件，还有 `ETMobaInputSink` 这种旧式 Runtime 直连输入路径。

建议拆分：

- `Model/Battle` 只保留战斗会话根组件和少量状态 DTO。
- `Model/Presentation/Cache` 放 `ETBattleEntityCacheComponent`、`ETViewSnapshotProvider`、`IETViewSnapshotProvider`。
- `Model/Test` 或 `Model/Demo/Test` 放 `ETBattleAutoTestComponent`、`ETBattleSkillTestComponent`、`BattleTestConfig`、`ETBattleAutoTestInitializer`。
- `ETMobaInputSink` 标记为 legacy，后续删除或移到 `Model/Legacy/MobaCore`。

### 3. `Driver` 名称承载过多概念

当前 `Driver` 包含宿主组件、生命周期分发、输入路由、世界创建、帧管线、同步接口、进场流程、工具类和旧 initializer。它现在能表达“战斗逻辑宿主层”，但目录内部还需要更清晰。

建议：

- `Driver/World` 保留，作为逻辑世界创建和模块注册入口。
- `Driver/Pipeline` 保留，作为每帧推进管线。
- `Driver/EnterGame` 保留，作为进场编排的子流程。
- `Driver/Handlers` 拆成 `Driver/Handlers/Input`、`Driver/Handlers/Lifecycle`、`Driver/Handlers/Snapshot`。
- `Driver/Snapshot` 保留 Runtime 输出快照到 ET/share DTO 的适配。
- `IETBattleSyncAdapter` 移到 `Model/Sync`，同步策略不是 Driver 核心职责。
- `MobaCoreWorldInitializer` 移到 `Model/Legacy/MobaCore` 或删除，避免和正式 `MobaLogicWorldCreator` 并列误导。

### 4. 平台服务、配置 DTO、出生数据混在一个服务文件

`ETConfigLoaderService` 同时包含配置加载、旧 JSON DTO、`ETPlayerSpawnData`、默认出生数据构造。这个文件承担了过多职责，且和当前正式进场流程的 `ETBattleEnterGameSpecBuilder` 有边界重叠。

建议拆分：

- `Model/Platform/Config/ETTextAssetLoader.cs`：只负责文件/文本加载。
- `Model/Platform/Config/ETConfigLoaderService.cs`：只负责 ET Demo 配置加载桥接。
- `Model/Platform/Config/Dto/*.cs`：放旧 JSON DTO，或迁移到 share DTO 后删除。
- `Model/Battle/Spawn/ETPlayerSpawnData.cs`：如果仍由 ET 侧负责玩家出生输入，则独立成明确 DTO。
- 默认出生数据构造应迁到 Demo fixture/test builder，不应在配置服务里生成战斗实体。

### 5. 表现/缓存边界还没有独立成一等目录

目前表现层相关类型散落在 `Battle`、根目录、Hotfix 下。你的目标是“新的逻辑层表现层/缓存数据层只需少量必须实现即可接入”，那 Model 里应该一眼看到 Presentation 接入面。

建议新增：

```text
Model/Presentation/
    Cache/
    Events/
    Spawn/
    Sinks/
```

职责：

- `Cache`：ET 本地表现缓存组件和 snapshot provider。
- `Events`：ET view event DTO、event type、sub feature interface。
- `Spawn`：表现实体创建桥接，可承接当前 `ETBattlePresentationSpawnBridge`。
- `Sinks`：ViewSink 接口或纯 Model 侧定义，Hotfix 实现仍可在 Hotfix 目录。

### 6. Demo 测试逻辑应从正式接入路径旁路出来

`ETBattleAutoTestComponent`、`ETBattleSkillTestComponent`、`ETBattleAutoTestInitializer`、以及 ViewSink 中的测试初始化，都是示例验证工具。它们对 Demo 有价值，但不应该被误认为接入 Runtime 的必需部分。

建议：

- 建立 `Model/Demo/Test` 或 `Model/Test`。
- 文档标注这些是 optional fixture，不是正式接入 contract。
- `ETBattleComponentSystem.StartBattle` 后续不要默认强制创建测试组件，改成由 Demo 场景配置或启动参数开启。

### 7. 多套事件/快照模型并存

当前存在：

- `BattleEvent` / `BattleEventTypes` 字符串事件。
- `ET.AbilityKit.Demo.ET.Share` 中的强类型 view event。
- `FrameSnapshotData` 共享快照。
- `ActorStateSnapshotData` 同步快照。
- `IETViewSnapshotProvider` 自定义 view snapshot。

这些模型各有历史原因，但正式示例应给出主路径，否则后续会继续长出平行 DTO。

推荐主路径：

```text
Runtime WorldStateSnapshot
    -> ETBattleWorldSnapshotAdapter
    -> FrameSnapshotData
    -> ISnapshotHandler
    -> IBattleViewEventSink
    -> ET typed view event / cache
```

治理建议：

- `FrameSnapshotData` 作为 Runtime 输出进入 ET 表现层的主 DTO。
- ET typed view event 只作为 ET 事件系统/表现系统消费事件。
- `BattleEvent` 字符串事件若无明确调用，应迁入 legacy 或删除。
- `ActorStateSnapshotData` 留给 SyncAdapter 层，不进入普通表现缓存路径。

## 推荐目标结构

建议最终把 `Model` 调整为以下结构：

```text
Model/
    App/
        Process/
        Login/
        Flow/
        Session/
    Battle/
        Components/
        Room/
        Spawn/
    Driver/
        Host/
        Lifecycle/
        Pipeline/
        Input/
        Snapshot/
        EnterGame/
        World/
    Presentation/
        Cache/
        Events/
        Spawn/
        Sinks/
    Platform/
        Config/
        Logging/
        Services/
    Sync/
        Adapters/
        Contracts/
    Demo/
        Test/
        Fixtures/
    Legacy/
        MobaCore/
```

如果不想一次改太多，也可以先采用低风险版本：

```text
Model/
    Battle/
    Driver/
    Input/
    Presentation/
    Platform/
    Room/
    Sync/
    Demo/
    Legacy/
```

## 本轮落地状态

已完成第一阶段中风险最低的一组归档和错误流程清理：

1. `ETMobaRoomComponent`、`PlayerRegistration` 已归档到 `Model/Room`。
2. `BattleEvent`、`BattleEventTypes` 已归档到 `Model/Presentation/Events`。
3. `IViewSubFeature` 已归档到 `Model/Presentation/SubFeatures`。
4. `ETTextAssetLoader` 已归档到 `Model/Platform/Config`。
5. `BattleTestConfig`、`ETBattleAutoTestComponent`、`ETBattleSkillTestComponent`、`ETBattleAutoTestInitializer` 已归档到 `Model/Demo/Test`。
6. `ETMobaInputSink`、`MobaCoreWorldInitializer` 已归档到 `Model/Legacy/MobaCore`，并加上 `Obsolete` 标记。
7. `ETMobaBattleDriver.OnAllPlayersReady` 不再默认触发 `ETBattleAutoTestInitializer`，Demo/Test fixture 后续应由测试入口或场景配置显式开启。
8. `AbilityKit.Demo.ET.Logic.csproj` 已收敛为 `Model/**/*.cs` 和 `Hotfix/**/*.cs` 两条包含规则，后续目录整理不需要反复维护编译项。

第二阶段继续完成一组低风险、无行为变化的职责归档：

1. `ETBattleEntityCacheComponent`、`ETViewSnapshotProvider`、`IETViewSnapshotProvider` 已归档到 `Model/Presentation/Cache`。
2. `ETUnit`、`ETUnitComponent` 已归档到 `Model/Presentation/Units`，明确 ET Unit 是表现实体而不是 Runtime 逻辑实体。
3. `ETLogSink` 已归档到 `Model/Platform/Logging`，和配置加载服务拆开。
4. `IETBattleSyncAdapter`、`ActorStateSnapshotData` 已归档到 `Model/Sync/Contracts`，同步契约从 Driver 主推进逻辑中拆出。

## 文件级归档建议

| 当前文件 | 建议位置 | 处理方式 | 原因 |
| --- | --- | --- | --- |
| `Model/ETMobaRoomComponent.cs` | `Model/Room/ETMobaRoomComponent.cs` | 移动 | 房间状态和玩家列表属于战斗房间上下文，不应挂在 Model 根目录 |
| `Model/PlayerRegistration.cs` | `Model/Room/PlayerRegistration.cs` | 移动或合并 | 仍是房间/玩家注册 DTO；如果房间快照已覆盖，可后续删除 |
| `Model/ETTextAssetLoader.cs` | `Model/Platform/Config/ETTextAssetLoader.cs` | 移动 | ET 平台文本资源加载适配，不是战斗模型 |
| `Model/BattleEvent.cs` | `Model/Presentation/Events/BattleEvent.cs` | 移动后评估删除 | 字符串事件属于表现事件旧路径，不能作为新接入主契约 |
| `Model/BattleEventTypes.cs` | `Model/Presentation/Events/BattleEventTypes.cs` | 移动后评估删除 | 同上，后续应让强类型事件或 `FrameSnapshotData` 成为主路径 |
| `Model/IViewSubFeature.cs` | `Model/Presentation/SubFeatures/IViewSubFeature.cs` | 移动 | 明确它是表现扩展点，而不是战斗逻辑入口 |
| `Model/Battle/ETBattleComponent.cs` | `Model/Battle/Components/ETBattleComponent.cs` | 移动并修注释 | 作为 ET battle session 根状态保留，但避免和表现缓存/测试混放 |
| `Model/Battle/ETBattleEntityCacheComponent.cs` | `Model/Presentation/Cache/ETBattleEntityCacheComponent.cs` | 移动 | 缓存的是表现层消费快照，不应被误认为 Runtime 权威状态 |
| `Model/Battle/ETViewSnapshotProvider.cs` | `Model/Presentation/Cache/ETViewSnapshotProvider.cs` | 移动 | View snapshot provider 是表现读取面 |
| `Model/Battle/IETViewSnapshotProvider.cs` | `Model/Presentation/Cache/IETViewSnapshotProvider.cs` | 移动 | 同上 |
| `Model/Battle/BattleTestConfig.cs` | `Model/Demo/Test/BattleTestConfig.cs` | 移动 | 测试参数是 fixture，不是正式接入必需类型 |
| `Model/Battle/ETBattleAutoTestComponent.cs` | `Model/Demo/Test/ETBattleAutoTestComponent.cs` | 移动 | 自动测试组件应从正式 Battle 路径旁路 |
| `Model/Battle/ETBattleSkillTestComponent.cs` | `Model/Demo/Test/ETBattleSkillTestComponent.cs` | 移动 | 技能测试组件同上 |
| `Model/Driver/ETBattleAutoTestInitializer.cs` | `Model/Demo/Test/ETBattleAutoTestInitializer.cs` | 移动并降低默认耦合 | 初始化测试组件不应由 Driver 默认执行 |
| `Model/Battle/ETMobaInputSink.cs` | `Model/Legacy/MobaCore/ETMobaInputSink.cs` | 移动并标注 obsolete | 旧输入路径直接写 Runtime ECS，和正式 InputPort 边界冲突 |
| `Model/Driver/MobaCoreWorldInitializer.cs` | `Model/Legacy/MobaCore/MobaCoreWorldInitializer.cs` | 移动并标注 obsolete | 旧世界初始化器和正式 `MobaLogicWorldCreator` 并存会误导新接入 |
| `Model/Driver/IETBattleSyncAdapter.cs` | `Model/Sync/Contracts/IETBattleSyncAdapter.cs` | 已移动 | 同步策略是独立横切能力，不是 Driver 核心推进逻辑 |
| `Model/Driver/EnterGame/ETBattlePresentationSpawnBridge.cs` | `Model/Presentation/Spawn/ETBattlePresentationSpawnBridge.cs` | 后续移动 | 它创建的是 ET 表现实体，语义上属于 Presentation |
| `Model/Services/ETLogSink.cs` | `Model/Platform/Logging/ETLogSink.cs` | 已移动 | 日志 sink 是平台适配能力 |
| `Model/Services/ETConfigLoaderService.cs` | `Model/Platform/Config/ETConfigLoaderService.cs` | 先移动后拆分 | 配置加载属于平台适配；DTO 和 spawn 构造应后续拆出 |
| `Model/Unit/ETUnit.cs` | `Model/Presentation/Units/ETUnit.cs` | 已移动 | ET Unit 是表现实体，不是 Runtime 逻辑实体 |
| `Model/Unit/ETUnitComponent.cs` | `Model/Presentation/Units/ETUnitComponent.cs` | 已移动 | 同上 |
| `Model/Process/*` | `Model/App/Process/*` | 后续移动 | Demo 进程启动属于应用流程，不是战斗模型 |
| `Model/Login/*` | `Model/App/Login/*` | 后续移动 | 登录流程属于 Demo App |
| `Model/Flow/*` | `Model/App/Flow/*` | 后续移动 | ET Demo 流程状态应和 AbilityKit Flow 抽象区分 |
| `Model/Session/*` | `Model/App/Session/*` | 后续移动 | ET 会话组件属于应用会话层 |

## 迁移验收标准

每一轮目录整理都应满足以下标准：

1. 编译通过，且不引入新的 nullable 或命名空间错误。
2. 新增目录名能表达职责，不靠文件注释才能理解边界。
3. `Driver` 只依赖端口、世界创建、生命周期、输入路由、快照分发，不直接依赖 Demo/Test。
4. `Presentation` 可以依赖 `FrameSnapshotData` 和 ET 表现组件，但不能回查 Runtime ECS 权威状态。
5. `Legacy` 内文件不允许被新代码依赖；若必须保留调用，调用点需要有注释说明过渡原因。
6. `Demo/Test` 内组件默认不参与正式战斗启动，由场景配置、启动参数或测试入口显式打开。
7. 文档和目录同步更新，避免文档推荐结构与实际路径再次分叉。

## 推荐迁移顺序

### 第一阶段：无行为变化的归档

目标是只移动/拆分职责清晰的文件，不改业务逻辑。

1. 新建 `Model/Room`，移动 `ETMobaRoomComponent`、`PlayerRegistration`。
2. 新建 `Model/Presentation/Events`，移动 `BattleEvent`、`BattleEventTypes`、`IViewSubFeature`。
3. 新建 `Model/Platform/Config`，移动 `ETTextAssetLoader`。
4. 新建 `Model/Demo/Test`，移动 `BattleTestConfig`、`ETBattleAutoTestComponent`、`ETBattleSkillTestComponent`、`ETBattleAutoTestInitializer`。
5. 新建 `Model/Legacy/MobaCore`，移动 `ETMobaInputSink` 和 `MobaCoreWorldInitializer`，并标注不再作为新增入口。

### 第二阶段：拆服务大文件

1. 拆 `ETConfigLoaderService` 中的 JSON DTO。
2. 独立 `ETPlayerSpawnData` 到 Battle/Spawn 或 Room/Spawn。
3. 把默认出生数据构造移动到 Demo fixture。
4. 如果 Runtime/share 配置 DTO 已覆盖旧 JSON DTO，则删除旧 DTO。

### 第三阶段：强化正式接入主干

1. `Driver/Handlers` 拆成 Input/Lifecycle/Snapshot。
2. `IETBattleSyncAdapter` 移入 `Model/Sync/Contracts`。
3. `ETBattlePresentationSpawnBridge` 可移到 `Model/Presentation/Spawn`，因为它创建的是宿主表现实体，不是 Runtime 规则。
4. 文档中固定最小接入面：InputPort、OutputPort、Presentation Spawn、ViewSink、Platform Services。

### 第四阶段：删除平行旧路径

1. 移除或禁用 `ETMobaInputSink`。
2. 移除 `MobaCoreWorldInitializer`，统一使用 `MobaLogicWorldCreator`。
3. 清理 `BattleEvent` 字符串事件，统一到强类型 ET view event。
4. ViewSink 中的 test 初始化迁出，改由 Demo/Test 系统监听 enter-game 后自行初始化。

## 当前不合理实现清单

| 位置 | 问题 | 建议 |
| --- | --- | --- |
| `Model/Battle/ETMobaInputSink.cs` | 直接通过 `MobaEntityManager` 写 Runtime Entity，是旧式越界输入路径 | 移入 Legacy，后续删除，正式输入只走 `IMobaBattleInputPort` |
| `Model/Driver/MobaCoreWorldInitializer.cs` | 与 `MobaLogicWorldCreator` 形成两套世界创建概念 | 移入 Legacy 或删除，保留 `MobaLogicWorldCreator` 作为唯一正式入口 |
| `Model/Services/ETConfigLoaderService.cs` | 服务、DTO、默认数据构造、spawn 构造混在一起 | 拆成 Config Service、Config DTO、Spawn DTO、Demo Fixture |
| `Model/ETMobaRoomComponent.cs` | 根目录文件且组件内包含较多命令代理方法 | 移入 Room；长远可把命令代理放到 Room System/Facade |
| `Model/Battle/ETBattleComponent.cs` | 注释乱码，且同时持有 ViewSink 与 BattleDriver 兼容接口 | 修注释；明确它只存 ET battle session 状态，Driver 是正式门面 |
| `Model/Input/ETInputComponent.cs` | 输入缓冲使用 `List<object>`，类型约束弱 | 定义 `IETInputCommand` 或 discriminated command wrapper，减少 switch/object |
| `Model/IViewSubFeature.cs` | View 接口在根目录，且像未来扩展但当前接入主线不明显 | 移到 Presentation/Events 或 Presentation/SubFeatures |
| `Model/Battle/BattleTestConfig.cs` 等 | 测试配置混在正式 Battle 目录 | 移到 Demo/Test，标记 optional |
| `Model/Driver/IETBattleSyncAdapter.cs` | 同步策略接口放在 Driver 根目录，扩大 Driver 概念 | 移到 Sync/Contracts |
| `Model/MobaCore` | 空目录/旧概念目录存在误导 | 删除或改成 Legacy/MobaCore |

## 推荐边界口径

整理后的 `Model` 应围绕这些边界命名：

- `Driver`：宿主 Runtime 世界，推进生命周期，连接输入输出端口。
- `World`：创建和配置 AbilityKit/MOBA Runtime world。
- `Input`：缓存宿主输入，转换为 Runtime input port command。
- `Snapshot`：把 Runtime output snapshot 转为共享/宿主 DTO。
- `Presentation`：表现实体、表现缓存、视图事件，不做权威规则。
- `Platform`：ET 平台能力，例如配置加载、日志、路径、会话。
- `Demo/Test`：示例自动化验证，不属于正式接入必要面。
- `Legacy`：仍需保留但不允许新增依赖的旧路径。

## 结论

当前目录结构已经具备正式化的主干：`Driver/World`、`Driver/Pipeline`、`Driver/EnterGame`、`Driver/Snapshot` 方向是对的；问题主要在根目录散文件、`Battle` 目录混杂、平台服务过胖、测试逻辑和正式路径混在一起，以及旧 MobaCore 直连路径还没有隔离。

下一轮最推荐做“低风险目录归档”：先不改行为，只新增 `Presentation`、`Platform`、`Room`、`Demo/Test`、`Legacy` 目录并移动最明显的文件。这样能快速降低认知成本，也能让后续新接入者一眼看出哪些是必须实现、哪些只是 Demo 辅助、哪些已经不推荐使用。
