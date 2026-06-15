# Step 4.5–4.8 路线图：表现分离 + Domain 拆分

> 目标：从当前 `GameFlowDomain`（684 行 god object）演进为"比 ET 更灵活、比 god object 更规范"的分层架构。
> 前置条件：Step 4.1–4.4 已完成（`IGameHost`/`ILogSink`/`IFeatureBinder` 三个宿主抽象已落地，162/162 测试通过）。

---

## 路径依赖关系

```
Step 4.5（路径 C：OnGUI 移出 Domain）
    ↓ Domain 不再直接包含表现代码
Step 4.6（路径 B：IFlowCommandSink + IPresentationSink 事件桥接）
    ↓ 表现层与逻辑层的通信契约确立
Step 4.7（路径 A：拆分 GameFlowDomain 为编排器 + 策略集合）
    ↓ 子组件通过 Sink 接口与表现层通信
Step 4.8（路径 D：Feature 数据/行为可选分离，远期）
```

**为什么是 C → B → A 而不是 A → B → C？**
- 路径 C 风险最低（~35 行 GUI 代码迁移），立即可验证
- 路径 B 需要一个"干净的 Domain"才能定义清晰的 Sink 接口——如果 Domain 内还有 OnGUI，Sink 接口的边界会模糊
- 路径 A 是最大改动，需要 B 的 Sink 接口作为拆分后子组件的通信契约

---

## Step 4.5：OnGUI 表现代码移出 Domain（路径 C）

### 目标
将 `GameFlowDomain.OnGUI()` 中 ~35 行调试 GUI 代码移到独立的 `RootDebugOnGUIFeature`，使 Domain 的 `OnGUI()` 方法只做 `_features.OnGUI(in _ctx)` 委托调用。

### 当前问题分析

`GameFlowDomain.OnGUI()`（161-197 行）包含两类代码：
1. **Feature 委托**：`_features.OnGUI(in _ctx)` — 这是对的，Domain 应该只做委托
2. **直接 GUI 逻辑**（168-196 行）：显示 HFSM 状态 + Enter Battle / Battle End / Return Lobby 按钮 — 这部分直接在 Domain 内使用 `GUILayout`，违反了"Domain 不包含表现代码"的原则

同时，已有的 Feature（`BootMenuOnGUIFeature`、`BattleDebugOnGUIFeature`）通过 `ctx.Entry.Get<GameFlowDomain>()` 反向获取 Domain 来调用 `EnterBattle()`、`ReturnToBoot()` 等方法。这意味着 Feature 需要一个**命令接口**来操作 Domain，而不是直接引用具体类型。

### 实施步骤

#### 4.5a：在 Flow.Core 定义 `IFlowCommandSink` 接口

```csharp
// Flow/Core/IFlowCommandSink.cs
namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 表现层向流程编排层提交命令的接口。
    /// Feature/View 通过此接口请求流程变更，不直接引用 GameFlowDomain。
    /// </summary>
    public interface IFlowCommandSink
    {
        void RequestEnterBattle(IBattleBootstrapper bootstrapper = null);
        void RequestBattleEnd();
        void RequestReturnLobby();
    }
}
```

#### 4.5b：`GameFlowDomain : IFlowCommandSink`

让 `GameFlowDomain` 实现 `IFlowCommandSink`，将现有方法映射到接口：
- `RequestEnterBattle` → `EnterBattle()`
- `RequestBattleEnd` → 通过 `_battleFsm.Trigger(MobaBattleEvent.Ended)` 实现
- `RequestReturnLobby` → `ReturnToBoot()`

#### 4.5c：创建 `RootDebugOnGUIFeature`

将 Domain 内 168-196 行的 GUI 代码移到新 Feature：
- 通过 `ctx.Entry.Get<IFlowCommandSink>()` 获取命令接口
- Feature 实现 `IGamePhaseFeature` + `IOnGUIFeature`
- 注册到 Boot/Lobby 阶段的 Feature Plan 中

#### 4.5d：简化 `GameFlowDomain.OnGUI()`

Domain 的 `OnGUI()` 简化为：
```csharp
public void OnGUI()
{
#if UNITY_EDITOR
    _features.OnGUI(in _ctx);
#endif
}
```

#### 4.5e：更新 `BootMenuOnGUIFeature` 和 `BattleDebugOnGUIFeature`

