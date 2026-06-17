# Runtime 根目录兼容入口清单

`Runtime` 根目录下保留的 `.cs` 文件主要用于兼容旧 namespace、旧 `.meta` GUID 或旧引用路径。新的代码应优先引用对应子目录中的正式实现。

## 主线替代路径

| 兼容入口 | 推荐替代 |
| --- | --- |
| `ActionContext.cs` | `Runtime/Context/ActionContext.cs` |
| `ActionDelegateAdapter.cs` | `Runtime/ActionScheduler/ActionDelegateAdapter.cs`，但主线不再使用该占位适配器 |
| `ActionExecutor.cs` | `Runtime/ActionScheduler/ActionExecutor.cs` |
| `ActionInstance.cs` | `Runtime/ActionScheduler/ActionInstance.cs` |
| `ActionScheduler.cs` | `Runtime/ActionScheduler/ActionScheduler.cs` |
| `ContextAdapter.cs` | `Runtime/Context/ContextAdapter.cs` |
| `EventBusDispatcher.cs` | `Runtime/Dispatcher/EventBusDispatcher.cs`，仅兼容旧 Dispatcher API |
| `ExecCtxAdapter.cs` | `Runtime/Context/ExecCtxAdapter.cs` |
| `ITriggerDispatcher.cs` | `Runtime/Dispatcher/ITriggerDispatcher.cs` |
| `NumericValueRefContextExtensions.cs` | `Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs` |
| `PlannedTrigger.cs` | `Runtime/Plan/PlannedTrigger.cs` |
| `TimedDispatcher.cs` | `Runtime/Dispatcher/TimedDispatcher.cs`，仅兼容旧 Dispatcher API |
| `TriggerDispatcherHub.cs` | `Runtime/Dispatcher/TriggerDispatcherHub.cs`，仅兼容旧 Dispatcher API |
| `TriggerDispatcherHub_new.cs` | 不建议新代码引用；后续应随兼容清理删除 |
| `TriggerExecutor.cs` | `Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs`，仅兼容旧执行器 API |
| `TriggerRunner.cs` | `Runtime/Runtime/TriggerRunner.cs` |

## 删除条件

1. 包内源码不再引用根目录兼容入口。
2. 外部包和样例完成迁移，并在迁移说明中记录替代路径。
3. Unity `.meta` GUID 不再被资产、asmdef 或示例引用。
4. 在一次 major 版本或明确的兼容清理批次中删除。

## 维护规则

- 不在根目录兼容入口中新增功能。
- 不把新 API 首发到根目录文件。
- 根目录文件只允许转发、占位、过时提示或兼容说明。
- 如果发现新代码引用根目录入口，应优先迁移到表格中的正式路径。
