# MOBA 依赖注入与 System/Service 协作深潜

> 本文说明 MOBA 示例为什么把主要业务逻辑放在 Service 层，把 System 层收敛成调度与遍历入口，并通过世界级依赖注入把配置、诊断、时间、事件、实体索引和能力执行解耦出来。

---

## 1. 设计目标

MOBA 的依赖注入与协作模式，核心不是“把对象都注入进去”，而是让不同职责落在不同层：

| 层级 | 职责 | 典型类型 |
|------|------|----------|
| 世界装配层 | 声明服务、系统、模块与生命周期 | `MobaWorldBootstrapModule`、`MobaServicesAutoModule` |
| 服务层 | 承载业务规则、状态机、配置读取、事件发布、校验逻辑 | `SkillCastCoordinator`、`MobaBuffService`、`MobaEffectInvokerService` |
| System 层 | 只负责按顺序调度、遍历 ECS、驱动服务执行 | `MobaSkillPipelineStepSystem`、`MobaBuffCommandDrainSystem`、`MobaProjectileSyncSystem` |
| 基础设施层 | 提供日志、诊断、异常策略、时间、实体索引、事件总线 | `MobaWorldSystemExecution`、`GameServiceBase`、`LogicWorldServiceBase` |

设计收益：

- System 变薄，减少和具体玩法逻辑的耦合。
- Service 更容易被单元测试，因为它们通常只依赖少量外部服务。
- 世界装配保持声明式，便于替换、裁剪和分层复用。
- 依赖注入让诊断、异常策略、时间源、配置源都能在测试里替换。

---

## 2. 源码入口

| 类型 | 源码 | 说明 |
|------|------|------|
| 服务自动注册模块 | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/Bootstrap/MobaServicesAutoModule.cs` | 按命名空间批量注册服务 |
| 世界引导模块 | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/MobaWorldBootstrapModule.cs` | 进入 Flow Bootstrap 并安装系统 |
| 系统顺序定义 | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/MobaSystemOrder.cs` | 规定 System 执行顺序 |
| 系统协作辅助 | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/MobaWorldSystemExecution.cs` | 统一 Resolve / Warn / Require / HandleException |
| 服务基类 | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Services/Templates/GameServiceBase.cs` | 统一日志、生命周期、事件发布 |
| 技能调度 System | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/Skill/MobaSkillPipelineStepSystem.cs` | 只遍历实体并调用 `SkillCastCoordinator` |
| Buff 调度 System | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/Buffs/MobaBuffCommandDrainSystem.cs` | 只触发 `MobaBuffService.DrainPending()` |
| 投射物同步 System | `Unity/Packages/com.abilitykit.demo.moba.runtime/Runtime/Application/Systems/Projectile/MobaProjectileSyncSystem.cs` | 调度投射物事件处理和快照输出 |

---

## 3. 世界装配：DI 是协作边界，不是业务入口

### 3.1 `MobaServicesAutoModule` 负责批量注册

`MobaServicesAutoModule` 把服务注册拆成三个命名空间组：

- `AbilityKit.Demo.Moba.Services`
- `AbilityKit.Demo.Moba.Gameplay`
- `AbilityKit.Demo.Moba.Systems`
- `AbilityKit.Demo.Moba.Util`

它并不直接关心每个服务怎么做业务，只负责把对应程序集里满足 `WorldService` 规则的类型装进容器。

```mermaid
flowchart TB
    Bootstrap[MobaWorldBootstrapModule] --> Auto[MobaServicesAutoModule]
    Auto --> App[MobaApplicationServicesModule]
    Auto --> Sys[MobaApplicationSystemsServicesModule]
    Auto --> Infra[MobaInfrastructureServicesModule]
    App --> Attr[AttributeWorldServicesModule]
    Sys --> Attr
    Infra --> Attr
    Attr --> Container[WorldContainer]
