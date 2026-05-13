---
name: abilitykit-development
description: AbilityKit 游戏技能框架开发技能。用于开发、调试 MOBA 技能系统，包括 Console Demo 运行、帧同步调试、日志分析、配置文件编辑。触发场景：用户提到 AbilityKit、Console Demo、MOBA、帧同步、Debug、日志分析。
---

# AbilityKit 开发指南

## 项目结构

```
AbilityKit/
├── Unity/Packages/          # Unity Package 源码（唯一源码位置）
│   ├── com.abilitykit.core/
│   ├── com.abilitykit.triggering/
│   ├── com.abilitykit.ability/
│   └── ...
├── src/                     # .NET SDK 项目（编译/测试用）
│   ├── AbilityKit.Demo.Moba.Console/   # Console Demo（主要开发入口）
│   ├── AbilityKit.Demo.Moba.Core/
│   └── ...
└── Docs/                    # 设计文档
```

## 源码位置规则

| 操作 | 去哪里 |
|-----|-------|
| **阅读/修改代码** | `Unity/Packages/` |
| **运行 dotnet build** | `src/` |
| **Unity 开发/运行** | `Unity/Packages/` |
| **编写新代码** | `Unity/Packages/` |

**禁止**：在 `src/` 目录创建与 `Unity/Packages/` 重复的源码文件

## Console Demo 调试

### 运行 Console Demo

```powershell
cd src/AbilityKit.Demo.Moba.Console
dotnet run
```

### 自动测试

程序启动后自动运行 7 项测试：
1. **Initialization** - 初始化流程
2. **Phase Transition** - 阶段切换
3. **Frame Sync** - 帧同步
4. **Skill Cast** - 技能释放
5. **Damage Calculation** - 伤害计算
6. **Cooldown System** - 冷却系统
7. **Move System** - 移动系统

### 日志级别

```csharp
Log.SetMinLevel(Log.LogLevel.System);  // 默认：只显示系统日志
Log.SetMinLevel(Log.LogLevel.Debug);    // 测试期间：显示调试日志
Log.EnableTrace();                      // 追踪模式：显示所有日志
```

### 常用调试命令

| 任务 | 命令 |
|-----|------|
| 编译 | `dotnet build` |
| 运行 | `dotnet run` |
| 查看日志 | `Select-String -Path "output.txt" -Pattern "ERROR"` |

## 配置文件

| 配置 | 路径 |
|-----|------|
| 角色配置 | `src/AbilityKit.Demo.Moba.Console/Configs/moba/characters.json` |
| 技能配置 | `src/AbilityKit.Demo.Moba.Console/Configs/moba/skills.json` |
| 属性模板 | `src/AbilityKit.Demo.Moba.Console/Configs/moba/attribute_templates.json` |
| Buff 配置 | `src/AbilityKit.Demo.Moba.Console/Configs/moba/buffs.json` |
| 弹道配置 | `src/AbilityKit.Demo.Moba.Console/Configs/moba/projectiles.json` |

### 技能 ID 命名规则

角色配置中的 `SkillIds` 需要与技能配置中的 `Id` 匹配：

```json
// characters.json
"SkillIds": [10010101, 10010201, 10010301]

// skills.json - 必须包含这些 Id
{"Id": 10010101, "Name": "廉颇-技能1", "CooldownMs": 5000}
{"Id": 10010201, "Name": "廉颇-技能2", "CooldownMs": 8000}
{"Id": 10010301, "Name": "廉颇-技能3", "CooldownMs": 12000}
```

## 关键模块

| 模块 | 路径 | 说明 |
|-----|------|------|
| 日志系统 | `Platform/Log.cs` | 支持分级日志输出 |
| 技能执行器 | `Services/SkillExecutor.cs` | 处理技能施放和冷却 |
| 战斗服务 | `Services/BattleServices.cs` | 伤害计算、角色管理 |
| 输入处理 | `Battle/ConsoleInputFeature.cs` | 技能输入触发 |
| 自动测试 | `AutoTest/AutoTestRunner.cs` | 流程完整性验证 |

## 帧同步调试

帧同步状态通过日志 `[SYNC]` 频道输出：

```
[SYNC] [Sync] Frame: 539, State: InMatch, Actors: 7
```

检查要点：
1. Frame 持续增长（约 30 FPS）
2. State 保持 `InMatch`
3. ActorCount 正确

## 故障排查

| 问题 | 排查方法 |
|-----|---------|
| 技能配置未加载 | 检查 `skills.json` 中技能 ID 是否与 `characters.json` 匹配 |
| 冷却不生效 | 确认 `CooldownMs > 0` 且配置正确加载 |
| 输入无响应 | 检查 `Context.State` 是否为 `InMatch` |
| 帧不同步 | 检查 `ConsoleSyncFeature` 是否正确 Tick |

## 参考文档

- 详细架构设计见 [Docs/通用技能系统架构设计.md](Docs/通用技能系统架构设计.md)
- Console 视图层设计见 [Docs/AbilityKit.Moba.Console视图层架构设计.md](Docs/AbilityKit.Moba.Console视图层架构设计.md)
