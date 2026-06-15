# ET 分层设计 vs 当前项目 Strangler Fig 迁移方向 — 架构对比分析

## 一、两种架构的核心分层模型

### 1.1 ET 框架的分层

```
┌─────────────────────────────────────────────────────────┐
│ ET.View（表现层）                                        │
│ - GameObject 渲染、动画、UI、VFX、SFX                    │
│ - 只读 ET.Logic 缓存数据                                 │
│ - 事件驱动：订阅 ET.Logic 发布的事件                     │
│ - 禁止：任何战斗逻辑、直接访问 moba.core                  │
└──────────────────────────┬──────────────────────────────┘
                           │ 读取数据 / 订阅事件
┌──────────────────────────▼──────────────────────────────┐
│ ET.Logic（逻辑层）= Model + Hotfix                      │
│                                                         │
│ Model/:  Component（纯数据）                             │
│   - ETBattleComponent : Entity（字段+空生命周期）        │
│   - ETUnit : Entity（纯属性数据）                        │
│   - ETMobaBattleDriver : Entity, IBattleDriver          │
│   - ETFlowComponent : Entity（流程状态数据）             │
│   - Presentation/Cache/（快照缓存数据）                  │
│                                                         │
│ Hotfix/: System（行为逻辑，扩展方法）                    │
│   - ETBattleComponentSystem（战斗生命周期）              │
│   - ETFlowComponentSystem（流程状态机驱动）              │
│   - ETBattleViewEventSink（逻辑→表现事件桥接）          │
│   - ETUnitComponentSystem（单位管理行为）                │
│                                                         │
│ 关键约束：Component 只存数据，System 包含所有逻辑        │
└──────────────────────────┬──────────────────────────────┘
                           │ 调用服务
┌──────────────────────────▼──────────────────────────────┐
│ moba.core / AbilityKit（核心框架层）                     │
│ - 所有战斗逻辑实现（伤害、技能、Buff、碰撞）             │
│ - ECS 世界管理                                          │
│ - 无 ET 依赖                                            │
└─────────────────────────────────────────────────────────┘
```

**ET 的关键设计模式：**

- **数据/行为分离**：`Model/` 中 Component 只有字段和空生命周期方法；`Hotfix/` 中 System 以 `static partial class` 扩展方法形式实现全部行为
- **Entity-Component-System**：ET 的 ECS 变体——Entity 是实体容器，Component 挂载在 Entity 上，System 以扩展方法操作 Component
- **事件桥接**：`IETViewEventSink` 接口定义在 `ET.Share` 中，`ET.Logic` 的 `ETBattleViewEventSink` 实现它，将逻辑事件推送到 View 层
- **单向依赖**：`Logic → Share ← View`

### 1.2 当前项目的分层（Step 4.4 后）

```
┌─────────────────────────────────────────────────────────┐
│ Unity 宿主层                                            │
│ - GameEntry : MonoBehaviour, IGameHost                  │
│ - MonoBehaviour 生命周期驱动（Awake/Start/Update/OnGUI） │
│ - 协程启动（StartCoroutine）                            │
│ - OnGUI 调试面板                                        │
└──────────────────────────┬──────────────────────────────┘
                           │ IGameHost 接口
┌──────────────────────────▼──────────────────────────────┐
│ GameFlowDomain（流程编排层，684 行）                     │
│ - HFSM 状态机（Boot/Lobby/Battle 子状态机）             │
│ - PhaseFeatureHost（Feature 生命周期管理）               │
│ - BattleWorldScopeHost（per-battle DI scope）            │
│ - MobaFlowActionExecutor/SwitchExecutor（流程动作派发）  │
│ - 依赖注入：ILogSink, IFeatureBinder, IGameHost         │
│                                                         │
│ Flow/Core/（纯 C# 核心，已抽离）                        │
│   - IMobaFlowActionTarget（动作目标接口）                │
│   - IGameHost（宿主抽象接口）                            │
│   - IFeatureBinder（Feature 绑定抽象）                   │
│   - MobaFlowConfiguration（流程配置）                    │
│   - MobaBattleAdvanceDecider（状态推进决策）             │
│   - BattleWorldModule/ScopeHost（世界生命周期）          │
└──────────────────────────┬──────────────────────────────┘
                           │ 调用
┌──────────────────────────▼──────────────────────────────┐
│ AbilityKit 核心框架层                                   │
│ - IEntity / EntityWorld（ECS）                          │
│ - AbilityKit.Core（日志、配置）                          │
│ - AbilityKit.World.DI（DI scope）                       │
└─────────────────────────────────────────────────────────┘
```

