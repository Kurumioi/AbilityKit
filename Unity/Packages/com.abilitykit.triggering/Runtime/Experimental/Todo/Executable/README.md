# Executable TODO 迁移说明

`Runtime/Executable` 是一套仍有价值的类行为树执行子系统，但它不是当前稳定触发器执行主线。

## 当前主线等价路径

- `Runtime/Plan/Executables/ITriggerPlanExecutable.cs`
- `Runtime/Plan/Executables/*TriggerPlanExecutable.cs`
- `Runtime/Plan/Json/TriggerPlanConverter.cs`
- `Runtime/Plan/Json/TriggerPlanJsonDatabase.cs`

不要删除 `Runtime/Executable`。

## 迁移方向

1. 将 `Runtime/Executable` 保留为兼容入口和实验性的行为组合支持。
2. 只有在 `Plan/Executables` 行为语义稳定后，再增加适配器或迁移节点。
3. 在 JSON 计划加载与执行结果语义统一前，不要将 `ExecutableTriggerDatabase` 接入主线。
4. 已验证稳定的节点语义应按节点逐个迁移到 `Plan/Executables`，避免整套体系一次性切换。

## 后续候选迁移项

- `ExecutableDsl` 流式构建能力 -> 可选的计划构建 DSL。
- `ScheduledExecutor` / `IScheduledExecutable` 概念 -> 合并到 `ActionScheduler` 或未来计划调度层。
- Decorator 概念 -> 等条件/Action 解析统一后转换为 `ITriggerPlanExecutable` 装饰器。
- `ExecutableRegistry` 标记扫描 -> 等 Source Generator 与 Plan Converter 路径稳定后再评估。
