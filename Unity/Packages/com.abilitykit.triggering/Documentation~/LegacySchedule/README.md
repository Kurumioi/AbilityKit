# Legacy Schedule Examples

该目录仅保留旧调度体系中的业务化示例源码，供迁移或历史参考使用；这里的源码不属于 `AbilityKit.Triggering` 运行时编译面。

## 边界

- `ScheduleEffectFactory.cs` 展示 Buff、Bullet、AOE 等旧业务调度效果工厂写法。
- 新功能应优先使用 `Runtime/RuleScheduler` 的 `RuleSchedulePlan`、`IRuleScheduleEffect` 与 `RuleSchedulerRegistry`。
- 触发计划内部动作调度应使用 `Runtime/ActionScheduler`，不要新增旧 `Runtime/Schedule/Factories` 入口。
