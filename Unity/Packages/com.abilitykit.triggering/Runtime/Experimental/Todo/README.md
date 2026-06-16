# Triggering Runtime 实验 TODO 迁移说明

本目录用于收纳仍有设计价值、但尚未进入稳定触发器执行主线的非主线实现。它的目标是保留可复用概念，同时避免实验实现继续和正式主线混在一起。

## 当前稳定主线

- `Runtime/Plan/Json/TriggerPlanJsonDatabase.cs`
- `Runtime/Plan/Executables/ITriggerPlanExecutable.cs`
- `Runtime/Plan/PlannedTrigger.cs`
- `Runtime/Runtime/TriggerRunner.cs`
- `Runtime/ActionScheduler/*`

## 迁移规则

1. 历史实现仍有设计价值时，不直接删除；先标记、镜像或迁移到本目录跟踪。
2. 兼容文件在所有包调用方完成迁移前，保留原命名空间与原入口。
3. 未完成的非主线概念优先在本目录沉淀，再将验证稳定的部分逐步接入 `Plan/Executables`。
4. 优先修复和正式化主线代码，再处理 TODO/实验实现。
5. 每一类 TODO 迁移都需要说明最终方向：合并进主线、适配器包装，或长期保留为实验能力。

## 当前已迁移到 TODO 跟踪的分组

- `TriggerScheduler`：未来可能抽象出的触发器执行策略层。当前主线尚未使用，但其中的调度/策略概念有保留价值。
- `Executable`：独立的类行为树可执行系统。当前主线使用 `Plan/Executables`，应等主线执行语义稳定后再按节点语义逐步迁移。

## 主线迁移进度

- `PlannedTrigger` 已改为按 Action 维度处理调度型 Action，不再由第一个 Action 决定整条 Trigger 的执行模式；同一次计划激活可混合执行立即、延迟、周期和持续 Action。
- `TriggerRunner.Dispatch` 已拆分为优先级阻断、条件评估、条件拒绝处理、执行、硬中断等阶段化方法，降低主派发流程复杂度。
- `ActionScheduler` 已正式化注册/替换语义、异常失败处理、实例索引清理和生命周期释放逻辑。
- `ActionScheduler` 在更新时会将具体 `ActionInstance` 传入 `ActionExecutionContext`，Executor 不再接收管理器级空实例上下文。
- `ActionInstance` 已具备延迟、周期和持续 Action 的正式调度窗口，不再只覆盖部分延迟处理。