将 `ctx.Entry.Get<GameFlowDomain>()` 替换为 `ctx.Entry.Get<IFlowCommandSink>()`，消除 Feature 对 `GameFlowDomain` 具体类型的依赖。

#### 4.5f：测试验证
- 桌面 xUnit 测试通过
- Unity Demo Build 0 错误
- OnGUI 功能行为不变（Enter Battle / Battle End / Return Lobby 按钮正常工作）

### 预期收益
- `GameFlowDomain` 减少 ~35 行 GUI 代码
- `OnGUI()` 方法只做委托，Domain 不再包含任何直接表现逻辑
- Feature 不再依赖 `GameFlowDomain` 具体类型，只依赖 `IFlowCommandSink` 接口
- 为 Step 4.6 的 `IPresentationSink` 铺路（双向通信契约的"命令"方向已确立）

---

## Step 4.6：引入 IPresentationSink 事件桥接（路径 B）

### 目标
建立 Logic→View 的事件推送契约，使 Flow 编排层完全不知道 View 的存在。

### 当前问题分析

当前 Flow 层向 View 层传递信息的方式：
1. **Feature 直接操作**：`BattleViewFeature`、`BattleHudFeature` 等在 Feature 内直接访问 `ctx.Root` 获取数据
2. **无事件通知**：状态变化（如 `MobaRootState` 从 Lobby 变为 Battle）没有通知机制，View 层只能轮询

ET 的做法是 `IETViewEventSink`——Logic 层通过接口推送事件，View 层订阅。但 ET 的接口是扁平的（20+ 方法平铺），且是单向的。

### 实施步骤

#### 4.6a：在 Flow.Core 定义 `IPresentationSink` 接口

```csharp
// Flow/Core/IPresentationSink.cs
namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Flow 编排层向表现层推送事件的接口。
    /// 灵感来自 ET 的 IETViewEventSink，但按关注点分离为多个子接口。
    /// </summary>
    public interface IPresentationSink
    {
        void OnPhaseChanged(MobaRootState root, MobaBattleState battle);
        void OnBattleStart();
        void OnBattleEnd();
        void OnError(string message);
    }
}
```

#### 4.6b：`GameFlowDomain` 构造函数注入 `IPresentationSink`

通过 DI 或构造函数参数注入。测试时传 mock，Unity 宿主传真实实现。

#### 4.6c：在状态机回调中推送事件

在 `BuildMobaRootStateMachine` 和 `BuildMobaBattleStateMachine` 的 `onEnter` 回调中，除了现有的 `_rootStateBindings.Enter()` 调用外，增加 `_presentationSink.OnPhaseChanged(_activeRoot, _activeBattle)` 调用。

#### 4.6d：Unity 宿主实现 `IPresentationSink`

在 `GameEntry` 或独立类中实现，处理：
- 日志输出
- 未来扩展：UI 状态切换、场景加载触发等

#### 4.6e：测试验证
- 桌面 xUnit 测试：验证 `IPresentationSink` 在状态变化时被正确调用
- Unity Demo Build 0 错误

### 预期收益
- Flow 层通过接口推送表现事件，完全不知道 View 的存在
- 比 ET 的 `IETViewEventSink` 更优：按关注点分离，不是 20+ 方法平铺
- 与 Step 4.5 的 `IFlowCommandSink` 形成**双向通信契约**：
  - `IFlowCommandSink`：View → Logic（命令方向）
  - `IPresentationSink`：Logic → View（事件方向）

---

## Step 4.7：拆分 GameFlowDomain 为编排器 + 策略集合（路径 A）

### 目标
将 684 行的 `GameFlowDomain` god object 拆分为职责单一的子组件，每个子组件可独立测试。

### 当前 GameFlowDomain 职责分布

| 职责 | 行数 | 说明 |
|------|------|------|
| 字段 + 构造函数 | ~120 | 状态字段、DI 注入、HFSM 初始化 |
| 公共 API | ~130 | Start/Tick/OnGUI/SwitchTo/Attach/Detach |
| 状态机构建 | ~146 | Root + Battle FSM 构建 + 转移配置 |
| Feature 绑定构建 | ~95 | Boot/Battle Plan + State Binding |
| Battle 生命周期 | ~167 | Session 事件、Scope 管理、推进决策 |
| Action 派发 | ~40 | FlowAction 执行 |
| EntityFeatureBinder | ~23 | IEntity 桥接 |

