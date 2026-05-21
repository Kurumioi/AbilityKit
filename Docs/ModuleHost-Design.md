# ModuleHost 架构设计文档

## 一、概述

`ModuleHost` 是 Console Demo 中的核心组件管理框架，提供：
- 组件生命周期管理（Attach/Detach/Tick/Rebind）
- 依赖自动排序
- 嵌套层级支持

```
FeatureHost (Feature 层级)
    └── Feature (实现 IGamePhaseFeature)
            └── ModuleHost (Module 层级)
                    ├── Module A
                    ├── Module B
                    │       └── 子 ModuleHost (嵌套层级)
                    │               ├── Child Module 1
                    │               └── Child Module 2
                    └── Module C
```

## 二、核心接口

### 2.1 模块标识接口

```csharp
/// <summary>
/// 模块唯一标识
/// </summary>
public interface IModuleId
{
    string Id { get; }
}

/// <summary>
/// 模块依赖声明
/// </summary>
public interface IModuleDependencies
{
    string[]? Dependencies { get; }
}
```

### 2.2 生命周期接口

```csharp
/// <summary>
/// 模块基础生命周期
/// </summary>
public interface IGameModule<TContext> where TContext : class
{
    void OnAttach(TContext context);
    void OnDetach(TContext context);
}

/// <summary>
/// 支持 Tick 的模块
/// </summary>
public interface IGameModuleTick<TContext> : IGameModule<TContext>
    where TContext : class
{
    void Tick(TContext context, float deltaTime);
}

/// <summary>
/// 支持 Rebind 的模块
/// </summary>
public interface IGameModuleRebind<TContext> : IGameModule<TContext>
    where TContext : class
{
    void Rebind(TContext context);
}
```

### 2.3 ModuleHost

```csharp
/// <summary>
/// 模块容器，管理一组模块的生命周期
/// </summary>
public sealed class ModuleHost<TContext, TModule> : IDisposable
    where TContext : class
    where TModule : class
{
    // 注册模块
    ModuleHost<TContext, TModule> Add(TModule module);
    ModuleHost<TContext, TModule> AddRange(IEnumerable<TModule> modules);
    
    // 生命周期
    void Attach(in TContext context);
    void Detach(in TContext context);
    void Tick(in TContext context, float deltaTime);
    void RebindAll(in TContext context);
    
    // 查询
    TModule? Get(string moduleId);
    bool Has(string moduleId);
    int Count { get; }
    bool IsAttached { get; }
}
```

## 三、使用模式

### 3.1 基本使用

```csharp
public sealed class MyFeature : IGamePhaseFeature, IMyHost
{
    private readonly ModuleHost<IMyHost, IMyModule> _moduleHost = new();

    public void OnAttach(Context ctx)
    {
        // 注册模块（顺序不重要，ModuleHost 自动排序）
        _moduleHost.Add(new ModuleA());
        _moduleHost.Add(new ModuleB());
        
        // 附加所有模块
        _moduleHost.Attach(this);
    }

    public void Tick(Context ctx, float dt)
    {
        // Tick 所有模块
        _moduleHost.Tick(this, dt);
    }

    public void OnDetach(Context ctx)
    {
        // 分离所有模块（自动反向顺序）
        _moduleHost.Detach(this);
    }
}
```

### 3.2 依赖声明

```csharp
public sealed class ModuleB : IMyModule
{
    public string Id => "module_b";
    
    // 声明依赖 ModuleA
    public string[]? Dependencies => new[] { "module_a" };
}
```

ModuleHost 会自动：
1. 检测依赖关系
2. 按依赖顺序 Attach（先 A 后 B）
3. 按反向顺序 Detach（先 B 后 A）

### 3.3 嵌套 ModuleHost

```csharp
/// <summary>
/// 组合模块 - 包含子模块
/// </summary>
public sealed class CompositeModule : IMyModule
{
    private readonly ModuleHost<IMyHost, IMyModule> _childHost = new();
    private IMyHost? _host;

    public string Id => "composite";
    public string[]? Dependencies => null;

    public void OnAttach(IMyHost host)
    {
        _host = host;
        
        // 添加子模块
        _childHost.Add(new ChildModuleA());
        _childHost.Add(new ChildModuleB());
        
        // 附加子模块
        _childHost.Attach(host);
    }

    public void OnDetach(IMyHost host)
    {
        _childHost.Detach(host);
        _host = null;
    }

    public void Tick(IMyHost host, float dt)
    {
        // Tick 子模块
        _childHost.Tick(_host!, dt);
    }

    public void Rebind(IMyHost host)
    {
        _childHost.RebindAll(_host!);
    }
}
```

