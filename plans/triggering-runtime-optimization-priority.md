# Triggering Runtime 后续优化优先级清单

本文按风险、收益、主线影响范围整理后续建议修改项，方便逐轮选择推进。

## 当前待办总览

### 已落地/可划掉

1. P0-1：`PlannedTrigger` 主线执行器第一轮拆分已完成。
   - `PlannedTriggerActionBindingResolver`：Action 绑定解析。
   - `PlannedTriggerPredicateEvaluator`：Function/Expr Predicate 评估与调度条件委托。
   - `PlannedTriggerArgumentResolver`：NamedArgs、位置参数与 NumericValueRef 解析。
   - `PlannedTriggerScheduleRegistrar`：调度型 Action 注册与 Executor 选择。
2. P0-2：`Legacy/TriggerScheduler/DefaultTriggerExecutor` 遇到带条件的 `TriggerPlan` 会显式失败，不再注册空条件委托伪装成功。
3. P0-3：`ActionScheduler/ActionDelegateAdapter` 已从有效主线路径移除；正式调度路径由 `PlannedTrigger.CreateActionDelegate` 复用立即执行解析，不再反向构造不完整 `ExecCtx`。
4. P2-9 的一部分：`ActionExecutor` 队列、同步、立即重试、执行器层延迟重试与计划级 retry 参数已收口；Queued 策略已改为检查真实队列运行状态；`ActionScheduler` 已维护调度累计时间并写入 Action 实例创建时间；Timeline 和 Rollback 已明确偏向显式不支持/失败语义。
5. `NumericValueRef` 表达式解析、Schema 数值解析、源计划非法配置降级等 P0/P1 风险已在主线收口。

### 仍待正式推进

1. P1-5：`Plan/Executables` 与 `Executable` 双体系收敛：旧示例已迁出，主线 DSL 已补齐 Action 常量/命名参数、组合节点 guard/weight 构造、RandomSelector、Success/NoOp、AlwaysSuccess/Failure/AlwaysFail/Not 与条件表达式桥接入口；旧 `ExecutableRegistry` 默认运行时扫描已收敛为内建显式注册 + 兼容扩展按需扫描；剩余 Decorator、ScheduledExecutable 仍需逐步评估是否迁入主线。
2. P2-8：持续更新 `TriggeringDesign.md`，确保新使用者只看到一条推荐主线。
3. P2-9：ActionScheduler 剩余语义策略：执行器层延迟重试已支持跨帧等待，计划级 retry 次数与延迟参数已接入 `ActionCallPlan`、JSON 计划和调度注册；若后续需要完整 Timeline 或 Rollback，需要作为独立功能实现。

## P0：优先修改，避免误导主线或隐藏运行风险

### 1. PlannedTrigger 主线执行器拆分（已完成第一轮）

- 文件：[`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)
- 状态：已落地。
- 已完成：
  1. 抽出 Action 绑定解析模块：`PlannedTriggerActionBindingResolver`。
  2. 抽出 Predicate 评估模块：`PlannedTriggerPredicateEvaluator`。
  3. 抽出 Numeric/NamedArgs 参数解析模块：`PlannedTriggerArgumentResolver`。
  4. 抽出调度注册模块：`PlannedTriggerScheduleRegistrar`。
  5. 保留 [`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs) 主要负责执行编排与单 Action 执行适配。
- 后续：如果继续拆分，应优先评估立即执行路径与 ExecutionControl 状态是否还需要进一步独立，而不是重复拆 Predicate/Args/Schedule。

### 2. TriggerScheduler 的占位执行风险（已完成）

- 文件：[`Legacy/TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs)
- 状态：已落地。
- 结果：带条件 `TriggerPlan` 在该兼容路径会显式失败，不再注册 `null` 条件委托后表现为成功。
- 后续：该路径继续保持非主线兼容定位；可复用的触发器级执行策略概念后续再迁入 `Plan/Executables`。

### 3. ActionDelegateAdapter 上下文构建 TODO（已完成）

- 文件：[`ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionDelegateAdapter.cs)
- 状态：已落地。
- 结果：主线调度 Action 不再通过该适配器从 `ITriggerDispatcherContext` 反向构造 `ExecCtx`；`PlannedTrigger` 直接复用已绑定的计划执行上下文。
- 后续：该文件如继续保留，应只作为兼容/占位文件，不再接回主线。

## P1：主线结构收敛，建议按批次推进

### 4. 调度体系收敛：ActionScheduler / Schedule / Scheduler

- 文件/目录：
  - [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)
  - [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)
  - [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)
