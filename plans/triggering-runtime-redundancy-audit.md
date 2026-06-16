# Triggering Runtime 冗余实现与目录整理审计

## 1. 当前稳定主线

当前触发器包稳定主线建议继续收敛到以下路径：

- [`Runtime/Runtime/TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)：事件订阅、派发顺序、短路控制、生命周期与 Cue 回调主入口。
- [`Runtime/Plan/PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)：计划触发器执行器，负责 Predicate/Action 解析与执行。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：可序列化触发器计划结构。
- [`Runtime/Plan/Json`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json)：JSON 计划加载与转换。
- [`Runtime/ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)：Action 级调度执行，服务于 `PlannedTrigger` 的延迟、周期、持续型 Action。
- [`Runtime/Eventing`](../Unity/Packages/com.abilitykit.triggering/Runtime/Eventing)：事件键、事件总线与事件 Schema。
- [`Runtime/Registry`](../Unity/Packages/com.abilitykit.triggering/Runtime/Registry)：Predicate 与 Action 委托注册表。
- [`Runtime/Variables/Numeric`](../Unity/Packages/com.abilitykit.triggering/Runtime/Variables/Numeric)：数值变量域与表达式求值。

正式化推进应优先保证这些主线目录的 API、注释、异常处理与运行语义稳定。

## 2. 明确冗余与非主线实现

### 2.1 根目录兼容文件过多

[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime) 根目录仍保留 16 个兼容入口文件，全部以 `Deprecated root-level compatibility file` 标记：

- [`ActionContext.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionContext.cs)
- [`ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionDelegateAdapter.cs)
- [`ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionExecutor.cs)
- [`ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionInstance.cs)
- [`ActionScheduler.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler.cs)
- [`ContextAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ContextAdapter.cs)
- [`EventBusDispatcher.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/EventBusDispatcher.cs)
- [`ExecCtxAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ExecCtxAdapter.cs)
- [`ITriggerDispatcher.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ITriggerDispatcher.cs)
- [`NumericValueRefContextExtensions.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/NumericValueRefContextExtensions.cs)
- [`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/PlannedTrigger.cs)
- [`TimedDispatcher.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TimedDispatcher.cs)
- [`TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerDispatcherHub.cs)
- [`TriggerDispatcherHub_new.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerDispatcherHub_new.cs)
- [`TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerExecutor.cs)
- [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerRunner.cs)

问题：

