# Triggering Formal API Boundary

本文档定义 `com.abilitykit.triggering` 作为正式稳定包时的主线 API、兼容 API 与迁移方向。

## 正式主线 API

以下 API 是新功能优先依赖的主线入口：

- `Runtime/Runtime/TriggerRunner.cs`：事件触发器注册、排序、派发入口。
- `Runtime/Plan/TriggerPlan.cs`：数据化触发计划。
- `Runtime/Plan/PlannedTrigger.cs`：计划到可执行触发器的桥接。
- `Runtime/Runtime/ExecCtx.cs`：动作和条件执行上下文。
- `Runtime/Registry/ActionRegistry.cs`：强类型动作注册。
- `Runtime/Registry/FunctionRegistry.cs`：条件函数注册。
- `Runtime/Plan/PredicateExprPlan.cs`：数据化条件表达式。
- `Runtime/ActionScheduler/ActionScheduler.cs`：`TriggerPlan` 内部动作调度。
- `Runtime/RuleScheduler/RuleScheduler.cs`：业务无关的规则时间意图调度。
- `Runtime/Validation`：运行前配置校验。

## 兼容 API

以下目录允许保留，但不作为新增功能首选：

- `Runtime/Schedule`：早期通用调度适配层。可以用于迁移旧代码或包内适配，但新规则调度应优先选择 `RuleScheduler`。
- `Runtime/Scheduler`：旧版 scheduler registry 和 scheduler 实现。仅用于兼容旧项目引用，新增代码应迁移到 `ActionScheduler` 或 `RuleScheduler`；包内 Samples 不再把旧 `SchedulerRegistry` 作为新入口示例。
- `Runtime/Legacy`：历史执行器、历史配置转换和旧 DSL。仅用于旧数据迁移。
- `Runtime/Experimental`：实验或 TODO 代码。不得作为正式运行时依赖。
- `Runtime/Compatibility`：根目录兼容入口的正式机器清单；当前清单为空，用于防止 Runtime 根目录 `.cs` 占位入口回流。

兼容、遗留和实验入口的分级、迁移优先级、删除条件统一以 [`LegacyMigrationPolicy.md`](LegacyMigrationPolicy.md) 为准。

## 调度选择规则

| 需求 | 使用 |
| --- | --- |
| 触发器动作延迟执行 | `ActionScheduler` |
| 触发器动作周期执行 | `ActionScheduler` |
| “当规则成立后每隔 N 秒执行一次” | `RuleScheduler` |
| “保持条件期间持续执行规则效果” | `RuleScheduler.WhileActive` |
| Buff 生命周期、暂停、结束、堆叠 | Core `IContinuousManager` 或业务连续行为系统 |
| 旧业务 ID 调度器迁移前兼容 | `Runtime/Scheduler` |

## Runtime 包词汇约束

正式 Runtime 代码应使用业务无关词汇：

- 推荐：Rule、Trigger、Action、Predicate、Condition、Schedule、Effect、Subject、Group、Context。
- 避免：Buff、Projectile、Bullet、AOE、Skill、Unit 等具体业务词汇。

如果必须提供业务样例，应放在 `Samples`、Demo 包或业务包中，不应成为 Triggering Runtime 的核心依赖。

## 迁移建议

1. 新增数据化触发逻辑时，从 `TriggerPlan<TArgs>` 与 `ActionRegistry` 开始。
2. 新增规则级时间意图时，从 `RuleSchedulePlan` 与 `IRuleSchedulerDriver` 开始。
3. Runtime 根目录不再新增 `.cs` 兼容占位入口；如确需兼容旧路径，必须先通过 `Runtime/Compatibility` 清单、文档和测试登记。
4. 旧 `SchedulerRegistry` 代码逐步迁移到 `RuleSchedulerRegistry`；旧 `SchedulerConfig` 先通过 `SchedulerMigration.ToRuleSchedulePlan` 明确语义后再接入正式调度入口。
5. 新样例应直接演示 `RuleSchedulerRegistry`、`ActionScheduler` 或 `Schedule`，不要把 `Runtime/Scheduler` 作为首选路径。
6. Buff 等持续效果只把 Triggering 当成规则触发源，不把 Triggering 当成生命周期管理器。