**当前项目的关键设计模式：**

- **Strangler Fig 渐进迁移**：从 god object 逐步抽离纯逻辑到 Flow.Core
- **接口抽象解耦**：`IGameHost`、`ILogSink`、`IFeatureBinder` 三个宿主抽象
- **HFSM 状态机驱动**：层次化状态机管理 Boot→Lobby→Battle 流程
- **Feature 插件化**：`IPhaseFeature<TContext>` 接口，Feature 按阶段挂载/卸载
- **DI Scope 管理**：per-battle 的依赖注入作用域

---

## 二、逐维度对比分析

### 2.1 数据与行为的分离


| 维度       | ET 框架                                              | 当前项目                                                         |
| -------- | -------------------------------------------------- | ------------------------------------------------------------ |
| **分离粒度** | **文件级**：Model/ 存数据，Hotfix/ 存行为                     | **类型级**：接口（`IGameHost`等）存契约，`GameFlowDomain` 同时持有数据和逻辑       |
| **强制程度** | **编译期强制**：Component 中写业务逻辑违反 ET 规范，Code Review 可检测 | **约定级**：靠构造函数注入和接口约束，但 `GameFlowDomain` 仍是 684 行的 god object |
| **可测试性** | System 是 static 扩展方法，需 mock Entity 体系              | Flow.Core 中的纯逻辑可独立测试（162 个测试），但 `GameFlowDomain` 本体仍难测试      |


**分析**：

- ET 的 Model/Hotfix 分离是**结构性**的——同一概念的数据和行为在物理上分属不同文件，由 ET 源码生成器桥接
- 当前项目的分离是**接口级**的——通过 `IGameHost`/`ILogSink`/`IFeatureBinder` 切断对 Unity 的直接依赖，但 `GameFlowDomain` 内部仍是数据+行为混合体
- ET 的方式更彻底但更重（需要 ECS 框架支撑）；当前项目的方式更轻量但不够彻底

### 2.2 逻辑与表现的分离


| 维度            | ET 框架                                                            | 当前项目                                                                 |
| ------------- | ---------------------------------------------------------------- | -------------------------------------------------------------------- |
| **分离方式**      | **进程级**：Logic 和 View 是不同的 ET Fiber/Scene，通过事件系统通信                | **类型级**：`IGameHost` 接口解耦，但 GameFlowDomain 仍包含 `OnGUI` 等 Unity 表现代码   |
| **通信机制**      | `IETViewEventSink`（Logic→View 推送）+ `IETInputSink`（View→Logic 提交） | `GamePhaseContext` 传递 `IGameHost`，Features 直接调用 `ctx.Entry.Get<T>()` |
| **View 层独立性** | View 有独立的 Component/System 体系，只读缓存数据                             | 无独立 View 层——Features（如 `BootMenuOnGUIFeature`）直接在 Flow 层内            |


**分析**：

- ET 的 Logic↔View 分离是**架构级**的——两层有独立的数据模型（`ETUnit` vs `ETUnitViewComponent`），通过事件桥接
- 当前项目的表现分离还在**起步阶段**——`OnGUI` 仍在 `GameFlowDomain` 中（虽然被 `#if UNITY_EDITOR` 保护），Features 如 `BootMenuOnGUIFeature` 直接使用 `GUILayout`
- ET 的 `IETViewEventSink` 是一个关键的架构接口——它让 Logic 层完全不知道 View 的存在，只通过接口推送事件

### 2.3 状态管理


