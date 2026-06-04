# AbilityKit 图式基础组件对比与使用场景设计文档

> 阅读对象：需要在 AbilityKit 中选择 Pipeline、HFSM、Flow、BehaviorTree 等基础组件实现复杂逻辑的开发者。
>
> 文档目标：说明这些模块虽然都能表达复合结构，但它们的运行语义不同；帮助后续设计时快速判断应该选哪一个、如何组合，以及什么时候不要互相替代。

---

## 一、背景

项目中存在多种基础组件：

- `com.abilitykit.pipeline`：能力管线，偏阶段化流程。
- `com.abilitykit.hfsm`：分层有限状态机，偏稳定状态和转换。
- `com.abilitykit.flow`：轻量流程运行时，偏任务树和异步/回调流程。
- `com.abilitykit.thirdparty.behaviortreeeditor`：行为树编辑器和运行时，偏 AI 决策树和可视化配置。

这些模块都能组织复杂逻辑，也都能表达顺序、分支、并行、组合等结构。看起来它们都像“图”，甚至很多需求用任何一种都能实现。

但真正决定选型的不是“能不能画成图”，而是：

- 图节点代表什么？
- 图边代表什么？
- 谁拥有生命周期？
- 中断和清理由谁负责？
- 运行时状态是否长期稳定？
- 是否需要策划可视化配置？
- 是否需要处理异步回调、资源作用域和异常收尾？

---

## 二、统一理解：它们本质上都是图

从结构上看，这几个模块都可以视为图执行模型：

```text
Graph
  Node: 一个可执行单元
  Edge: 节点之间的关系
  Runtime: 当前执行位置和上下文
  Tick / Event: 推进图执行
  State: 运行状态
```

但它们的关键差异在于“边”的语义不同。

| 模块 | 节点语义 | 边语义 | 当前运行点 | 结果语义 |
| --- | --- | --- | --- | --- |
| Pipeline | 能力阶段 phase | 阶段顺序、条件、组合 | 当前 phase / run | running / completed / interrupted / canceled |
| HFSM | 稳定状态 state | 状态转换 transition | 当前 active state | 当前状态是什么 |
| Flow | 任务节点 node | 任务组合关系 | 当前执行节点树 | succeeded / failed / canceled |
| BehaviorTree | AI 行为/条件节点 | 决策优先级和组合关系 | 当前 tick 的决策路径 | success / failure / running |

因此它们不是简单的替代关系，而是同一种“图结构”在不同业务语义下的特化。

---

## 三、核心定位

### 3.1 Pipeline：阶段化能力流程

Pipeline 回答的问题是：一次能力或业务动作应该经历哪些阶段？

适合表达：

- 技能施法流程：前摇、吟唱、检测、消耗、释放、后摇。
- 一次复杂能力的阶段图。
- 需要调试追踪的运行中能力 run。
- 需要暂停、恢复、中断、取消的阶段化过程。
- 多个 phase 的顺序、并行、条件组合。

核心特征：

- 以 run 为中心。
- phase 有 `Execute` / `OnUpdate` / `IsComplete`。
- run 有 `Tick` / `Pause` / `Resume` / `Interrupt` / `Cancel`。
- 适合描述“一个过程从开始到结束经历哪些阶段”。

不适合：

- 长期表达一个实体当前处于哪个稳定状态。
- 高频 AI 决策优先级重评估。
- 大量异步回调、资源作用域和异常收尾。

### 3.2 HFSM：稳定状态与状态转换

HFSM 回答的问题是：实体现在是什么状态，以及什么时候切到另一个状态？

适合表达：

- 角色状态：Idle、Move、Attack、Hurt、Dead。
- 战斗单位生命周期：Spawn、Alive、Dying、Dead。
- UI 页面状态：Closed、Opening、Opened、Closing。
- 有层级的状态：Combat 下包含 Chase、Attack、Evade。
- 状态之间有明确转换条件和退出协调。

核心特征：

