# Shooter 示例正式化步骤 1-4 实施计划

## 目标

将 Shooter 示例从“框架验收样板”推进到“可继续产品化的稳定运行时边界”，本阶段只处理前四项：端口收敛、快照职责拆分、规则配置版本化、确定性验证夹具。

## 原则

- 保持现有 `IShooterBattleRuntimePort` 调用方兼容，不做破坏性删除。
- 新增窄接口和服务优先，旧聚合端口作为 facade 过渡。
- 逻辑运行时继续保持 Unity-free，遵守 `noEngineReferences=true`。
- 每一步都以可测试、可替换、可迁移为验收标准。

## 步骤 1：收敛运行时端口

新增窄端口：

- `IShooterGameStartPort`：开局与 StartSpec 读取。
- `IShooterInputPort`：提交输入。
- `IShooterSimulationClock`：帧推进与当前帧。
- `IShooterSnapshotReadPort`：读取逻辑快照。
- `IShooterPackedSnapshotPort`：导入/导出打包快照。
- `IShooterStateHashProvider`：计算状态 Hash。

实施方式：让 `ShooterBattleRuntimePort` 同时实现这些窄端口，并继续实现旧 `IShooterBattleRuntimePort`。

## 步骤 2：拆分快照与序列化职责

新增服务：

- `ShooterStateSnapshotExporter`：从实体状态导出 `ShooterStateSnapshotPayload`。
- `ShooterStateHasher`：根据实体状态计算稳定 Hash。
- `ShooterPackedSnapshotExporter`：导出 `ShooterPackedSnapshotPayload`。
- `ShooterPackedSnapshotImporter`：导入 `ShooterPackedSnapshotPayload`。
- `ShooterPackedSnapshotBytesCodec`：处理 byte[] 序列化桥接。

实施方式：先把职责从 `ShooterBattleRuntimePort` 中剥离到服务类，端口保留委托方法。

## 步骤 3：规则配置版本化

新增 `ShooterRuleSet` 值对象，包含：

- `RuleSetId`
- `ConfigVersion`
- `PlayerSpeed`
- `BulletSpeed`
- `BulletLifeFrames`
- `HitRadius`
- `HitDamage`

实施方式：让 `ShooterBattleRules` 从 `ShooterRuleSet` 构建，并暴露当前 `RuleSet`。StartGame 阶段先支持默认规则，不强依赖协议新增字段。

## 步骤 4：确定性验证夹具

新增运行时级 deterministic fixture：

- 固定 tick delta。
- 固定输入序列。
- 导出 hash、snapshot、packed snapshot。
- 支持 packed snapshot round-trip 后再次计算 hash。

实施方式：新增 `ShooterDeterminismSpecRunner`，供无头测试、Unity 验收和 CI 后续复用。

## 完成标准

- 旧调用方不需要修改即可编译。
- 新窄端口可被 DI 单独解析或由 `ShooterBattleRuntimePort` 直接向上转型使用。
- 快照/Hash/打包导入导出逻辑不再集中在 `ShooterBattleRuntimePort` 主体中。
- 默认规则有稳定版本标识。
- 至少存在一条固定输入的确定性验证路径。