| 维度       | ET 框架                                                       | 当前项目                                                         |
| -------- | ----------------------------------------------------------- | ------------------------------------------------------------ |
| **状态载体** | Entity + Component（ET 的 ECS）                                | `GameFlowDomain` 字段 + `IEntity`（AbilityKit 的 ECS）            |
| **状态机**  | `ETFlowComponent` + `ETFlowComponentSystem`（手写 switch-case） | HFSM 层次化状态机（`StateMachine<,>`）+ 配置驱动                         |
| **生命周期** | ET 的 `IAwake/IUpdate/IDestroy` 由框架调度                        | `GameEntry` 的 Unity 生命周期 → `GameFlowDomain.Start/Tick/OnGUI` |


**分析**：

- ET 的状态管理更**分散**——每个 Component 只管自己的状态，System 操作 Component
- 当前项目的状态管理更**集中**——`GameFlowDomain` 是单一状态枢纽，HFSM 提供了更强大的状态编排能力
- ET 的 `ETFlowComponentSystem` 用 switch-case 实现状态机，比 HFSM 简单但缺乏层次化和配置驱动能力
- 当前项目的 `MobaFlowConfiguration` + `PhaseStateFeatureRegistry` 提供了声明式的状态→Feature 绑定，这是 ET 所没有的

### 2.4 可替换性（Unity 作为可选运行项）


| 维度       | ET 框架                                        | 当前项目                                                              |
| -------- | -------------------------------------------- | ----------------------------------------------------------------- |
| **宿主替换** | ET 自身就是跨进程的——Server/Client 共享 Logic，View 可替换 | `IGameHost` 接口允许非 Unity 实现，Console Demo 已验证                       |
| **测试策略** | ET 的 Server 逻辑可在纯 C# 环境运行（无 Unity）           | Flow.Core 可在桌面 xUnit 测试（162 个测试），但 `GameFlowDomain` 本体仍需 Unity 类型 |
| **热更新**  | ET 原生支持 Hotfix 层热更新（Model 不变，Hotfix 可替换）     | 当前无热更新支持——`GameFlowDomain` 是 sealed class                         |


**分析**：

- ET 的 Model/Hotfix 分离天然支持热更新——Model 是稳定的契约，Hotfix 是可替换的行为
- 当前项目的 Strangler Fig 迁移目标是让 Unity 成为可选运行项，但尚未达到 ET 那种进程级可替换性
- ET 的热更新能力是一个重要的架构优势，尤其在移动端游戏开发中

### 2.5 复杂度与学习曲线


| 维度       | ET 框架                                                                    | 当前项目                                                               |
| -------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------ |
| **框架依赖** | 强依赖 ET 框架（Entity/Component/System/EventSystem/Fiber）                     | 轻量依赖（IEntity/EntityWorld + HFSM 第三方库）                              |
| **代码生成** | 需要 ET SourceGenerator（`[EntitySystem]` 等）                                | 无代码生成，纯手写                                                          |
| **概念数量** | Entity, Component, System, Event, Fiber, Scene, ComponentOf, FriendOf... | IGameHost, ILogSink, IFeatureBinder, IPhaseFeature, HFSM, DI Scope |
| **调试难度** | System 是 static 扩展方法，调用栈跨越多个 partial class                               | `GameFlowDomain` 是单一类，调用栈线性                                        |


---

## 三、当前项目相对于 ET 的优势

1. **HFSM 状态机更强大**：层次化状态机 + 配置驱动 + 声明式 Feature 绑定，比 ET 的 switch-case 状态机更具表达力和可维护性
2. **Feature 插件化架构**：`IPhaseFeature<TContext>` 接口让功能模块可以按阶段挂载/卸载，比 ET 的 Component 更轻量、更灵活
3. **DI Scope 管理**：`BattleWorldScopeHost` 提供了 per-battle 的依赖注入作用域，生命周期管理比 ET 的手动 Component 添加/移除更系统化
4. **渐进式迁移**：Strangler Fig 模式允许在不重写的情况下逐步改善架构，风险可控
5. **调试友好**：`GameFlowDomain` 是单一类，调用栈清晰；ET 的 static 扩展方法模式让调试跳转分散
6. **无框架锁定**：不依赖特定 ECS 框架，`IGameHost`/`ILogSink`/`IFeatureBinder` 是通用抽象

