# 5.2 对象池：Pools、PoolScope、ObjectPool 与配置化复用

> 本文从源码解释 AbilityKit Core 对象池。它不是简单的 `Rent/Return` 包装，而是一套包含全局门面、命名作用域、类型+Key 分池、弱表反向归还、生命周期钩子、配置仲裁和调试统计的复用体系。

---

## 1. 能力定位

对象池解决的是高频短生命周期对象反复分配导致的 GC 抖动问题。

在 AbilityKit 中，它主要用于：

- 事件参数、临时列表、快照列表等短生命周期运行时对象。
- 投射物、Buff、伤害事件、表现事件等频繁创建和归还的对象。
- 需要按战斗、场景、UI、功能域拆分生命周期的缓存对象。
- 编辑器或诊断工具读取对象池统计，定位池容量和泄漏风险。

它不适合：

- 生命周期很长、数量很少的对象。
- 状态清理成本高于重新创建成本的对象。
- 持有外部资源且销毁语义复杂的对象，除非明确实现 `onDestroy` 或 `IPoolable.OnPoolDestroy()`。

源码入口：

| 源码 | 作用 |
|------|------|
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/Pools.cs` | 全局静态门面，默认使用 `PoolRegistry.Global` |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PoolRegistry.cs` | 管理全局 scope、命名 scope、配置 provider 和调试快照 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PoolScope.cs` | 一个作用域内的一组池，负责生命周期和配置解析 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PoolManager.cs` | 按 `(Type, PoolKey)` 保存池，并用 `ConditionalWeakTable` 记录对象归还句柄 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/ObjectPool.cs` | 单个类型对象池，负责 Get/Release/Prewarm/Trim/Clear/Stats |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/PooledObject.cs` | `IDisposable` 归还句柄，用于 `using` 风格自动归还 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Core/IPoolable.cs` | 对象池生命周期钩子 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Config/PoolConfigCenter.cs` | 配置提供者仲裁、优先级、诊断报告 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Config/PoolConfigModule.cs` | 字典式配置模块和 `PoolConfigBuilder` 构建器 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Config/PoolItemConfig.cs` | 单个池的 enabled、capacity、prewarm、trim、neverTrim 配置 |
| `Unity/Packages/com.abilitykit.core/Runtime/Pooling/Config/PoolConfigRequest.cs` | 配置查询键：scopeName + elementType + PoolKey |

---

## 2. 总体结构

```mermaid
flowchart TB
    Caller[调用方] --> Pools[Pools 静态门面]
    Pools --> Registry[PoolRegistry]
    Registry --> Global[Global PoolScope]
    Registry --> Named[Named PoolScope\nBattle/UI/Scene]

    Global --> ManagerA[PoolManager]
    Named --> ManagerB[PoolManager]

    ManagerA --> PoolA[ObjectPool<T>\nType + PoolKey]
    ManagerB --> PoolB[ObjectPool<T>\nType + PoolKey]

    PoolA --> Stack[Stack<T> inactive]
    PoolA --> Stats[PoolStats]
    PoolA --> Hooks[IPoolable + lifecycle delegates]

    Registry --> Config[PoolConfigCenter]
    Config --> Providers[IPoolConfigProvider list]
```

这个结构把三类问题分开：

| 层级 | 负责内容 |
|------|----------|
| `Pools` | 给调用方提供最短入口，默认走全局池 |
| `PoolRegistry` | 管理 scope 和配置 provider |
| `PoolScope` | 决定对象池生命周期边界，解析配置并创建池 |
| `PoolManager` | 管理 `(Type, PoolKey)` 到具体池的映射，支持按对象实例反向归还 |
| `ObjectPool<T>` | 单池 Get/Release/Trim/Prewarm/Clear 和统计 |

---

## 3. 最短使用路径

源码中的真实 API 使用 `Get`/`Release`，不是传统示例里的 `Rent`/`Return`。

```csharp
var evt = Pools.Get(
    createFunc: () => new DamageEvent(),
    onRelease: e => e.Reset(),
    defaultCapacity: 32,
    maxSize: 256);

evt.AttackerId = attackerId;
evt.TargetId = targetId;
evt.Value = damage;

Pools.Release(evt);
```

