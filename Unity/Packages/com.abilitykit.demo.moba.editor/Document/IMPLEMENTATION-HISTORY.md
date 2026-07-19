# MOBA 战斗诊断实施历史

> 本文记录已经完成的实施批次及当时的验证边界。
>
> 当前能力以 [CURRENT-CAPABILITIES.md](CURRENT-CAPABILITIES.md) 为准。历史记录中的“当前”“尚未”等措辞只描述对应批次结束时的状态。
>
> 最后整理：2026-07-19

## 阅读规则

- 本文用于追踪设计如何落地，不作为当前 API 或能力清单。
- 每批只记录目标、关键结果和验证边界。
- 历史测试数量不代表当前分支持续通过。
- 没有实际执行 Unity Test Runner 的批次，不以项目编译代替 NUnit 结果。
- 更详细的第一至第二十五批原始记录暂保留在架构设计第 39 节内嵌归档中，后续可在不丢失审计信息的前提下逐步精简。

## 第一至四批：Core 地基与本地 Session

### 第一批：交互与查询语义

建立平台无关 Diagnostics Core，定义 Session/World/Epoch 身份、稳定选择、帧游标、保留范围、过滤、分页、查询状态和 Workspace 控制器。Core 不引用 Unity、Editor 或活动战斗对象。

### 第二批：DTO 与 Event Ring Store

建立 World、Actor、Event、Trace 基础不可变 DTO，以及统一只读 Session/Query 契约。实现固定容量 Event Ring Store、严格 Sequence、有界淘汰、Freeze、Clear、组合过滤和 revision 一致分页。

### 第三批：Runtime Collector 与首批 Producer

将 Diagnostics Core 提升到 Runtime 包，建立 `MobaBattleDiagnosticEventCollector` 和 Draft 提交边界，接入技能生命周期与最终伤害。Producer 不感知 Ring Store，采集失败与战斗主流程隔离。

### 第四批：State Store、Sampler 与 Local Session

建立 World/Actor 状态快照 Store、Runtime 状态采样器和 `MobaBattleDiagnosticLocalSession`，统一路由 State 与 Event 查询。状态 Store 与事件历史形成互补数据面。

## 第五至十二批：Producer 覆盖

### 第五批：自动状态采样与 Buff

增加 Late 阶段自动状态采样系统，并接入 Buff 添加和移除事件。

### 第六批：Projectile 生成

在弹丸成功生成和链接后提交 `ProjectileSpawned`，保留来源、目标、配置、Trace 与 Skill Runtime 关联。

### 第七批：Projectile 结束

在弹丸链接移除前捕获来源上下文，提交 `ProjectileEnded`。

### 第八批：Heal 与直接伤害

补齐不经过完整伤害管线的直接 Damage 和 Heal 事件。

### 第九批：Summon 生命周期

接入 Summon 生成与销毁事件，保留稳定 Actor、Config 和上下文关联。

### 第十批：TraceNode 生命周期

从统一 Trace Registry 网关采集节点创建和结束，避免逐业务服务重复插桩。

### 第十一批：Area 生命周期

接入 Area 生成和结束；将纯映射器与 Entitas 系统类分离，保持聚焦测试边界。

### 第十二批：Effect 执行生命周期

接入 Effect 开始、成功结束和异常 Dispose 路径结束事件。

## 第十三至十六批：首批 Editor 与补充 Producer

### 第十三批：首批诊断面板

在既有 Panel Registry 上新增“诊断事件”和“诊断状态”，统一通过只读 Session 查询，不从面板直接访问 Store。该批没有迁移既有 Actor 详情面板。

### 第十四批：Projectile Hit 与 ViewModel 分层

接入 `ProjectileHit`，并把诊断事件、诊断状态的查询和缓存逻辑从 Panel 抽到不依赖 UnityEditor 的 ViewModel。

### 第十五批：Warning 与 Exception

接入结构化 Warning/Exception Producer，复用诊断服务既有限流，避免异常路径重复采集。

### 第十六批：首批同步事件

只采集真实收到的权威状态哈希快照。没有从空对账报告推断 Snapshot Gap、Rollback 或 Full Snapshot 事件。

## 第十七至二十批：契约正式化

### 第十七批：独立 Revision 与原子状态快照

拆分 Event 和 State revision，完善 ViewModel 缓存键。State Store 使用原子 World/Actor 提交并正式定义 latest-only 查询语义。Local Session capability 收敛到真实公开查询表面。

### 第十八批：强类型 Event Payload

建立版本化纯值类型 `BattleDiagnosticEventPayload` 判别联合，首个 schema 覆盖同步状态哈希，并保留 Summary 文本兼容。

### 第十九批：Collector 窄端口

将职责混杂接口拆分为 Producer Sink、只读 Store、状态写入和采集控制端口，通过共享适配器保证同一 World Scope 内各端口指向同一 Collector。

### 第二十批：Trace 图只读查询

从真实 Trace Registry 导出节点图，增加独立 Trace revision、明确结束状态和 Local Session 动态 Trace capability，不从事件 Summary 重建因果图。

## 第二十一至二十五批：Actor 详情 DTO 化

### 第二十一批：Actor Attributes

增加 Attribute 与 Modifier DTO、latest-only Store、Runtime 采样、动态 capability 和只读属性面板。面板不再读取活动 Attribute 实例。

### 第二十二批：Actor Buffs

增加 Buff DTO、独立 Store、Runtime 投影、Capture 生命周期和独立 Buff 面板。规范化非有限和负时间。

### 第二十三批：Actor Tags

增加 Tag DTO 和独立 Store，采样时固化名称；既有标签面板迁移到只读 Session。

### 第二十四批：Actor Effects

增加 Effect DTO 和独立 Store，显式表达计时字段适用性；既有效果面板迁移到只读 Session。

### 第二十五批：Overview 聚合

Overview 组合 Actor、Tag 和 Effect 查询，移除面板对活动 Unit、Tag Container 和 Effect Container 的直接读取。联合缓存键包含 Session Scope、三类 revision、ActorId 和 Frame。

验证边界：Diagnostics Tests 和 Editor 生成项目完成范围化构建且为 0 errors；Unity Editor 当时正在运行，因此未启动第二实例、未结束用户进程、未运行 EditMode Test Runner，也未宣称 NUnit 通过。

## 后续记录格式

新增批次使用以下模板：

```text
## 第 N 批：主题

目标：
关键结果：
未改变的边界：
测试：
构建：
Unity 手工验收：
已知限制：
当前能力文档更新：
```

每批结束时同步更新 [CURRENT-CAPABILITIES.md](CURRENT-CAPABILITIES.md)，不要在历史记录中建立新的“当前事实”段落。
