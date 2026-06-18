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
4. P2-9 的一部分：`ActionExecutor` 队列、同步、立即重试、执行器层延迟重试与计划级 retry 参数已收口；Queued 策略已改为检查真实队列运行状态；`ActionScheduler` 已维护调度累计时间并写入 Action 实例创建时间；Timeline 和 Rollback 已明确为显式不支持/失败语义，其中 Rollback 已在校验层前置拒绝。
5. `NumericValueRef` 表达式解析、Schema 数值解析、源计划非法配置降级等 P0/P1 风险已在主线收口。

6. P1-4：调度体系收敛本轮已推进。旧业务化 `ScheduleEffectFactory` 已迁到 [`Documentation~/LegacySchedule`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacySchedule)，不再进入运行时编译面；`Runtime/Scheduler` 与通用 `Runtime/Schedule` 继续按兼容边界保留。
7. P1-5：`Plan/Executables` 与 `Executable` 双体系收敛本轮已推进。空壳 `ScheduledExecutableFactory.cs` 已删除，真实兼容调度工厂保留在 [`ScheduledExecutables.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ScheduledExecutables.cs)；旧 DSL、旧配置转换和真实兼容执行器保留给反序列化与外部兼容。
8. P1-6：Dispatcher 兼容层收口本轮已推进。`TriggerDispatcherHub`、`TriggerDispatcherRegistry`、`EventBusDispatcher` 与 `TimedDispatcher` 已显式标记为旧 Dispatcher 兼容入口；`ITriggerDispatcherContext` 与委托类型继续保留给 `ActionScheduler` / `PlannedTrigger` 主线桥接。

### 仍待正式推进

1. P2-8：Legacy 迁移与弃用策略已沉淀到 [`LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)，统一记录入口分级、迁移优先级、删除条件和延后决策；后续随外部调用方迁移、发布说明和运行时统计维护。
2. P2-9：ActionScheduler 深水区能力决策：retry、延迟重试、Timeline 显式拒绝与 Rollback 前置拒绝已落地；若后续需要完整 Timeline 或 Rollback，需要作为独立功能实现。
3. P2-10：产品化验收材料：商业化清单中仍保留编辑器工具、性能/确定性基线、更完整测试矩阵、FAQ、升级说明、样板工程和诊断统计等交付项。
4. P2-7：根目录兼容文件收敛已完成；后续只维护 [`Runtime/Compatibility`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility) 空机器清单、清理记录和未知根目录 `.cs` 文件告警。

### 当前过时内容盘点

| 类别 | 现状 | 是否建议立即删除 | 下一步 |
| --- | --- | --- | --- |
| 旧 `Runtime/Scheduler` | 有实际类型、迁移工具和外部兼容价值；包内 Samples 已迁到 `RuleSchedulerRegistry` | 否 | 搜索包外调用方，优先迁移到 `RuleScheduler`、`Schedule` 或 `ActionScheduler`，major 时决定合并或删除 |
| `Runtime/Schedule` 业务样例/旧工厂 | 旧业务化 `ScheduleEffectFactory` 已迁到 `Documentation~/LegacySchedule`，运行时编译面不再包含 Buff、Bullet、AOE 示例工厂 | 否 | 保留 `SimpleScheduleManager` / `GroupedScheduleManager` 主体；外部调用方清空后再评估旧 `Runtime/Schedule` 合并 |
| `Runtime/Executable` / `Runtime/Legacy/Executable` | 旧行为组合体系仍有真实类型、旧配置转换和反序列化兼容价值；空壳旧 `ScheduledExecutableFactory.cs` 已删除 | 否 | 只迁移产品确需的强领域语义；删除前先确认旧 JSON、示例和外部扩展无依赖 |
| `Runtime/Legacy/TriggerScheduler` | 非主线执行策略，已显式失败未支持的条件/Action 路径 | 暂不 | 不接回主线；仅作为未来触发器级执行策略设计输入 |
| `Runtime/Dispatcher` | 旧 dispatcher 聚合和持续行为兼容层已通过 `[Obsolete]` 降权，主线桥接上下文继续保留 | 暂不 | 继续盘点 `EventBusDispatcher`、`TimedDispatcher`、`TriggerDispatcherHub` 外部调用方 |
| `ActionSchemaRegistry` object 兼容入口 | `ParseArgs`、`TryResolveNumericRef`、`ResolveNumericRef` 已标记过时 | 否 | 包内新代码继续使用泛型 `ExecCtx<TCtx>` 路径；外部调用方清空后再考虑移除 |
| `ActionContext.Entities` / `IEntityFinder` | 目标查找职责已迁出 triggering 边界，仅保留过渡兼容 | 否 | 后续由 targeting 包提供正式谓词扩展，禁止 triggering 新增目标查找依赖 |
| Timeline / Rollback | 当前为显式拒绝或校验前置失败 | 否 | 产品明确需要时另开完整功能设计，不作为兼容清理顺手实现 |

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

