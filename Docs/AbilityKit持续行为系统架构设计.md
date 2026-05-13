# AbilityKit 持续行为系统架构设计

## 1. 设计目标

### 1.1 核心问题
游戏中的"持续行为"（Continuous）是一个广泛存在的概念：
- Buff/DEBUFF
- 引导技能
- AI 行为（巡逻、追击）
- 移动/冲刺
- DOT（持续伤害）

这些对象都有共同特征：
- 有生命周期（激活、暂停、恢复、结束）
- 可能被中断
- 有时长限制

### 1.2 设计原则

| 原则 | 说明 |
|------|------|
| **最小化核心** | Core 包只定义抽象，不包含具体业务逻辑 |
| **接口组合优于继承** | 扩展点通过可选接口实现 |
| **职责分离** | 生命周期管理 vs 业务执行 分离 |
| **业务层定制** | 具体逻辑由业务层实现 |

---

## 2. 核心接口设计

### 2.1 IContinuous - 持续体外壳

```
Runtime/Core/Continuous/IContinuous.cs
```

统一所有具有"持续时间、可被中断"的对象的外壳。

```csharp
public interface IContinuous
{
    IContinuousConfig Config { get; }
    ContinuousState State { get; }
    bool IsActive { get; }
    bool IsTerminated { get; }
    bool IsPaused { get; }
    float ElapsedSeconds { get; }

    void Activate();
    void Pause();
    void Resume();
    void Abort(string reason);

    event Action<IContinuous, ContinuousEndReason> OnEnded;
}
```

### 2.2 IContinuousConfig - 配置接口

```
Runtime/Core/Continuous/IContinuousConfig.cs
```

采用**接口组合模式**，核心接口最小化，扩展点通过可选接口实现。

#### 核心接口（必须实现）

```csharp
public interface IContinuousConfig
{
    string Id { get; }           // 唯一标识
    long OwnerId { get; }        // 所属实体ID
    bool CanBeInterrupted { get; } // 是否可被中断
}
```

#### 扩展接口（按需实现）

| 接口 | 说明 | 使用场景 |
|------|------|----------|
| `ITagConfig` | 标签匹配、暂停/阻止规则 | BUFF 标签系统 |
| `IMutexConfig` | 互斥组管理 | 同一类型 BUFF 互斥 |
| `IDurationConfig` | 定时过期 | 有时长的 BUFF/技能 |
| `IHierarchyConfig` | 嵌套层级 | 父子 BUFF 级联 |
| `IStackConfig` | 堆叠层数 | 可叠加 BUFF |

#### 扩展接口示例

```csharp
// 互斥配置扩展
public interface IMutexConfig
{
    string MutexGroup { get; }   // 互斥组名称
    int Priority { get; }        // 优先级
}

// 时长配置扩展
public interface IDurationConfig
{
    float? DurationSeconds { get; }  // null 表示无限期
}

// 标签配置扩展
public interface ITagConfig
{
    ITagContainer Tags { get; }
    ITagContainer PauseByTags { get; }
    ITagContainer BlockByTags { get; }
}
```

### 2.3 IContinuousManager - 管理器接口

```
Runtime/Core/Continuous/IContinuousManager.cs
```

管理器接口由**业务层实现**，core 包不提供默认实现。

```csharp
public interface IContinuousManager
{
    bool Register(IContinuous continuous);
    void Unregister(IContinuous continuous, ContinuousEndReason reason);
    bool TryActivate(IContinuous continuous);

    IReadOnlyList<IContinuous> GetOwnerContinuous(long ownerId);
    IReadOnlyList<IContinuous> GetOwnerActiveContinuous(long ownerId);

    void InterruptAll(long ownerId, string reason);
    void PauseAll(long ownerId);
    void ResumeAll(long ownerId);

    int ActiveCount { get; }
    int TotalCount { get; }
}
```

---

## 3. 状态机设计

### 3.1 ContinuousState

```
Runtime/Core/Continuous/ContinuousState.cs
```

```
  ┌─────────────┐
  │ Inactive    │ ← 创建后初始状态
  └──────┬──────┘
         │ Activate()
         ▼
  ┌─────────────┐
  │ Active      │ ← 正常运行
  └──────┬──────┘
         │
    ┌────┴────┬─────────────┐
    │         │             │
    │ Pause() │ Expire()    │ Abort()
    │         │             │
    ▼         ▼             ▼
┌────────┐ ┌────────┐ ┌────────┐
│Paused  │ │ Expired │ │Aborted │
└────────┘ └────────┘ └────────┘
    │                        │
    │ Resume()               │
    ▼                        ▼
┌────────┐              ┌────────┐
│ Active │              │Aborted │ (终态)
└────────┘              └────────┘
```

### 3.2 ContinuousEndReason

