# Executable TODO 迁移说明

`Runtime/Executable` 是一套仍有价值的类行为树执行子系统，但它不是当前稳定触发器执行主线；`Runtime/Legacy/Executable` 仅保留旧 DSL、旧配置转换器和兼容模块入口。

## 归类结果

- 主线替代：`Runtime/Plan/Executables/ITriggerPlanExecutable.cs`、`Runtime/Plan/Executables/*TriggerPlanExecutable.cs`。
- 部分落地：`Runtime/Plan/Executables/TriggerPlanExecutableDsl.cs` 已补齐常量参数 Action、命名参数 Action、带 guard/weight 的组合节点构造、Random/RandomSelector、Success/NoOp、AlwaysSuccess/Failure/AlwaysFail/Not、条件表达式桥接入口，以及 `Scheduled` / `Timed` / `Periodic` / `Continuous` / `External` 调度包装入口。
- 部分落地：`Runtime/Executable/ExecutableRegistry.cs` 已将旧内建 Executable/Condition 改为显式注册，默认构造路径不再运行时扫描程序集；Attribute 扫描保留为兼容扩展的按需入口。
- 已收敛：旧调度工厂注册表入口已删除，`Runtime/Legacy/Executable/ConfigToExecutableConverter.cs` 只保留旧 schedule 配置到固定兼容包装类型的映射，不再允许通过旧工厂扩展新调度模式。
- 已收敛：`Runtime/Executable/Decorators/DecoratorDsl.cs` 中标签、修饰器、堆叠、层级、能力、持续时间和持续通道描述语义已迁移或适配到主线元数据节点；真正改变宿主状态的更强语义仍按产品需求独立评估。
- 兼容保留：`Runtime/Legacy/Executable/ExecutableDsl.cs`、`Runtime/Legacy/Executable/ConfigToExecutableConverter.cs`、`Runtime/Legacy/Executable/ExecutableModule.cs` 中的 legacy/Obsolete 路径，仅用于旧配置、旧反序列化或旧注册方式兼容。
- 待下线候选：在 `Plan/Executables` 完成节点级迁移后仍无调用方依赖的旧入口、硬编码 fallback 和旧示例。

## 当前主线等价路径

- `Runtime/Plan/Executables/ITriggerPlanExecutable.cs`
- `Runtime/Plan/Executables/*TriggerPlanExecutable.cs`
- `Runtime/Plan/Json/TriggerPlanConverter.cs`
- `Runtime/Plan/Json/TriggerPlanJsonDatabase.cs`

不要删除 `Runtime/Executable` 或 `Runtime/Legacy/Executable`。

## 迁移方向

1. 将 `Runtime/Executable` 作为旧行为组合体系保留，将 `Runtime/Legacy/Executable` 作为旧 DSL/转换器兼容入口保留。
2. 只有在 `Plan/Executables` 行为语义稳定后，再增加适配器或迁移节点。
3. 在 JSON 计划加载与执行结果语义统一前，不要将 `ExecutableTriggerDatabase` 接入主线。
4. 已验证稳定的节点语义应按节点逐个迁移到 `Plan/Executables`，避免整套体系一次性切换。

## 迁移与优化路线

### P0：先冻结边界，避免继续分叉

- 已完成：`Runtime/Executable` 与 `Runtime/Legacy/Executable` 已明确标记为兼容/实验入口，新功能不得继续直接接入这些目录。
- 已完成：新增触发器执行节点只进入 `Runtime/Plan/Executables`，避免出现第二套主线行为树。
- 已完成：legacy API 保留但 fallback 必须可定位、可统计、可最终删除。
- 已完成：文档和示例统一指向 `TriggerPlan`、`PlannedTrigger` 与 `Plan/Executables`。

### P1：按节点语义迁移可复用能力