1. 根目录同时存在稳定目录与兼容文件，容易误导新代码继续引用旧入口。
2. [`TriggerDispatcherHub_new.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerDispatcherHub_new.cs) 命名带 `_new`，即使已废弃也会造成目录观感不正式。
3. 这些兼容文件仍参与编译，未来删除或迁移需要明确版本策略。

建议：

- 短期：保留兼容文件，但统一中文注释并补充删除条件。
- 中期：建立 `Runtime/Compatibility` 目录承载兼容入口，或在根目录增加兼容入口清单文档。
- 长期：在一次 major 版本中移除根目录兼容文件。

### 2.2 `TriggerScheduler` 是非主线并且仍有占位执行

[`Runtime/TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerScheduler/TriggerExecutor.cs) 已标记 `TODO-OPTIMIZE`，并已镜像到 [`Runtime/Experimental/Todo/TriggerScheduler/TriggerExecutorTodo.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/TriggerScheduler/TriggerExecutorTodo.cs)。

主要问题：

- 使用占位 Action 委托，仍包含 `Console.WriteLine`。
- `CreateActionDelegate` 未接入 `ActionRegistry`。
- `CreateConditionDelegate` 未接入 `FunctionRegistry`。
- 注册 Action 时使用旧的 `Register` 语义，不如当前主线 [`ActionScheduler.RegisterOrReplace`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionScheduler.cs) 明确。
- 与 [`PlannedTrigger`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs) 职责重叠，但能力不完整。

建议：

- 短期：不要把它接回主线；改成更明确的兼容/实验提示。
- 中期：将可复用的“触发器级执行策略”概念迁入 `Plan/Executables`，不要保留平行执行链路。
- 长期：如果 `Plan/Executables` 完全覆盖该能力，应删除此路径或只保留实验目录镜像。

### 2.3 `Executable` 与 `Plan/Executables` 形成两套行为树/可执行节点体系

[`Runtime/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 是一套较完整的行为树式执行系统，但当前稳定主线已转向 [`Runtime/Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)。

重叠点：

- `Executable` 有 `IExecutable`、组合节点、修饰器、转换器、示例与注册表。
- `Plan/Executables` 有 `ITriggerPlanExecutable`、Sequence/Selector/Parallel/Repeat/Until 等计划执行节点。
- 二者都表达“可组合执行逻辑”，但数据结构、返回结果、调度接入方式并未统一。

当前风险：

- 包体积和理解成本偏高。
- 新业务不知道应该使用 `Executable` 还是 `Plan/Executables`。
- [`Executable/IExecutable.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/IExecutable.cs) 仍有目标检测 TODO，占位语义不适合主线暴露。
- 示例文件含 `Console.WriteLine`，不适合运行时主包长期保留在默认编译路径。

建议：

- 短期：继续标记为非主线能力，并在设计文档中明确推荐主线是 `Plan/Executables`。
- 中期：将示例移动到 `Samples~` 或 `Documentation~`，避免 Runtime 编译示例代码。
- 中期：把 Decorator、ScheduledExecutable 等成熟概念逐个迁移到 `Plan/Executables`。
- 长期：若保留 `Executable`，应改名为 `LegacyExecutable` 或 `Experimental/Executable`，避免与主线混淆。

### 2.4 `Scheduler`、`Schedule`、`ActionScheduler` 三套调度概念并存

当前存在三套调度相关目录：

- [`Runtime/ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)：Action 级调度，当前已接入 `PlannedTrigger` 主线。
- [`Runtime/Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)：句柄式调度管理器，包含 `SimpleScheduleManager`、`GroupedScheduleManager`、`DefaultScheduleManager`。
- [`Runtime/Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)：另一套回调式调度器与注册表。

主要问题：

- `Schedule` 与 `Scheduler` 命名过近，职责边界不清。
- `Schedule.Data.ScheduleItemState` 和 `Scheduler.SchedulerData` 都定义了 `EScheduleMode`，但位于不同命名空间，容易产生概念重复。
- [`Schedule/DefaultScheduleManager.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule/DefaultScheduleManager.cs) 标记过时，但提示“请使用 TriggerScheduleManager 替代”，当前目录中未看到同名主线类型，提示不够准确。
- `ActionScheduler` 已成为触发器主线调度能力，`Schedule`/`Scheduler` 更像通用或历史调度能力。

建议：

- 短期：文档中明确 `ActionScheduler` 是 TriggerPlan Action 主线调度器。
- 短期：给 `Scheduler` 目录补充 legacy/experimental 说明，避免误用。
- 中期：将 `Schedule` 定位为通用行为调度能力；将 `Scheduler` 迁移、合并或废弃。
- 长期：统一调度命名，建议仅保留：
  - `ActionScheduler`：计划 Action 调度。
  - `Schedule`：通用句柄式调度。
  - 删除或迁移 `Scheduler`。

### 2.5 `Dispatcher` 与 `Runtime/TriggerRunner` 两条派发入口并存

[`Runtime/Runtime/TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs) 是当前事件触发主线，而 [`Runtime/Dispatcher/TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs) 仍提供另一套 `Event/Timed/Continuous` 调度中心。

问题：

- `TriggerDispatcherHub` 内有临时注释：`✅ 新增`、`✅ 更新`，不符合正式代码风格。
- `TriggerDispatcherHub` 使用 `TriggerDispatcherRegistry`、`TimedDispatcher` 的路径更偏旧式 dispatcher 主线。
- `TriggerRunner` 与 `TriggerDispatcherHub` 的推荐使用边界需要在设计文档中明确。

建议：

- 短期：清理临时注释，补充正式中文说明。
- 中期：文档明确：事件计划触发优先走 `TriggerRunner`；`TriggerDispatcherHub` 只保留给持续行为/旧 dispatcher 集成。
- 长期：如果 `TriggerRunner + ActionScheduler` 能覆盖多数能力，逐步将 `Dispatcher` 降级为兼容层。

## 3. 现存 TODO 与待优化热点

### 3.1 P0：会误导主线或存在占位运行逻辑

- [`Runtime/TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerScheduler/TriggerExecutor.cs)：占位委托、`Console.WriteLine`、未接 Registry，建议隔离或废弃。
- [`Runtime/Dispatcher/TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs)：临时注释需要正式化。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：`ActionCallPlan` 可表达 `arity > 2`，但主线 `PlannedTrigger` 传统委托只稳定支持 0/1/2，具名参数路径虽扩展了表达能力，但需要明确运行边界。
- [`Runtime/ActionScheduler/ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionDelegateAdapter.cs)：`BuildExecCtx` 仍是 TODO，占位返回默认值，若被主线调用会存在上下文缺失风险。

### 3.2 P1：结构存在但语义未完全闭环

- [`Runtime/ActionScheduler/ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionExecutor.cs)：重试执行器 TODO 依赖调度器支持延迟执行。
- [`Runtime/ActionScheduler/ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionInstance.cs)：回滚策略与 Timeline 模式仍是 TODO。
- [`Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs)：表达式型 `NumericValueRef` 尚未集成表达式编译器。
- [`Runtime/Context/ExecCtxAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Context/ExecCtxAdapter.cs)：实体查找器仍为占位实现。

### 3.3 P2：文档、示例与目录观感问题

- [`Runtime/Experimental/Todo/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/README.md) 和 [`Runtime/Experimental/Todo/Executable/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/Executable/README.md) 仍为英文，应切换为中文。
- [`Runtime/TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md) 没有充分反映 `ActionScheduler`、`Plan/Executables` 与非主线目录的当前状态。
- [`Runtime/Executable/ExecutableExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ExecutableExamples.cs) 和 [`Runtime/Executable/RefactoredExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/RefactoredExamples.cs) 示例代码在 Runtime 默认编译路径中，建议迁出。

## 4. 目录规划建议

### 4.1 推荐稳定目录分层

```text
Runtime/
  ActionScheduler/          # TriggerPlan Action 调度主线
  Eventing/                  # 事件键、事件总线、事件 Schema
  Runtime/                   # TriggerRunner、ExecCtx、ExecutionControl、生命周期
  Plan/                      # TriggerPlan、PlannedTrigger、Predicate/Action 计划
    Json/                    # JSON 加载转换
    Executables/             # 计划级可执行节点
  Registry/                  # Function/Action registry
  Variables/                 # 数值变量与表达式
  Blackboard/ Payload/       # 数据读取来源
  Dispatcher/                # 旧 dispatcher 与持续行为兼容层
  Schedule/                  # 通用句柄式调度，需确认是否保留
  Experimental/Todo/         # 非主线能力迁移区
  Compatibility/             # 未来承载根目录兼容入口
```

### 4.2 建议降级或迁移目录

- `Runtime/TriggerScheduler`：建议从默认主线视角降级为 `Experimental/Todo` 或 `Compatibility`。
- `Runtime/Scheduler`：建议标记为 legacy 或合并进 `Schedule`。
- `Runtime/Executable`：建议标记为非主线行为系统，成熟节点逐步迁入 `Plan/Executables`。
- 根目录兼容文件：建议建立 `Compatibility` 迁移计划。

## 5. 推荐下一轮低风险整理

按收益与风险排序，建议下一轮执行：

1. 清理 [`TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs) 临时注释，并补充 `ActionSchedulerManager` 的正式职责说明。
2. 将 [`Experimental/Todo/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/README.md) 与 [`Experimental/Todo/Executable/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/Executable/README.md) 中文化，明确非主线迁移规则。
3. 给 [`Runtime/Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 增加目录级 legacy 说明，避免与 `Schedule`、`ActionScheduler` 混淆。
4. 修正 [`Schedule/DefaultScheduleManager.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule/DefaultScheduleManager.cs) 的过时提示，改为指向真实存在的 `SimpleScheduleManager` 或 `GroupedScheduleManager`。
5. 将 [`TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerScheduler/TriggerExecutor.cs) 的英文 TODO-OPTIMIZE 说明中文化，并去掉 `Console.WriteLine` 占位执行，改为显式失败或异常，避免误执行。

## 6. 推荐中期重构路线

1. 主线巩固：继续拆分 [`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)，特别是 Predicate 解析、Action 解析、NamedArgs 解析与调度执行。
2. 调度收敛：明确 `ActionScheduler` 与 `Schedule` 的边界，废弃或合并 `Scheduler`。
3. 行为树收敛：将 `Executable` 的成熟概念迁移到 `Plan/Executables`，避免双体系长期并行。
4. 兼容入口收敛：根目录兼容文件统一中文注释，增加删除版本条件，后续迁入 `Compatibility` 或删除。
5. 文档同步：更新 [`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)，让新使用者只看到一条推荐主线。