- 状态：已完成第一轮边界文档化。
- 目标：明确三套调度体系边界，减少命名与职责混淆。
- 建议步骤：
  1. 在 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 增加目录级 legacy 说明。
  2. 文档明确 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler) 是 TriggerPlan Action 主线调度器。
  3. 将 [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 定位为通用句柄式调度。
  4. 评估 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 是否迁入 [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 或长期废弃。
- 优先原因：目录规划问题明显，会影响新代码选型。
- 风险：低到中等；先做文档和标记，后做迁移。

### 5. Plan/Executables 与 Executable 双体系收敛

- 文件/目录：
  - [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)
  - [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable)
  - [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable)
- 状态：已完成低风险收敛第一轮，并开始推进 Registry 扫描收敛。
- 目标：让新业务只优先使用 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)，逐步吸收 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 与 [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 的成熟概念。
- 已完成：[`ExecutableExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/ExecutableExamples.cs) 和 [`RefactoredExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/RefactoredExamples.cs) 已迁出 Runtime 默认编译路径，保留为 LegacyExecutable 文档参考。
- 已完成：[`TriggerPlanExecutableDsl.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/TriggerPlanExecutableDsl.cs) 已补齐常量参数 Action、命名参数 Action、组合节点 guard/weight 构造、RandomSelector、Success/NoOp、AlwaysSuccess/Failure/AlwaysFail/Not 与条件表达式桥接入口，旧 `ExecutableDsl.Action` / `ExecutableDsl.RandomSelector` / `ExecutableDsl.Success` / `ExecutableDsl.NoOp`、失败语义别名、反转装饰器、组合节点守卫和基础条件构造方式可迁移到主线 DSL。
- 已完成：[`ExecutableRegistry.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ExecutableRegistry.cs) 默认构造路径不再扫描程序集，旧内建 Executable/Condition 改为显式注册；Attribute 扫描保留为 `ScanAssemblies` / `ScanRuntimeExecutableAssembly` 兼容入口。
- 建议步骤：
  1. 逐个评估 Decorator、ScheduledExecutable 是否迁入 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)，并继续观察外部扩展是否仍依赖 Registry Attribute 扫描。
  2. 在设计文档中明确 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 是旧行为组合体系，[`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 是旧 DSL/转换器兼容入口。
- 优先原因：这是当前最大的双体系理解成本来源。
- 风险：中等；涉及文件多，建议先从示例迁出或禁编开始。

### 6. Dispatcher 与 TriggerRunner 使用边界正式化

- 文件/目录：
  - [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)
  - [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher)
  - [`TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs)
- 状态：已完成目录级兼容说明，后续按调用方继续收口。
- 目标：明确事件计划触发优先走 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)，旧 Dispatcher 只承担兼容或持续行为集成。
- 已完成：`TriggeringDesign.md`、`TriggerDispatcherHub` 注释与 [`Dispatcher/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/README.md) 已描述推荐主线和兼容定位。
- 建议步骤：
  1. 给 [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher) 补充目录级兼容层说明。
  2. 逐步评估 `EventBusDispatcher`、`TimedDispatcher` 是否仍需被新代码直接使用。
  3. 如果 `TriggerRunner + ActionScheduler` 覆盖多数能力，将旧 Dispatcher 进一步降级为兼容层。
- 优先原因：派发入口双轨会影响后续集成方式。
- 风险：低，前期主要是文档和注释。

## P2：兼容入口、文档与目录观感优化

### 7. 根目录兼容文件收敛

- 目录：[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime)
- 状态：已完成兼容入口清单第一轮。
- 目标：处理根目录大量 Deprecated compatibility files。
- 建议步骤：
  1. 统一根目录兼容文件中文注释。
  2. 增加删除条件，例如“下一个 major 版本移除”。
  3. 建立 `Compatibility` 目录或兼容入口清单。
  4. 后续 major 版本再删除或迁移。
- 优先原因：目录观感和新代码引用路径容易被误导。
- 风险：低，先不改 namespace 和 API。

### 8. 更新 TriggeringDesign 设计文档

- 文件：[`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)
- 状态：部分完成，后续随代码收敛持续更新。
- 已补充：
  1. [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler) 的主线地位。
  2. [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables) 与旧 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) / [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 的边界。
  3. [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 与 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 的定位差异。
  4. 推荐入口：事件计划触发使用 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)。
- 后续：每次目录/代码收敛后同步更新，避免设计文档再次滞后。
- 风险：低。

### 9. ActionScheduler 剩余 TODO 正式化

- 文件：
  - [`ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionExecutor.cs)
  - [`ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionInstance.cs)
- 状态：部分完成。
- 已完成：队列、同步、立即重试、执行器层跨帧延迟重试、计划级 retry 次数/延迟参数、注册/替换、生命周期释放与 Timeline 显式失败语义已收口；Queued 策略已使用 `QueuedActionExecutor.IsQueued` 运行状态，不再把 `QueuePriority` 当作已入队标记；`ActionScheduler` 已维护 `ElapsedMs` 并在注册时写入 `ActionInstance.CreatedAtMs`；等待队列/同步/延迟重试等未实际执行帧不会提前终结 Action 实例。
- 已完成：`ActionCallPlan` 已暴露 `RetryMaxRetries` / `RetryDelayMs`，`WithRetry` 与 JSON `ExecutionPolicy=WithRetry` 可配置重试次数和重试延迟，`PlannedTriggerScheduleRegistrar` 会将参数传入 `RetryActionExecutor`。
- 仍待决策：
  1. 是否实现完整 Rollback/补偿语义。
  2. 是否引入真正 Timeline 子 Action 序列执行模型。
- 优先原因：主线已接入 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)，剩余语义会影响使用者预期。
- 风险：中等，涉及执行语义。

## 推荐落地顺序

1. P1-5：继续评估 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 的剩余 Decorator、ScheduledExecutable 是否迁入主线；Registry 默认扫描已先收敛为显式注册 + 兼容按需扫描，`Not` 反转装饰器入口已先收敛到主线 DSL，并保持 [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 仅做旧 DSL/转换器兼容。
2. P2-8：随代码收敛持续更新 [`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)。
3. P2-9：计划级 retry 参数已落地；后续按产品需求决定是否继续实现 Timeline、Rollback 等完整能力。