## 四、当前项目相对于 ET 的劣势

1. **God Object 未消除**：`GameFlowDomain` 仍是 684 行的 god object，数据和行为混合。ET 的 Model/Hotfix 分离在文件级强制了数据/行为分离
2. **表现层未独立**：`OnGUI` 仍在 Domain 中，Features 如 `BootMenuOnGUIFeature` 直接使用 Unity API。ET 有独立的 View 层和事件桥接接口
3. **无热更新支持**：ET 的 Model/Hotfix 分离天然支持热更新；当前项目的 sealed class 无法热更新
4. **状态分散度不足**：所有流程状态集中在 `GameFlowDomain`，而 ET 将状态分散到多个 Component（`ETBattleComponent`、`ETFlowComponent`、`ETInputComponent` 等），每个 Component 职责单一
5. **缺少事件桥接接口**：ET 有 `IETViewEventSink` / `IETInputSink` 明确的层间通信契约；当前项目的 Features 直接通过 `GamePhaseContext` 访问宿主，耦合度更高
6. **可测试性差距**：ET 的 System 可针对 Component 单独测试；当前项目的核心逻辑（状态机构建、Feature 绑定）仍在 `GameFlowDomain` 内部，难以单独测试

---

## 五、改进方向建议

基于对比分析，以下是当前项目可以借鉴 ET 设计的方向（按优先级排序）：

### 5.1 引入 Logic→View 事件桥接接口（高优先级）

借鉴 ET 的 `IETViewEventSink`，定义 `IBattleViewSink` 接口：

- Flow 层只通过接口推送表现事件（单位创建/销毁、伤害显示、状态变化）
- Unity 宿主提供实现（创建 GameObject、播放动画等）
- 纯 C# 测试可提供 mock 实现

### 5.2 拆分 GameFlowDomain 为数据+行为（中优先级）

借鉴 ET 的 Model/Hotfix 模式（不需要完全照搬）：

- 将 `GameFlowDomain` 的状态字段提取为 `GameFlowState` 数据类
- 将行为逻辑提取为独立的策略/服务类
- 保持 `GameFlowDomain` 作为编排门面，但内部委托给分离的组件

### 5.3 将 OnGUI 等表现代码移出 Domain（高优先级）

- `OnGUI` 已被 `#if UNITY_EDITOR` 保护，但仍在 Domain 内
- 应移到独立的 `IGamePhaseFeature` 或 `IOnGUIFeature` 实现中
- Domain 只负责调用 `_features.OnGUI(in _ctx)`，不直接包含 GUI 逻辑

### 5.4 考虑 Feature 的数据/行为分离（低优先级，远期）

- 当前 Feature 同时持有状态和行为
- 可借鉴 ET 模式，将 Feature 拆为 FeatureState（数据）+ FeatureLogic（行为）
- 但这增加了复杂度，需权衡收益

---

## 六、总结


|          | ET 框架                      | 当前项目                                   |
| -------- | -------------------------- | -------------------------------------- |
| **核心理念** | ECS + 数据/行为分离 + 进程级逻辑/表现分离 | Strangler Fig 渐进迁移 + 接口抽象 + HFSM 状态编排  |
| **分离粒度** | 文件级（Model vs Hotfix）       | 接口级（IGameHost/ILogSink/IFeatureBinder） |
| **优势**   | 强制分离、热更新、进程隔离、状态分散         | 渐进改善、调试友好、状态机强大、Feature 插件化、无框架锁定      |
| **劣势**   | 框架重、学习曲线高、调试分散             | God Object 未消除、表现层未独立、无热更新             |
| **适用场景** | 大型多人在线游戏、需要热更新的项目          | 中小型项目、快速迭代、已有代码库渐进改善                   |


**核心结论**：两种架构并不矛盾。当前项目的 Strangler Fig 迁移可以借鉴 ET 的数据/行为分离和事件桥接思想，但不需要完全照搬 ET 的 ECS 模式。下一步应聚焦于：

1. 将表现代码（OnGUI）移出 Domain
2. 引入 Logic→View 事件桥接接口
3. 继续拆分 GameFlowDomain 的职责到独立的纯逻辑类

