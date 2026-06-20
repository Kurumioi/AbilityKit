# Triggering 模块剩余缺失优先级清单

## P0
- 统一调度边界：收敛 `ActionScheduler`、`TriggerScheduler`、`Dispatcher`、`Schedule`、`Continuous` 的职责分层，避免同类语义多入口。
- 补齐计划级校验闭环：把 `Plan`、`Action`、`Condition`、`Schedule`、`Context` 的一致性校验整合到一个明确入口。

## P1
- 诊断指标化：将 `Diagnostics` 从采集点升级为可聚合、可对比、可回放的性能/行为指标体系。
- 黑板数值收口：继续收紧 `Blackboard`、`NumericValueRef`、`Resolver` 的隐式兼容与域边界。
- 测试语义覆盖：补强调度边界、条件短路、上下文切换、周期执行、失败路径的编辑器测试。

## P2
- 模板版本管理：补齐 `Plan` / `Json` / `Dsl` 的模板版本化与演进策略。
- 编辑器化能力：如果后续需要配置视图，再补可视化编辑语义与导出流程。
