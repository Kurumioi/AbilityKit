# Flow 池化优化方案

## 目标

- 接入 `com.abilitykit.core` 的 `AbilityKit.Core.Pooling`，减少 Flow 运行时反复创建短生命周期对象导致的 GC。
- 将池初始化、预热、裁剪、清理时机显式化，便于按全局、场景、战斗、UI、一次 Flow 会话等生命周期管理。
- 池配置必须支持注入：业务可注入一份 profile/settings，按具体对象类型、集合类型、数组长度档位覆盖容量、上限、预热、裁剪与销毁策略。
- 避免每个类内部各自硬编码池参数，统一由可注入的 Flow 池配置提供者生成完整 `ObjectPoolOptions<T>`。
- 提供更少参数、更语义化的 option 构造扩展，降低使用方直接组装 `ObjectPoolOptions<T>` 的重复代码。
- 尽量让所有可安全 reset 的对象都能走对象池，包括普通对象、字典、队列、列表、数组；对不可安全 reset 的对象保留显式禁用与调试检查。

## 现状梳理

### Core Pool 能力

`com.abilitykit.core` 已提供：

- `ObjectPool<T>`：支持 `DefaultCapacity` 预热、`MaxSize` 上限、`CollectionCheck`、`TrimPolicy`、统计信息、`IPoolable` 生命周期钩子。
- `PoolScope`：按生命周期拥有一组池，可 `TrimAll`、`Clear`、`Dispose`。
- `Pools` / `PoolRegistry`：全局 scope 与命名 scope 管理。
- `PoolKey`：同类型多池区分。
- `IPoolable`：`OnPoolGet`、`OnPoolRelease`、`OnPoolDestroy`。

现有不足：

- `PoolScope.GetPool` / `Pools.GetPool` 主要暴露长参数列表，调用点容易分散硬编码。
- `ObjectPoolOptions<T>` 只有基础字段，没有更语义化的工厂/扩展组合。
- 如果 Flow 直接在类内创建 pool options，会重复且不利于统一调参。

### Flow 可池化对象热点

优先级从高到低：

1. `FlowContext` 内部 scope 字典与 scope handle：`BeginScope()` 每次创建 `Dictionary<Type, object>` 和 `ScopeHandle`。
2. `FlowSession` / `FlowRunner` / `FlowContext`：会话频繁创建时产生对象链。
3. `FlowEventQueue<TEvent>`：HFSM 或事件驱动 Flow 场景下可复用内部 `Queue<TEvent>`。
4. 组合节点运行时数组：`ParallelAllNode`、`RaceNode` 构造时创建 `FlowStatus[]`，`IReadOnlyList` 构造时复制 `IFlowNode[]`。
5. Staged root 构建临时 `List<IFlowNode>`：`StagedFlowRootProvider`、`OrderedStagedFlowRootProvider` 每次构建根节点创建临时列表。
6. 各类节点本体：是否池化取决于节点是否安全 reset，包含委托/资源/订阅句柄的节点风险较高，不建议第一阶段大面积池化。

## 推荐架构

### 1. Core 层增强：Pool option 构造扩展

新增建议：

- `ObjectPoolOptionsExtensions` 或 `PoolOptions` 静态类。
- 目标是用少量参数返回完整 `ObjectPoolOptions<T>`，同时统一默认值。

建议 API：

```csharp
public static class PoolOptions
{
    public static ObjectPoolOptions<T> For<T>(
        Func<T> create,
        int defaultCapacity = 0,
        int maxSize = 1024,
        bool collectionCheck = true,
        PoolTrimPolicy trimPolicy = default) where T : class;

    public static ObjectPoolOptions<T> Poolable<T>(
        Func<T> create,
        int defaultCapacity = 0,
        int maxSize = 1024,
        bool collectionCheck = true,
        PoolTrimPolicy trimPolicy = default) where T : class, IPoolable;

    public static ObjectPoolOptions<T> WithLifecycle<T>(
        this ObjectPoolOptions<T> options,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        Action<T> onDestroy = null) where T : class;

    public static ObjectPoolOptions<T> WithCapacity<T>(
        this ObjectPoolOptions<T> options,
        int defaultCapacity,
        int maxSize) where T : class;

    public static ObjectPoolOptions<T> WithTrim<T>(
        this ObjectPoolOptions<T> options,
        PoolTrimPolicy trimPolicy) where T : class;
}
```

