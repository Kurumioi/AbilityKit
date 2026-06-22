# Triggering 对象池接入设计

## 目标

Triggering 的对象池接入目标不是简单减少 `new`，而是让运行时实例化更可控，降低战斗/玩法运行中的内存碎片和 GC 抖动。

接入原则：

1. **优先运行时热点。** 先处理高频、短生命周期、可安全 Reset 的对象。
2. **保持正式主线稳定。** 不改变 [`TriggerRunner<TCtx>`](Unity/Packages/com.abilitykit.triggering/Document/FormalApiBoundary.md:1) 与计划/调度主线职责。
3. **显式作用域管理。** 使用 [`PoolScope`](Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PoolScope.cs:9) 绑定 Triggering 生命周期，而不是全局散落使用静态池。
4. **可配置、可诊断、可回收。** 池必须支持预热、修剪、统计与按场景清理。
5. **不强行池化所有实例。** 对构建期对象、长期持有对象和外部引用对象保持保守。

## 已有基础能力

Core 包已有完整池化基础：

- [`PoolScope`](Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PoolScope.cs:9)
- [`Pools`](Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/Pools.cs:1)
- [`ObjectPool<T>`](Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/ObjectPool.cs:6)
- [`IPoolable`](Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/IPoolable.cs:1)

这意味着 Triggering 侧不需要重造池系统，只需要定义：

- 该从哪里创建 scope
- 哪些类型适合池化
- 何时归还
- 如何 Reset
- 如何诊断

## 推荐池作用域

建议 Triggering 至少支持以下作用域命名：

- `triggering-runtime`
- `triggering-world`
- `triggering-match`
- `triggering-scope/<feature>`

原则：

- **世界级常驻玩法** 用 world scope。
- **单局/单战斗** 用 match scope。
- **子系统试点** 用 feature scope。
- **编辑器/测试** 可使用临时 scope 便于隔离。

## 第一批对象池接入候选

### 1. Action 调度链

最高优先级。

候选对象：

- [`ActionInstance`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/ActionInstance.cs:38)
- [`DefaultActionExecutor`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/ActionExecutor.cs:1)
- [`QueuedActionExecutor`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/ActionExecutor.cs:76)
- [`RetryActionExecutor`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/ActionExecutor.cs:155)

原因：

- 运行时注册频繁
- 生命周期短
- 具备明确 Reset 入口
- 与调度状态机紧耦合，适合做试点

### 2. 调度适配上下文

候选对象：

- [`ScheduleToBehaviorContextAdapter`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Strategies/ScheduleToBehaviorContextAdapter.cs:12)
- [`ScheduleBlackboard`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Strategies/ScheduleToBehaviorContextAdapter.cs:73)
- 其他单次执行适配器或临时上下文对象

原因：

- 单次调度执行会反复创建
- 生命周期短
- 一般不会跨帧外泄

### 3. 业务级调度包装对象

候选对象：

- `RuleScheduleEntry`
- 其他 `RuleScheduler`/`GroupedScheduleManager` 内部的短生命周期 class

原因：

- 调度系统经常创建/替换
- 适合与 scope 生命周期绑定

## 暂不优先池化的对象

以下对象不建议第一阶段池化：

- DSL 构建对象
- Json 解析/转换对象
- 验证期临时对象
- 长生命周期 registry/dictionary
- 外部 API 返回并可能长期持有的集合
- 无法彻底 Reset 的复杂对象

尤其是这些对象更适合先优化为：

- 复用 buffer
- 原地更新
- 延迟分配
- 结构重用

而不是直接放进池中。

## 实施阶段

### P15：设计与接入边界定义

产出：

- 池作用域命名规范
- 池化对象白名单/黑名单
- Reset 约定
- 预热与修剪策略
- 调试与统计规范

### P16：ActionScheduler 试点

落点：

- 将 [`ActionScheduler`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/ActionScheduler.cs:15) 作为首个运行时试点
- 为 `ActionInstance` 和 executor 包装器提供池化创建/归还路径
- 保持旧行为兼容

### P17：调度适配对象池化

落点：

- `ScheduleToBehaviorContextAdapter`
- 相关临时行为黑板或上下文包装对象

目标：

- 降低单帧调度中临时对象数量
- 避免频繁 new 带来的碎片化

### P18：配置与诊断接入

落点：

- 默认池配置
- 预热入口
- 修剪策略
- 诊断快照输出

目标：

- 让池行为可控
- 可按场景/功能调节
- 便于排查泄漏与误用

### P19：验证与回归

落点：

- 热点扫描
- 回归测试
- 构建验证
- 池误用测试

目标：

- 确认池化后行为不变
- 确认没有生命周期污染
- 确认实例化热点下降

## 设计约束

1. **池对象必须可 Reset。**
2. **池对象释放后不能再被外部持有。**
3. **池化不应改变调度语义。**
4. **优先对内部对象池化，不优先对对外暴露对象池化。**
5. **所有池化接入都必须可在测试中验证。**

## 建议的代码落点

后续实现时，建议优先在这些位置接入：

- [`Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions/`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Actions)
- [`Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Strategies/`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Strategies)
- [`Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Rules/`](Unity/Packages/com.abilitykit.triggering/Runtime/Scheduling/Rules)

避免一开始就大范围改动：

- [`Runtime/Plans`](Unity/Packages/com.abilitykit.triggering/Runtime/Plans)
- [`Runtime/Validation`](Unity/Packages/com.abilitykit.triggering/Runtime/Validation)
- [`Runtime/Executables`](Unity/Packages/com.abilitykit.triggering/Runtime/Executables)

这些区域多数属于构建期、配置期或正式主线基础类型，不适合作为第一批对象池试点。