也可以先拿到池对象：

```csharp
var pool = Pools.GetPool(
    createFunc: () => new DamageEvent(),
    onRelease: e => e.Reset(),
    defaultCapacity: 32,
    maxSize: 256);

var evt = pool.Get();
try
{
    evt.Value = 100;
}
finally
{
    pool.Release(evt);
}
```

如果希望作用域自动归还，可以使用 `PooledObject<T>`：

```csharp
using var rented = Pools.GetPooled(() => new DamageEvent(), onRelease: e => e.Reset());
var evt = rented.Value;
evt.Value = damage;
```

`PooledObject<T>.Dispose()` 内部会调用创建它的 `ObjectPool<T>.Release(Value)`。

---

## 4. Get 流程

`ObjectPool<T>` 内部使用 `Stack<T>` 保存未激活对象，并用锁保护状态。

```mermaid
flowchart TB
    Start[ObjectPool<T>.Get] --> Lock[lock _syncRoot]
    Lock --> Inc[_getTotal++]
    Inc --> Has{_stack.Count > 0?}
    Has -->|是| Pop[Pop inactive object]
    Pop --> Hit[_hitCount++]
    Hit --> EditorRemove[UNITY_EDITOR: inactiveSet.Remove]
    EditorRemove --> Peak[UpdatePeakActiveCount]
    Has -->|否| Miss[_missCount++]
    Miss --> Create[createFunc]
    Create --> Null{created == null?}
    Null -->|是| Throw[throw InvalidOperationException]
    Null -->|否| Created[_createdTotal++]
    Created --> Peak
    Peak --> PoolGet[TryOnPoolGet]
    PoolGet --> OnGet[onGet?.Invoke]
    OnGet --> Return[return object]
```

调用顺序非常明确：

1. 从池里取出或创建对象。
2. 更新统计和峰值激活数量。
3. 如果对象实现 `IPoolable`，调用 `OnPoolGet()`。
4. 调用创建池时传入的 `onGet` 委托。
5. 返回对象给调用方。

---

## 5. Release 流程

归还路径会先清理对象状态，再决定入栈还是销毁。

```mermaid
flowchart TB
    Start[ObjectPool<T>.Release] --> Null{element == null?}
    Null -->|是| ThrowNull[ArgumentNullException]
    Null -->|否| Lock[lock _syncRoot]
    Lock --> Inc[_releaseTotal++]
    Inc --> Check{UNITY_EDITOR collectionCheck?}
    Check -->|重复归还| ThrowDouble[InvalidOperationException]
    Check -->|通过| PoolRelease[TryOnPoolRelease]
    PoolRelease --> OnRelease[onRelease?.Invoke]
    OnRelease --> Full{_stack.Count >= _maxSize?}
    Full -->|是| Destroy[DestroyElementUnsafe]
    Destroy --> Overflow[_overflowDestroyCount++]
    Full -->|否| Push[_stack.Push]
    Push --> EditorAdd[UNITY_EDITOR: inactiveSet.Add]
```

这条顺序决定了一个重要约束：对象状态清理发生在是否溢出销毁之前。即使池已满，对象也会先走 `OnPoolRelease()` 和 `onRelease`，再走销毁逻辑。

`DestroyElementUnsafe` 会先递增 `_destroyedTotal`，再调用销毁钩子：

1. `_destroyedTotal++`。
2. `element.TryOnPoolDestroy()`，即 `IPoolable.OnPoolDestroy()`。
3. `_onDestroy?.Invoke(element)`。

---

## 6. IPoolable 生命周期

`IPoolable` 是对象自己感知池生命周期的方式：

```csharp
public interface IPoolable
{
    void OnPoolGet();
    void OnPoolRelease();
    void OnPoolDestroy();
}
```

它和委托生命周期共同生效：

