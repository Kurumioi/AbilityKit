# AbilityKit ET Demo 架构规范

## 一、架构分层总览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 表现层 (ET.View)                                                            │
│ - 渲染表现，读取缓存数据                                                     │
│ - 禁止：任何战斗逻辑、业务计算                                               │
│ - 职责：GameObject渲染、动画、UI、VFX、SFX                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▲
                                    │ 读取数据
┌─────────────────────────────────────────────────────────────────────────────┐
│ 逻辑层 (ET.Logic)                                                          │
│ - 缓存 moba.core 快照数据                                                   │
│ - 管理输入缓冲                                                              │
│ - 驱动 SessionCoordinator                                                   │
│ - 禁止：渲染逻辑                                                            │
│ - 职责：战斗逻辑、输入处理、状态管理                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▲
                                    │ 调用服务
┌─────────────────────────────────────────────────────────────────────────────┐
│ 核心框架层 (moba.core / AbilityKit)                                         │
│ - 所有战斗逻辑实现                                                          │
│ - ECS 世界管理                                                              │
│ - 禁止：ET 特定代码                                                         │
│ - 职责：伤害计算、技能执行、Buff管理、碰撞检测                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 二、各层代码规范

### 2.1 核心框架层 (moba.core / AbilityKit)

**位置**: `Unity/Packages/com.abilitykit.*` 和 `AbilityKit.Demo.Moba.*`

**职责**:
- 所有战斗逻辑实现
- ECS 世界管理
- 服务注册和解析

**规范**:

| 规范 | 说明 |
|-----|-----|
| 跨平台 | 不使用 Unity 特定 API |
| 接口抽象 | 通过接口与外部交互 |
| 无 ET 依赖 | 不引用 ET 命名空间 |

**示例目录结构**:
```
AbilityKit.Demo.Moba.Core/
├── Ability/              # 技能系统
├── Combat/              # 战斗系统（伤害、Buff）
├── Motion/              # 移动系统
├── Entity/             # 实体管理
└── Services/           # 服务定义

AbilityKit.Ability/
├── Config/              # 配置数据模型
├── Triggering/         # 触发器系统
└── Pipeline/           # 技能管道
```

---

### 2.2 逻辑层 (ET.Logic)

**位置**: `src/AbilityKit.Demo.ET.Logic/`

**职责**:
- 缓存 moba.core 快照数据
- 管理输入缓冲
- 驱动 SessionCoordinator
- 协调表现层事件

**规范**:

| 规范 | 说明 |
|-----|-----|
| Component 只存数据 | 无业务逻辑方法 |
| System 包含所有逻辑 | 扩展方法模式 |
| 单向依赖 | Logic → Share ← View |
| 禁止渲染逻辑 | 不包含任何渲染代码 |

#### 2.2.1 Component 规范 (纯数据)

```csharp
// ✅ 正确：Component 只存数据
[ComponentOf(typeof(Scene))]
public class ETBattleComponent : Entity, IAwake, IUpdate, IDestroy
{
    // 数据字段
    public long BattleId { get; set; }
    public long PlayerId { get; set; }
    public int CurrentFrame { get; set; }

    // 引用
    public ETBattleDriver BattleDriver { get; set; }
    public IETViewEventSink ViewSink { get; set; }

    // 空生命周期方法
    public void Awake() { }
    public void Update(ETBattleComponent self) { }
    public void OnDestroy(ETBattleComponent self) { }
}
```

```csharp
// ❌ 错误：Component 包含业务逻辑
[ComponentOf(typeof(Scene))]
public class ETBattleComponent : Entity, IAwake
{
    public long BattleId { get; set; }

    // ❌ 业务逻辑不应该在 Component 中
    public void StartBattle()
    {
        Log.Info("Starting battle..."); // ❌
        // ... 业务逻辑
    }
}
```

#### 2.2.2 System 规范 (行为)

```csharp
// ✅ 正确：System 包含所有业务逻辑
[EntitySystemOf(typeof(ETBattleComponent))]
[FriendOf(typeof(ETBattleComponent))]
[FriendOf(typeof(ETBattleDriver))]
public static partial class ETBattleComponentSystem
{
    [EntitySystem]
    private static void Awake(this ETBattleComponent self)
    {
        Log.Info("[ETBattle] Component awake");
    }

    [EntitySystem]
    private static void Update(this ETBattleComponent self)
    {
        // 每帧更新逻辑
    }

    // 业务方法作为扩展方法
    public static void StartBattle(this ETBattleComponent self, BattleStartPlan plan)
    {
        // ... 业务逻辑
    }

    public static void StopBattle(this ETBattleComponent self)
    {
        // ... 业务逻辑
    }
}
```

