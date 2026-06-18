# AbilityKit Triggering

`com.abilitykit.triggering` 是 AbilityKit 的事件触发与规则执行包。正式接入时应优先使用 `TriggerRunner<TCtx>`、`TriggerPlan<TArgs>`、`PlannedTrigger<TArgs, TCtx>`、`ExecCtx<TCtx>`、`ActionRegistry`、`FunctionRegistry`、`ActionScheduler`、`RuleScheduler` 与 `Runtime/Validation`。

## 快速接入

1. 在项目层定义事件参数与上下文类型。
2. 通过 `ActionRegistry` 注册强类型动作。
3. 用 `TriggerPlan<TArgs>` 描述条件、动作和动作调度。
4. 用 `TriggerRunner<TCtx>` 注册计划并通过 `EventBus` 派发事件。
5. 数据化计划进入运行前先执行 `Runtime/Validation` 校验。

最小示例与完整运行时边界见 `Document/README.md`。

## 正式 API

| 场景 | API |
| --- | --- |
| 事件触发注册与派发 | `TriggerRunner<TCtx>` |
| 数据化触发计划 | `TriggerPlan<TArgs>` |
| 计划桥接到可执行触发器 | `PlannedTrigger<TArgs, TCtx>` |
| 条件函数扩展 | `FunctionRegistry` + `PredicateExprPlan` |
| 动作扩展 | `ActionRegistry` + `ActionCallPlan` |
| 触发器动作延迟/周期执行 | `ActionScheduler` |
| 规则级时间意图调度 | `RuleScheduler` |
| 运行前配置检查 | `Runtime/Validation` |

正式边界详见 `Document/FormalApiBoundary.md`。

## Legacy / Experimental 边界

以下路径仅用于兼容或迁移跟踪，不应作为新功能入口：

- `Runtime/Legacy`
- `Runtime/Experimental`
- `Runtime/Scheduler`
- `Runtime/Dispatcher` 中旧 `TriggerDispatcherHub` / `EventBusDispatcher` / `TimedDispatcher` 聚合入口
- `Documentation~/LegacySchedule` 中旧业务调度工厂示例

新功能需要迁移到 `TriggerRunner + PlannedTrigger + ActionRegistry`，调度需求按语义选择 `ActionScheduler` 或 `RuleScheduler`；旧 Executable 示例见 `Documentation~/LegacyExecutable`，旧 Schedule 业务示例见 `Documentation~/LegacySchedule`。

## 编辑器生成

Unity 菜单：`AbilityKit/Triggering/Codegen/Generate Ids`

生成器会扫描 `TriggerActionAttribute`、`TriggerFunctionAttribute`、`TriggerPayloadFieldAttribute` 与条件配置，并输出稳定 ID 到 `Runtime/Generated/Triggering.GeneratedIds.cs`。生成前会检查同类重复名称并在 Console 输出可定位的成员信息。

## 验证与测试

推荐在改动正式运行时后至少运行 Editor 测试中的以下覆盖：

- `TriggerRunnerMainlineTests`
- `ActionCallPlanValidatorTests`
- `TriggerConfigTests`

重点验证：主线触发顺序、动作调度上下文、`RuleScheduler` 组合语义、validator 对 unsupported schedule 的拒绝，以及 codegen 输出是否可编译。

## 深度文档

- `Document/README.md`：正式主线说明与最小接入示例。
- `Document/FormalApiBoundary.md`：正式 API、兼容 API、迁移方向。
- `Document/LegacyMigrationPolicy.md`：legacy、compatibility、experimental 入口的迁移、保留和下线策略。
- `Document/调度系统设计文档.md`：历史调度设计背景。
- `Document/Triggering-Commercial-Remediation-Checklist.md`：商业化整改清单。