进一步建议为 `PoolScope` / `Pools` 增加 option 入口，避免长参数传递：

```csharp
public ObjectPool<T> GetPool<T>(PoolKey key, ObjectPoolOptions<T> options) where T : class;
public static ObjectPool<T> GetPool<T>(PoolKey key, ObjectPoolOptions<T> options) where T : class;
```

这样 Flow 侧可以只处理配置与 options，不需要调用长参数接口。

### 2. Flow 层新增可注入配置中心

新增建议文件：

- `IFlowPoolingProfile` / `IFlowPoolingSettingsProvider`
- `FlowPoolingSettings`
- `FlowPoolItemSettings`
- `FlowPoolIds` / `FlowPoolKeys`
- `FlowPoolOptionsFactory`
- `FlowPools` 或 `FlowPooling`
- `FlowCollectionPools` / `FlowArrayPools`

职责划分：

- `IFlowPoolingProfile`：外部注入入口，按类型、pool key、集合类型、数组元素类型与长度档位解析配置。
- `FlowPoolingSettings`：默认 profile 实现，保存 Flow 模块内置池和业务覆盖项。
- `FlowPoolItemSettings`：单个池的容量、上限、预热、裁剪、永不回收等策略。
- `FlowPoolOptionsFactory`：根据 profile 生成完整 `ObjectPoolOptions<T>`。
- `FlowPools`：持有/解析 `PoolScope`，集中初始化、预热、Trim、Clear。
- `FlowCollectionPools`：统一提供 `Dictionary<TKey,TValue>`、`List<T>`、`Queue<T>`、`Stack<T>` 等集合池。
- `FlowArrayPools`：统一提供数组池，按元素类型与长度 bucket 管理，避免 `new T[n]` 高频分配。
- `FlowSession` / `FlowHost`：可接收 profile / pools / pool scope，不在类内部硬编码 pool 参数。

建议配置结构：

```csharp
public interface IFlowPoolingProfile
{
    bool Enabled { get; }
    string ScopeName { get; }
    bool DestroyScopeOnDispose { get; }

    FlowPoolItemSettings GetObject(Type type, PoolKey key = default);
    FlowPoolItemSettings GetCollection(Type collectionType, Type keyType, Type valueType, PoolKey key = default);
    FlowArrayPoolItemSettings GetArray(Type elementType, int minimumLength, PoolKey key = default);
}

public sealed class FlowPoolingSettings : IFlowPoolingProfile
{
    public bool Enabled = true;
    public string ScopeName = "AbilityKit.Flow";
    public bool DestroyScopeOnDispose = false;

    public FlowPoolItemSettings DefaultObject = FlowPoolItemSettings.Default(8, 64);
    public FlowPoolItemSettings DefaultCollection = FlowPoolItemSettings.Default(16, 256);
    public FlowArrayPoolItemSettings DefaultArray = FlowArrayPoolItemSettings.Default(16, 256, clearOnRelease: true);

    public Dictionary<Type, FlowPoolItemSettings> ObjectOverrides = new Dictionary<Type, FlowPoolItemSettings>();
    public Dictionary<string, FlowPoolItemSettings> KeyOverrides = new Dictionary<string, FlowPoolItemSettings>();
    public List<FlowArrayBucketSettings> ArrayBuckets = new List<FlowArrayBucketSettings>();
}

public readonly struct FlowPoolItemSettings
{
    public readonly bool Enabled;
    public readonly int DefaultCapacity;
    public readonly int MaxSize;
    public readonly int PrewarmCount;
    public readonly bool CollectionCheck;
    public readonly bool NeverTrim;
    public readonly bool DestroyInactiveOnClear;
    public readonly PoolTrimPolicy TrimPolicy;
}

public readonly struct FlowArrayPoolItemSettings
{
    public readonly bool Enabled;
    public readonly int DefaultCapacity;
    public readonly int MaxSize;
    public readonly int PrewarmCount;
    public readonly int BucketLength;
    public readonly bool ClearOnGet;
    public readonly bool ClearOnRelease;
    public readonly bool NeverTrim;
    public readonly PoolTrimPolicy TrimPolicy;
}
```

