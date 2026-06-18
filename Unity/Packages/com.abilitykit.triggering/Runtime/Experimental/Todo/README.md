# Triggering Runtime 实验 TODO 迁移说明

本目录用于收纳仍有设计价值、但尚未进入稳定触发器执行主线的非主线实现。它的目标是保留可复用概念，同时避免实验实现继续和正式主线混在一起。

## 当前稳定主线

- `Runtime/Plan/Json/TriggerPlanJsonDatabase.cs`
- `Runtime/Plan/Executables/ITriggerPlanExecutable.cs`
- `Runtime/Plan/PlannedTrigger.cs`
- `Runtime/Plan/PlannedTriggerActionBindingResolver.cs`
- `Runtime/Plan/PlannedTriggerArgumentResolver.cs`
- `Runtime/Plan/PlannedTriggerPredicateEvaluator.cs`
- `Runtime/Plan/PlannedTriggerScheduleRegistrar.cs`
- `Runtime/Runtime/TriggerRunner.cs`
- `Runtime/ActionScheduler/*`

## 当前待办任务

1. 调度体系收敛：`ActionScheduler` / `Schedule` / `Scheduler` 的边界已完成第一轮文档化，旧 `SchedulerConfig` 到正式 `RuleSchedulePlan` / 推荐运行时的迁移映射已落地，包内 Samples 旧调度入口已迁到 `RuleSchedulerRegistry`；后续只剩外部调用方迁移、目录合并或 major 废弃。
2. Executable 双体系收敛：`Legacy/Executable` 已完成主线降级、`Plan/Executables` 已承接常见节点与 Decorator 元数据语义；后续仅在产品需要时补更强领域语义。
3. Dispatcher 边界收敛：`TriggerRunner` 主线定位、`Dispatcher` 兼容边界和目录说明已同步；后续只剩调用方继续收口。
4. 根目录兼容入口收敛：Runtime 根目录 `.cs` 空占位入口已清理完毕，`Compatibility` 机器清单当前为空并用于防止占位入口回流。
5. Legacy 迁移策略：`Document/LegacyMigrationPolicy.md` 已统一 legacy、compatibility、experimental 入口分级、迁移优先级和删除条件；后续随外部调用方迁移继续维护。
6. ActionScheduler 深水区能力决策：retry、延迟重试、Timeline 显式拒绝与 Rollback 前置拒绝均已落地；若后续需要完整 Timeline/Rollback，应作为独立功能实现。

## 迁移规则

1. 历史实现仍有设计价值时，不直接删除；先标记、镜像或迁移到本目录跟踪。
2. 兼容文件在所有包调用方完成迁移前，保留原命名空间与原入口。
3. 未完成的非主线概念优先在本目录沉淀，再将验证稳定的部分逐步接入 `Plan/Executables`。
4. 优先修复和正式化主线代码，再处理 TODO/实验实现。
5. 每一类 TODO 迁移都需要说明最终方向：合并进主线、适配器包装，或长期保留为实验能力。

## 当前已迁移到 TODO 跟踪的分组

- `TriggerScheduler`：未来可能抽象出的触发器执行策略层。当前主线尚未使用，但其中的调度/策略概念有保留价值。
- `Executable`：独立的类行为树可执行系统。当前主线使用 `Plan/Executables`，应等主线执行语义稳定后再按节点语义逐步迁移。
- `Scheduler`：旧版通用回调式调度体系。当前不作为 Trigger Action 调度入口，`SchedulerMigration` 已提供到 `RuleScheduler` / 正式运行时的迁移映射，后续应合并到 `Schedule` 或长期保留为兼容层。

## 主线迁移进度

