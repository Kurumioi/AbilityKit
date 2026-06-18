# Scheduler 兼容目录说明

`Runtime/Scheduler` 是旧版通用回调式调度注册体系，当前不作为 TriggerPlan Action 调度主线。

## 当前定位

- TriggerPlan Action 的延迟、周期、持续型执行统一走 `Runtime/ActionScheduler`。
- 规则级“立即/延迟/每隔/持续激活”语义优先使用 `Runtime/RuleScheduler`。
- 通用业务句柄式调度优先使用 `Runtime/Schedule` 下的 `SimpleScheduleManager` 或 `GroupedScheduleManager`。
- `SchedulerMigration` 提供旧 `SchedulerConfig` 到正式 `RuleSchedulePlan` 与推荐运行时的迁移映射。
- 包内调度 Samples 已改为演示 `RuleSchedulerRegistry`；样例中的旧 `SchedulerConfig` 仅作为迁移数据兼容字段。
- 本目录保留给旧 API、旧注册表或尚未迁移调用方兼容使用。

## 使用规则

1. 新的触发器计划 Action 不要接入本目录。
2. 新示例、新文档和新业务调度不要直接创建旧 `SchedulerRegistry`。
3. 迁移旧 `SchedulerConfig` 时优先通过 `SchedulerMigration.ToRuleSchedulePlan` 明确语义，再接入 `RuleScheduler`、`ActionScheduler` 或 `Schedule`。
4. 新的通用业务调度优先评估 `Runtime/Schedule`。
5. 如果发现仍依赖本目录的业务调用，应记录调用方并评估迁移到 `RuleScheduler`、`Schedule` 或 `ActionScheduler`。
6. 本目录后续方向是在兼容期后合并进 `Schedule`、迁入兼容目录，或随 major 版本移除。