配置注入建议：

```csharp
public sealed class FlowPools
{
    public FlowPools(IFlowPoolingProfile profile, PoolScope scope = null);
}

public sealed class FlowSession : IDisposable
{
    public FlowSession();
    public FlowSession(FlowPools pools);
}
```

默认构造保持兼容；工业化接入时由业务启动器注入 profile，例如战斗服/客户端战斗模块传入 `BattleFlowPoolingSettings`。

### 3. 类型级、集合级、数组级配置策略

#### 类型级对象池

- 所有可 reset 的对象都按 `Type + PoolKey` 查配置。
- 默认配置兜底，业务可以覆盖 `FlowContext`、`FlowRunner`、`FlowSession`、具体节点类型、业务自定义 context item 等。
- `PoolKey` 用于同类型不同用途，例如普通 `List<IFlowNode>` 与 staged root 临时 `List<IFlowNode>` 可有不同容量。

#### 集合池

建议提供统一集合池 facade：

```csharp
Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(PoolKey key = default);
void ReleaseDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, PoolKey key = default);

List<T> GetList<T>(PoolKey key = default);
void ReleaseList<T>(List<T> list, PoolKey key = default);

Queue<T> GetQueue<T>(PoolKey key = default);
void ReleaseQueue<T>(Queue<T> queue, PoolKey key = default);
```

要求：

- `OnRelease` 默认 `Clear()`，避免引用泄露。
- 支持 `initialCapacity`，例如 scope dictionary、node list、event queue 可配置初始容量。
- 支持按泛型参数配置，例如 `Dictionary<Type, object>`、`List<IFlowNode>`、`Queue<TEvent>`。

#### 数组池

数组池建议按 bucket 管理，而不是每个精确长度一个池：

- 请求长度 `n`，profile 解析到 `BucketLength`，例如 4、8、16、32、64、128。
- 返回数组长度可能大于等于请求长度，调用方必须记录实际有效长度。
- `FlowStatus[]`、`IFlowNode[]`、临时排序数组都可以走数组池。
- 引用类型数组默认 `ClearOnRelease = true`；值类型可配置不清理以降低开销。

#### 永不回收 / 常驻池策略

需要支持两层含义：

1. `NeverTrim`：池内 inactive 对象不参与普通 `TrimAll()`，仅在 scope destroy 或强制 clear 时释放。
2. `NeverDestroyOnOverflow` 不建议默认支持：超过 `MaxSize` 仍无限保存会导致内存不可控。若业务确实需要常驻，可通过 `MaxSize = int.MaxValue` 或大上限表达。

建议补充 Core 能力：

```csharp
public readonly struct PoolRetentionPolicy
{
    public readonly bool NeverTrim;
    public readonly bool DestroyOnScopeDispose;
    public readonly bool DestroyInactiveOnClear;
}
```

短期可以不新增类型，先在 Flow profile 层把 `NeverTrim` 映射为 `PoolTrimPolicy(0, int.MaxValue)`，并在 `FlowPools.TrimAll()` 中跳过标记为 never trim 的 pool。

### 4. Flow 生命周期接入点

#### 全局/模块级

- 在 Flow 模块初始化时创建命名 `PoolScope`，例如 `AbilityKit.Flow`。
- 预热通用池：`FlowContext`、`FlowRunner`、scope dictionary、scope handle、常见事件队列等。
- 在模块卸载或编辑器退出时 `Clear/Dispose`。

#### 场景/战斗级

