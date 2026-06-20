# Triggering 产品化验收指南

## 验收目标

| 维度 | 要求 | 验收方式 |
| --- | --- | --- |
| 主线入口 | 新功能只依赖 `TriggerRunner<TCtx>`、`TriggerPlan<TArgs>`、`PlannedTrigger<TArgs, TCtx>`、`ExecCtx<TCtx>`、`ActionRegistry`、`FunctionRegistry`、`ActionScheduler`、`RuleScheduler` 与 `Runtime/Validation` | 搜索新增代码是否继续引入旧派发、旧调度或旧执行入口 |
| 确定性 | `ExecPolicy.RequireDeterministic` 下不得依赖随机数、系统时间、网络请求或未登记的非确定性函数/动作 | 同一计划、同一事件序列、同一上下文快照重复执行，动作顺序和结果一致 |
| 计划动作 | 计划内动作必须通过正式的 `ActionScheduler` 组织 | 检查计划执行路径是否仍然保留直达执行捷径 |
| 业务调度 | 持续、周期或延迟类业务节奏必须通过 `RuleScheduler` 组织 | 检查样例与测试是否都只使用正式调度入口 |

## 记录要求

- 验收报告只记录正式入口、正式边界和正式样例。
- 文档、示例与测试不再维护旧兼容结论。
- 新增能力必须先落到正式主线，再补充对应验证。

## 常用检查

1. 搜索新增代码是否仍然引用旧调度或旧派发入口。
2. 检查示例是否仅使用正式计划、上下文与调度类型。
3. 检查验收测试是否覆盖计划执行、动作调度与业务调度。
4. 检查产品说明是否仅描述正式能力。
