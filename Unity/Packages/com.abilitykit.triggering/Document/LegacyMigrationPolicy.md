# Triggering Legacy 迁移与弃用策略

本文定义 `com.abilitykit.triggering` 中 legacy、compatibility、experimental 入口的迁移、保留和下线规则，避免兼容层继续扩散为第二套主线。

## 入口分级

| 分级 | 定义 | 新代码规则 | 下线方向 |
| --- | --- | --- | --- |
| Formal | 当前正式主线入口，例如 `TriggerRunner`、`TriggerPlan`、`PlannedTrigger`、`ActionRegistry`、`FunctionRegistry`、`ActionScheduler`、`RuleScheduler`、`Runtime/Validation` | 允许新增能力 | 持续维护 |
| Compatibility | 为旧引用路径、旧 `.meta` GUID 或旧序列化数据保留的兼容入口 | 不新增能力，只允许转发、迁移辅助或明确失败 | 外部调用方清空后进入 major 删除候选 |
| Legacy | 已被正式主线替代但仍可能被旧项目调用的历史实现 | 不新增业务逻辑，不作为新示例入口 | 保留迁移说明、统计调用方，分批废弃 |
| Experimental | 尚未进入正式主线的试验或迁移跟踪实现 | 不接入生产主线 | 稳定后迁入 Formal，或长期留作实验参考 |

## 迁移优先级

1. 会误导新用户的示例、文档和 README 优先迁到正式入口。
2. 会造成静默成功、隐藏失败或运行时语义不确定的旧路径优先改为显式失败。
3. 仍有旧数据价值的配置优先提供迁移映射，而不是重新接回旧运行时。
4. 无包内调用方、无测试依赖、无样例引用的旧入口进入删除候选。
5. 仍可能影响 Unity `.meta` GUID、旧资产或外部包的入口，只能在 major 兼容清理批次删除。

## 当前状态

| 区域 | 当前状态 | 推荐替代 | 剩余动作 |
| --- | --- | --- | --- |
| `Runtime/Scheduler` | 旧回调式调度兼容层；包内 Samples 已迁到 `RuleSchedulerRegistry` | `RuleScheduler`、`ActionScheduler`、`Runtime/Schedule` | 继续迁移外部调用方，major 时决定合并或删除 |
| `Runtime/Schedule` 中业务样例/旧工厂 | 旧业务化 `ScheduleEffectFactory` 已迁到 `Documentation~/LegacySchedule`，不再进入运行时编译面 | `RuleScheduler` 与 `IRuleScheduleEffect` | 运行时只保留通用调度管理器和必要兼容适配，外部调用方清空后再评估旧 `Runtime/Schedule` 合并 |
| `Runtime/Executable` | 旧行为组合体系；常见构造、调度和元数据语义已由 `Plan/Executables` 承接 | `TriggerPlanExecutableDsl` 与 `Plan/Executables` | 保留旧执行器和转换器支撑反序列化；不再新增旧调度工厂或示例入口 |
| `Runtime/Legacy/Executable` | 旧 DSL、旧配置转换和旧模块入口；空壳 `ScheduledExecutableFactory.cs` 已删除，真实兼容工厂只保留在 `Runtime/Executable/ScheduledExecutables.cs` | `Runtime.Plan.Json`、`TriggerPlanExecutableDsl` | 禁止新导出链路接入，保留反序列化兼容 |
| `Runtime/Legacy/TriggerScheduler` | 非主线执行策略兼容入口 | `TriggerRunner + PlannedTrigger` | 不接回主线；可复用策略仅作为未来独立节点设计输入 |
| `Runtime/Dispatcher` | 旧 dispatcher 聚合和持续行为兼容层 | `TriggerRunner`、`EventBus`、`ActionScheduler` | 按调用方继续收口，必要时长期兼容 |
| `Runtime` 根目录占位文件 | 旧路径和 `.meta` GUID 兼容占位已完成清理；`Runtime/Compatibility` 机器清单当前为空，用于防止根目录 `.cs` 占位入口回流 | 对应正式子目录实现 | 不再新增根目录 `.cs` 占位入口；如确需兼容旧路径，必须重新登记清单、文档和测试 |
| Timeline / Rollback | 已明确为当前主线不支持或前置拒绝语义 | 独立正式能力设计 | 产品明确需要时另开设计实现 |

## 删除条件

删除 legacy 或 compatibility 入口前必须同时满足：

1. 包内源码、样例和测试不再引用该入口。
2. 文档已给出正式替代路径和迁移说明。
3. 对应旧数据要么可被正式路径读取，要么会产生明确诊断。
4. Unity `.meta` GUID 不再被资产、asmdef、示例或外部说明引用。
5. 删除动作进入明确的 major 版本或兼容清理批次。

## 文档同步规则

1. 新增或变更兼容入口时，同步更新 `Document/FormalApiBoundary.md`、`Runtime/Compatibility.md`、相关目录 README 和计划清单。
2. 示例迁移完成后，应同步更新 `Documentation~/README.md` 与商业化整改清单，避免文档继续指向旧入口。
3. 如果只完成文档降权而未完成代码迁移，应明确标为“兼容保留”或“延后决策”，不要标成已删除。
4. 构建或测试新增的兼容约束应优先放在 `RuntimeCompatibilityCatalogTests` 或相邻主线测试中。
