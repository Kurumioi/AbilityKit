# Triggering 旧版派发目录

`Runtime/Dispatcher` 仅保留历史说明，不再作为正式接入路径。

- 正式触发流程请使用 `TriggerRunner`、`PlannedTrigger` 与 `ActionScheduler`。
- 旧派发聚合入口不再扩展新能力。
- `ITriggerDispatcherContext` 仍作为桥接类型保留给正式执行链使用。