```
Runtime/Core/Continuous/ContinuousEndReason.cs
```

| 枚举值 | 说明 |
|--------|------|
| `Completed` | 正常完成（到期） |
| `Interrupted` | 被中断（Abort） |
| `Replaced` | 被替换（互斥） |
| `OwnerDead` | 所属实体死亡 |
| `CleanedUp` | 被清理 |

---

## 4. 包定位差异

### 4.1 Behavior 包 vs Triggering 包

虽然两个包都实现了 `IContinuous`，但定位不同：

| 维度 | Behavior 包 | Triggering 包 |
|------|------------|---------------|
| **核心问题** | "我应该做什么？" | "我应该如何执行？" |
| **设计模式** | Decision + Executor | TriggerPlan + Executor |
| **典型场景** | 巡逻、追击、逃跑、闪避 | 技能释放、BUFF叠加、DOT |
| **配置方式** | 代码定义 BehaviorTree | 数据配置（JSON） |
| **决策频率** | 每帧/每 N 帧决策 | 事件触发 |
| **执行模型** | Decision → Executor（主动轮询） | TriggerPlan 解析（事件驱动） |

### 4.2 为什么保持独立？

1. **问题域不同**：AI 决策 vs 技能执行
2. **开发团队可能不同**：AI 开发者 vs 技能策划
3. **可独立演进**：各自优化不影响对方
4. **灵活性**：不同项目可能只需要其中一个

### 4.3 统一外壳 IContinuous

`IContinuous` 作为**统一外壳**，让两种不同领域的系统可以被同一个 `IContinuousManager` 管理：

```
IContinuousManager（业务层实现）
├── 管理 BehaviorRuntime（AI 行为）
│   └── 内部：Decision.Decide() → Executor.Execute()
│
└── 管理 ProcessUnit（技能/BUFF）
    └── 内部：TriggerPlan 解析 → ContinuousExecutor 执行
```

---

## 5. 业务层接入指南

### 5.1 步骤一：实现 IContinuousManager

业务层根据游戏需求实现管理器：

```csharp
public class MobaContinuousManager : IContinuousManager
{
    private readonly Dictionary<long, List<IContinuous>> _ownerContinuous = new();

    public bool Register(IContinuous continuous)
    {
        // 实现注册逻辑
        // 可以包含互斥检查、标签检查等
    }

    public void Unregister(IContinuous continuous, ContinuousEndReason reason)
    {
        // 实现注销逻辑
    }

    // ... 其他方法
}
```

### 5.2 步骤二：定义业务配置

```csharp
public class BuffConfig : IContinuousConfig,
    ITagConfig, IMutexConfig, IDurationConfig
{
    public string Id { get; set; }
    public long OwnerId { get; set; }
    public bool CanBeInterrupted { get; set; }

    // ITagConfig
    public HashSet<string> Tags { get; set; }
    public HashSet<string> PauseByTags { get; set; }
    public HashSet<string> BlockByTags { get; set; }

    // IMutexConfig
    public string MutexGroup { get; set; }
    public int Priority { get; set; }

    // IDurationConfig
    public float? DurationSeconds { get; set; }
}
```

### 5.3 步骤三：使用

```csharp
var manager = new MobaContinuousManager();
var config = new BuffConfig
{
    Id = "speed_boost_001",
    OwnerId = playerId,
    MutexGroup = "movement_buff",
    Priority = 1,
    DurationSeconds = 5f,
    Tags = new HashSet<string> { "buff", "speed" }
};

var behavior = new MyBehavior(config);
if (manager.TryActivate(behavior))
{
    // 成功激活
}
```

---

## 6. 文件结构

```
Unity/Packages/com.abilitykit.core/
└── Runtime/Core/Continuous/
    ├── ContinuousState.cs        # 状态枚举
    ├── ContinuousEndReason.cs   # 结束原因枚举
    ├── IContinuous.cs           # 持续体接口（核心）
    ├── IContinuousConfig.cs     # 配置接口 + 扩展接口
    └── IContinuousManager.cs    # 管理器接口

Unity/Packages/com.abilitykit.behavior/
└── Runtime/Runtime/
    └── BehaviorRuntime.cs       # 实现 IContinuous

Unity/Packages/com.abilitykit.triggering/
└── Runtime/Continuous/
    ├── ProcessUnit.cs           # 继承 IContinuous
    └── ContinuousExecutorAdapter.cs  # 适配器
```

---

## 7. 设计决策记录

| 日期 | 决策 | 原因 |
|------|------|------|
| 2026-05-13 | 配置采用接口组合模式 | 避免 core 包写死业务逻辑 |
| 2026-05-13 | ContinuousManager 定义为接口 | 业务层需要自定义互斥、标签等逻辑 |
| 2026-05-13 | Behavior 和 Triggering 保持独立 | 定位不同，可独立演进 |