#### 2.2.3 命名空间规范

```csharp
// 按目录细分命名空间
namespace ET.Logic.Battle      // Model/Battle/*.cs
namespace ET.Logic.Unit         // Model/Unit/*.cs
namespace ET.Logic.MobaCore     // Model/MobaCore/*.cs
namespace ET.Logic.Driver       // Model/Driver/*.cs
namespace ET.Logic.Coordinator  // Model/Coordinator/*.cs
```

#### 2.2.4 目录结构规范

```
ET.Logic/
├── Model/                              # Component 定义（纯数据）
│   ├── Battle/
│   │   ├── ETBattleComponent.cs        # 战斗组件
│   │   ├── ETBattleAutoTestComponent.cs # 自动测试组件
│   │   └── ETBattleSkillTestComponent.cs
│   ├── Unit/
│   │   ├── ETUnit.cs                  # 单位实体
│   │   ├── ETUnitComponent.cs          # 单位管理器
│   │   └── Components/
│   │       ├── ETUnitMetaComponent.cs
│   │       ├── ETUnitTransformComponent.cs
│   │       └── ETUnitCharacterComponent.cs
│   ├── Driver/
│   │   ├── ETBattleDriver.cs           # 战斗驱动（数据）
│   │   └── ETBattleDriverAdapter.cs    # IBattleDriver 适配器
│   ├── Input/
│   │   └── ETInputComponent.cs         # 输入缓冲组件
│   ├── MobaCore/
│   │   ├── Components/
│   │   │   ├── ETWorldResolverComponent.cs
│   │   │   ├── ETBattleSessionHooks.cs
│   │   │   └── ETPredictionConfig.cs
│   │   └── SubFeatures/
│   │       ├── ETBattleTickLoopComponent.cs
│   │       ├── ETBattleEventsComponent.cs
│   │       └── ETBattleLifecycleComponent.cs
│   └── Coordinator/
│       └── ETBattleViewFeature.cs
│
├── Hotfix/                             # System 实现（行为逻辑）
│   ├── Battle/
│   │   ├── ETBattleComponentSystem.cs
│   │   ├── ETInputComponentSystem.cs
│   │   ├── ETBattleDriverBridge.cs     # 驱动桥接
│   │   ├── ETBattleDriverBridgeSystem.cs
│   │   ├── AutoTest/
│   │   │   ├── ETBattleAutoTestComponentSystem.cs
│   │   │   └── ETBattleSkillTestComponentSystem.cs
│   │   └── Events/
│   │       └── ETBattleViewEventSink.cs # 事件桥接
│   ├── Unit/
│   │   ├── ETUnitComponentSystem.cs
│   │   ├── ETUnitSystem.cs             # 单位行为扩展
│   │   └── Components/
│   │       ├── ETUnitMetaComponentSystem.cs
│   │       ├── ETUnitTransformComponentSystem.cs
│   │       └── ETUnitCharacterComponentSystem.cs
│   ├── Driver/
│   │   ├── ETBattleDriverSystem.cs     # 驱动逻辑
│   │   ├── ETBattleDriverHost.cs      # Coordinator 桥接
│   │   └── SyncAdapters/
│   │       ├── ETSyncAdapterFactory.cs
│   │       ├── ETFrameSyncAdapter.cs
│   │       └── ETStateSyncAdapter.cs
│   └── MobaCore/
│       ├── ETWorldResolverComponentSystem.cs
│       └── ETBattleSessionHooksSystem.cs
│
└── Share/                             # 通过项目引用
    └── (来自 AbilityKit.Demo.ET.Share)
```

---

### 2.3 表现层 (ET.View)

**位置**: `src/AbilityKit.Demo.ET.View/`

**职责**:
- 读取 ET.Logic 缓存数据渲染
- 只包含渲染相关逻辑
- 禁止任何战斗逻辑

**规范**:

