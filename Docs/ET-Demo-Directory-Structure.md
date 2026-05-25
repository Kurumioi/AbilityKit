# ET Demo 代码结构规范

## 一、总体架构

```
AbilityKit.Demo.ET.Logic/
├── Model/                          # 数据层（只定义数据，不包含业务逻辑）
│   ├── Config/                      # 配置数据
│   ├── Events/                     # 事件定义
│   ├── Data/                       # 数据结构
│   └── Components/                 # Component 定义
│       ├── Battle/                 # 战斗相关 Component
│       ├── Unit/                   # 单位相关 Component
│       ├── Input/                  # 输入相关 Component
│       ├── Session/                # 会话相关 Component
│       └── MobaCore/               # MobaCore 适配 Component
│
├── Hotfix/                         # 逻辑层（所有业务逻辑）
│   ├── Battle/                     # 战斗逻辑
│   │   ├── Systems/                # Battle System 实现
│   │   ├── Commands/               # 输入命令处理
│   │   └── Features/               # Battle Features
│   │
│   ├── Unit/                       # 单位逻辑
│   │   └── Systems/                # Unit System 实现
│   │
│   ├── Input/                      # 输入逻辑
│   │   └── Systems/                # Input System 实现
│   │
│   ├── Session/                    # 会话逻辑
│   │   └── Systems/                # Session System 实现
│   │
│   ├── Coordinator/                # Coordinator 适配
│   │   └── Systems/                # Coordinator System 实现
│   │
│   ├── Driver/                     # Driver 适配
│   │   └── Systems/                # Driver System 实现
│   │
│   ├── Hooks/                      # Hooks 系统
│   │   └── Systems/                # Hooks System 实现
│   │
│   └── Process/                    # 流程控制
│       └── Systems/                # Process System 实现
│
├── Services/                       # 服务层
│   └── Systems/                    # Service System 实现
│
└── Platform/                       # 平台相关
    └── Console/                    # Console 平台实现
```

## 二、详细目录规范

### 2.1 Model 层目录

| 目录 | 职责 | 命名空间 |
|-----|------|---------|
| `Model/Components/Battle/` | 战斗相关 Component | `ET.Logic.Model.Components.Battle` |
| `Model/Components/Unit/` | 单位相关 Component | `ET.Logic.Model.Components.Unit` |
| `Model/Components/Input/` | 输入相关 Component | `ET.Logic.Model.Components.Input` |
| `Model/Components/Session/` | 会话相关 Component | `ET.Logic.Model.Components.Session` |
| `Model/Components/MobaCore/` | MobaCore 适配 Component | `ET.Logic.Model.Components.MobaCore` |
| `Model/Events/` | 事件定义（强类型事件） | `ET.Logic.Model.Events` |
| `Model/Data/` | 数据结构（record/class） | `ET.Logic.Model.Data` |
| `Model/Config/` | 配置数据 | `ET.Logic.Model.Config` |

### 2.2 Hotfix 层目录

| 目录 | 职责 | 命名空间 |
|-----|------|---------|
| `Hotfix/Battle/Systems/` | 战斗逻辑 System | `ET.Logic.Hotfix.Battle.Systems` |
| `Hotfix/Battle/Commands/` | 输入命令处理 | `ET.Logic.Hotfix.Battle.Commands` |
| `Hotfix/Unit/Systems/` | 单位逻辑 System | `ET.Logic.Hotfix.Unit.Systems` |
| `Hotfix/Input/Systems/` | 输入逻辑 System | `ET.Logic.Hotfix.Input.Systems` |
| `Hotfix/Session/Systems/` | 会话逻辑 System | `ET.Logic.Hotfix.Session.Systems` |
| `Hotfix/Coordinator/Systems/` | Coordinator 适配 | `ET.Logic.Hotfix.Coordinator.Systems` |
| `Hotfix/Driver/Systems/` | Driver 适配 | `ET.Logic.Hotfix.Driver.Systems` |
| `Hotfix/Hooks/Systems/` | Hooks 系统 | `ET.Logic.Hotfix.Hooks.Systems` |
| `Hotfix/Process/Systems/` | 流程控制 | `ET.Logic.Hotfix.Process.Systems` |

## 三、文件命名规范

### 3.1 Component 文件

```
Model/Components/<Category>/<Name>Component.cs
```

| 示例 | 说明 |
|-----|------|
| `ETBattleComponent.cs` | 战斗 Component |
| `ETUnitComponent.cs` | 单位管理器 Component |
| `ETInputComponent.cs` | 输入缓冲 Component |
| `ETBattleSessionComponent.cs` | 会话 Component |

