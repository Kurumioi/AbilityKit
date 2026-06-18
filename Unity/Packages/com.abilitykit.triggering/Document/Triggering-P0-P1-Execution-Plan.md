# Triggering P0-P1 落地执行清单

## 范围
- 适用包：`com.abilitykit.triggering`
- 目标：优先完成主线收口、调度统一、工程化验收、编辑器与测试增强

## P0 落地项

### 1. 主线入口冻结（已落地）
- 保持 `TriggerPlan`、`PlannedTrigger`、`ExecCtx`、`ActionRegistry`、`FunctionRegistry`、`TriggerRunner`、`RuleScheduler`、`Validation` 为正式入口。
- 新增功能不得再扩散到 `Runtime/Legacy`、`Runtime/Scheduler`、`Runtime/Experimental`。
- `Runtime` 根目录 `.cs` 兼容占位文件已清理完成，后续不再作为新代码落点或旧路径保留区。

### 2. 调度体系收口（已落地第一轮）
- 新动作延迟/周期执行统一走 `ActionScheduler`。
- 规则级持续执行统一走 `RuleScheduler`。
- 旧 `Scheduler` 和 `SchedulerRegistry` 仅保留迁移兼容。
- 包内文档、示例、测试已指向主线调度方案；外部调用方后续按迁移策略继续收口。

### 3. Formal API 边界固化（已落地）
- 将 [`Document/FormalApiBoundary.md`](FormalApiBoundary.md) 作为正式边界说明。
- 通过 [`Document/LegacyMigrationPolicy.md`](LegacyMigrationPolicy.md) 统一 legacy 目录的替代方案、删除条件和延后决策。
- 对含义模糊的同名旧入口，补充“正式类型优先”的说明。

### 4. 验收基线建立
- 增加确定性验证：相同输入、相同序列化计划、相同上下文应得到相同结果。
- 增加计划加载兼容性验证：旧 JSON / 旧计划字段可正常解析或明确报错。
- 增加配置校验基线：动作参数、计划引用、调度语义、UGC 限制必须在执行前拦截。
- 增加性能基线：注册、构建、验证、执行的关键路径都要有最低性能目标。

## P1 落地项

### 5. 编辑器工具增强
- 为 Id 生成入口补充结果可追踪说明。
- 增加计划/配置校验反馈聚合展示。
- 增加错误定位与生成失败提示。
- 让编辑器工具成为“校验 + 生成 + 诊断”一体化入口。

### 6. 测试矩阵补齐
- 补充主线执行、计划转换、校验器、调度器、同步器、数值表达式的回归测试。
- 补充边界场景：空值、循环依赖、重复注册、非法参数、错误调度模式。
- 补充兼容场景：legacy 数据与正式路径并存时的行为一致性。
- 补充确定性场景：随机数、时间、执行顺序、回放结果。

### 7. 文档与接入体验（已完成主线入口与迁移边界整理）
- `Documentation~/README.md` 已提供快速接入、正式 API、Legacy / Experimental 边界和验证建议。
- `Document/FormalApiBoundary.md` 已提供正式 API、兼容 API 和调度选择表。
- `Document/LegacyMigrationPolicy.md` 已提供 legacy 迁移指南、删除条件和延后决策。
- 仍可在后续产品化阶段补充常见问题、性能约束和 sample 到生产接入模板。

## 推荐推进顺序
1. 主线入口、调度边界、Formal API 边界和 legacy 迁移策略已完成第一轮固化。
2. 后续优先补编辑器工具、性能/确定性验收基线和更完整测试矩阵。
3. 最后做 FAQ、样板工程、生产接入模板和 major 兼容清理批次。

## 交付标准
- 新功能默认只进入正式主线目录。
- legacy 路径不再承载新增业务逻辑。
- 所有主要行为都有可回归测试。
- 接入方可以在不阅读完整设计文档的情况下完成基础接入。