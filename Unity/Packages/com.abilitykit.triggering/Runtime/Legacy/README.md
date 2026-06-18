# Triggering Legacy Runtime

这里集中存放已经被主线路径替代、但暂时保留兼容性的运行时代码。

## 目录策略

- `Executable/`：旧 Runtime/Executable 入口、旧配置转换器与旧示例。旧调度工厂扩展入口已下线，旧 schedule 配置只通过固定兼容映射转换。命名空间暂时保持不变，避免一次迁移破坏反序列化和外部引用。
- `Schedule/`：已被新调度管理器替代的旧调度实现。
- `TriggerScheduler/`：非主线 Trigger 执行策略，迁移价值已在 Experimental/Todo 中跟踪。
- 触发器包不再承载目标查找框架职责；`HasTargetCondition`、`EntityFinderAdapter` 这类临时实现只允许作为迁移/删除的过渡对象，不应作为正式能力继续扩展。
- `ExecutableDsl`、各类 `*Builder` 与 `ScheduledExecutableBuilderExtensions` 均属于旧 Runtime/Executable 兼容入口；新代码不得继续使用这些类型，应迁移到 `Runtime/Plan`、`TriggerPlanBuilder`、`Plan/Executables`、`TriggerPlanExecutableDsl` 与 `ActionScheduler`。
- 旧配置中的 `PayloadCompare`、`HasTarget` 不再转换为可运行条件；它们会在转换阶段显式失败，避免在触发执行期产生伪成功或包边界污染。
- 旧配置转换器不再用 `true`、`Equal`、`Const(0)` 或 `0` 这类隐式默认值补全非法配置；缺失条件、未知比较符、未知数值引用和旧 switch 表达式都会显式失败。

## 迁移规则

- 新功能不得依赖本目录。
- 可复用语义应迁移到 `Runtime/Plan`、`Runtime/ActionScheduler` 或正式调度目录。
- 条件迁移优先使用通用谓词/注册条件扩展；目标查找类能力应由 targeting 包提供，再以正式扩展点接入触发计划。
- 删除前需要确认旧 JSON、旧示例和外部 API 不再引用对应类型。