### 3.2 System 文件

```
Hotfix/<Category>/Systems/<Name>ComponentSystem.cs
```

| 示例 | 说明 |
|-----|------|
| `ETBattleComponentSystem.cs` | 战斗 System |
| `ETUnitComponentSystem.cs` | 单位管理器 System |
| `ETInputComponentSystem.cs` | 输入 System |

### 3.3 事件文件

```
Model/Events/<EventName>Event.cs
```

| 示例 | 说明 |
|-----|------|
| `ActorSpawnEvent.cs` | 单位出生事件 |
| `ActorMoveEvent.cs` | 单位移动事件 |
| `ActorDamageEvent.cs` | 单位受伤事件 |

### 3.4 数据文件

```
Model/Data/<Name>.cs
```

| 示例 | 说明 |
|-----|------|
| `MoveCommand.cs` | 移动命令 |
| `SkillCommand.cs` | 技能命令 |

## 四、当前结构问题

### 4.1 目录扁平化问题

**问题**：当前大量文件平铺在 `Hotfix/Battle/` 目录下，没有分类。

**现状**：
```
Hotfix/Battle/
├── ETBattleComponentSystem.cs
├── ETBattleAutoTestComponentSystem.cs
├── ETBattleDriverBridgeSystem.cs
├── ETBattleEntityCacheComponentSystem.cs
├── ETBattleSessionComponentSystem.cs
├── ETBattleSkillTestComponentSystem.cs
├── ETBattleViewComponentSystem.cs
├── ETBattleViewEventSink.cs
├── ETFlowComponentSystem.cs
├── ETInputComponentSystem.cs
├── ETSessionSubFeatureSystems.cs
└── ...
```

### 4.2 重复文件问题

**问题**：同一文件出现在多个路径（Windows/Unix 路径分隔符差异）。

### 4.3 职责边界模糊

**问题**：
- `ETBattleViewEventSink.cs` 放在 `Hotfix/Battle/` 但它属于 Coordinator 适配
- `ETBattleDriverBridge.cs` 和 `ETBattleDriverBridgeSystem.cs` 职责不清

## 五、重构计划

### Phase 1: 目录重组

1. 创建 `Hotfix/Battle/Systems/` 目录
2. 移动 Battle System 文件到 `Systems/` 子目录
3. 创建 `Hotfix/Input/Systems/` 目录
4. 移动 Input System 文件
5. 创建 `Hotfix/Session/Systems/` 目录
6. 移动 Session 相关 System

### Phase 2: 职责分离

1. 将 `ETBattleViewEventSink.cs` 移到 `Hotfix/Coordinator/Systems/`
2. 将 `ETBattleDriverBridge.cs` 合并到 `Hotfix/Driver/Systems/`
3. 清理空的目录

### Phase 3: 命名空间统一

1. 更新所有文件的 `namespace`
2. 更新 `using` 语句
3. 验证编译通过

## 六、命名空间规范

### 6.1 Model 层

```
ET.Logic.Model.Components.<Category>  // Component 命名空间
ET.Logic.Model.Events                 // 事件命名空间
ET.Logic.Model.Data                  // 数据命名空间
ET.Logic.Model.Config                // 配置命名空间
```

### 6.2 Hotfix 层

```
ET.Logic.Hotfix.<Category>.Systems   // System 命名空间
```

### 6.3 示例

```csharp
// Model/Components/Battle/ETBattleComponent.cs
namespace ET.Logic.Model.Components.Battle
{
    [ComponentOf(typeof(Scene))]
    public class ETBattleComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // ...
    }
}

// Hotfix/Battle/Systems/ETBattleComponentSystem.cs
namespace ET.Logic.Hotfix.Battle.Systems
{
    [EntitySystemOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETBattleComponentSystem
    {
        // ...
    }
}
```

## 七、禁止事项

| 禁止 | 说明 | 正确做法 |
|-----|------|---------|
| ❌ 在 Model 层写业务逻辑 | Component 只存储数据 | 业务逻辑放在 Hotfix 层 |
| ❌ 跨目录依赖 | 依赖关系必须是单向的 | 按层级依赖 |
| ❌ 空目录 | 留下目录但不包含文件 | 删除空目录或添加文件 |
| ❌ 混合命名空间 | 文件不在对应命名空间的目录 | 按命名空间放置文件 |