- 推荐业务层传入战斗 scope：`Pools.GetOrCreateScope("BattleFlow")`。
- 战斗开始预热，战斗结束 `TrimAll` 或 `Dispose`。
- Flow 默认可使用全局 scope，重度业务可覆盖为 battle scope。

#### Flow 会话级

- `FlowSession.Dispose()` / `Stop()` 只释放本次运行持有对象，不销毁整个 scope。
- 对 `FlowRunner`、`FlowContext`、`FlowEventQueue` 等明确 reset 后归还。
- 注意事件委托、异常 handler、回调订阅必须清空。

## 分阶段推进方案

### 阶段 0：补齐 Core Pool API

目标：先解决“池配置不灵活、不统一”的基础问题。

改动：

1. 新增 `PoolOptions` / `ObjectPoolOptionsExtensions`。
2. `PoolScope` 与 `Pools` 增加 `GetPool(PoolKey, ObjectPoolOptions<T>)` 入口。
3. 保持现有长参数接口兼容，内部改为使用 options 入口。
4. 增加少量单测/示例，确保 option 默认值一致。

收益：后续 Flow 与其他模块都能统一使用 option factory。

### 阶段 1：Flow 可注入配置中心与 scope 管理

目标：不改变 Flow 行为，先建立统一入口。

改动：

1. 新增 `IFlowPoolingProfile`、`FlowPoolingSettings`、`FlowPoolItemSettings`、`FlowArrayPoolItemSettings`。
2. 新增 `FlowPoolOptionsFactory`，从注入 profile 生成 options。
3. 新增 `FlowPools`，负责拿 scope、建池、预热、trim、clear。
4. 新增 `FlowCollectionPools` 与 `FlowArrayPools`，集中处理字典、列表、队列、栈、数组。
5. `FlowSession` 增加可选构造参数：profile / scope / pools。
6. 默认行为保持向后兼容：不传参数仍可创建并运行。

### 阶段 2：低风险对象池化

优先池化不改变外部语义、reset 清晰的对象：

1. `FlowContext`
   - 实现 `IPoolable` 或内部 `ResetForPool()`。
   - `OnPoolRelease` 调用 `Clear()`。
2. `FlowContext` scope dictionary
   - `BeginScope()` 从池拿 `Dictionary<Type, object>`。
   - `EndScope()` 清空并归还字典。
3. `ScopeHandle`
   - 从池拿 handle，Dispose 后清空 `_ctx` 并归还。
4. `FlowEventQueue<TEvent>`
   - `OnPoolRelease` 清空队列。

收益：直接减少高频 scope 与事件队列分配。

### 阶段 3：Runner / Session 池化

目标：让会话级对象可复用。

改动：

1. `FlowRunner` 增加 `Reset(...)` 或内部 `Initialize(FlowContext ctx)`。
2. `_wakeUp` 当前绑定构造期 `Wake` 委托，可改为可 reset 的 `FlowWakeUp` 或保留 runner 生命周期内复用。
3. `FlowSession` 增加静态/工厂创建方式：
   - `FlowSession.Create()`
   - `FlowPools.GetSession()`
   - `FlowSession.Dispose()` 归还到池
4. 归还前清理：事件、handler、root、status、callbacks、scope、wake flags。

风险：外部可能持有已 Dispose 的 session/context；需要文档约束或 debug 检查。

### 阶段 4：临时集合与数组池化全面接入

目标：减少构造 root 或组合节点时的临时集合/数组分配。

建议：

1. `StagedFlowRootProvider` 使用池化 `List<IFlowNode>`，构建完成后归还。
2. `FlowContext` scope 栈、scope map、临时查找表全部走集合池。
3. `FlowEventQueue<TEvent>` 内部队列支持由 `FlowCollectionPools` 注入或替换为池化队列 wrapper。
4. 对 `ParallelAllNode` / `RaceNode` 的 `FlowStatus[]` 增加可选数组池。
5. 对 `SequenceNode(IReadOnlyList<IFlowNode>)`、`ParallelAllNode(IReadOnlyList<IFlowNode>)`、`RaceNode(IReadOnlyList<IFlowNode>)` 的 `IFlowNode[]` 增加数组池路径。
6. profile 支持配置具体数组对象数量，例如 `FlowStatus[8]` bucket 预热 32 个、上限 256 个，`IFlowNode[16]` bucket 预热 8 个、上限 64 个。