| 规范 | 说明 |
|-----|-----|
| 只读数据 | 不修改 ET.Logic 数据 |
| 事件驱动 | 订阅 ET.Logic 发布的事件 |
| 禁止业务逻辑 | 不包含伤害计算、技能逻辑 |
| 禁止直接访问 moba.core | 只通过 ET.Logic 间接访问 |

#### 2.3.1 Component 规范

```csharp
// ✅ 正确：View Component 只存渲染相关数据
[ComponentOf(typeof(Scene))]
public class ETUnitViewComponent : Entity, IAwake, IUpdate
{
    public long UnitId { get; set; }           // 对应 ET.Logic 的 Unit.Id
    public int MobaActorId { get; set; }       // 对应 ET.Logic 的 Unit.ActorId
    public string Name { get; set; }

    // 渲染相关
    public float X { get; set; }
    public float Y { get; set; }
    public float CurrentHp { get; set; }
    public float MaxHp { get; set; }

    public void Awake() { }
    public void Update(ETUnitViewComponent self) { }
}
```

#### 2.3.2 目录结构规范

```
ET.View/
├── Model/                              # View Component 定义
│   └── Unit/
│       ├── ViewComponents/
│       │   └── ETUnitViewComponent.cs
│       └── ViewSystems/
│           └── ETUnitViewComponentSystem.cs
│
├── Hotfix/                             # View System 实现
│   ├── Battle/
│   │   ├── ActorSpawnEventHandler.cs   # 单位创建
│   │   ├── ActorMoveEventHandler.cs    # 单位移动
│   │   ├── ActorDamageEventHandler.cs  # 伤害显示
│   │   ├── ActorDeadEventHandler.cs    # 死亡处理
│   │   ├── BattleStartEventHandler.cs
│   │   └── BattleEndEventHandler.cs
│   └── Unit/
│       ├── AfterUnitCreate_CreateUnitView.cs
│       ├── ChangePosition_SyncGameObjectPos.cs
│       └── ChangeRotation_SyncGameObjectRotation.cs
│
└── Platform/                           # 平台特定实现
    └── Unity/
        └── (Unity 渲染实现)
```

---

## 三、层级交互规范

### 3.1 数据流向

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 数据流向图                                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. 输入处理                                                                 │
│     用户输入 → ET.InputComponent → ET.BattleDriver → moba.core             │
│                                                                             │
│  2. 逻辑处理                                                                 │
│     moba.core (伤害/技能/移动) → 快照数据                                   │
│                                                                             │
│  3. 事件发布                                                                 │
│     ET.BattleViewEventSink.OnActorSpawn()                                   │
│     → EventSystem.Publish<ActorSpawnEvent>()                                │
│                                                                             │
│  4. 视图更新                                                                 │
│     ET.View.ActorSpawnEventHandler                                           │
│     → 创建 ET.UnitViewComponent                                              │
│     → 渲染 GameObject / ASCII                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 事件桥接模式

```csharp
// ET.Logic: 发布事件
public class ETBattleViewEventSink : IBattleViewEventSink
{
    private readonly ETBattleComponent _battleComponent;

    public void OnActorSpawn(in ActorSpawnData spawn)
    {
        var scene = _battleComponent.Scene();

        // 1. 发布到 ET 事件系统
        var evt = new ActorSpawnEvent
        {
            ActorId = spawn.ActorId,
            EntityCode = spawn.EntityCode,
            Name = spawn.Name,
            X = spawn.X,
            Y = spawn.Y
        };
        EventSystem.Instance.Publish<Scene, ActorSpawnEvent>(scene, evt);
    }
}

// ET.View: 订阅事件
[Event(SceneType.DemoBattle)]
public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
{
    protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
    {
        // 1. 创建 View Component
        var unitViewComponent = scene.AddChild<ETUnitViewComponent>();
        unitViewComponent.MobaActorId = args.ActorId;
        unitViewComponent.Name = args.Name;

        // 2. 渲染
        Log.Info($"[View] Spawn unit: {args.Name} at ({args.X}, {args.Y})");
    }
}
```

---

## 四、禁止事项清单

### 4.1 ET.Logic 禁止事项

| 禁止 | 说明 | 正确做法 |
|-----|-----|---------|
| Component 包含业务逻辑 | 违反 ET 标准 | 所有逻辑移到 System |
| View 层代码在 Logic | 职责混乱 | 移到 ET.View |
| 直接访问 moba.core 实体 | 违反单一入口 | 通过 IWorldInputSink 提交 |
| 在 Component 中调用 Log | 应在 System 中 | System 中记录日志 |
| 静态字典存储数据 | 反模式 | 使用 Component 实例存储 |

