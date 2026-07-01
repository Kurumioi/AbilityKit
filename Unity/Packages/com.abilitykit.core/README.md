# AbilityKit Core

`com.abilitykit.core` 是 AbilityKit 的基础设施包。它不表达具体玩法规则，而是提供上层战斗、技能、同步、工具和示例都可以复用的通用运行时能力。

## 定位

Core 是第一批内部推广的 P0 基础底座包，适合被 `world.di`、`triggering`、`pipeline`、`combat.*`、`record`、`diagnostics` 等模块依赖。

它主要负责：

- 纯 C# 数学类型：`Vec2`、`Vec3`、`Quat`、`Transform3`。
- 日志抽象：`Log`、`ILogSink`、`NullLogSink`。
- 事件基础设施：`EventDispatcher`、`GlobalEventDispatcher`、`EventKey`。
- 对象池：`ObjectPool<T>`、`PoolScope`、`PoolRegistry`、`PoolConfigCenter`。
- Marker 扫描与注册：`MarkerAttribute`、`MarkerRegistry`、`MarkerScanner`。
- 轻量数值系统：`NumberValue`、`NumberModifier`、`NumberEffect`。
- 持续行为生命周期：`IContinuous`、`IContinuousManager`、`DefaultContinuousManager`。
- 配置和 JSON 设置加载：`LayeredJsonSettingsStore`、`PersistentJsonConfigLoader`。

## 不负责什么

- 不承载项目业务技能、Buff、角色或怪物逻辑。
- 不直接依赖 `demo.moba.*`、`demo.shooter.*` 或服务端 Demo。
- 不决定具体网络、帧同步或表现层方案。
- 不替代 `attributes` 的持久属性系统；Core 的 `Numerics` 更适合临时计算和轻量修饰。

## 推荐接入

最小推广组合从 `Foundation` 开始：

```text
com.abilitykit.core
com.abilitykit.world.di
```

在这个组合中，Core 提供日志、事件、对象池、数值和 Marker；`world.di` 负责战斗世界或关卡作用域的服务装配。

## 验收要求

Core 进入内部 Starter 前至少满足：

- 包根目录有 README，说明定位、边界和推荐接入方式。
- `Documentation~/README.md` 能索引 Core 的主要子能力。
- Foundation 示例能展示日志、事件、对象池或持续行为中的至少两个能力。
- 不引入 Demo 包反向依赖。
- `package.json` 合法且版本与内部依赖策略一致。

## 相关文档

- [`Documentation~/README.md`](./Documentation~/README.md)：Core 文档索引。
- [`Runtime/Markers/README.md`](./Runtime/Markers/README.md)：Marker 系统说明。
- [`../README.md`](../README.md)：AbilityKit 包分级、推荐组合和 Starter 推进顺序。
