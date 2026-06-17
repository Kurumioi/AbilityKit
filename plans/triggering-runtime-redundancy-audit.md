# Triggering Runtime 冗余实现与目录整理审计

## 1. 当前稳定主线

当前触发器包稳定主线建议继续收敛到以下路径：

- [`Runtime/Runtime/TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)：事件订阅、派发顺序、短路控制、生命周期与 Cue 回调主入口。
- [`Runtime/Plan/PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)：计划触发器执行编排入口，Predicate、参数解析、Action 绑定和调度注册已拆到独立 helper。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：可序列化触发器计划结构。
- [`Runtime/Plan/Json`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json)：JSON 计划加载与转换。
- [`Runtime/Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)：计划级可执行节点主线。
- [`Runtime/ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)：Action 级调度执行，服务于 `PlannedTrigger` 的延迟、周期、持续型 Action。
- [`Runtime/Eventing`](../Unity/Packages/com.abilitykit.triggering/Runtime/Eventing)：事件键、事件总线与事件 Schema。
- [`Runtime/Registry`](../Unity/Packages/com.abilitykit.triggering/Runtime/Registry)：Predicate 与 Action 委托注册表。
- [`Runtime/Variables/Numeric`](../Unity/Packages/com.abilitykit.triggering/Runtime/Variables/Numeric)：数值变量域与表达式求值。

正式化推进应优先保证这些主线目录的 API、注释、异常处理与运行语义稳定。

## 2. 明确冗余与非主线实现

### 2.1 根目录兼容文件过多

[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime) 根目录仍保留多个兼容入口文件，全部以 `Deprecated root-level compatibility file` 或类似兼容说明标记。

问题：

1. 根目录同时存在稳定目录与兼容文件，容易误导新代码继续引用旧入口。
2. [`TriggerDispatcherHub_new.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerDispatcherHub_new.cs) 命名带 `_new`，即使已废弃也会造成目录观感不正式。
3. 这些兼容文件仍参与编译，未来删除或迁移需要明确版本策略。

建议：

- 已完成：[`Runtime/Compatibility.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility.md) 已建立根目录兼容入口清单、替代路径和删除条件。
- 短期：保留兼容文件，不在根目录兼容入口中新增功能。
- 中期：如需进一步收口，可建立 `Runtime/Compatibility` 目录承载兼容入口。
- 长期：在一次 major 版本中移除根目录兼容文件。

### 2.2 `Legacy/TriggerScheduler` 是非主线兼容路径

[`Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs) 已降级为兼容路径，并已镜像到 [`Runtime/Experimental/Todo/TriggerScheduler/TriggerExecutorTodo.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/TriggerScheduler/TriggerExecutorTodo.cs)。

已完成：

- 带条件 `TriggerPlan` 会显式失败，不再注册 `null` 条件委托后表现为成功。
- 该路径不再作为主线执行入口；主线条件与 Action 解析走 [`PlannedTrigger`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)。

剩余建议：

- 短期：继续保持兼容/实验提示，不把它接回主线。
- 中期：将可复用的“触发器级执行策略”概念迁入 `Plan/Executables`，不要保留平行执行链路。
- 长期：如果 `Plan/Executables` 完全覆盖该能力，应删除此路径或只保留实验目录镜像。

### 2.3 `Executable` / `Legacy/Executable` 与 `Plan/Executables` 形成两套行为树/可执行节点体系