```mermaid
sequenceDiagram
    participant Pool as ObjectPool<T>
    participant Obj as T/IPoolable
    participant Delegate as lifecycle delegates

    Pool->>Obj: OnPoolGet()
    Pool->>Delegate: onGet(obj)
    Note over Obj,Delegate: 调用方使用对象
    Pool->>Obj: OnPoolRelease()
    Pool->>Delegate: onRelease(obj)
    alt 池满、Clear(destroy) 或 Trim 销毁
        Pool->>Obj: OnPoolDestroy()
        Pool->>Delegate: onDestroy(obj)
    end
```

推荐分工：

| 位置 | 推荐职责 |
|------|----------|
| `OnPoolGet()` | 重置运行态标记、重新启用对象 |
| `onGet` | 注入外部上下文或统计埋点 |
| `OnPoolRelease()` | 清空引用、重置字段、防止旧状态污染下次使用 |
| `onRelease` | 模块级清理、列表 Clear、解绑外部关系 |
| `OnPoolDestroy()` / `onDestroy` | 释放非托管资源、断开最终引用 |

---

## 7. Scope 与 PoolKey

`PoolScope` 用来表达“这一组池一起创建、一起清理”。例如：

```csharp
var battleScope = Pools.GetOrCreateScope("Battle:1001");
var projectile = battleScope.Get(
    key: new PoolKey("Projectile.Fast"),
    createFunc: () => new ProjectileEntity(),
    defaultCapacity: 128,
    maxSize: 512);

battleScope.Release(new PoolKey("Projectile.Fast"), projectile);
```

作用域设计：

```mermaid
flowchart LR
    Registry[PoolRegistry] --> Global[Global scope\ndestroyOnDispose=false]
    Registry --> Battle[Battle scope\ndestroyOnDispose=true]
    Registry --> UI[UI scope\ndestroyOnDispose=true]

    Battle --> ProjectileFast[(ProjectileEntity + Fast key)]
    Battle --> ProjectileSlow[(ProjectileEntity + Slow key)]
    UI --> FloatingText[(FloatingText + Default key)]
```

同一个类型可以通过不同 `PoolKey` 拆成多个池。适合以下情况：

- 同一类型对象有不同容量和清理策略。
- 同一类型对象用于不同玩法域，不希望互相争抢池容量。
- 需要按配置区分默认容量、最大容量、裁剪策略。

---

## 8. PoolManager 的反向归还

`PoolManager` 有一个关键细节：`ConditionalWeakTable<object, ReleaseHandle>`。

当池被 `RegisterForObjectRelease` 注册后，`PoolManager` 会向池追加一个 `onGet`：

```mermaid
sequenceDiagram
    participant Scope as PoolScope
    participant Manager as PoolManager
    participant Pool as ObjectPool<T>
    participant Table as ConditionalWeakTable
    participant Obj as rented object

    Scope->>Manager: RegisterForObjectRelease(pool)
    Manager->>Pool: AppendOnGet(obj => update release handle)
    Pool->>Obj: Get()
    Pool->>Table: obj -> handle(pool.Release)
    Scope->>Manager: TryRelease(object obj)
    Manager->>Table: lookup obj
    Table-->>Manager: ReleaseHandle
    Manager->>Pool: Release((T)obj)
```

这就是 `Pools.TryRelease(object)` 能工作的基础。事件系统发布后自动释放事件参数时，会调用 `Pools.TryRelease(boxed)`。只要对象是从池里取出的，弱表里就能找到对应池的归还句柄。

这也解释了为什么不要把“自己 new 出来的对象”直接交给 `Pools.TryRelease`：没有归还句柄时它会返回 `false`。

---

## 9. 配置中心

`PoolScope.GetPool` 不只是直接使用传入容量，它会构造 `PoolConfigRequest` 并查询 `PoolConfigCenter`。

```mermaid
flowchart TB
    Start[PoolScope.GetPool<T>] --> Request[PoolConfigRequest\nscopeName + typeof(T) + PoolKey]
    Request --> Center[PoolConfigCenter.GetConfigOrDefault]
    Center --> Providers[遍历 IPoolConfigProvider]
    Providers --> Match{TryGetConfig 命中且 IsSpecified?}
    Match -->|否| Fallback[使用 fallback/default config]
    Match -->|是| Winner[按 priority/registrationOrder 选 winner]
    Winner --> Enabled{config.Enabled?}
    Fallback --> Enabled
    Enabled -->|否| Throw[Pool is disabled by config]
    Enabled -->|是| Options[PoolOptions.FromConfig]
    Options --> Manager[PoolManager.GetOrCreate]
```

