# SearchTarget 使用示例（组合思路）

本文仅给出“怎么组合”的示例思路，便于业务侧快速拼装查询。

> 说明：本模块强调复用 `SearchContext` 的服务和数据来注入能力与动态参数。

## 示例 1：给定标识列表（上下文/外部传入） -> 输出单位门面

- 候选提供者：`ExplicitListCandidateProvider(ids)`
- 规则：可选（如 `RequireValidIdRule`、`ExcludeEntityRule`）
- 评分器：`ZeroScorer` 或距离评分器
- 选择器：`StreamingTopKByScoreSelector`（若只要前若干个结果）
- 映射器：`EntitasUnitFacadeMapper`

组合要点：
- `SearchContext` 注入 `IUnitResolver`（用于映射器）
- 若包含形状/距离规则，还要注入 `IPositionProvider`

## 示例 2：索引候选（阵营/类型） + 圆形形状 + 最近优先 + 前若干个结果

候选来源（两种常见方式）：

### 2.1 索引是 `IReadOnlyList`（如自维护列表索引）
- `IEntityIdIndex` + `IndexedListCandidateProvider(key)`

### 2.2 索引是 `HashSet`（来自实体管理器键索引）
- `IEntityIdCollectionIndex` + `IndexedCollectionCandidateProvider(key)`

过滤与排序：
- 规则：
  - `ResolvedCircleRule2D(frameResolver, circleParams)` 或 `CircleShapeRule`
  - 可选 `ExcludeEntityRule(self)`
- 评分器：`DistanceToEntityScorer2D(self)`（返回负距离平方，最近分数最高）
- 选择器：`StreamingTopKByScoreSelector`，设置 `MaxCount` 为需要的数量

参考系解析器（圆心/朝向来源）：
- 圆心来自施法者：可用自定义参考系解析器（或把 `origin` 写入 `SearchContext` 上下文数据，再实现一个数据参考系解析器）
- 圆心来自两实体中点：`EntityToEntityFrameResolver2D(a, b, useMidPointAsOrigin:true)`

## 示例 3：矩形宽固定，长度 = 两实体距离（动态） + 起点锚定 + 偏移

需求：
- 矩形沿来源实体到目标实体的方向
- 宽固定
- 长度动态：来源实体到目标实体的距离乘以缩放值再加偏移值
- 锚点：从来源实体开始向前延伸
- 再叠加局部偏移（例如向前推 1m、向右偏 0.5m）

组合：
- 参考系解析器：
  - `EntityToEntityFrameResolver2D(source, target, useMidPointAsOrigin:false)`
  - `OffsetFrameResolver2D(inner, offsetLocal: (rightOffset, forwardOffset))`
- 参数解析器：`RectLengthFromEntityDistanceResolver2D(source, target, width, scale, add, minLength, maxLength)`
- 规则：`ResolvedOrientedRectRule2D(frame, rectParams, pivot: Start)`

注意：
- 这类组合对 `IPositionProvider` 是强依赖（严格模式下缺失直接无结果）。

## 示例 4：扇形（方向来自两实体） + 半径固定 + 前若干个结果

- 参考系解析器：`EntityToEntityFrameResolver2D(source, target, useMidPointAsOrigin:false)`
- 扇形参数：`SectorParamsConstantResolver2D(radius, halfAngleDeg)`
- 规则：`ResolvedSectorRule2D(frame, sectorParams)`
- 选择器：`StreamingTopKByScoreSelector`（或在线前若干个结果选择器）

## 示例 5：技能落点（非实体）作为圆心 / 参数来自上下文（数据驱动解析器）

需求：
- 圆心是技能落点（Vector2）
- 半径来自技能计算结果（float）

做法：
- 把 `originXZ`、`radius` 写入 `SearchContext` 上下文数据
- 参考系解析器使用 `DataFrameResolver2D(originKey)`
- 圆形参数使用 `DataCircleParamsResolver2D(radiusKey)`
- 规则使用 `ResolvedCircleRule2D(frame, circleParams)`

## 示例 6：黑名单/白名单（例如“已命中过的目标不再命中”）

- 使用 `BlacklistRule(IActorIdSet)` 或 `WhitelistRule(IActorIdSet)`
- 黑白名单集合实现放业务层，框架层只要求 `IActorIdSet.Contains(actorId)`

## 示例 7：可控随机（种子控制确定性）

需求：
- 同一帧/同一次技能释放在相同种子下随机结果稳定

做法：
- 将种子（整数）写入 `SearchContext` 上下文数据
- 使用 `SeededHashRandomScorer(seedKey)` 作为评分器
- 配合 `StreamingTopKByScoreSelector`，并设置 `MaxCount=1` 或需要的数量

## 示例 8：统计钩子（调试候选量/命中量/结果量）

做法：
- 创建一个 `SearchStats` 并注入 `SearchContext`：`ctx.SetService<ISearchStats>(stats)`
- 一次查询完成后读取：`stats.Candidates / stats.Hits / stats.Results`

## 常见扩展点建议

- 多阵营/多类型：
  - 高频：建立复合索引 `(camp,type)->set`，候选提供者枚举多个键
  - 通用：使用候选提供者组合器表达集合运算：
    - `UnionDistinctCandidateProvider`：多集合并集（需要 `IVisitedSet` 去重）
    - `IntersectCandidateProvider`：交集（需要 `IVisitedSet`）
    - `ExceptCandidateProvider`：差集（需要 `IVisitedSet`）
  - 去重服务：在 `SearchContext` 注入 `IVisitedSet`（例如 `VersionedVisitedSet`），组合器会在一次搜索开始时调用 `Next()`

- 动态参数来源：
  - 来自实体：用参考系解析器和参数解析器通过 `IPositionProvider` 获取位置
  - 来自上下文数据：使用 `SearchContext.SetData` 写入参数，再实现对应的数据参数解析器

- 确定性：
  - 使用 `IEntityKeyProvider` 保证同分时稳定排序