- 以 active state 为中心。
- state 有 `OnEnter` / `OnLogic` / `OnExit` / `OnExitRequest`。
- transition 表达状态间切换条件。
- 支持层级状态机，一个 state 可以是子状态机。
- `needsExitTime` 可以表达“不能立刻退出，要等状态自己允许退出”。

不适合：

- 一次性任务链路，例如加载资源、等待网络回包、最后清理。
- 复杂技能阶段编排时，把每个施法阶段都变成长期状态。
- AI 每帧动态选择行为优先级。

### 3.3 Flow：任务树与异步流程

Flow 回答的问题是：一串任务如何执行、等待、取消、失败和清理？

适合表达：

- 登录、匹配、加载、进入战斗等业务流程。
- UI 引导流程。
- 网络请求、资源加载、回调等待。
- 可取消、可超时、可并行的任务树。
- 需要作用域资源和可靠清理的流程。

核心特征：

- 以 task tree / workflow 为中心。
- node 有 `Enter` / `Tick` / `Exit` / `Interrupt`。
- 返回 `FlowStatus`：Running、Succeeded、Failed、Canceled。
- 支持 `Wake/Pump`，适合事件回调触发推进。
- `FlowContext` 支持 scope，用于上下文注入和自动回收。
- `FlowRunner` 捕获异常并统一收尾。

不适合：

- 作为角色长期状态机。
- 作为 AI 决策树的语义底座。
- 对每帧极高频的战斗微行为做过重编排。

### 3.4 BehaviorTree：AI 决策树和可视化行为配置

BehaviorTree 回答的问题是：AI 在当前条件下应该做什么？

适合表达：

- NPC/怪物 AI 决策。
- 巡逻、追击、攻击、逃跑、技能选择。
- 条件优先级重评估。
- 策划可视化编辑行为逻辑。
- Selector、Sequence、Parallel、Decorator 组合。

核心特征：

- 以每次 tick 的决策路径为中心。
- action / condition 返回 Running、Success、Failure。
- selector 表达优先级选择。
- sequence 表达条件和动作链。
- 支持 blackboard 和 shared value。
- 行为树编辑器提供可视化配置入口。

不适合：

- 技能释放的严格阶段生命周期。
- 资源加载、网络回调等异步业务流程。
- 表达实体当前稳定状态，尤其是有明确状态转换和退出条件时。

---

## 四、快速选择表

| 需求 | 首选模块 | 原因 |
| --- | --- | --- |
| 技能前摇、吟唱、释放、后摇 | Pipeline | 阶段清晰，适合 run 级中断和追踪 |
| 角色 Idle/Move/Attack/Hurt/Dead | HFSM | 稳定状态和转换是主语 |
| 角色移动中播放一段组合行为 | HFSM + Action/Behavior 或 Flow | 状态由 HFSM 管，状态内行为可组合 |
| 登录、加载、匹配、进入战斗 | Flow | 异步回调、取消、异常和资源清理更重要 |
| UI 引导步骤 | Flow | 步骤链、等待点击、超时、清理自然 |
| 怪物巡逻/追击/攻击/逃跑 | BehaviorTree 或 HFSM | 决策优先级复杂用 BT，状态稳定性强用 HFSM |
| Boss 多阶段战斗状态 | HFSM + BehaviorTree | 大阶段用 HFSM，阶段内策略用 BT |
| 复杂技能内部多阶段弹幕 | Pipeline-backed sequence | 发射器外层生命周期不变，内部阶段用 pipeline 编排 |
| 战斗伤害计算流程 | Service + Pipeline 可选 | 固定计算链可用服务/管线，但不要引入长期状态 |
| 网络请求等待并恢复流程 | Flow | Wake/Pump 和异常收尾更贴合 |
| 策划想可视化配置 AI | BehaviorTreeEditor | 编辑器和 blackboard 更贴合 |
| 策划想可视化配置能力阶段 | Pipeline 图资产方向 | phase/run 语义比 BT 更贴合技能阶段 |

---

## 五、判断准则

### 5.1 先问“主语是什么”

| 主语 | 推荐模块 |
| --- | --- |
| 当前状态是什么 | HFSM |
| 一次能力经历哪些阶段 | Pipeline |
| 一串任务如何完成和收尾 | Flow |
| AI 当前应该选哪个行为 | BehaviorTree |

