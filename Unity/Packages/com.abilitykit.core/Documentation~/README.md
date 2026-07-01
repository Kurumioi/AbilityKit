# Core 基础设施文档索引

> AbilityKit Core 基础设施包文档入口

---

## 包定位

`com.abilitykit.core` 是 AbilityKit 的 P0 基础底座包，提供上层模块共享的纯 C# 基础设施。它不是业务战斗包，也不是 Demo 包。

Core 当前覆盖以下能力：

- 数学基础：`Runtime/Math`。
- 日志：`Runtime/Logging`。
- 事件：`Runtime/Event`、`Runtime/Eventing`。
- 对象池：`Runtime/Pooling`。
- Marker 扫描与注册：`Runtime/Markers`。
- 轻量数值修饰：`Runtime/Numerics`。
- 持续行为生命周期：`Runtime/Continuous`。
- 配置与 JSON 设置：`Runtime/Config`。

---

## 文档列表

### 1. [数值系统模块开发设计文档](./数值系统模块开发设计文档.md)

**阅读对象**：首次接触数值系统的开发者

**内容概要**：
- 数值系统 vs 属性系统的关系（互补而非互斥）
- 核心概念：NumberValue、Modifier、Handle、Effect
- 架构图和完整计算流程
- 设计模式总结
- 适用场景说明

**推荐阅读顺序**：从本文档开始

### 2. [Marker 系统说明](../Runtime/Markers/README.md)

**阅读对象**：需要用 Attribute/Marker 自动注册类型的框架或业务模块开发者

**内容概要**：
- MarkerAttribute 的用途
- MarkerRegistry 和扫描入口
- 上层包如何通过 Marker 降低手写注册成本

---

## 快速入门

### 想了解 Core 是什么？

先阅读包根目录 [`README.md`](../README.md)，确认 Core 的定位、边界和 Starter 验收要求。

### 想了解数值系统是什么？

👉 阅读 [数值系统模块开发设计文档](./数值系统模块开发设计文档.md) 第一章「设计理念」

### 想学习如何使用？

👉 阅读 [数值系统模块开发设计文档](./数值系统模块开发设计文档.md) 第六章「使用指南」

### 想了解与属性系统的关系？

👉 阅读 [数值系统模块开发设计文档](./数值系统模块开发设计文档.md) 第七章「与属性系统的关系」

### 想验证 Foundation Starter？

Foundation Starter 的第一阶段只应依赖 `core` 和 `world.di`，用于验证日志、事件、对象池、World 服务注册和宿主驱动 Tick。示例落点优先使用 `src/AbilityKit.Samples.Logic`，避免直接依赖任何 `demo.*` 包。

---

## 概念速查

### Core 子能力

| 子能力 | 路径 | 用途 |
|------|------|------|
| Math | `Runtime/Math` | 跨 Unity/服务端/测试的基础数学类型 |
| Logging | `Runtime/Logging` | 日志入口与输出 Sink 抽象 |
| Event | `Runtime/Event` | 轻量事件发布、订阅和取消订阅 |
| Pooling | `Runtime/Pooling` | 对象池、池作用域、池配置与诊断 |
| Markers | `Runtime/Markers` | Attribute 标记和类型扫描注册 |
| Numerics | `Runtime/Numerics` | 临时数值修饰与效果句柄 |
| Continuous | `Runtime/Continuous` | DOT/HOT/持续行为生命周期管理 |
| Config | `Runtime/Config` | 分层 JSON 配置和持久设置加载 |

### 核心类

| 类 | 职责 |
|------|------|
| `NumberValue` | 数值容器，管理基础值和修饰器 |
| `NumberValueMode` | 计算模式选择器 |
| `NumberModifier` | 修饰器，包含操作和数值 |
| `NumberModifierHandle` | 修饰器句柄，用于移除 |
| `NumberEffect` | 效果包，多个修饰器的组合 |
| `NumberEffectHandle` | 效果句柄，实现 IDisposable |

### Starter 推荐 API

| 能力 | 推荐 API |
|------|------|
| 日志 | `Log.SetSink(...)`、`ILogSink` |
| 事件 | `EventDispatcher`、`EventKey` |
| 对象池 | `ObjectPool<T>`、`PoolScope`、`PoolRegistry` |
| Marker | `MarkerAttribute`、`MarkerScanner`、`MarkerRegistry` |
| 持续行为 | `IContinuous`、`DefaultContinuousManager` |

### 修饰器操作

| 操作 | 说明 |
|------|------|
| `Add` | 直接加到基础值 |
| `Mul` | 乘法叠加 |
| `FinalAdd` | 最终加法 |
| `Override` | 强制覆盖 |

### 计算模式

| 模式 | 说明 |
|------|------|
| `BaseOnly` | 只返回基础值 |
| `BaseAdd` | Base + Add + FinalAdd |
| `BaseAddMul` | (Base+Add)*(1+Mul)+FinalAdd |
| `OverrideOnly` | Override 或 Base |

### 计算公式

```
damage = (BaseDamage + FlatBonus) * (1 + PctBonus) + FinalBonus
```

---

## 相关文档

- [AbilityKit 包总览](../../README.md) - 包分级、推荐组合和 Starter 推进顺序
- [属性系统模块](../../com.abilitykit.attributes/Document/) - 持久属性系统，与 Core 数值系统互补
- [能力管线模块](../../com.abilitykit.pipeline/Document/) - 技能执行管线
- [触发器模块](../../com.abilitykit.triggering/Document/) - 事件触发系统

---

## 典型使用场景

| 场景 | 说明 |
|------|------|
| 伤害计算 | 基础伤害 + 各类加成 |
| Buff/Debuff | 效果叠加和移除 |
| 技能加成 | 多种加成的组合 |
| 临时计算 | 不需要持久化的中间结果 |
| 管线处理 | Pipeline 中的数据处理 |

---

## 源码路径

```
com.abilitykit.core/Runtime/
├── Config/                  # JSON 设置和配置加载
├── Continuous/              # 持续行为生命周期
├── Event/                   # 事件发布订阅
├── Logging/                 # 日志抽象
├── Markers/                 # Marker 扫描注册
├── Math/                    # 纯 C# 数学类型
├── Numerics/                # 轻量数值修饰
└── Pooling/                 # 对象池和诊断
```

---

*最后更新：2026-07-01*
