# Dispatcher 兼容边界说明

`Runtime/Dispatcher` 是旧 Dispatcher API 与外部驱动方式的兼容聚合层，不是新的事件计划触发主线。

## 当前定位

- 新的事件订阅、条件评估、执行控制与短路流程优先使用 `Runtime/Runtime/TriggerRunner.cs`。
- `TriggerDispatcherHub` 保留给旧 `EventBusDispatcher`、`TimedDispatcher`、持续 tick 驱动和外部系统适配。
- 由 `TriggerPlan.Actions` 派生的延迟、周期、持续型 Action 调度统一交给 `Runtime/ActionScheduler`。

## 使用规则

1. 新增 TriggerPlan 事件触发能力时，不要直接扩展本目录。
2. 需要兼容旧 Dispatcher 调用方时，可以通过 `TriggerDispatcherHub` 包装，但应在迁移计划中记录调用方。
3. 新增持续行为如果能表达为 TriggerPlan Action 调度，优先接入 `ActionScheduler`；只有外部生命周期强绑定的 tick 行为才保留在 Dispatcher 适配层。
4. 本目录后续方向是兼容层长期保留或随旧调用方迁移完成后降级到 `Compatibility`。