```

这种设计的好处是：

- 新增 Service 不需要手写一堆注册样板。
- 按命名空间分层后，逻辑服务、系统辅助服务、基础设施服务边界清晰。
- 可以单独替换某一层的服务集合，不影响其它层。

### 3.2 `WorldService` 和 `WorldInject` 让依赖关系显式化

MOBA 中很多服务都通过 `[WorldService]` 声明生命周期，再通过 `[WorldInject]` 获取依赖。例如 `MobaBuffService`：

- 自身是入口服务，负责把请求收敛成命令队列。
- 通过注入拿到 `MobaActorLookupService`、`MobaConfigDatabase`、`IMobaEffectiveTagQueryService` 等依赖。
- 再把复杂生命周期交给 `BuffLifecycleExecutor`。

这种写法的关键收益是：

- 依赖是可见的，不需要在方法内部到处 `Resolve`。
- 测试时可以替换注入对象，验证单个服务的行为。
- 生命周期对象和外部协作对象解耦。

---

## 4. System/Service 的协作方式

### 4.1 System 只做调度，不承载主要业务规则

MOBA 的 System 典型模式是：

1. 在 `OnInit` 里解析少量服务。
2. 在 `OnExecute` 里遍历 ECS 实体或读取时钟。
3. 调用某个 Service 的一个高层方法。
4. 记录诊断、处理异常、继续执行。

例如 `MobaSkillPipelineStepSystem`：

- 只负责拿到 `SkillCastCoordinator`、`IWorldClock` 和 actor group。
- 遍历所有 actor。
- 对每个 actor 调 `SkillCastCoordinator.Step(actorId)`。
- 用 `MobaWorldSystemExecution` 记录统计和错误。

```mermaid
sequenceDiagram
    participant Sys as WorldSystem
    participant DI as WorldResolver
    participant Svc as Service
    participant Diag as Diagnostics

    Sys->>DI: TryResolve services
    Sys->>Sys: select entities / read clock
    Sys->>Svc: Step(actorId)
    alt ok
        Svc-->>Sys: success
    else exception
        Sys->>Diag: HandleException
    end
    Sys->>Diag: Sample / RecordDuration
```

这意味着：

- System 是“什么时候执行”的问题。
- Service 是“执行什么业务”的问题。
- 两者分工后，System 不会膨胀成大段业务逻辑。

### 4.2 Service 才是测试友好的逻辑单元

以 `SkillCastCoordinator` 为例，它通过构造函数接收：

- `IWorldResolver`
- `IWorldClock`
- `IFrameTime`
- `IEventBus`
- `IUnitResolver`
- `MobaSkillLoadoutService`
- `MobaActorLookupService`
- `IMobaSkillPipelineLibrary`
- 可选的诊断和异常策略

这种依赖形式让它非常适合做单元测试：

- 可以 mock `IWorldClock` 来控制时间。
- 可以 mock `IUnitResolver` 来控制实体解析。
- 可以替换 `IMobaSkillPipelineLibrary` 来验证不同技能管线。
- 可以注入假的诊断和异常策略来检查日志与错误边界。

换句话说，MOBA 的主业务不是写在 System 里，而是写在可组合、可替换、可测试的 Service 里。

### 4.3 `GameServiceBase` 统一了服务基类能力

`GameServiceBase.cs` 里提供了几层基类：

- `LogicWorldServiceBase<TService>`：统一日志、释放、`ObjectDisposedException`。
- `LogicWorldInitializableServiceBase<TService>`：统一 `IWorldResolver` 注入。
- `LogicWorldLifecycleServiceBase<TService>`：统一初始化/反初始化。
- `LogicWorldEventServiceBase<TService>`：统一事件总线发布。

它的意义是把重复的“服务基础设施”抽出来，让业务服务专注于：

- 规则。
- 状态。
- 协作。
- 输出。

---

## 5. `MobaWorldSystemExecution` 的作用

`MobaWorldSystemExecution` 是 System 层和 Service 层之间的轻量胶水：

- `Resolve(...)`：一次性解析诊断和异常策略。
- `Warn(...)`：优先走诊断服务，没有诊断时回退到全局日志。
- `Require(...)`：做前置条件校验。
- `HandleException(...)`：统一异常包装、分域和降级处理。
- `Sample(...)` / `RecordDuration(...)`：统一采样。

这样做的优点是：

- System 代码里不会重复写一套日志和异常模板。
- 诊断体系可以统一升级，不用逐个 System 改。
- 业务代码只关心“是否执行成功”，不关心“日志基础设施怎么落地”。

---

## 6. 典型协作链路

### 6.1 技能执行链路

```mermaid
flowchart TB
    Input[PlayerInputCommand] --> Sys[MobaSkillPipelineStepSystem]
    Sys --> Coordinator[SkillCastCoordinator]
    Coordinator --> Loadout[MobaSkillLoadoutService]
    Coordinator --> ActorLookup[MobaActorLookupService]
    Coordinator --> Library[IMobaSkillPipelineLibrary]
    Coordinator --> EventBus[IEventBus]
    Coordinator --> Trace[Trace / Context / Effect]
