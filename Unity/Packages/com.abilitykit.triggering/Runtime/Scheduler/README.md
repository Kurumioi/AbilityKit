# Scheduler 兼容目录说明

`Runtime/Scheduler` 是旧版通用回调式调度注册体系，当前不作为 TriggerPlan Action 调度主线。

## 当前定位

- TriggerPlan Action 的延迟、周期、持续型执行统一走 `Runtime/ActionScheduler`。
- 通用业务句柄式调度优先使用 `Runtime/Schedule` 下的 `SimpleScheduleManager` 或 `GroupedScheduleManager`。
- 本目录保留给旧 API、旧注册表或尚未迁移调用方兼容使用。

## 使用规则

1. 新的触发器计划 Action 不要接入本目录。
2. 新的通用业务调度优先评估 `Runtime/Schedule`。
3. 如果发现仍依赖本目录的业务调用，应记录调用方并评估迁移到 `Schedule` 或 `ActionScheduler`。
4. 本目录后续方向是在兼容期后合并进 `Schedule`、迁入兼容目录，或随 major 版本移除。
