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

正式化推进应优先保证这些主线目录的 API、注释、异常处理与运行语义稳定；legacy、compatibility、experimental 入口的迁移、保留和下线规则统一以 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md) 为准。

## 2. 明确冗余与非主线实现

### 2.1 根目录兼容文件已清理

[`Runtime`](../Unity/Packages/com.abilitykit.triggering/Runtime) 根目录不再保留 `.cs` 兼容占位入口，正式实现均位于对应子目录；[`Runtime/Compatibility`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility) 机器清单当前为空，用于防止占位入口回流。

已解决的问题：

1. 根目录不再与稳定目录并列提供兼容 `.cs` 占位文件，减少新代码误引用旧入口的风险。
2. 无类型空占位入口已删除，已废弃路径不再被误认为可用 API。
3. 兼容清单、文档和测试改为防回流约束，新根目录 `.cs` 文件会被扫描测试识别为缺失登记。

结果：

- 已完成：[`Runtime/Compatibility.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility.md) 已改为根目录兼容入口清理记录，保留已删除入口和正式替代路径。
- 已完成：[`Runtime/Compatibility/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility/README.md) 已明确机器清单当前为空，并作为防止 Runtime 根目录 `.cs` 占位入口回流的边界。
- 已完成：[`RootRuntimeCompatibilityCatalog.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility/RootRuntimeCompatibilityCatalog.cs) 当前无登记项；[`RuntimeCompatibilityCatalogTests.cs`](../Unity/Packages/com.abilitykit.triggering/Tests/Editor/RuntimeCompatibilityCatalogTests.cs) 校验空清单、人类文档和未知根目录 `.cs` 文件告警。
- 已完成：根目录无类型空占位 `ActionContext.cs`、`ActionExecutor.cs`、`ActionInstance.cs`、`ActionScheduler.cs`、`ContextAdapter.cs`、`EventBusDispatcher.cs`、`ExecCtxAdapter.cs`、`ITriggerDispatcher.cs`、`NumericValueRefContextExtensions.cs`、`PlannedTrigger.cs`、`TimedDispatcher.cs`、`TriggerDispatcherHub.cs`、`TriggerExecutor.cs`、`TriggerRunner.cs` 已删除，并从工程文件中移除。
- 已完成：legacy / compatibility / experimental 的统一入口分级、迁移优先级和删除条件已沉淀到 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)。
- 后续：不在 Runtime 根目录新增 `.cs` 兼容占位入口；如确需兼容旧路径，必须同步机器清单、文档和相关测试。

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
- `Runtime/Legacy/Executable` 有旧 `ExecutableDsl`、旧配置转换器与模块入口；旧调度工厂注册表已删除。
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
- 已完成：`Runtime/Executable` 中 `DOT` / `HOT` / `Buff` / `Aura` 等调度包装，以及 `DecoratorExtensions.WithDuration` / `WithTags` / `WithModifiers` / `WithStack` / `WithHierarchy` / `WithContinuous` / `WithCapability` 等链式 Decorator 入口已先降级为兼容桥接。
- 已完成：[`MetadataTriggerPlanExecutable.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Executables/MetadataTriggerPlanExecutable.cs) 与 `TriggerPlanExecutableDsl.Metadata/Tags/Modifiers/Stack/Hierarchy/Capability/Duration/ContinuousMetadata` 已为旧 Decorator 常用标签、修饰器、堆叠、层级、能力、持续时间和持续通道描述提供主线元数据节点；JSON `ExecutionRoot` 也支持 `Metadata` 及其常用别名。
- 中期：如产品需要更强执行语义，再将具体 Decorator 行为以正式节点或领域扩展迁入 `Plan/Executables`，并继续观察外部扩展是否仍依赖旧 Registry Attribute 扫描。
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
- [`Runtime/Scheduler/SchedulerMigration.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler/SchedulerMigration.cs) 已提供旧 `SchedulerConfig` 到正式 `RuleSchedulePlan` 的迁移映射，并按语义推荐 `Runtime.ActionScheduler` / `Runtime.RuleScheduler` / `Runtime.Schedule`。
- [`Samples`](../Unity/Packages/com.abilitykit.triggering/Samples) 中直接演示旧 `SchedulerRegistry` 的调度样例已迁到 `RuleSchedulerRegistry`；旧 `SchedulerConfig` 只作为兼容数据字段出现，并通过 `SchedulerMigration` 转换后执行。

剩余建议：

- 已完成：旧调度入口的迁移优先级和删除条件已纳入 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)。
- 中期：通过 `SchedulerMigration` 将外部旧 `Scheduler` 实际调用方迁移到 `RuleScheduler`、`Schedule` 或 `ActionScheduler`，再决定合并或废弃。
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
- `Runtime/ActionScheduler/ActionDelegateAdapter.cs`：空占位文件已随首批兼容清理删除，正式调度路径由 `PlannedTrigger.CreateActionDelegate` 复用立即执行解析。
- [`Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs)：表达式型 `NumericValueRef` 已集成表达式编译器和正式求值路径。
- [`Runtime/Context/ExecCtxAdapter.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Context/ExecCtxAdapter.cs)：实体查找器占位实现已从自动适配中移除，目标查找交由正式谓词/Attribute 扩展或 targeting 包处理。
- [`Runtime/Plan/TriggerPlan.cs`](../Unity/Packages/com.abilitykit.triggering/Runtime/Plan/TriggerPlan.cs)：`arity > 2` 的具名参数 Action 不再静默截断，转换阶段显式失败。

### 3.2 已收敛第一轮、剩余延后决策项

- [`Runtime/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Executable)：旧 Decorator 链式入口已降级为兼容桥接，常用元数据、持续时间与持续通道描述语义已通过 `MetadataTriggerPlanExecutable` 迁入 `Plan/Executables`；剩余只在产品需要实际状态变更时评估更强执行语义是否独立迁入主线节点。
- [`Runtime/Legacy/Executable`](../Unity/Packages/com.abilitykit.triggering/Runtime/Legacy/Executable)：旧 DSL 与旧配置转换器仅保留兼容用途，不再新增调度工厂或新执行扩展入口；替代路径和删除条件已统一记录到 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)。
- 根目录兼容文件：Runtime 根目录 `.cs` 空占位入口已清理完成，兼容机器清单当前为空；人类可读清理记录、目录 README 和测试已改为防止占位入口回流的约束。
- [`Runtime/ActionScheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/ActionScheduler)：retry、跨帧延迟重试、运行时计时、Timeline 显式拒绝与 Rollback 前置拒绝已完成；剩余只有在产品需要完整 Timeline/Rollback 时再作为独立能力设计。
- [`Runtime/Scheduler`](../Unity/Packages/com.abilitykit.triggering/Runtime/Scheduler)：旧配置到正式调度语义的 `SchedulerMigration` 已完成，包内 Samples 直接旧入口已迁到 `RuleSchedulerRegistry`，迁移/删除策略已纳入 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)；剩余只剩外部调用方迁移、目录合并或 major 废弃。

### 3.3 文档、示例与目录观感问题

- [`Runtime/Experimental/Todo/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/README.md) 和 [`Runtime/Experimental/Todo/Executable/README.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Experimental/Todo/Executable/README.md) 已中文化并同步第一轮迁移状态；后续重点是随外部调用方迁移继续维护。
- [`Runtime/TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md) 已补充 `ActionScheduler`、`Schedule`、`Scheduler`、`Dispatcher` 和 `Plan/Executables` 边界；[`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md) 已补齐统一 legacy 迁移与弃用策略。
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
  Compatibility/             # 根目录兼容入口空清单与防回流规则
```

### 4.2 建议降级或迁移目录

- `Runtime/Legacy/TriggerScheduler`：保持非主线兼容，不接回主线。
- `Runtime/Scheduler`：已补充 legacy 说明，后续合并进 `Schedule` 或废弃。
- `Runtime/Executable`：保持非主线行为系统，成熟节点逐步迁入 `Plan/Executables`。
- `Runtime/Legacy/Executable`：保持旧 DSL/转换器兼容入口，不新增主线能力。
- 根目录兼容文件：Runtime 根目录 `.cs` 空占位入口已清理完成，`Compatibility` 目录保留空机器清单和防回流规则。

## 5. 推荐下一轮低风险整理

按收益与风险排序，建议下一轮执行：

1. 外部旧 `Runtime/Scheduler` 调用方迁移：包内 Samples 已清理，下一轮只处理包外或项目侧真实调用方。
2. 产品化文档补齐：发布说明、升级风险、FAQ、性能/确定性约束和 sample 到生产接入模板。
3. 如产品需要更强领域执行语义，再继续评估 `Runtime/Executable` 中具体 Decorator 行为迁入 `Plan/Executables`；当前常用元数据、持续时间与持续通道描述语义已完成主线承接。
4. Timeline、Rollback 已显式拒绝，后续按产品需求评估是否升级为完整独立能力。

## 6. 推荐中期重构路线

1. 调度收敛：通过 `SchedulerMigration` 继续迁移外部旧 `Scheduler` 调用方，最终废弃或合并 `Scheduler`。
2. 行为树收敛：将 `Runtime/Executable` 的剩余成熟执行语义按产品需求迁移到 `Plan/Executables`，避免双体系长期并行；`Legacy/Executable` 保持旧 DSL/转换器兼容。
3. 兼容入口收敛：保持 [`Runtime/Compatibility`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility) 空机器清单、[`Runtime/Compatibility.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility.md) 清理记录和相关测试同步，防止 Runtime 根目录 `.cs` 占位入口回流。
4. 文档同步：持续更新 [`TriggeringDesign.md`](../Unity/Packages/com.abilitykit.triggering/Runtime/TriggeringDesign.md)、[`Document/FormalApiBoundary.md`](../Unity/Packages/com.abilitykit.triggering/Document/FormalApiBoundary.md) 与 [`Document/LegacyMigrationPolicy.md`](../Unity/Packages/com.abilitykit.triggering/Document/LegacyMigrationPolicy.md)，让新使用者只看到一条推荐主线。
