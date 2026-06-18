# com.abilitykit.triggering 整改清单（商业化项目化）

## 目标
- 将当前“主线可用 + 兼容层较多”的状态，收敛为“主线稳定、边界清晰、可交付、可维护”的商业化框架包。
- 优先处理会影响接入成本、稳定性、版本迁移和调试效率的问题。

## P0：必须先做的收口项（已完成第一轮）

### 1. 收敛正式主线 API（已落地）
- 已明确 `TriggerPlan`、`PlannedTrigger`、`RuleScheduler`、`ActionRegistry`、`TriggerPlanJsonDatabase` 为正式主线。
- `Runtime/Legacy`、`Runtime/Scheduler`、`Runtime/Executable`、`Runtime/Experimental` 的公开入口已通过目录 README、过时标记和正式边界文档降权。
- 根目录 `.cs` 兼容占位文件已清理完成，`Runtime/Compatibility` 机器清单当前为空并用于防止占位入口回流。
- legacy API 的统一迁移指引和弃用策略已沉淀到 `Document/LegacyMigrationPolicy.md`。

### 2. 统一调度体系职责（已落地第一轮）
- 已明确 `SimpleScheduleManager`、`GroupedScheduleManager`、`RuleScheduler`、旧 `Scheduler` 的边界。
- 已停止新增逻辑进入旧 `Scheduler` / legacy executable 路径；旧入口仅保留兼容、迁移或显式失败语义。
- 包内文档、样例、测试已指向主线调度方案；调度 Samples 已迁到 `RuleSchedulerRegistry`。
- “计划调度 / 规则调度 / legacy 调度”的术语已在正式边界、设计文档和 README 中同步。

### 3. 建立正式 API 边界规范（已落地）
- 已固化 `Document/FormalApiBoundary.md` 中的正式与兼容目录约束。
- 已通过 `Document/LegacyMigrationPolicy.md` 补齐 legacy/compatibility/experimental 的替代 API、删除条件和延后决策。
- 新增代码只能进入正式层；兼容层只允许转发、迁移辅助、显式失败或兼容说明。

### 4. 建立商业化验收基线
- 增加确定性执行基线：同输入同输出、同序列化数据同执行结果。
- 增加序列化兼容基线：旧版本计划可正常读取，关键字段不丢失。
- 增加无效配置校验基线：动作参数、调度语义、引用关系、UGC 限制必须在运行前拦截。
- 增加性能基线：注册、解析、执行、验证的时间和分配量都要有目标阈值。

## P1：显著提升项目可用性的改造

### 5. 完善编辑器工具链
- 增加计划/配置的可视化检查入口。
- 提供错误定位、预览、导出、校验结果聚合展示。
- 为 `TriggerCodegenMenu` 增加生成结果说明和失败提示。
- 让生成的 Id、Schema、映射表具备可追踪性。

### 6. 增强测试矩阵
- 补充跨模块回归测试：计划、校验、执行、调度、同步、表达式、数值引用。
- 增加边界测试：空配置、重复注册、非法引用、循环依赖、重复计划、异常恢复。
- 增加兼容测试：legacy 数据和新数据的读取行为一致性。
- 增加确定性测试：随机数、时间、执行顺序、回放一致性。

### 7. 强化文档产品化（已完成主线入口与迁移边界，剩余产品化材料延后）
- `Documentation~/README.md` 已整理快速接入、正式 API、Legacy / Experimental 边界、验证与测试建议。
- `Document/FormalApiBoundary.md` 已提供“什么时候用 `TriggerPlan`、什么时候用 `RuleScheduler`、什么时候不要碰 legacy”的主线决策表。
- `Document/LegacyMigrationPolicy.md` 已补充推荐接入/迁移顺序、反模式和 legacy 删除条件。
- 性能、确定性约束和 FAQ 可在后续产品化文档批次补齐。

### 8. 收敛命名与目录结构
- 统一同类概念命名，减少 `Schedule` / `Scheduler` / `RuleScheduler` / `ActionScheduler` 的认知冲突。
- 让目录更能体现主线、兼容、实验、示例四个层级。
- 去除“看起来像正式 API、实际是兼容入口”的模糊区域。

## P2：商业化成熟度提升项

### 9. 建立版本迁移策略（已完成策略页，发布说明延后）
- `Document/LegacyMigrationPolicy.md` 已制定 deprecation policy、入口分级、迁移优先级和删除条件。
- 主要 legacy 入口已标记迁移目标、保留原因、替代方案和剩余动作。
- 对外发布版本时仍需补充面向版本号的升级说明与风险说明。

### 10. 增加可观测性与诊断
- 对验证失败、legacy 命中、执行耗时、调度命中率做统计。
- 提供运行时 trace 和 debug record 导出能力。
- 为接入方提供最少一套可定位问题的诊断链路。

### 11. 提供样板工程与接入模板
- 增加最小商业接入样板。
- 提供战斗、技能、状态、持续效果等典型业务示例。
- 将 sample 从“演示代码”升级为“可复制模板”。

### 12. 规划下线 legacy 入口（已完成包内第一轮，外部调用方延后）
- 包内样例和主线文档已完成旧调度入口清理；剩余 legacy 路径多为兼容实现自身、旧数据迁移或显式失败入口。
- 根目录 `.cs` 占位文件已完成删除；旧 Scheduler、旧 Dispatcher、旧 Executable 的删除条件继续记录到 `Document/LegacyMigrationPolicy.md`。
- 外部调用方迁移完成前不删除仍有实际类型或旧数据价值的 legacy 实现，后续在 major 兼容清理批次分阶段减少维护面。

## 推荐执行顺序
- P0 的主线收口、调度边界、正式 API 边界和 legacy 迁移策略已完成第一轮。
- 下一阶段优先补 P1 的编辑器工具、性能/确定性验收基线和更完整测试矩阵。
- P2 继续推进可观测性、发布升级说明、样板工程和 major legacy 下线批次。

## 预期结果
- 接入方只需要识别少量稳定入口即可完成集成。
- 框架的“能用”升级为“可长期维护、可版本演进、可商业交付”。
- 旧路径不再继续膨胀，新能力统一进入正式主线。