- 已完成：`TriggerPlanExecutableDsl.Action` 已支持常量参数与命名参数 Action 构造，旧 Action DSL 常见用法可迁移到 `ActionCallTriggerPlanExecutable`。
- 已完成：`TriggerPlanExecutableDsl.Random` / `RandomSelector` 已对齐旧 `ExecutableDsl.RandomSelector` 的主线入口。
- 已完成：`TriggerPlanExecutableDsl.Success` / `NoOp` / `AlwaysSuccess` 已对齐旧成功/空操作入口，复用 `SucceedTriggerPlanExecutable` 成功语义。
- 已完成：`TriggerPlanExecutableDsl.Failure` / `AlwaysFail` 已补齐失败语义别名，复用 `FailTriggerPlanExecutable`。
- 已完成：`TriggerPlanExecutableDsl.Not` 已对齐 JSON `not` 别名，复用 `InvertTriggerPlanExecutable` 反转装饰语义。
- 已完成：`TriggerPlanExecutableDsl.Condition` / `ConstCondition` / `CompareCondition` 与 `EqCondition` / `NeCondition` / `GtCondition` / `GeCondition` / `LtCondition` / `LeCondition` 已桥接到 `PredicateExprTriggerPlanCondition`，基础条件构造可走主线谓词表达式。
- 已完成：`AtomicAction` 已对齐到 `ActionCallTriggerPlanExecutable`，只保留构造注入路径，旧 `ActionRegistry` 属性维持兼容。
- 已完成：`Sequence`、`Selector`、`Parallel`、`Repeat`、`If`、`Until` 等稳定节点已映射到 `Plan/Executables` 等价实现。
- 已完成：`DecoratorDsl` 中可证明稳定的装饰器语义已迁移为 `ITriggerPlanExecutable` 元数据节点或计划包装节点，其中 Duration/Continuous 描述已进入 `MetadataTriggerPlanExecutable`，调度执行仍使用 `ScheduledTriggerPlanExecutable`。
- 已完成：对每个迁移节点补充了最小行为用例，确保成功、失败、中断、并行完成语义一致。

### P2：把正确但未落地的能力产品化

- 已完成：`ExecutableDsl` 的 Action、组合节点 guard/weight 构造、RandomSelector、Success、NoOp、AlwaysSuccess、Failure、AlwaysFail、Not、基础条件构造与调度包装便利能力已收敛到 `TriggerPlanExecutableDsl`。
- 已完成：`ExecutableDsl` 的剩余流式构建能力已降级为可选的 `TriggerPlan` 构建 DSL，不再作为独立执行体系扩展入口。
- 已完成：`ScheduledExecutor` / `IScheduledExecutable` 的常见调度包装思想已并入 `ScheduledTriggerPlanExecutable`；旧执行器层控制器仅作为兼容路径保留。
- 已完成：`ExecutableRegistry` 默认路径已移除运行时程序集扫描，内建类型改为显式注册；旧 Attribute 扫描需要显式调用入口。
- 已完成：`ExecutableRegistry` 兼容扩展扫描已收口为按需入口，后续如需进一步优化再评估 Source Generator 静态注册表替代。
- 已完成：JSON 计划、注册表、执行节点之间的 ID 解析链路已收口，避免配置加载阶段和执行阶段重复解析。

### P3：下线旧路径与稳定性加固

- 当外部调用方不再依赖 `Runtime/Executable` 旧入口后，删除硬编码 `ConvertByTypeIdLegacy` fallback。
- 将 `[Obsolete]` 兼容 API 分批升级为编译告警、运行时统计、最终移除。
- 保留必要的反序列化兼容层，但禁止其进入新配置导出链路。
- 建立迁移完成判定：无新调用、无测试依赖、无示例引用、无 JSON 导出引用后才允许删除旧实现。

## 后续候选迁移项

- `ExecutableDsl` 剩余流式构建能力 -> 已降级为可选的计划构建 DSL。
- `ScheduledExecutor` / `IScheduledExecutable` 概念 -> 常见包装语义已进入 `ScheduledTriggerPlanExecutable`，旧控制器实现只保留兼容，不再作为新扩展入口。
- Decorator 概念 -> `Not` 反转装饰器入口已先收敛到 `TriggerPlanExecutableDsl`，标签、修饰器、堆叠、层级、能力、持续时间和持续通道等常用描述语义已由 `MetadataTriggerPlanExecutable` 承接，剩余更强领域语义仅在产品需要时继续转换为 `ITriggerPlanExecutable`。
- `ExecutableRegistry` 标记扫描 -> 默认内建注册已收敛为显式注册；外部兼容扩展扫描等 Source Generator 与 Plan Converter 路径稳定后再评估。