### 拆分方案

```
GameFlowDomain（684 行 god object）
    ↓ 拆分为
GameFlowOrchestrator（编排门面，~200 行）
    ├── 持有所有子组件引用
    ├── 公共 API（Start/Tick/OnGUI）委托给子组件
    └── 构造函数组装子组件

FlowStateMachineBuilder（状态机构建，~150 行）
    ├── BuildMobaRootStateMachine()
    ├── BuildMobaBattleStateMachine()
    ├── AddRootTransitions() / AddBattleTransitions()
    └── 纯逻辑，可独立测试

FeatureScheduler（Feature 生命周期，~120 行）
    ├── PhaseFeatureHost 管理
    ├── PhaseFeaturePlan / Registry 构建
    ├── Attach/Detach/Clear 调度
    └── 纯逻辑，可独立测试

BattleScopeManager（per-battle scope，~100 行）
    ├── BattleWorldScopeHost 封装
    ├── EnterBattle / ReturnToBoot scope 管理
    ├── BattleSessionFeature 创建 + 事件订阅
    └── 纯逻辑，可独立测试

FlowActionDispatcher（动作派发，~50 行）
    ├── MobaFlowActionExecutor / SwitchExecutor 封装
    ├── ExecuteFlowAction / ExecuteSwitchFlow
    └── 已有基础（Step 4.2 已拆出 executor 接口）
```

### 实施步骤

#### 4.7a：提取 `FlowStateMachineBuilder`

将 `BuildMobaRootStateMachine`、`BuildMobaBattleStateMachine`、`AddRootTransitions`、`AddBattleTransitions`、`EvaluateRootTransitionCondition`、`BuildFlowConditionContext` 提取为独立类。

这是最大的一块（~150 行），且是纯逻辑——不依赖任何 Unity 类型。

#### 4.7b：提取 `FeatureScheduler`

将 `BuildBootFeaturePlan`、`BuildBattleFeaturePlan`、`BuildMobaRootStateBindings`、`BuildMobaBattleStateBindings`、所有 `Build*Binding` 方法、`AttachFeatureCore`、`DetachFeatureCore`、`TickFeatureCore`、`ClearFeatures` 提取为独立类。

#### 4.7c：提取 `BattleScopeManager`

将 `EnterBattle`、`ReturnToBoot`、`OnBattleSessionStarted`、`OnBattleFirstFrameReceived`、`OnBattleSessionFailed`、`CreateBattleSessionFeature`、`ResetBattleSessionRuntimeState`、`TryAdvanceOnStateEnter`、`ReturnLobbyAfterBattleEnd` 提取为独立类。

#### 4.7d：简化 `GameFlowDomain` 为 `GameFlowOrchestrator`

Domain 变为薄编排层：
- 构造函数创建子组件
- `Start()` 委托给 `_stateMachine.Start()` + `_scopeManager.EnterBattle()`
- `Tick()` 委托给 `_stateMachine.Step()` + `_featureScheduler.Tick()`
- `OnGUI()` 委托给 `_featureScheduler.OnGUI()`

#### 4.7e：测试验证
- 每个提取的子组件都有对应的 xUnit 测试
- 集成测试验证编排器行为与拆分前一致
- Unity Demo Build 0 错误

### 预期收益
- `GameFlowOrchestrator` 从 684 行降到 ~200 行
- 每个子组件职责单一，可独立测试
- 比 ET 的 Model/Hotfix 分离更优：按职责拆分而非按数据/行为拆分
- 新增子组件都是纯 C#，零 Unity 依赖

---

## Step 4.8：Feature 数据/行为可选分离（路径 D，远期）

### 目标
为复杂 Feature 提供可选的数据/行为分离模式，简单 Feature 保持当前模式不变。

### 设计原则

**不是强制分离**（像 ET 那样），而是**可选分离**：
- 简单 Feature（如 `BootMenuOnGUIFeature`）：数据和 behavior 在同一个类（当前模式）
- 复杂 Feature（如 `BattleSyncFeature`）：可以拆为 `FeatureState`（数据）+ `FeatureLogic`（行为），通过 DI scope 共享状态