- 文件：`Runtime/ActionScheduler/ActionDelegateAdapter.cs`
- 状态：已落地并随兼容清理删除。
- 结果：主线调度 Action 不再通过该适配器从 `ITriggerDispatcherContext` 反向构造 `ExecCtx`；`PlannedTrigger` 直接复用已绑定的计划执行上下文。
- 后续：不再恢复该占位入口；如出现新的适配需求，应在正式调度路径中设计明确上下文来源。

## P1：主线结构收敛，建议按批次推进

### 4. 调度体系收敛：ActionScheduler / Schedule / Scheduler

- 文件/目录：
  - [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)
  - [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)
  - [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)
- 状态：已完成边界文档化、旧配置迁移映射、包内样例调用方迁移与旧业务工厂运行时隔离。
- 目标：明确三套调度体系边界，减少命名与职责混淆。
- 已完成：[`Scheduler/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler/README.md) 已声明旧调度注册体系只保留兼容用途。
- 已完成：[`SchedulerMigration.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler/SchedulerMigration.cs) 已提供旧 `SchedulerConfig` 到正式 `RuleSchedulePlan` 的映射，并按语义推荐 `Runtime.ActionScheduler` / `Runtime.RuleScheduler` / `Runtime.Schedule`。
- 已完成：包内调度 Samples 已迁到 `RuleSchedulerRegistry`；Buff 样例仅把 `SchedulerConfig` 作为旧数据兼容字段，并在运行前通过 `SchedulerMigration` 转换为正式 `RuleSchedulePlan`。
- 已完成：旧业务化 [`ScheduleEffectFactory.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacySchedule/ScheduleEffectFactory.cs) 已迁到 `Documentation~/LegacySchedule`，不再作为 `Runtime/Schedule/Factories` 编译入口。
- 建议步骤：
  1. 继续盘点外部包是否仍直接依赖 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)。
  2. 有调用方时先通过 `SchedulerMigration` 明确语义，再迁到 [`RuleScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/RuleScheduler)、[`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 或 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)。
  3. 外部调用方清空后再决定 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 是合并到 [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule)、迁入兼容目录，还是随 major 版本废弃。
- 优先原因：目录规划问题明显，会影响新代码选型。
- 风险：低到中等；先做文档和标记，后做迁移。

### 5. Plan/Executables 与 Executable 双体系收敛

- 文件/目录：
  - [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)
  - [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable)
  - [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable)
- 状态：已完成低风险收敛第一轮、Registry 扫描收敛、旧 Scheduled 入口收敛、空壳旧工厂删除、`DecoratorDsl` 中 DOT/HOT/Buff/Aura 与 `DecoratorExtensions.With*` 链式入口的主线降级标记，以及 Decorator 常用描述语义的主线承接。
- 目标：让新业务只优先使用 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)，逐步吸收 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 与 [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 的成熟概念。
- 已完成：[`ExecutableExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/ExecutableExamples.cs) 和 [`RefactoredExamples.cs`](../Unity/Packages/com.abilitykit.triggering/Documentation~/LegacyExecutable/RefactoredExamples.cs) 已迁出 Runtime 默认编译路径，保留为 LegacyExecutable 文档参考。
- 已完成：[`TriggerPlanExecutableDsl.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/TriggerPlanExecutableDsl.cs) 已补齐常量参数 Action、命名参数 Action、组合节点 guard/weight 构造、RandomSelector、Success/NoOp、AlwaysSuccess/Failure/AlwaysFail/Not 与条件表达式桥接入口，旧 `ExecutableDsl.Action` / `ExecutableDsl.RandomSelector` / `ExecutableDsl.Success` / `ExecutableDsl.NoOp`、失败语义别名、反转装饰器、组合节点守卫和基础条件构造方式可迁移到主线 DSL。
- 已完成：[`ExecutableRegistry.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/ExecutableRegistry.cs) 默认构造路径不再扫描程序集，旧内建 Executable/Condition 改为显式注册；Attribute 扫描保留为 `ScanAssemblies` / `ScanRuntimeExecutableAssembly` 兼容入口。
- 已完成：旧 Scheduled 调度包装常见语义已迁入 [`ScheduledTriggerPlanExecutable.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/ScheduledTriggerPlanExecutable.cs)，旧调度工厂注册表入口已删除，旧配置转换仅保留固定兼容映射，空壳 `Runtime/Legacy/Executable/ScheduledExecutableFactory.cs` 已删除。
- 已完成：[`DecoratorDsl.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/Decorators/DecoratorDsl.cs) 中 `DOT` / `HOT` / `Buff` / `Aura` 已标记为旧 DSL 兼容入口，主线替代入口指向 [`TriggerPlanExecutableDsl`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/TriggerPlanExecutableDsl.cs)。
- 已完成：[`DecoratorDsl.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable/Decorators/DecoratorDsl.cs) 中 `DecoratorExtensions.WithDuration` / `WithTags` / `WithModifiers` / `WithStack` / `WithHierarchy` / `WithContinuous` / `WithCapability` 已标记为旧 `Runtime/Executable` 链式兼容入口。
- 已完成：[`MetadataTriggerPlanExecutable.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/MetadataTriggerPlanExecutable.cs) 与 `TriggerPlanExecutableDsl.Metadata/Tags/Modifiers/Stack/Hierarchy/Capability/Duration/ContinuousMetadata` 已承接旧 Decorator 常用的非执行元数据与持续描述语义；JSON `ExecutionRoot.Kind=Metadata/Tags/Modifiers/Stack/Hierarchy/Capability/Duration/Continuous` 也可转换为正式主线节点。
- 建议步骤：
  1. 后续仅在产品需要会实际改变宿主状态、数值应用或运行时生命周期的更强领域执行语义时，继续把具体 Decorator 行为迁入 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables) 的独立节点或领域扩展，并继续观察外部扩展是否仍依赖 Registry Attribute 扫描。
  2. 在设计文档中持续明确 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) 是旧行为组合体系，[`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 是旧 DSL/转换器兼容入口。
- 优先原因：这是当前最大的双体系理解成本来源。
- 风险：中等；涉及文件多，建议先从示例迁出或禁编开始。

### 6. Dispatcher 与 TriggerRunner 使用边界正式化

- 文件/目录：
  - [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)
  - [`Dispatcher`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher)
  - [`TriggerDispatcherHub.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/TriggerDispatcherHub.cs)