仲裁规则：

| 规则 | 说明 |
|------|------|
| 优先级更高者胜出 | `PoolConfigProviderInfo.Priority` 数值越大越优先 |
| 优先级相同时后注册者胜出 | `RegistrationOrder` 更大者覆盖旧 provider |
| 可输出冲突报告 | `TryGetConfigReport` 会返回所有候选和最终 winner |
| 可输出快照 | `TryGetConfigSnapshot` 返回最终配置和来源 provider |

这套配置链让框架可以在不改调用点的情况下，按包、模块、场景或调试工具统一调整池容量。

`PoolConfigModule` 是内置的字典式 provider。`PoolRegistry.RegisterConfigModule` 会创建 `PoolConfigBuilder`，让外部模块用强类型方式声明配置：

```csharp
PoolRegistry.RegisterConfigModule(
    builder => builder
        .Add<DamageEvent>(defaultCapacity: 64, maxSize: 256, prewarmCount: 64)
        .Add<ProjectileEvent>("Battle", "FastProjectile", defaultCapacity: 128, maxSize: 512),
    defaultScopeName: "Global",
    moduleName: "CombatRuntime",
    source: "com.abilitykit.combat",
    priority: 100);
```

配置请求的键是 `PoolConfigRequest(scopeName, typeof(T), key)`。`scopeName` 为空时会归一化为 `Global`，`PoolKey` 为空时会归一化为 `PoolKey.Default`，因此同一个类型在不同 scope 或不同 key 下可以拿到不同配置。

`PoolOptions.FromConfig` 还有一个容易忽略的细节：它会把 `ObjectPoolOptions.DefaultCapacity` 设置为 `Math.Max(config.DefaultCapacity, config.PrewarmCount)`。因为 `ObjectPool<T>` 构造函数会执行 `Prewarm(options.DefaultCapacity)`，所以配置里的 `prewarmCount` 实际通过扩大初始容量影响预热数量。`PoolItemConfig` 中 `prewarmCount < 0` 时会回落到 `defaultCapacity`。

---

## 10. Prewarm、Trim 与 Clear

对象池提供三类容量操作：

| API | 行为 |
|-----|------|
| `Prewarm(int count)` | 预创建对象并放入 inactive stack，直到达到指定数量或 `maxSize` |
| `Trim()` / `Trim(policy)` | 按裁剪策略移除一部分 inactive 对象 |
| `ForceTrim(policy)` | 强制按传入策略裁剪，即使池本身配置为 never trim 也会尝试 |
| `Clear(bool destroy)` | 清空 inactive 对象；`destroy=true` 时执行销毁钩子 |

典型流程：

```mermaid
flowchart TB
    SceneLoad[战斗/场景加载] --> Prewarm[Prewarm 常用对象]
    Running[运行中] --> GetRelease[Get/Release 高频复用]
    Pressure[内存压力或场景切换] --> Trim[TrimAll / ForceTrimAll]
    End[战斗/场景结束] --> Dispose[PoolScope.Dispose]
    Dispose --> Clear{destroyOnDispose?}
    Clear -->|true| Destroy[ClearAll destroy=true]
    Clear -->|false| Drop[ClearAll destroy=false]
```

`PoolRegistry.DestroyScope(name, destroy)` 会移除命名 scope 并调用 `scope.Dispose(destroy)`。全局 scope 不会被移除，销毁全局 scope 时只会清空其中的池。

裁剪策略由 `PoolTrimPolicy.ResolveTargetInactiveCount(defaultCapacity)` 决定：未指定策略时保留 `defaultCapacity`，`KeepNone` 会裁到 0，`KeepAll` 和 `KeepDefaultCapacity` 在当前实现中都会至少保留 `defaultCapacity` 且没有上限。`ObjectPool<T>.Trim()` 会尊重 `NeverTrim`，`ForceTrim(policy)` 则绕过 `NeverTrim` 判断，按传入策略强制裁剪 inactive 对象。

---

