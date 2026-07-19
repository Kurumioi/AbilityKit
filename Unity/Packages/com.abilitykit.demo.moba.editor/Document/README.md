# AbilityKit Demo MOBA Editor 工具指南

> 适用包：`com.abilitykit.demo.moba.editor`
>
> Unity 版本：2022.3 或更高的 2022.3 LTS 版本
>
> 最后更新：2026-07-19

本包提供 MOBA 示例的配置生产、场景预览、战斗诊断、帧同步检查、Scene Gizmo 和热重载等 Unity Editor 工具。本文是工具使用与维护文档的统一入口。

## 快速开始

### 打开战斗调试窗口

1. 在 Unity 中打开 MOBA 示例工程。
2. 打开可启动 `BattleLogicSession` 的 MOBA 战斗场景。
3. 进入 Play Mode，并等待战斗会话启动。
4. 选择菜单 `Tools/AbilityKit/Battle/战斗调试`。
5. 在左侧实体列表选择 Actor，在右侧切换诊断面板。

窗口只在 Play Mode 下工作。未进入 Play Mode、`BattleDebugFacadeProvider.Current` 为空或没有活动 `BattleLogicSession` 时，窗口会显示对应提示。

### 常用操作

- 在顶部“过滤”输入框按实体标识或工具支持的文本条件筛选实体。
- 在左侧输入 Actor ID 后点击“跳转”，定位对应实体。
- 点击“刷新”立即重建实体列表；窗口默认每 0.25 秒自动刷新。
- 选择实体后，在“总览”“属性”“标签”“效果”“Buff”等面板查看只读快照。
- 在“诊断事件”中按选中 Actor、失败结果和文本条件过滤事件。
- 在“诊断状态”中查看当前 World 与 Actor 状态快照。
- 面板提供复制按钮时，复制内容来自诊断 DTO 在采样时固化的数据。

## 当前面板

| 面板 | 主要用途 | 数据边界 |
| --- | --- | --- |
| 总览 | Actor 类型、名称、Tag 和 Effect 数量 | Actor、Tag、Effect 只读诊断查询 |
| 属性 | 属性最终值及 Modifier | Actor Attributes 只读诊断查询 |
| 标签 | Actor 当前 Tag | Actor Tags 只读诊断查询 |
| 效果 | Ability Effect 实例和计时状态 | Actor Effects 只读诊断查询 |
| Buff | MOBA Buff 实例和上下文 | Actor Buffs 只读诊断查询 |
| 诊断事件 | 战斗事件过滤与结果检查 | Event Ring Store 查询 |
| 诊断状态 | World 与 Actor 最新状态 | State Store 查询 |
| 帧同步/* | 帧、预测、回滚、对账和网络状态 | 既有帧同步调试接口；按运行环境动态显示 |

诊断面板只消费 `IBattleDiagnosticReadOnlySession` 返回的不可变 DTO，不应回退读取活动 Runtime 对象。当前主窗口的实体枚举、选择解析，以及左侧列表中的 Tag/Effect 简要计数仍依赖活动 `IBattleDebugFacade` 和 `IUnitFacade`；这部分不属于已完成的面板 DTO 化范围。

## 使用边界

- 当前可用入口是本地 Unity Play Mode。
- 当前状态类 Actor Store 使用 latest-only 语义：帧 `0` 表示最新快照；指定帧只有等于当前快照帧时才可查询。
- 事件 Store 有界保留历史，并以 revision 保证同一次分页读取的一致性。
- 远端会话、离线文件、导出、完整 Timeline 和完整 Skill Runtime 检查尚未形成可用工作流。
- 本工具是只读诊断客户端，不应修改生命、属性、Buff、Effect、技能运行时或 Trace。
- 工具不替代 Unity Profiler、Memory Profiler、正式 Record/Replay 或网络抓包工具。

## 其他 Editor 工具入口

| 工具 | 菜单入口 |
| --- | --- |
| 打开 Demo 场景 | `Tools/AbilityKit/MOBA Demo/Open Demo Scene` |
| 创建或刷新 Demo 场景 | `Tools/AbilityKit/MOBA Demo/Create Or Refresh Demo Scene` |
| 播放 Demo 场景 | `Tools/AbilityKit/MOBA Demo/Play Demo Scene` |
| Editor Flow Pump | `Tools/AbilityKit/Preview/编辑器驱动(Flow Pump)` |
| 帧同步测试 | `Tools/AbilityKit/FrameSync Test` |
| 编译 Hotfix | `Tools/AbilityKit/Hot Reload/Compile Hotfix` |
| 重载 Hotfix | `Tools/AbilityKit/Hot Reload/Reload Hotfix` |
| 创建英雄 | `AbilityKit/Moba/Hero/Create New Hero…` |

配置 JSON 同步和 VFX 导出也位于 `AbilityKit` 菜单下。执行导入、导出前应确认 Project 窗口选中的资产或文件夹符合菜单命令要求。

## 文档导航

- [当前能力与限制](CURRENT-CAPABILITIES.md)：查询、能力位、面板、Producer 及未实现范围的当前快照。
- [故障排查](TROUBLESHOOTING.md)：Session、Capability、查询状态、刷新和 Unity 工程问题。
- [扩展开发指南](EXTENDING.md)：新增 DTO、Store、Session Query、ViewModel 和 Panel 的标准步骤。
- [测试与验证](TESTING.md)：构建、EditMode 测试、元数据和提交前检查。
- [架构设计](Moba战斗诊断与溯源工具设计.md)：长期目标、架构约束、数据模型与设计决策。
- [实施历史](IMPLEMENTATION-HISTORY.md)：按批次记录已完成的诊断正式化工作和当时验证结果。
- [Editor 包模块设计](DemoMobaEditor示例编辑器工具模块开发设计文档.md)：整个 Editor 包的职责和依赖边界。

文档中“当前能力”以 [当前能力与限制](CURRENT-CAPABILITIES.md) 为准；架构设计和实施历史中的阶段性描述不替代当前能力快照。