## 四、生命周期顺序

```
Attach 顺序（依赖排序后）:
    Module A (无依赖)
    Module B (依赖 A)
    Module C (依赖 A, B)
    ...
    Composite (包含子模块)

Tick 顺序:
    按注册顺序（或自定义顺序）

Detach 顺序（反向）:
    Composite 的子模块（反向）
    Module C
    Module B
    Module A
```

## 五、Host 接口设计

Module 通过 Host 接口获取能力，而不是直接引用其他 Module：

```csharp
/// <summary>
/// Host 接口 - 暴露能力给 Module
/// </summary>
public interface IMyHost
{
    Context Context { get; }
    ServiceA ServiceA { get; }
    ServiceB ServiceB { get; }
    void RegisterThing(IMyModule module);
}

/// <summary>
/// Module 实现
/// </summary>
public sealed class MyModule : IMyModule
{
    public void OnAttach(IMyHost host)
    {
        // 通过 Host 获取能力
        var ctx = host.Context;
        var svc = host.ServiceA;
        
        // 注册自己
        host.RegisterThing(this);
    }
}
```

## 六、错误处理

ModuleHost 内置错误处理：

```csharp
public void Attach(in TContext context)
{
    foreach (var module in _sortedModules)
    {
        try
        {
            if (module is IGameModule<TContext> gameModule)
            {
                gameModule.OnAttach(context);
            }
        }
        catch (Exception ex)
        {
            // 记录错误但继续处理其他模块
            Platform.Log.Error($"[ModuleHost] Attach failed for '{moduleId}': {ex.Message}");
        }
    }
}
```

## 七、与 FeatureHost 的关系

```
┌─────────────────────────────────────────────────────────────┐
│ FeatureHost                                               │
│ 管理 Feature                                               │
│                                                           │
│ ┌─────────────────────────────────────────────────────┐  │
│ │ Feature (IGamePhaseFeature)                          │  │
│ │                                                     │  │
│ │ ┌───────────────────────────────────────────────┐  │  │
│ │ │ ModuleHost                                    │  │  │
│ │ │ 管理 Module                                    │  │  │
│ │ │                                               │  │  │
│ │ │ ┌─────────────────────────────────────────┐  │  │  │
│ │ │ │ Module (实现 IConsoleViewModule 等)      │  │  │  │
│ │ │ │                                           │  │  │  │
│ │ │ │ ┌─────────────────────────────────────┐  │  │  │  │
│ │ │ │ │ 嵌套 ModuleHost (可选)               │  │  │  │  │
│ │ │ │ └─────────────────────────────────────┘  │  │  │  │
│ │ │ └─────────────────────────────────────────┘  │  │  │
│ │ └───────────────────────────────────────────────┘  │  │
│ └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 八、设计原则

| 原则 | 说明 |
|-----|------|
| **单向依赖** | Module 只依赖 Host，不直接依赖其他 Module |
| **Host 作为中间人** | Module 间通信通过 Host 接口 |
| **显式依赖声明** | 通过 Dependencies 属性声明 |
| **嵌套解耦** | 复杂功能拆分为嵌套的 ModuleHost |
| **错误隔离** | 单个 Module 错误不影响其他 Module |

## 九、适用场景

| 场景 | 推荐使用 |
|-----|---------|
| 功能模块化 | ✅ 清晰拆分 |
| 生命周期管理 | ✅ 自动 Attach/Detach |
| 依赖排序 | ✅ 自动处理 |
| 嵌套组合 | ✅ 支持多层级 |
| 简单场景 | ⚠️ 考虑直接实例化 |

## 十、命名规范

```
接口: I<功能>Module 或 I<功能>Feature
实现: <功能>Module 或 <功能>Feature
Host: I<功能>ModulesHost
```

示例：
- `IConsoleViewModule` / `ConsoleBindingModule`
- `IConsoleViewFeatureModulesHost` / `ConsoleViewFeature`