### 4.2 ET.View 禁止事项

| 禁止 | 说明 | 正确做法 |
|-----|-----|---------|
| 包含战斗逻辑 | 违反分层 | 只读取数据渲染 |
| 直接访问 moba.core | 跨层访问 | 通过 ET.Logic 事件 |
| 修改 ET.Logic 数据 | 破坏数据一致性 | 只读操作 |
| 计算伤害/Buff | 业务逻辑 | ET.Logic 负责 |

### 4.3 moba.core 禁止事项

| 禁止 | 说明 | 正确做法 |
|-----|-----|---------|
| 引用 ET 命名空间 | 破坏跨平台 | 只用接口交互 |
| 使用 Unity API | 破坏跨平台 | 使用跨平台替代 |
| 包含 ET.View 代码 | 职责混乱 | 只负责逻辑 |

---

## 五、当前问题清单

### 5.1 架构问题

| 问题 | 严重程度 | 位置 |
|-----|---------|-----|
| Component 包含业务逻辑 | 高 | ETBattleDriver, ETInputComponent |
| 静态字典存储单位 | 中 | ETUnitComponentSystem._units |
| 命名空间未细分 | 中 | 全部使用 ET.Logic |
| View 代码在 Logic 层 | 中 | Model/View/, Hotfix/View/ |
| 重复文件存在 | 低 | 需清理 |

### 5.2 代码问题

| 问题 | 文件 | 说明 |
|-----|-----|-----|
| Tick() 在 Component 中 | ETBattleDriver.cs | 应移到 System |
| AddMoveCommand() 在 Component 中 | ETInputComponent.cs | 应移到 System |
| OnUpdate() 在 Component 中 | ETBattleSkillTestComponent.cs | 应移到 System |

---

## 六、优化计划

### 阶段一：清理 Component 业务逻辑 (当前)

- [x] ETBattleDriver - 移除 Tick/Start/Stop/Destroy 方法
- [x] ETInputComponent - 移除 AddMoveCommand 等方法
- [x] ETBattleSkillTestComponent - 移除 OnUpdate 方法
- [ ] 移动 View 层代码到 ET.View

### 阶段二：统一命名空间

- [ ] ET.Logic.Battle - 战斗相关
- [ ] ET.Logic.Unit - 单位相关
- [ ] ET.Logic.Driver - 驱动相关
- [ ] ET.Logic.MobaCore - MobaCore 相关

### 阶段三：清理重复文件

- [ ] 删除 Model/View/ 目录（移到 ET.View）
- [ ] 删除 Hotfix/View/ 目录（移到 ET.View）
- [ ] 合并重复的 Component

### 阶段四：修复静态字典问题

- [ ] 将 ETUnitComponent._units 从静态改为实例

### 阶段五：验证

- [ ] dotnet build 通过
- [ ] 运行时测试通过

---

## 七、文件归属检查清单

### 应该在 ET.Logic

- [x] ETBattleComponent - 战斗生命周期
- [x] ETUnitComponent - 单位管理
- [x] ETUnit - 单位数据
- [x] ETInputComponent - 输入缓冲
- [x] ETBattleDriver - 战斗驱动
- [x] ETBattleDriverAdapter - 接口适配
- [x] ETBattleViewEventSink - 事件桥接

### 应该移到 ET.View

- [ ] ETBattleViewComponent
- [ ] ETUnitViewComponent
- [ ] ETViewTimelineComponent
- [ ] ActorSpawnEventHandler
- [ ] ActorMoveEventHandler
- [ ] ActorDamageEventHandler
- [ ] ActorDeadEventHandler

### 应该删除

- [ ] Model/View/ - View 代码应移走
- [ ] Hotfix/View/ - View 代码应移走
- [ ] 重复的 ETBattleViewEventHandler

---

## 八、验证检查清单

完成优化后验证：

```
□ Component 中没有业务逻辑方法（除空生命周期）
□ System 包含所有业务逻辑
□ 命名空间按目录细分
□ View 层代码在 ET.View 项目
□ 没有重复文件
□ dotnet build 通过
□ 运行测试通过
```