不要先问“哪个模块也能做”。几乎都能做，但用错主语会让后续维护成本变高。

### 5.2 看生命周期归属

| 生命周期特征 | 推荐模块 |
| --- | --- |
| 长期存在，持续响应事件和转换 | HFSM |
| 一次运行，有开始和结束 | Pipeline |
| 一次任务，有成功/失败/取消 | Flow |
| 每帧重新评估优先级 | BehaviorTree |

### 5.3 看边的含义

| 边的含义 | 推荐模块 |
| --- | --- |
| A 阶段完成后进入 B 阶段 | Pipeline / Flow |
| 状态 A 满足条件切到状态 B | HFSM |
| 条件 A 失败则尝试条件 B | BehaviorTree |
| 子任务 A 成功后执行子任务 B | Flow |

### 5.4 看失败语义

| 失败语义 | 推荐模块 |
| --- | --- |
| 阶段被打断，整个能力取消 | Pipeline |
| 任务失败，需要走错误处理和资源释放 | Flow |
| 条件失败，只是尝试下一个行为 | BehaviorTree |
| 状态不叫失败，只是切换到另一个状态 | HFSM |

---

## 六、组合使用建议

这些模块可以组合，但要遵守“外层只保留一个生命周期 owner”的原则。

### 6.1 HFSM 外层，状态内使用 Flow

适合：角色状态稳定，但某个状态进入后要执行一串可取消任务。

```text
CharacterHFSM
  State: Interact
    OnEnter: start Flow
    OnLogic: step Flow
    OnExit: stop Flow
```

例子：进入交互状态后，先转身、播放动作、等待 UI 确认、发请求、展示结果。

### 6.2 HFSM 外层，状态内使用 BehaviorTree

适合：角色大状态稳定，状态内决策复杂。

```text
BossHFSM
  State: Phase1
    BT: 巡逻 / 追击 / 普攻 / 技能A
  State: Phase2
    BT: 召唤 / 范围攻击 / 狂暴追击
```

HFSM 管大阶段切换，BT 管当前阶段内每帧选什么行为。

### 6.3 Pipeline 外层，phase 内调用服务或 Flow

适合：技能施法阶段清晰，但某个 phase 需要等待异步结果。

```text
SkillCastPipeline
  Phase: CheckCost
  Phase: LoadPresentationAssetFlow
  Phase: ReleaseProjectile
```

Pipeline 管技能 run，Flow 管异步加载或回调等待。

### 6.4 Flow 外层，节点里驱动 HFSM

项目中已经有 `HfsmFlowRunner`，说明 HFSM 可以作为 Flow 的一部分被驱动。

适合：某个流程步骤中，需要一个短期状态机辅助完成局部逻辑。

```text
Flow
  Node: StartLocalStateMachine
  Node: WaitUntilStateMachineComplete
  Node: Cleanup
```

注意：如果这个状态机会长期伴随实体存在，就不应该藏在 Flow 里。

### 6.5 BehaviorTree 节点调用 Pipeline 或 Flow

适合：AI 决策选中某个行为后，行为本身是复杂流程。

```text
BehaviorTree
  Selector
    Condition: CanCastSkill
    Action: StartSkillPipeline
```

BT 只负责“选不选这个行为”，不要让 BT 直接展开完整技能阶段。技能阶段交给 Pipeline。

---

## 七、避免误用

### 7.1 不要因为都像图就随意替换

下面这些替换通常会带来后续维护问题：

| 错误倾向 | 问题 |
| --- | --- |
| 用 BT 表达完整技能释放阶段 | BT 的失败语义是决策失败，不是能力中断 |
| 用 HFSM 表达一次性加载流程 | 状态机会被大量临时状态污染 |
| 用 Pipeline 表达角色长期状态 | run 结束语义和 active state 语义冲突 |
| 用 Flow 表达 AI 高频决策 | Flow 更重视任务收尾，不适合每帧优先级重评估 |
| 用 Pipeline 包所有小步骤 | 简单逻辑会被阶段图拖重 |