- `PlannedTrigger` 已改为按 Action 维度处理调度型 Action，不再由第一个 Action 决定整条 Trigger 的执行模式；同一次计划激活可混合执行立即、延迟、周期和持续 Action。
- `PlannedTrigger` 已完成第一轮主线职责拆分：Action 绑定解析、Predicate 评估、NamedArgs/Numeric 参数解析和调度注册分别由独立 helper 承担，`PlannedTrigger` 收敛为执行编排与单 Action 执行适配。
- `PlannedTriggerPredicateEvaluator` 已统一 Function/Expr Predicate 评估路径，调度型 Action 的条件委托复用相同评估逻辑，不再只覆盖 Function Predicate。
- `PlannedTriggerArgumentResolver` 已统一 NamedArgs、位置参数和 NumericValueRef 解析，错误消息集中格式化，避免主线执行器内部分散解析逻辑。
- `PlannedTriggerScheduleRegistrar` 已统一调度型 Action 的 `ActionScheduler.RegisterOrReplace` 注册和 Executor 选择，保持每个计划槽位只有一个活跃实例。
- `TriggerRunner.Dispatch` 已拆分为优先级阻断、条件评估、条件拒绝处理、执行、硬中断等阶段化方法，降低主派发流程复杂度。
- `ActionScheduler` 已正式化注册/替换语义、异常失败处理、实例索引清理和生命周期释放逻辑。
- `ActionScheduler` 在更新时会将具体 `ActionInstance` 传入 `ActionExecutionContext`，Executor 不再接收管理器级空实例上下文。
- `ActionInstance` 已具备延迟、周期和持续 Action 的正式调度窗口，不再只覆盖部分延迟处理。
- `NumericValueRefContextExtensions.ResolveExpr` 已接入 `NumericExpressionCompiler` 的缓存编译结果，并通过 `ActionContext.Variables` 完成主线表达式求值。
- `NumericRpnTokenEvaluator` 已抽出为表达式 token 求值核心，`NumericExpressionEvaluator`、`NumericValueRefExtensions` 和 `ActionSchemaRegistry` 统一复用该路径，避免表达式求值逻辑分叉。
- `ActionSchemaRegistry.TryResolveNumericRef<TArgs, TCtx>` 已提供基于 `TArgs + ExecCtx<TCtx>` 的正式数值解析入口，`PlannedTrigger` 和 `RpnIntExprEval` 已复用该核心，Blackboard、Payload、变量和表达式解析不再依赖分散实现。
- `NumericValueRefExtensions.ResolveExpr` 已从占位 `0.0` 返回升级为正式表达式求值，支持变量、四则运算和默认数值函数注册表。
- `TriggerWaitHandle.WaitOne` 已收口为主线显式不支持的同步等待入口，避免在 Unity 主线程中引入隐式阻塞语义。
- `ActionExecutor` 已完成队列、同步和重试语义收口：队列执行器会在执行结束后释放队列状态，同步执行器改为非阻塞信号检查，重试执行器支持立即重试，也支持 `retryDelayMs > 0` 的跨帧延迟重试；`ActionCallPlan`、JSON 计划和 `PlannedTriggerScheduleRegistrar` 已接入计划级 retry 次数/延迟参数；`ActionInstance` 会在等待重试等未实际执行帧保持实例存活。
- `ValueResolver` 已从旧行为工厂中的静默 `0` 占位解析推进为显式失败语义，并复用 `NumericExpressionCompiler` 与 `NumericRpnTokenEvaluator` 完成表达式求值；变量解析改为从可解析上下文读取，避免递归调用自身。
- `ActionSchemaRegistry.ResolveNumericRef` 已收口为 `TryResolve` 与显式异常分层：正式泛型解析失败不再静默返回 `0.0`，旧 `object` 兼容路径新增 `TryResolveNumericRef`，反射解析 helper 也改为返回成功状态。
- `TriggerPlanSourceConverter.ParsePhase` 已从未知阶段静默回落到 `0` 改为对非空未知值显式失败，避免源计划配置的非法 `phase` 被悄悄转成默认即时阶段。
- 源计划 JSON 转换继续收口非法配置降级：未知条件类型、未知执行节点类型、未知 Action 参数对象和不支持的参数 token/string 不再静默写成 `true`、`Action/Sequence` 或 `0`，而是显式失败。
- `NumericValueRefExtensions` 已补齐 `TryResolve` 分层，旧 `Resolve(object)` 不再把 Blackboard、Payload、Var、Expr 解析失败静默转成 `0.0`。
- `PlannedTrigger` 执行阶段已将缺失 Action slot 和不支持 arity 从 warning/null invoke 提升为显式异常，避免触发计划表现为成功但实际未执行。
- JSON DTO 反序列化字段已用局部 `#pragma warning disable 0649` 标记来源，减少构建警告噪音而不改变运行时语义。
- `TriggerPlanDirectoryLoader` 的 manifest DTO 字段已标记为 JSON 反序列化填充，继续压低主线构建警告噪音。
- `NumericValueRefContextExtensions.Resolve(ActionContext)` 已改为复用 `TryResolve` 并在解析失败时显式抛错，ActionContext 旧入口不再把 Blackboard、Payload、Var、Expr 失败静默转成 `0.0`。
- `ExecCtxAdapter.VariableRepositoryAdapter.GetNumeric()` 已收口为显式不支持的兼容入口，避免旧适配层继续伪装成功。
- 源计划转换继续收紧：未知 `scope`、空条件组、空条件项、缺失条件类型和空 Action 参数不再隐式写成 Global、`true` 或 `0`。
- `TriggerPlanAnalyzer.CalculateValueRefComplexity` 对未知 `NumericValueRef` 类型改为显式失败，只保留 `Const` 复杂度为 `0` 的合法评分语义。
- `TriggerPlanDirectoryLoader` 已统一文件缺失、解析失败和异常失败的错误诊断路径，非抛出模式仍可继续合并其它文件，但失败会按 Error 记录。
- `TriggerRunner` 已抽出短路通知和 Action 失败通知 helper，生命周期、观察者和 Cue 回调链保持一致，减少分支重复。
- `ActionSchemaRegistry` 的 `object` 兼容参数解析/数值解析入口已标记为过时，未注册 Schema 不再静默返回 `null`。
- `ExecCtxAdapter.VariableRepositoryAdapter.SetNumeric()` 已与 `GetNumeric()` 一样收口为显式不支持，避免旧适配层继续 no-op 伪装成功。
- `ExecCtxAdapter` 不再为旧 `ActionContext` 自动提供 `EntityFinderAdapter` 占位实现；目标查找属于目标框架包职责，后续应通过正式谓词/Attribute 扩展接入触发计划。
- 旧 `Runtime/Executable` 中的 `HasTargetCondition` 与 `PayloadCompareCondition` 不再伪装成功执行：前者标记为过时并显式失败，后者在旧路径被调用时显式不支持。
- 旧 `ExecutableDsl` 的 builder、条件扩展和调度扩展已整体标记为过时，避免新代码继续把 Runtime/Executable 当作正式入口。
- 旧配置转换器不再把 `PayloadCompare`、`HasTarget` 构造成运行期条件，而是在转换阶段显式失败；正式条件应走 `TriggerPlan` 谓词/注册条件扩展，目标查找应由 targeting 包提供。
- 旧配置转换器继续收紧非法配置降级：条件推断、组合条件、比较符、数值引用和 switch 选择器表达式不再回落到 `true`、`Equal`、`Const(0)` 或 `0`。
- `ActionDelegateFactory` 绑定后的 Action 委托在运行期发现注册表签名缺失时会显式失败，不再静默跳过并伪装执行成功。
- `ActionScheduler/ActionDelegateAdapter` 空占位文件已随首批兼容清理删除；正式调度路径由 `PlannedTrigger.CreateActionDelegate` 直接复用立即执行解析，不再尝试从 `ITriggerDispatcherContext` 反向构造不完整 `ExecCtx`。
- `TriggerPlanConverter` 的具名参数 Action 不再把 `arity > 2` 静默截断为 `2`；在主线正式支持更高 arity 前，非法配置会在转换阶段显式失败。
- `Legacy/TriggerScheduler/DefaultTriggerExecutor` 遇到带条件的 `TriggerPlan` 会显式失败，不再注册 `null` 条件委托后表现为成功；该路径仍仅作为非主线兼容入口。
- `SchedulerMigration` 已为旧 `Runtime/Scheduler` 配置提供到 `RuleSchedulePlan` 的正式迁移映射，并能按语义推荐 `Runtime.ActionScheduler` / `Runtime.RuleScheduler` / `Runtime.Schedule`，避免旧 `SchedulerRegistry` 被重新接回主线。
- 包内调度 Samples 已从旧 `SchedulerRegistry` 调用迁移到 `RuleSchedulerRegistry`；保留的旧 `SchedulerConfig` 仅作为迁移数据字段使用。
- `LegacyMigrationPolicy.md` 已统一记录 legacy / compatibility / experimental 入口的迁移优先级、删除条件和延后决策。