### 实施步骤（概要，远期细化）

1. 定义 `IFeatureState` 接口标记接口
2. 定义 `IFeatureLogic<TState>` 接口
3. 在 `PhaseFeatureHost` 中支持可选的 State + Logic 注册
4. 选择一个复杂 Feature 作为试点（如 `BattleSyncFeature`）

### 预期收益
- 比 ET 的"一刀切"更灵活——开发者按需选择
- 复杂 Feature 的状态可独立测试
- 简单 Feature 不增加任何复杂度

---

## 最终架构愿景（Step 4.8 完成后）

```
┌─────────────────────────────────────────────────────────┐
│ 表现层 - Unity 宿主                                      │
│ - GameEntry : MonoBehaviour, IGameHost                  │
│ - IPresentationSink 实现                                 │
│ - RootDebugOnGUIFeature 等 Unity 特化 Feature           │
│ - BootMenuOnGUIFeature / BattleDebugOnGUIFeature        │
│ - 通过 IFlowCommandSink 提交命令                         │
│ - 禁止：任何流程逻辑、状态判断                            │
└──────────────────────────┬──────────────────────────────┘
                           │ IGameHost + IPresentationSink
                           │ IFlowCommandSink
┌──────────────────────────▼──────────────────────────────┐
│ Flow 编排层 - 纯 C#，Unity 可选                           │
│                                                         │
│ GameFlowOrchestrator（编排门面，~200 行）                │
│   ├── FlowStateMachineBuilder（HFSM 构建+驱动）         │
│   ├── FeatureScheduler（Feature 生命周期）               │
│   ├── BattleScopeManager（per-battle DI scope）          │
│   └── FlowActionDispatcher（动作派发）                   │
│                                                         │
│ Flow.Core/（纯 C# 核心）                                │
│   - IGameHost, ILogSink, IFeatureBinder（宿主抽象）     │
│   - IFlowCommandSink（View→Logic 命令接口）             │
│   - IPresentationSink（Logic→View 事件接口）            │
│   - IPhaseFeature（Feature 插件契约）                    │
│   - MobaFlowConfiguration（声明式配置）                  │
│   - BattleWorldModule（DI 模块注册）                     │
│                                                         │
│ 关键约束：零 Unity 依赖，桌面 xUnit 完整覆盖             │
└──────────────────────────┬──────────────────────────────┘
                           │ 调用
┌──────────────────────────▼──────────────────────────────┐
│ AbilityKit 核心框架层                                   │
│ - IEntity / EntityWorld（ECS）                          │
│ - AbilityKit.Core / World.DI / HFSM.Core               │
└─────────────────────────────────────────────────────────┘
```

### 双向通信契约

```
  View 层                    Flow 编排层
  ┌──────┐  IFlowCommandSink  ┌──────────┐
  │      │ ──────────────────>│          │
  │      │  RequestEnterBattle│          │
  │      │  RequestBattleEnd  │          │
  │      │  RequestReturnLobby│          │
  │      │                    │          │
  │      │<──────────────────│          │
  │      │  IPresentationSink │          │
  │      │  OnPhaseChanged    │          │
  │      │  OnBattleStart     │          │
  │      │  OnBattleEnd       │          │
  └──────┘                    └──────────┘
```

---

## 对比总结：优化后 vs ET

| 维度 | ET 框架 | 优化后的当前项目 |
|------|---------|-----------------|
| 数据/行为分离 | 强制文件级 Model/Hotfix | **按职责灵活拆分**，可选数据/行为分离 |
| 逻辑/表现分离 | 进程级 Logic/View Fiber | **接口级**双向契约，更轻量 |
| 状态机 | switch-case 无层次化 | **HFSM 层次化 + 配置驱动** |
| Feature 管理 | 手动 Add/RemoveComponent | **声明式绑定 + 按阶段自动挂载/卸载** |
| 生命周期 | 手动管理 | **DI Scope 自动 Dispose** |
| 跨层通信 | 多个独立接口 | **IFlowCommandSink + IPresentationSink 双向契约** |
| 可测试性 | 需 mock Entity 体系 | **纯 C#，xUnit 直接覆盖** |
| 框架依赖 | 强依赖 ET | **零框架锁定** |
| 特化能力 | 受限于 ECS 规范 | **Feature 可按需选择复杂度** |
