# 技能模块命名与职责重构规划

## 目标

当前 `SkillExecutor` 及相关类名已经能支撑基本实现，但随着技能主链路、输入分发、预备校验、策略解析、运行时聚合逐步正式化，命名与职责边界开始不完全一致。这里需要做一次系统性规划：先稳定语义，再逐步重命名与拆分，避免后续继续把新职责堆到旧入口上。

## 当前判断

### 1. `SkillExecutor` 已经不是纯执行器

它目前实际承担的工作包括：

- 输入相位分发
- 技能槽位解析
- 技能预备与启动
- 运行时创建
- 管线启动
- 部分失败语义归一化

这更接近“技能施法编排入口”或“技能施法外观层”，而不是单一执行器。

### 2. `Cast` 子域是合理的，但内部职责仍可再分

当前 `Cast` 下已经形成一组比较清晰的概念：

- `SkillCastContext`：一次施法的上下文
- `SkillCastPreparationService`：施法预备
- `SkillCastPolicyResolver`：施法策略
- `SkillCastPreparationResult`：预备结果
- `SkillFailureCodes`：失败码契约

这说明方向是对的，但 `SkillExecutor` 仍然把调度和执行揉在一起。

### 3. `Pipeline` 与 `Runtime` 命名相对稳定

- `SkillPipelineRunner`：职责清楚，保留
- `SkillPipelineContext`：职责清楚，保留
- `MobaSkillCastRuntimeService`：职责清楚，保留

这两块不建议为了“统一风格”而强改。

## 建议命名映射

| 当前名称 | 建议方向 | 说明 |
|---|---|---|
| `SkillExecutor` | `SkillCastOrchestrator` / `SkillExecutionFacade` | 更符合“入口编排”职责 |
| `SkillCastPreparationService` | 保留 | 当前语义清晰 |
| `SkillCastPolicyResolver` | 保留 | 当前语义清晰 |
| `SkillCastContext` | 保留 | 领域语义明确 |
| `SkillCastPreparationResult` | 保留 | 结果对象合理 |
| `MobaSkillLoadoutService` | 视职责评估改名为 `SkillSlotLoadoutService` | 如果只负责槽位与技能映射，可改得更具体 |
| `SkillPipelineRunner` | 保留 | 不建议改 |
| `MobaSkillCastRuntimeService` | 保留 | 不建议改 |

## 职责拆分建议

### 第一层：入口编排层

建议让 `SkillExecutor` 逐步退化为纯入口协调器，只做：

- 接收输入
- 校验请求
- 选择 Cast / Cancel / Update 路径
- 调用预备服务与 pipeline
- 返回结构化结果

如果后续继续增长，可以将其演进为：

- `SkillCastOrchestrator`
- 或 `SkillExecutionFacade`

### 第二层：施法预备层

建议把准备、策略、校验继续固化在 `Cast` 子域中：

- 目标是否合法
- 技能是否存在
- 施法策略如何解析
- 预备阶段是否可以启动 runtime

如果职责继续扩大，再拆成：

- `SkillCastValidator`
- `SkillCastPlanBuilder`
- `SkillCastPolicyResolver`

### 第三层：运行时层

保留 runtime 聚合语义，不要因为命名重构去破坏：

- retain/release
- child lifetime
- pipeline ended / runtime ended 分离
- blackboard 访问

## 推荐落地顺序

### P0：先稳定语义

- 收敛失败码
- 补齐启动校验契约
- 让 `SkillExecutor` 的行为边界稳定下来

### P1：再做轻量改名

- `SkillExecutor` → `SkillCastOrchestrator` 或 `SkillExecutionFacade`
- 视情况改 `MobaSkillLoadoutService`

### P2：最后做职责拆分

- 把预备校验进一步拆成 `Validator` / `PlanBuilder`
- 把入口类削薄到只保留协调逻辑

## 结论

有必要系统性规划，而且应当优先做“职责收口 + 命名校准”而不是一次性大改名。当前最合适的策略是：

1. 先把行为和失败语义固定住。
2. 再重命名最外层入口类。
3. 最后按职责继续拆分内部服务。