注意：数组池化需要严格确保节点释放后归还，当前节点没有统一 Dispose/Release 契约。临时数组可先接入；随节点生命周期持有的数组建议等节点池化契约确定后推进。

### 阶段 5：节点本体池化（可选/后置）

目标：对大量动态生成节点的业务场景提供可选节点池。

要求：

1. 新增节点 reset 契约，例如 `IReusableFlowNode`：
   - `ResetForReuse(...)`
   - `ReleaseToPool()` 或由 factory 归还。
2. 不强制所有 `IFlowNode` 都可池化。
3. 组合节点负责释放子节点时必须明确所有权。
4. 对包含委托、订阅、资源句柄的节点必须谨慎，防止闭包引用泄露和二次回调。

## 首批推进范围

建议首批只做：

1. Core option 扩展与 option 入口。
2. Flow 可注入 profile / settings / factory / pools。
3. 类型级对象池配置解析：按 `Type + PoolKey` 获取容量、上限、预热、trim、never trim。
4. 集合池 facade：先覆盖 `Dictionary<Type, object>`、`Stack<Dictionary<Type, object>>`、`List<IFlowNode>`、`Queue<TEvent>`。
5. 数组池 facade：先覆盖 `FlowStatus[]`、`IFlowNode[]` 的 bucket 配置，不强制改所有节点。
6. `FlowContext` scope dictionary 与 scope handle 池化。
7. `FlowEventQueue<TEvent>` 池化。
8. `FlowSession` 提供可选使用 pool 的构造/工厂，不立即强制替换所有调用点。

暂缓：

- 节点本体全面池化。
- 需要节点 release 契约才能安全归还的长生命周期数组。
- 对现有所有业务 Flow 构建代码的大规模替换。

## 风险与约束

- 必须保证归还前清空事件、委托、上下文 map、scope、队列、数组引用、root 引用。
- `IPoolable.OnPoolRelease()` 不能抛异常；否则 pool release 路径会污染上层停止/异常处理。
- `FlowContext.BeginScope()` 的嵌套顺序必须保持 LIFO；池化字典不能在 scope 未结束时提前归还。
- 引用类型集合/数组默认 release 时清空；值类型数组是否清空交给 profile 配置。
- `NeverTrim` 只能表示普通 trim 不回收，不能绕过 scope destroy，避免编辑器/domain reload 或战斗卸载后泄漏。
- 不建议默认在节点层面使用全局池，否则容易出现跨战斗/跨场景引用泄露。
- `CollectionCheck` 编辑器开启，线上可通过 settings 关闭。
- Pool scope 生命周期建议由业务模块显式控制：全局默认兜底，战斗/场景独立 scope 更适合重度场景。

## 验收标准

- 默认不传池配置时，现有 Flow API 行为不变。
- 可注入 profile 能覆盖具体类型对象、具体集合、具体数组 bucket 的预热数量、上限、trim 与 never trim 策略。
- 打开池化后，FlowContext scope 进入/退出不会再持续分配 `Dictionary<Type, object>` 和 `ScopeHandle`。
- 打开集合/数组池后，临时 `List<IFlowNode>`、`Queue<TEvent>`、`FlowStatus[]`、`IFlowNode[]` 可按 profile 命中对象池。
- Flow 停止、完成、异常中断后，context、scope、queue、runner、集合、数组内部引用均清空。
- 可通过 `PoolScope.GetDebugSnapshots()` 在编辑器查看 Flow 池命中率、active/inactive、overflow、never trim pool 等数据。
- 模块/战斗结束时可显式 `TrimAll` 或 `Clear`，初始化时可显式 `Prewarm`。
