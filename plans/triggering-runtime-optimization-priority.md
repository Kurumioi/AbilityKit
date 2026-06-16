# Triggering Runtime 后续优化优先级清单

本文按风险、收益、主线影响范围整理后续建议修改项，方便逐轮选择推进。

## P0：优先修改，避免误导主线或隐藏运行风险

### 1. 继续拆分 PlannedTrigger 主线执行器

- 文件：[`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)
- 目标：把 Predicate 解析、Action 解析、NamedArgs 解析、调度注册从单类中继续拆出，降低主线复杂度。
- 建议步骤：
  1. 抽出 Action 绑定解析模块：`ActionBindingResolver`。
  2. 抽出 Predicate 评估模块：`PredicateEvaluator`。
  3. 抽出 Numeric 参数解析模块：`NumericValueResolver` 或复用现有变量解析能力。
  4. 保留 [`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs) 只负责执行编排。
- 优先原因：当前主线已稳定，但类体仍偏大，是后续功能扩展的主要复杂度来源。
- 风险：中等，需要分批改动并每批编译。

### 2. 修复 TriggerScheduler 的占位执行风险

- 文件：[`TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerScheduler/TriggerExecutor.cs)
- 目标：去掉非主线路径中的 `Console.WriteLine` 占位执行，改为显式异常或不可用提示。
- 建议步骤：
  1. 将文件头 TODO 中文化，明确该路径是实验/兼容执行策略。
  2. `CreateActionDelegate` 不再返回假执行委托。
  3. 如果未接入 Registry，直接抛出 `NotSupportedException` 或返回会显式失败的委托。
  4. 同步更新 [`Experimental/Todo`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo) 说明。
- 优先原因：这是会产生“看似成功执行”的占位逻辑，误用风险最高。
- 风险：低到中等；如果外部有人依赖该占位输出，会改变行为，但这是正确收敛方向。

### 3. 明确 ActionDelegateAdapter 的上下文构建 TODO

- 文件：[`ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionDelegateAdapter.cs)
- 目标：处理 `BuildExecCtx` 占位返回默认上下文的问题。
- 建议步骤：
  1. 先确认该适配器是否仍被主线调用。
  2. 如果非主线使用，标记兼容/实验并补充显式限制。
  3. 如果主线可能调用，则接入真实 `ExecCtx` 构建依赖或要求调用方传入上下文。
- 优先原因：上下文缺失会导致 Action 执行时黑板、Payload、Registry 不完整。
- 风险：中等，需先检索调用方。

## P1：主线结构收敛，建议按批次推进

### 4. 调度体系收敛：ActionScheduler / Schedule / Scheduler

- 文件/目录：
  - [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)
  - [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)
  - [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)
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
- 目标：让新业务只优先使用 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)，逐步吸收 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 的成熟概念。
- 建议步骤：
  1. 将 [`ExecutableExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ExecutableExamples.cs) 和 [`RefactoredExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/RefactoredExamples.cs) 从 Runtime 默认编译路径移出或降级。
  2. 逐个评估 Decorator、ScheduledExecutable、Registry 扫描是否迁入 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)。
  3. 在设计文档中明确 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 是实验/兼容行为组合体系。
- 优先原因：这是当前最大的双体系理解成本来源。
- 风险：中等；涉及文件多，建议先从示例迁出开始。

### 6. Dispatcher 与 TriggerRunner 使用边界正式化

- 文件/目录：
  - [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)
  - [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher)
  - [`TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs)
- 目标：明确事件计划触发优先走 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)，旧 Dispatcher 只承担兼容或持续行为集成。
- 建议步骤：
  1. 更新设计文档说明推荐入口。
  2. 给 [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher) 补充兼容层说明。
  3. 逐步评估 `EventBusDispatcher`、`TimedDispatcher` 是否仍需被新代码直接使用。
- 优先原因：派发入口双轨会影响后续集成方式。
- 风险：低，前期主要是文档和注释。

## P2：兼容入口、文档与目录观感优化

### 7. 根目录兼容文件收敛

- 目录：[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime)
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
- 目标：让设计文档反映当前真实主线。
- 建议补充：
  1. [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler) 的主线地位。
  2. [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables) 与 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 的边界。
  3. [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 与 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 的定位差异。
  4. 推荐入口：事件计划触发使用 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)。
- 优先原因：避免后续修改和文档脱节。
- 风险：低。

### 9. ActionScheduler 剩余 TODO 正式化

- 文件：
  - [`ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionExecutor.cs)
  - [`ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionInstance.cs)
- 目标：处理 Retry 延迟执行、Rollback、Timeline 等未闭环语义。
- 建议步骤：
  1. 先将 TODO 分类为“暂不支持”或“待实现”。
  2. Timeline 未实现时显式返回不可用状态或保留跳过说明。
  3. Retry 延迟如果不做真实调度，需明确当前仅立即重试。
- 优先原因：主线已接入 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)，剩余 TODO 会影响使用者预期。
- 风险：中等，涉及执行语义。

## 推荐落地顺序

1. P0-1：继续拆分 [`PlannedTrigger.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/PlannedTrigger.cs)，先抽 Action 绑定解析。
2. P0-2：处理 [`TriggerScheduler/TriggerExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggerScheduler/TriggerExecutor.cs) 占位执行。
3. P0-3：梳理 [`ActionDelegateAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionDelegateAdapter.cs) 的上下文构建。
4. P1-4：调度目录边界文档化与 legacy 标记。
5. P1-5：迁出 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 示例，再逐步迁移成熟节点概念。
6. P1-6：明确 [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher) 与 [`TriggerRunner`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs) 边界。
7. P2-7：根目录兼容入口统一整理。
8. P2-8：更新 [`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)。
9. P2-9：处理 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler) 剩余 TODO。