- 状态：已完成目录级兼容说明与旧聚合入口降权，后续按外部调用方继续收口。
- 目标：明确事件计划触发优先走 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)，旧 Dispatcher 只承担兼容或持续行为集成。
- 已完成：`TriggeringDesign.md`、`TriggerDispatcherHub` 注释与 [`Dispatcher/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Dispatcher/README.md) 已描述推荐主线和兼容定位。
- 已完成：`TriggerDispatcherHub`、`TriggerDispatcherRegistry`、`EventBusDispatcher` 与 `TimedDispatcher` 已标记为旧 Dispatcher 兼容入口；`ITriggerDispatcherContext` 与调度委托继续作为 `ActionScheduler` / `PlannedTrigger` 的主线桥接类型保留。
- 建议步骤：
  1. 逐步评估 `EventBusDispatcher`、`TimedDispatcher`、`TriggerDispatcherHub` 是否仍被包外代码直接使用。
  2. 如果调用方只需要事件计划触发，迁到 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs) 与 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)。
  3. 如果调用方依赖外部生命周期强绑定的 tick 行为，保留在 Dispatcher 兼容层并记录长期兼容原因。
  4. 如果 `TriggerRunner + ActionScheduler` 覆盖多数能力，将旧 Dispatcher 进一步降级为 Compatibility 或 major 删除候选。
- 优先原因：派发入口双轨会影响后续集成方式。
- 风险：低，前期主要是文档和注释。

## P2：兼容入口、文档与目录观感优化

### 7. 根目录兼容文件收敛（已完成）

- 目录：[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime)
- 状态：Runtime 根目录 `.cs` 空占位入口已清理完成，兼容机器清单当前为空。
- 目标：处理根目录大量 Deprecated compatibility files，避免目录观感和新代码引用路径被误导。
- 已完成：[`Runtime/Compatibility/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility/README.md) 已明确该目录承载空机器清单、验证门面复用关系与防回流规则。
- 已完成：[`RootRuntimeCompatibilityCatalog.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility/RootRuntimeCompatibilityCatalog.cs) 当前无登记项；[`Compatibility.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility.md) 已改为清理记录，保留已删除入口和正式替代路径。
- 已完成：[`RuntimeCompatibilityCatalogTests.cs`](../Unity/Packages/com.abilitykit.triggering/Tests/Editor/RuntimeCompatibilityCatalogTests.cs) 已改为校验空清单、人类文档同步和未知根目录 `.cs` 文件告警，防止占位入口回流。
- 已完成：根目录 `.cs` 空占位文件及 `.meta` 已删除，并从工程文件中移除。
- 后续：不再新增 Runtime 根目录 `.cs` 兼容占位入口；如确需兼容旧路径，必须同步机器清单、文档和相关测试。

