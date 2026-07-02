# AbilityKit Context

ECS 风格的实体-属性管理框架，让非 ECS 系统也能享受类似 ECS 的开发体验。

## 核心概念对齐

| ECS 概念 | 本框架对应 | 说明 |
|---------|-----------|------|
| **World** | **ContextRegistry** | 世界，管理所有实体 |
| **Entity** | **Entity** | 实体，唯一 ID |
| **Component** | **Property** | 属性，实体上的数据 |
| **ComponentType** | **PropertyType** | 属性类型，自动注册 |
| **Query** | **Query** | 查询，筛选实体 |

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                    ContextRegistry                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  EntityData                                          │   │
│  │  ┌─────────────────────────────────────────────┐   │   │
│  │  │ Id: long                                     │   │   │
│  │  │ CreatedAtMs: long                            │   │   │
│  │  │ Properties: Dictionary<TypeId, IProperty>  │   │   │
│  │  └─────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  支持: Create() / Destroy() / Add() / Get() / Set()        │
│  支持: Query 查询 / 事件通知                                 │
└─────────────────────────────────────────────────────────────┘
```

## 使用方式

### 1. 定义属性类型

```csharp
// 定义属性（对齐 ECS 的 Component）
public sealed class HealthProperty : IProperty
{
    public int TypeId => 0; // 由 PropertyTypeRegistry 自动分配
    public int Current { get; set; }
    public int Max { get; set; }
}

public sealed class PositionProperty : IProperty
{
    public int TypeId => 0;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
```

### 2. 创建实体并添加属性

```csharp
var registry = new ContextRegistry();

// 方式一：使用构建器
var entityId = registry.Create()
    .With(new HealthProperty { Current = 100, Max = 100 })
    .With(new PositionProperty { X = 0, Y = 0, Z = 0 })
    .Build();

// 方式二：直接操作
var entityId2 = registry.Create().Build();
registry.Add(entityId2, new HealthProperty { Current = 50, Max = 50 });
```

### 3. 查询实体

```csharp
// 查询拥有特定属性的所有实体
var healthEntities = registry.GetEntitiesWith<HealthProperty>();

// 使用 Query 进行复杂查询
var query = new Query()
    .With<HealthProperty>()
    .With<PositionProperty>();

var result = query.Execute(registry);
```

### 4. 事件通知

```csharp
registry.Subscribe(evt =>
{
    switch (evt.Type)
    {
        case ContextEventType.Created:
            Console.WriteLine($"Entity {evt.EntityId} created");
            break;
        case ContextEventType.Updated:
            Console.WriteLine($"Entity {evt.EntityId} updated: {evt.ChangedKey}");
            break;
        case ContextEventType.Destroyed:
            Console.WriteLine($"Entity {evt.EntityId} destroyed");
            break;
    }
});
```

### 5. 快照存储

```csharp
// 快照用于持久化实体状态
public struct EntitySnapshot : IContextSnapshot, IDestroyableSnapshot
{
    public long EntityId { get; set; }
    public long CreatedAtMs { get; set; }
    public int Health { get; set; }
    public bool IsDestroyed { get; private set; }
    public void MarkDestroyed() => IsDestroyed = true;
}

var storage = new SnapshotStorage();

// 保存快照
storage.Save(new EntitySnapshot { EntityId = 1, Health = 100 });

// 查询快照
var snapshot = storage.Get(1);
```

### 6. 统一读取实时值与快照值

```csharp
public sealed class HealthProperty : IProperty, IContextValueProvider
{
    public int TypeId => PropertyTypeRegistry.Instance.Register<HealthProperty>().Id;
    public int Current { get; set; }
    public int Max { get; set; }

    public bool TryGetValue<T>(string key, out T value)
    {
        object raw = key == "Current" ? Current : key == "Max" ? Max : null;
        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}

var registry = new ContextRegistry();
var storage = new SnapshotStorage();
var resolver = new ContextValueResolver(registry, storage);

var contextId = registry.Create()
    .With(new HealthProperty { Current = 80, Max = 100 })
    .Build();

var current = resolver.GetValue<int, HealthProperty>(
    contextId,
    "Current",
    defaultValue: 0,
    mode: ContextValueReadMode.RealtimeThenSnapshot);

if (current.Found && current.IsRealtime)
{
    Console.WriteLine(current.Value);
}
```

`ContextValueResolver` 是上下文触发模块的统一读入口：调用方只需要传入上下文 ID、属性类型和可选键名，即可按 `ContextValueReadMode` 决定优先读取实时注册中心还是快照存储。属性或快照实现 `IContextValueProvider` 后可暴露多个命名字段；快照也可以继续实现 `ISnapshotAccessor` 兼容旧的按键读取方式。

## API 概览

### ContextRegistry

| 方法 | 说明 |
|------|------|
| `Create()` | 创建实体，返回 EntityBuilder |
| `Destroy(id)` | 销毁实体 |
| `Add<T>(id, property)` | 添加属性 |
| `Get<T>(id)` | 获取属性 |
| `Set<T>(id, property)` | 设置属性（覆盖） |
| `Remove<T>(id)` | 移除属性 |
| `Has<T>(id)` | 检查是否拥有属性 |
| `GetEntitiesWith<T>()` | 查询拥有该属性的所有实体 |
| `Subscribe(handler)` | 订阅全局事件 |
| `Exists(id)` | 检查实体是否存在 |

### PropertyTypeRegistry

| 方法 | 说明 |
|------|------|
| `Register<T>()` | 注册属性类型 |
| `Get<T>()` | 获取属性类型 |
| `Instance` | 单例实例 |

### Query

| 方法 | 说明 |
|------|------|
| `With<T>()` | 添加查询条件 |
| `Execute(registry)` | 执行查询 |

### ContextValueResolver

| 方法 | 说明 |
|------|------|
| `GetProperty<TProperty>(contextId, mode)` | 按上下文 ID 读取实时属性或快照属性 |
| `GetValue<T, TProperty>(contextId, key, defaultValue, mode)` | 按属性类型和键名读取具体值 |
| `TryGetValue<T, TProperty>(contextId, key, out value, mode)` | 只在实时值或快照值命中时返回 true |
| `TryGetRealtimeProperty<TProperty>(contextId, out property)` | 只读取注册中心内的实时属性 |
| `TryGetSnapshot(contextId, out snapshot)` | 只读取快照存储内的快照 |

### IContextValueProvider

| 方法 | 说明 |
|------|------|
| `TryGetValue<T>(key, out value)` | 由属性或快照实现，向统一解析器暴露命名值 |

## 模块结构

```
com.abilitykit.context/
├── Property/
│   ├── IProperty.cs        # 属性接口
│   └── PropertyType.cs     # 属性类型注册表
├── Query/
│   └── Query.cs            # 查询器
├── Registry/
│   └── ContextRegistry.cs  # 核心注册中心 + EntityBuilder
├── Snapshot/
│   ├── IContextSnapshot.cs # 快照接口
│   ├── ISnapshotAccessor.cs
│   └── SnapshotStorage.cs # 快照存储
├── Value/
│   ├── ContextValueResolver.cs
│   ├── ContextValueTypes.cs
│   └── IContextValueProvider.cs
└── Events/
    ├── ContextEventType.cs
    ├── ContextEvent.cs
    └── ContextEventHandler.cs
```
