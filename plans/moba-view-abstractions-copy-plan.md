# MOBA View Abstractions 复制骨架实施计划

## 当前进展

已完成抽象包最小骨架的第一轮复制：

- `Unity/Packages/com.abilitykit.demo.moba.view.abstractions/package.json`
- `Unity/Packages/com.abilitykit.demo.moba.view.abstractions/Runtime/com.abilitykit.demo.moba.view.abstractions.asmdef`
- `IBattleViewEventSink`
- `IBattleViewTimeSource`
- `IBattleHudActorPositionResolver`
- `IBattleViewShellLoader`
- `IBattleHudInputSink`
- `BattleFloatingTextSpec`
- `BattleProjectileVfxSpawnSpec`
- `BattlePresentationCueModels`
- `BattleViewPositionSample`
- `BattleViewPositionBuffer`
- `BattleDamageTextSpec`
- `BattleViewPositionInterpolator`

## 下一步建议

1. 继续复制 `BattlePresentationCue` 相关的更多 DTO 和判定结果。
2. 复制 `BattleViewPositionSample` 相关的更完整 buffer/采样协议。
3. 复制和整理 `BattleHudInput` 的输入状态模型。
4. 建立 runtime 到 abstractions 的引用调整，但暂时不替换主线实现。

## 说明

当前阶段采取“平行复制骨架”的方式，只扩展抽象边界，不破坏原有 `view.runtime` 行为。等抽象包足够稳定后，再逐步把逻辑和 Unity 适配迁移到新的边界上。
