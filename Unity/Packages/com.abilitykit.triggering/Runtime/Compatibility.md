# Triggering 兼容记录

该文件仅保留历史边界记录。正式主线请使用 `Runtime/Plan`、`ActionScheduler`、`RuleScheduler` 与相关验证/诊断入口。

- 已清理的旧主线入口：`Runtime/Dispatcher`、`Runtime/Scheduler`、`Runtime/Executable`、`Runtime/Legacy` 中可运行的迁移壳。
- 当前不应新增任何旧兼容入口；若必须保留历史类型，也应保持显式失败或只读历史语义。
- 相关正式边界请参考 `Document/FormalApiBoundary.md` 与 `Document/LegacyMigrationPolicy.md`。