## 11. 统计与调试

`ObjectPool<T>.Stats` 包含创建、获取、释放、命中、未命中、溢出销毁、裁剪、清理等计数。编辑器下还可以读取 `PoolDebugSnapshot`。

```mermaid
flowchart LR
    ObjectPool[ObjectPool<T>] --> Stats[PoolStats]
    PoolManager --> Snapshot[PoolDebugSnapshot]
    PoolScope --> ScopeSnapshots[GetDebugSnapshots]
    PoolRegistry --> AllSnapshots[GetDebugSnapshots scopeName]
```

这些数据能回答：

- 池是否频繁 miss，说明默认容量太小或预热不足。
- 是否频繁 overflow destroy，说明最大容量太小或归还节奏异常。
- active 峰值是否明显高于预期，说明对象生命周期可能泄漏。
- trim/clear 是否发生在错误时机，导致后续重新创建。

---

## 12. 与事件系统的关系

事件系统有两处依赖池化：

```mermaid
flowchart TB
    EventDispatcher --> SnapshotPool[Channel<TArgs> snapshot List pool]
    EventDispatcher --> AutoRelease[Publish autoReleaseArgs]
    SnapshotPool --> ObjectPoolList[ObjectPool<List<Listener<TArgs>>>]
    AutoRelease --> PoolsTryRelease[Pools.TryRelease(object)]
    PoolsTryRelease --> PoolManager[ConditionalWeakTable release handle]
```

这意味着：

- 多监听者事件派发时，临时 snapshot 列表不会每次分配新列表。
- 池化事件参数可以在发布后自动归还。
- 监听者如果需要跨帧保存事件内容，必须复制字段，不能长期持有池化事件对象引用。

---

## 13. 新手常见误区

### 13.1 以为 `Pools.Release(obj)` 可以归还任何对象

不一定。对象必须来自对应池，否则 `PoolScope.Release(object)` 会找不到归还句柄并抛异常，`TryRelease` 则返回 `false`。

### 13.2 只清字段，不清引用

池化对象最容易出现旧引用污染。`OnPoolRelease()` 或 `onRelease` 应清掉集合、引用、回调和外部句柄。

### 13.3 所有对象都池化

池化有复杂度成本。低频、长生命周期、状态复杂的对象直接创建更清晰。

### 13.4 忽略作用域

全局池适合跨场景复用的底层对象。战斗、UI、场景对象更适合命名 scope，结束时统一销毁，避免残留。

### 13.5 配了 prewarmCount 却以为它独立于 defaultCapacity

当前配置转换会把 `DefaultCapacity` 提升到 `Max(DefaultCapacity, PrewarmCount)`，而构造池时会按 `DefaultCapacity` 预热。因此 `prewarmCount` 的效果是提高初始预热数量，同时也影响未指定裁剪策略时的保留基线。

---

## 14. 推荐阅读顺序

1. 先读 `Pools.cs`，建立全局门面的 API 视图。
2. 再读 `PoolScope.cs`，理解 scope、config 和 `PoolKey`。
3. 再读 `ObjectPool.cs`，重点看 `Get()`、`Release()`、`Prewarm()`、`Trim()`。
4. 再读 `PoolManager.cs`，理解为什么对象实例可以被 `TryRelease(object)` 反向归还。
5. 最后读 `PoolConfigCenter.cs` 和 `PoolConfigModule.cs`，理解配置 provider 的优先级、注册顺序、强类型配置构建和诊断报告。

---

## 15. 和其他文档的关系

- [事件系统](./01-EventSystem.md)：事件参数自动释放和 snapshot 列表池化都依赖本模块。
- [定时器框架](./03-TimerFramework.md)：调度器当前使用任务对象和内部数组列表，未来若需要高频任务复用，可接入本池化体系。
- [配置系统](./04-ConfigurationSystem.md)：池化配置 provider 是运行时配置思想在 Core 层的一个轻量版本。
- [投射物系统](../08-GameplayModules/04-ProjectileSystem.md)：投射物是最典型的短生命周期高频对象，适合按战斗 scope 管理。

---

*文档版本：v2.1 | 最后更新：2026-07-04*