### 7.2 不要让多个模块同时拥有同一生命周期

一个业务对象外层只能有一个生命周期 owner。

例如复杂投射物发射器：

```text
MobaProjectileLaunchContinuous  owns lifecycle
  PipelineProjectileLaunchSequence owns inner stage run
    Phase: Prepare
    Phase: Charge
    Phase: Release
```

这里外层中断入口仍是 continuous，pipeline 只是 sequence 内部的编排工具。不要让 continuous、pipeline run、projectile schedule 三者都独立决定“发射是否结束”。

### 7.3 不要把模块边界变成技术偏好

选型应该来自业务语义，而不是“这个模块更强”。

一个经验规则：

```text
稳定身份/状态 -> HFSM
一次能力阶段 -> Pipeline
一次任务流程 -> Flow
每帧决策选择 -> BehaviorTree
```

---

## 八、与 MOBA 战斗系统的推荐映射

| MOBA 场景 | 推荐设计 |
| --- | --- |
| 战斗整体流程：加载、准备、开局、结算 | Flow 或 HFSM，取决于是否长期状态化 |
| 战斗单位生命周期 | HFSM |
| 单位 AI | BehaviorTree；Boss 大阶段可外包给 HFSM |
| 技能施法流程 | Pipeline；当前持续行为系统作为战斗运行时生命周期入口 |
| Buff 生命周期 | 持续行为系统；内部复杂阶段可用 Pipeline |
| 投射物发射器 | 默认 sequence/pattern；复杂多阶段时用 pipeline-backed sequence |
| 伤害结算 | 统一 service/pipeline 可选；不要用 BT/HFSM |
| 表现播放和等待回调 | Flow |
| 战斗外 UI 流程 | Flow |

---

## 九、扩展方向建议

### 9.1 建立统一图模型术语

后续文档可以统一使用这些词：

| 术语 | 含义 |
| --- | --- |
| Graph | 可执行结构，不限定具体模块 |
| Node | 图中的执行单元 |
| Edge | 节点关系，语义由模块决定 |
| Runtime | 一次运行或当前实例 |
| Owner | 生命周期拥有者 |
| Context | 运行上下文和依赖入口 |
| Blackboard | 决策共享数据，主要用于 BT/AI |
| Phase | Pipeline 的阶段节点 |
| State | HFSM 的稳定状态节点 |
| FlowNode | Flow 的任务节点 |
| BehaviorNode | BT 的行为/条件节点 |

### 9.2 做适配器，而不是强行合并

不建议把这些模块合并成一个万能图系统。更合适的是做轻量适配器：

- HFSM state 内运行 Flow。
- Flow node 内驱动 HFSM。
- BT action 启动 Pipeline。
- Pipeline phase 等待 Flow 完成。
- Pipeline-backed sequence 用于复杂发射器。

适配器要保持边界清晰：谁启动、谁 tick、谁 interrupt、谁 cleanup 必须明确。

### 9.3 图编辑器能力可以共享，但运行时语义不要混淆

未来如果要做统一编辑器，可以共享：

- 节点绘制。
- 连线绘制。
- 搜索和节点库。
- 参数面板。
- 运行时调试 overlay。

但运行时仍应保持不同语义：

- Pipeline phase graph。
- HFSM state graph。
- Flow task tree。
- BehaviorTree decision tree。

统一的是编辑器基础设施，不是业务语义。

---

## 十、结论

Pipeline、HFSM、Flow、BehaviorTree 都是图，但它们解决的问题不同。

最重要的选择准则是：

- 想表达“现在是什么状态”，选 HFSM。
- 想表达“一次能力有哪些阶段”，选 Pipeline。
- 想表达“一串任务如何完成、失败、取消和清理”，选 Flow。
- 想表达“AI 当前应该做什么”，选 BehaviorTree。

组合使用时，先确定外层生命周期 owner，再把其他模块作为内部实现细节接入。这样既能利用各模块的组合能力，又不会让中断、完成、清理和调试责任散掉。

---

*最后更新：2026-06-02*
