# Runtime 根目录兼容入口清理记录

当前 Runtime 根目录已不再保留 .cs 兼容占位入口；正式实现均位于对应子目录中，新的代码应直接引用正式路径。

正式机器清单位于 [`Runtime/Compatibility`](../Unity/Packages/com.abilitykit.triggering/Runtime/Compatibility) 目录，并由 [`RuntimeCompatibilityCatalog`](../Unity/Packages/com.abilitykit.triggering/Runtime/Validation/RuntimeCompatibilityCatalog.cs) 复用；当前清单为空，用于防止新的根目录兼容入口重新出现。

## 已删除占位入口

- `ActionContext.cs` -> `Runtime/Context/ActionContext.cs`
- `ActionExecutor.cs` -> `Runtime/ActionScheduler/ActionExecutor.cs`
- `ActionInstance.cs` -> `Runtime/ActionScheduler/ActionInstance.cs`
- `ActionScheduler.cs` -> `Runtime/ActionScheduler/ActionScheduler.cs`
- `ContextAdapter.cs` -> `Runtime/Executable/ContextAdapter.cs`
- `EventBusDispatcher.cs` -> `Runtime/Dispatcher/EventBusDispatcher.cs`
- `ExecCtxAdapter.cs` -> `Runtime/Context/ExecCtxAdapter.cs`
- `ITriggerDispatcher.cs` -> `Runtime/Dispatcher/ITriggerDispatcher.cs`
- `NumericValueRefContextExtensions.cs` -> `Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs`
- `PlannedTrigger.cs` -> `Runtime/Plan/PlannedTrigger.cs`
- `TimedDispatcher.cs` -> `Runtime/Dispatcher/TimedDispatcher.cs`
- `TriggerDispatcherHub.cs` -> `Runtime/Dispatcher/TriggerDispatcherHub.cs`
- `TriggerExecutor.cs` -> `Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs`
- `TriggerRunner.cs` -> `Runtime/Runtime/TriggerRunner.cs`

## 维护规则

- 不在 Runtime 根目录新增 .cs 兼容占位入口。
- 不把新 API 首发到 Runtime 根目录文件。
- 如果确需兼容旧路径，应先迁移调用方；必须保留时同步更新机器清单、本文档和相关测试。