```

这里 System 只负责把 actor 批量送进 Coordinator，真正的技能判定、输入阶段处理、运行态切换、失败原因解释都在 Service 里。

### 6.2 Buff 命令链路

```mermaid
flowchart LR
    System[MobaBuffCommandDrainSystem] --> Service[MobaBuffService]
    Service --> Queue[Pending Command Queue]
    Queue --> Lifecycle[BuffLifecycleExecutor]
    Lifecycle --> Tags[Tag Query / Tag Template]
    Lifecycle --> Snapshot[Presentation / Snapshot]
```

`MobaBuffCommandDrainSystem` 只负责调用 `DrainPending`，避免系统自己去理解 Buff 生命周期、重入保护、标签终止、连续效果同步。

### 6.3 投射物同步链路

`MobaProjectileSyncSystem` 更像一个事件路由器：

- 从 `IProjectileService` 取出待处理事件。
- 调用不同 handler。
- 依赖 `MobaEntityManager`、`MobaActorRegistry`、`MobaTriggerExecutionGateway`、`MobaTraceRegistry` 等服务完成实际业务。

这种方式的意义是：

- 投射物事件分发是 System 的职责。
- 命中、退出、生成、销毁、快照、追踪才是服务/处理器的职责。

---

## 7. 为什么这种模式更适合测试和演进

### 7.1 更容易做单元测试

Service 层可以直接构造并注入 mock：

- 输入源可控。
- 时间可控。
- 事件总线可控。
- 配置可控。
- 实体索引可控。

所以测试往往可以绕开完整 World，直接验证：

- 某个输入是否触发正确技能。
- 某个 Buff 是否进入正确生命周期。
- 某个异常是否被正确归类。
- 某个诊断指标是否被采样。

### 7.2 更容易替换实现

因为 System 只依赖接口和少量门面，MOBA 可以：

- 替换 `IEventBus`。
- 替换 `IWorldClock`。
- 替换 `IMobaSkillPipelineLibrary`。
- 替换 `IMobaBattleExceptionPolicy`。
- 替换诊断实现。

这让同一套逻辑可以在：

- 本地单机。
- 客户端远程驱动。
- 服务端权威世界。
- 测试环境。

之间复用。

### 7.3 更容易保持 System 顺序稳定

`MobaSystemOrder` 把执行顺序显式化，避免 System 之间靠“碰巧注册顺序”工作。

这对于复杂战斗特别重要，因为：

- 实体管理、移动、技能、效果、Buff、持续运行时、投射物清理都存在前后依赖。
- 顺序一旦不稳定，就会出现难以复现的问题。

---

## 8. 设计约束与扩展点

### 8.1 约束

- System 不应堆叠过多业务规则，否则会破坏分层。
- Service 不应直接假设所有依赖都存在，必要依赖要做显式校验。
- DI 注册要与生命周期匹配，避免把短生命周期对象注册成单例。
- System 的执行顺序要和实体生命周期、快照时序保持一致。
- 测试替身必须尽量保持接口语义一致，否则会掩盖时序问题。

### 8.2 扩展点

| 扩展点 | 用法 |
|--------|------|
| 新服务类型 | 新增 `IService` / `IWorldInitializable` / `IWorldDeinitializable` 实现 |
| 新 System | 只负责调度新服务，不把业务逻辑写进 System |
| 新诊断策略 | 扩展 `MobaWorldSystemExecution` 或注入新的诊断接口 |
| 新注册分组 | 给 `MobaServicesAutoModule` 增加新的命名空间模块 |
| 新测试场景 | 用 mock resolver、clock、event bus、config database 构造服务 |

---

## 9. 小结

MOBA 的设计重点是把“运行时调度”和“业务规则执行”拆开：

- `System` 负责顺序、遍历、帧节拍和异常兜底。
- `Service` 负责技能、Buff、投射物、效果、验证、诊断等主要逻辑。
- `DI` 负责把这些能力组合成可替换、可测试、可分层复用的世界。

这也是为什么 MOBA 的战斗代码看起来很多，但主逻辑并没有散落在 System 中，而是集中在可组合的服务单元里。

---

## 下一步

- [MOBA Demo 专题总览](./00-Overview.md)
- [世界启动与运行时装配](./01-WorldAndBootstrap.md)
- [技能执行深潜](./05-SkillExecutionDeepDive.md)

---

*文档版本：v1.0 | 最后更新：2026-06-23*