### 8. 更新 TriggeringDesign 与迁移策略文档

- 文件：[`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)、[`LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)
- 状态：主线边界与 legacy 迁移策略已完成第一轮，后续随代码收敛持续更新。
- 已补充：
  1. [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler) 的主线地位。
  2. [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables) 与旧 [`Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable) / [`Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable) 的边界。
  3. [`Schedule`](../Unity/Packages/com.abilitykit.triggering/Runtime/Schedule) 与 [`Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler) 的定位差异。
  4. 推荐入口：事件计划触发使用 [`TriggerRunner.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Runtime/TriggerRunner.cs)。
- 已补充：[`LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md) 已统一 legacy、compatibility、experimental 入口分级、迁移优先级、删除条件和文档同步规则。
- 后续：每次目录/代码收敛后同步更新，避免设计文档与迁移策略再次滞后。
- 风险：低。

### 9. ActionScheduler 剩余 TODO 正式化

- 文件：
  - [`ActionExecutor.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionExecutor.cs)
  - [`ActionInstance.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler/ActionInstance.cs)
- 状态：部分完成。
- 已完成：队列、同步、立即重试、执行器层跨帧延迟重试、计划级 retry 次数/延迟参数、注册/替换、生命周期释放、Timeline 显式失败语义与 Rollback 校验层前置拒绝已收口；Queued 策略已使用 `QueuedActionExecutor.IsQueued` 运行状态，不再把 `QueuePriority` 当作已入队标记；`ActionScheduler` 已维护 `ElapsedMs` 并在注册时写入 `ActionInstance.CreatedAtMs`；等待队列/同步/延迟重试等未实际执行帧不会提前终结 Action 实例。
- 已完成：`ActionCallPlan` 已暴露 `RetryMaxRetries` / `RetryDelayMs`，`WithRetry` 与 JSON `ExecutionPolicy=WithRetry` 可配置重试次数和重试延迟，`PlannedTriggerScheduleRegistrar` 会将参数传入 `RetryActionExecutor`。
- 仍待决策：
  1. 是否将 Rollback 从显式不支持升级为完整补偿语义。
  2. 是否引入真正 Timeline 子 Action 序列执行模型。
- 优先原因：主线已接入 [`ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)，剩余语义会影响使用者预期。
- 风险：中等，涉及执行语义。

## 推荐落地顺序

1. 外部调用方盘点：优先搜索 `Runtime/Scheduler`、`Runtime/Dispatcher`、`Runtime/Executable`、`ActionSchemaRegistry` object 兼容入口和 `IEntityFinder` 的包外引用，先迁真实调用方再删实现。
2. 旧调度后续：`ScheduleEffectFactory` 业务样例已迁到 Documentation，下一步只处理外部调用方迁移与旧 `Scheduler` major 合并/删除决策。
3. Dispatcher 后续：旧聚合入口已降权，下一步按调用方区分“事件计划触发迁主线”和“外部 tick 生命周期长期兼容”。
4. Executable 后续：空壳旧工厂已删除；只在产品需要更强领域执行语义时继续把旧 Decorator/行为语义迁入 [`Plan/Executables`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables)，不要为了清理而删除仍有旧配置价值的转换器。
5. 产品化验收：补性能/确定性基线、测试矩阵、FAQ、升级说明、样板工程和 legacy 命中诊断统计。
6. Major 清理批次：只有当包内外调用方、旧 JSON、示例、测试和 Unity `.meta` 引用都清空后，再删除旧 Scheduler、Dispatcher 或 Executable 入口。
7. 根目录兼容入口：已完成清理；后续仅维护空机器清单、清理记录和测试防回流约束。