[`Runtime/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 是仍在默认编译路径中的旧行为树式执行系统；[`Runtime/Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 保留旧 DSL、旧配置转换器和兼容模块入口。当前稳定主线已转向 [`Runtime/Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)。

重叠点：

- `Runtime/Executable` 有 `IExecutable`、组合节点、修饰器、调度包装器与注册表。
- `Runtime/Legacy/Executable` 有旧 `ExecutableDsl`、旧配置转换器、旧调度工厂注册表与模块入口。
- `Plan/Executables` 有 `ITriggerPlanExecutable`、Sequence/Selector/Parallel/Repeat/Until 等计划执行节点。
- 二者都表达“可组合执行逻辑”，但数据结构、返回结果、调度接入方式并未统一。

当前风险：

- 包体积和理解成本偏高。
- 新业务不知道应该使用 `Executable` / `Legacy/Executable` 还是 `Plan/Executables`。
- 旧 DSL 已标记过时，但成熟概念仍需要逐项评估是否迁入主线。
- 示例代码不适合运行时主包长期保留在默认编译路径。

建议：

- 已完成：旧示例已移动到 `Documentation~`，避免 Runtime 默认编译示例代码。
- 已完成：[`TriggerPlanExecutableDsl.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/TriggerPlanExecutableDsl.cs) 已补齐常量参数 Action、命名参数 Action、组合节点 guard/weight 构造、RandomSelector、Success/NoOp、AlwaysSuccess/Failure/AlwaysFail/Not 与条件表达式桥接入口，承接旧 DSL 常见构造方式、组合节点守卫和反转装饰器最小入口。
- 已完成：[`ExecutableRegistry.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ExecutableRegistry.cs) 已将旧内建 Executable/Condition 改为显式注册，默认构造路径不再运行时扫描程序集；Attribute 扫描仅保留为兼容扩展的按需入口。
- 短期：继续标记为非主线能力，并在设计文档中明确推荐主线是 `Plan/Executables`。
- 中期：把 `Runtime/Executable` 中剩余 Decorator、ScheduledExecutable 等成熟概念逐个迁移到 `Plan/Executables`，并继续观察外部扩展是否仍依赖旧 Registry Attribute 扫描。
- 长期：若保留 `Runtime/Executable` / `Legacy/Executable`，应只作为兼容/实验入口，避免与主线混淆。

### 2.4 `Scheduler`、`Schedule`、`ActionScheduler` 三套调度概念并存

当前存在三套调度相关目录：

- [`Runtime/ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)：Action 级调度，当前已接入 `PlannedTrigger` 主线。
- [`Runtime/Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)：句柄式调度管理器，包含 `SimpleScheduleManager`、`GroupedScheduleManager`。
- [`Runtime/Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)：另一套旧版回调式调度器与注册表。

已完成：

- [`Runtime/TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md) 已描述三者边界。
- [`Runtime/Legacy/Schedule/DefaultScheduleManager.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Schedule/DefaultScheduleManager.cs) 的过时提示已指向真实存在的 `SimpleScheduleManager` / `GroupedScheduleManager`。
- [`Runtime/Scheduler/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler/README.md) 已增加目录级兼容说明。

剩余建议：

- 中期：将 `Scheduler` 的实际调用方迁移、合并或废弃。
- 长期：统一调度命名，建议仅保留：
  - `ActionScheduler`：计划 Action 调度。
  - `Schedule`：通用句柄式调度。
  - 删除或迁移 `Scheduler`。

### 2.5 `Dispatcher` 与 `Runtime/TriggerRunner` 两条派发入口并存

[`Runtime/Runtime/TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs) 是当前事件触发主线，而 [`Runtime/Dispatcher/TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs) 仍提供另一套 `Event/Timed/Continuous` 调度中心。

已完成：

- `TriggerDispatcherHub` 的临时注释已清理为正式中文说明。
- `TriggerDispatcherHub` 已明确自身是兼容聚合入口，新事件订阅、条件评估和执行控制优先使用 `TriggerRunner`。

已完成：

- [`Runtime/Dispatcher/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/README.md) 已补充目录级兼容层说明。
- 文档已明确：事件计划触发优先走 `TriggerRunner`；`TriggerDispatcherHub` 只保留给持续行为/旧 dispatcher 集成。

剩余建议：

- 长期：如果 `TriggerRunner + ActionScheduler` 能覆盖多数能力，逐步将 `Dispatcher` 降级为兼容层。

## 3. 现存 TODO 与待优化热点

### 3.1 已完成的 P0/P1 风险项

- [`Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs)：条件计划显式失败，不再伪装成功。
- [`Runtime/ActionScheduler/ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionDelegateAdapter.cs)：已从有效主线代码面移除，正式调度路径由 `PlannedTrigger.CreateActionDelegate` 复用立即执行解析。
- [`Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs)：表达式型 `NumericValueRef` 已集成表达式编译器和正式求值路径。
- [`Runtime/Context/ExecCtxAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Context/ExecCtxAdapter.cs)：实体查找器占位实现已从自动适配中移除，目标查找交由正式谓词/Attribute 扩展或 targeting 包处理。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：`arity > 2` 的具名参数 Action 不再静默截断，转换阶段显式失败。

### 3.2 仍待处理的结构收敛项

- [`Runtime/ActionScheduler/ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionExecutor.cs)：执行器层延迟重试已具备跨帧等待语义，`retryDelayMs > 0` 不再直接失败；Queued 策略已公开真实运行状态供实例策略判断。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：计划级 retry 次数与延迟参数已进入 `ActionCallPlan`，`WithRetry` 会设置 `ExecutionPolicy=WithRetry` 并保留调度/重试参数。
- [`Runtime/Plan/Json`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json)：JSON Action DTO 已支持 `ExecutionPolicy`、`RetryMaxRetries`、`RetryDelayMs`，缺省重试次数保持 3，显式 `RetryMaxRetries=0` 表示不重试。
- [`Runtime/ActionScheduler/ActionScheduler.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionScheduler.cs)：已维护调度累计时间 `ElapsedMs`，注册 Action 时可写入真实创建时间。
- [`Runtime/ActionScheduler/ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionInstance.cs)：Queued 策略已改为读取 `QueuedActionExecutor.IsQueued`，不再用优先级误判是否已入队；`CreatedAtMs` 已由调度器传入，不再固定为 `0`；等待队列/同步/延迟重试等未实际执行帧不会提前终结实例；Timeline 与 Rollback 若要从显式不支持升级为完整能力，需要独立设计。
- [`Runtime/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable)：旧调度和剩余装饰器仍需逐项评估迁移到 `Plan/Executables`；`ExecutableRegistry` 默认运行时扫描已改为内建显式注册 + 兼容扩展按需扫描；Action、组合节点 guard/weight 构造、RandomSelector、结果别名、比较条件别名与 `Not` 反转装饰入口已先收敛到主线 DSL。
- [`Runtime/Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable)：旧 DSL、旧配置转换器和旧调度工厂注册表仅保留兼容用途。
- 根目录兼容文件：兼容清单与删除条件已记录，后续只剩实际迁移或 major 版本删除。

### 3.3 文档、示例与目录观感问题

- [`Runtime/Experimental/Todo/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/README.md) 和 [`Runtime/Experimental/Todo/Executable/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/Executable/README.md) 已中文化；后续重点是保持迁移状态同步。
- [`Runtime/TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md) 已补充 `ActionScheduler`、`Schedule`、`Scheduler`、`Dispatcher` 和 `Plan/Executables` 边界；后续随代码收敛继续维护。
- [`ExecutableExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/ExecutableExamples.cs) 和 [`RefactoredExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/RefactoredExamples.cs) 已迁出 Runtime 默认编译路径，保留为 LegacyExecutable 文档参考。

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
  Schedule/                  # 通用句柄式调度
  Legacy/                    # 旧执行体系与旧调度兼容入口
  Experimental/Todo/         # 非主线能力迁移区
  Compatibility/             # 未来承载根目录兼容入口
```

### 4.2 建议降级或迁移目录

- `Runtime/Legacy/TriggerScheduler`：保持非主线兼容，不接回主线。
- `Runtime/Scheduler`：已补充 legacy 说明，后续合并进 `Schedule` 或废弃。
- `Runtime/Executable`：保持非主线行为系统，成熟节点逐步迁入 `Plan/Executables`。
- `Runtime/Legacy/Executable`：保持旧 DSL/转换器兼容入口，不新增主线能力。
- 根目录兼容文件：建议建立 `Compatibility` 迁移计划。

## 5. 推荐下一轮低风险整理

按收益与风险排序，建议下一轮执行：

1. 继续评估 `Runtime/Executable` 中剩余 Decorator、ScheduledExecutable 迁入 `Plan/Executables` 的最小切入点；Registry 默认扫描已先完成显式注册收敛，`Not` 反转装饰入口已先完成主线 DSL 收敛。
2. 如需进一步收口根目录兼容入口，迁入 `Runtime/Compatibility` 目录或在 major 版本删除。
3. 计划级 retry 参数已完成，后续按产品需求评估 Timeline、Rollback 等完整能力。

## 6. 推荐中期重构路线

1. 调度收敛：明确 `ActionScheduler` 与 `Schedule` 的边界，废弃或合并 `Scheduler`。
2. 行为树收敛：将 `Runtime/Executable` 的成熟概念迁移到 `Plan/Executables`，避免双体系长期并行；`Legacy/Executable` 保持旧 DSL/转换器兼容。
3. 兼容入口收敛：根目录兼容文件统一中文注释，增加删除版本条件，后续迁入 `Compatibility` 或删除。
4. 文档同步：持续更新 [`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)，让新使用者只看到一条推荐主线。
