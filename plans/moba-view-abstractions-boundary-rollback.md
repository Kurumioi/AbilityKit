# MOBA View Abstractions 边界回收计划

## 背景

当前抽象包在复制骨架过程中，已经引入了 `UnityEngine` 命名空间和对象类型，例如：

- `Vector3`
- `Color`

这与抽象包“只保留接口 / DTO / 纯值对象”的目标不一致。

## 回收目标

把 `com.abilitykit.demo.moba.view.abstractions` 收紧为真正的跨层契约包：

- 不依赖 `UnityEngine`
- 不依赖 `MonoBehaviour`
- 不依赖 `GameObject`
- 不依赖 `Transform`
- 不依赖 `Color`
- 不依赖 `Vector3`

## 回收原则

1. `UnityEngine` 类型全部回退到 `com.abilitykit.demo.moba.view.runtime`。
2. 抽象包只保留：
   - 接口
   - 事件契约
   - 纯 DTO
   - 轻量值对象
3. 需要坐标 / 颜色 / 旋转等数据时，用纯值对象替代，例如：
   - `MobaFloat3`
   - `MobaColor32`
   - `MobaQuaternion4`
4. Unity 适配层负责在边界上做类型转换。

## 需要优先回收的文件

- `BattleFloatingTextSpec`
- `BattleProjectileVfxSpawnSpec`
- `BattlePresentationCueModels`
- `BattleViewPositionSample`
- `BattleViewPositionBuffer`
- `BattleDamageTextSpec`
- `BattleViewPositionInterpolator`

## 建议替代方案

### 1. 位置类型

把 `Vector3` 替换为纯值对象：

- `MobaFloat3`
- `MobaFloat2`（如需要）

### 2. 颜色类型

把 `Color` 替换为纯值对象：

- `MobaColor32`
- 或 `MobaColorRgba`

### 3. Unity 依赖接口

像 `IBattleViewShellLoader` 当前返回 `GameObject`，建议后续改为：

- 资源句柄
- 抽象视图实例句柄
- 或 Unity runtime 独有接口

## 迁移顺序

1. 先新增纯值对象。
2. 再把抽象包里所有 `UnityEngine` DTO 替换掉。
3. 最后让 runtime 包做适配转换。

## 当前结论

抽象包现在处在“骨架已搭，但边界还不够纯”的阶段。下一步重点不是继续加文件，而是先把已有文件中的 Unity 依赖逐个消除，确保 abstractions 真正可被纯逻辑和普通测试项目直接引用。
