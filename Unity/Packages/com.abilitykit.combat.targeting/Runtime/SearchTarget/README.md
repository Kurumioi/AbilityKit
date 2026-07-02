# SearchTarget（战斗查找目标模块）

本模块提供一套通用、可扩展、可替换实体系统后端、并面向高频调用优化（低/零分配）的“目标查找”框架。

## 目标

- 统一入口管理各种“找目标”需求。
- 可扩展：候选来源、过滤条件、几何形状、排序/选择策略均可替换。
- 可替换：不依赖具体实体系统（例如 Entitas、ActorEntity 或其他实现）。
- 高性能：
  - 单次遍历候选（候选提供者的 `ForEachCandidate` 与结构体消费者）。
  - 可选流式前若干个结果选择（不构建命中列表）。
  - 候选集合可直接引用索引集合（避免创建/复制临时列表）。
- 确定性：除“显式随机”策略外，相同输入应得到稳定一致的结果；稳定排序使用 `IEntityKeyProvider`。

## 数据流（管线）

1. **候选生成（候选提供者）**：`ICandidateProvider`
2. **过滤（规则）**：`ITargetRule[]`
3. **评分（评分器）**：`ITargetScorer`
4. **选择（选择器）**：`ITargetSelector`（可选 `IStreamingHitSelector`）
5. **映射输出（映射器）**：`ITargetMapper<T>`（例如输出 `IUnitFacade`）

核心执行由 `TargetSearchEngine` 完成：
- `SearchIds(in query, context, results)`：写入调用方提供的 `List<IEntityId>`，兼容旧调用方式。
- `SearchIds(in query, context)`：返回池化 `SearchResult`，使用完成后 `Dispose()` 或 `TargetingPool.Release(result)`。
- `Search<T>(..., ITargetMapper<T> mapper)`：输出任意类型列表（如 `IUnitFacade`）。

## 关键接口

### SearchQuery
- `Provider`：候选来源
- `Rules`：过滤链
- `Scorer`：评分（用于“最近/最优”等排序）
- `Selector`：选择策略（全量排序 / 在线前若干个结果 / 流式前若干个结果）
- `MaxCount`：最大数量（大于 0 时启用前若干个结果逻辑）

### SearchContext
- `SetService<T>/TryGetService<T>`：注入能力（位置、稳定键、索引、解析器等）
- `SetData/TryGetData<T>`：存放上下文数据（技能落点、锁定目标、动态参数等）
- 高频调用建议使用 `TargetingPool.RentContext()` 获取，结束后 `Dispose()` 或 `TargetingPool.Release(context)`。

### TargetingPool
- 统一接入 `AbilityKit.Core.Pooling`，池化查询上下文、查询结果、命中列表、标识列表、规则列表、流式前若干个结果缓冲。
- `TargetSearchEngine` 内部不再常驻私有临时列表，而是单次查询租借临时集合，减少引擎实例并发/复用时的状态污染。
- `StreamingTopKByScoreSelector` 不再直接使用 `ArrayPool<T>`，统一走目标查找模块对象池入口。

### TargetQueryDatabase
- `Register(queryId, ITargetQueryFactory)`：包外模块可按标识注册上下文驱动的查询工厂。
- `Register(queryId, in SearchQuery)`：注册静态查询。
- `TrySearchIds(queryId, context, out SearchResult)`：以数据库式查询标识执行查询并返回池化结果。
- `ITargetQueryFactory.TryBuild(context, out query)`：业务包可根据技能等级、阵营、锁定目标、落点等上下文动态构造查询。

### 候选提供者（零分配单次遍历）
`ICandidateProvider.ForEachCandidate<TConsumer>(..., ref TConsumer consumer)`
- 消费者为结构体，实现 `ICandidateConsumer.Consume(EcsEntityId)`
- 设计为推送模式，避免生成中间候选列表

### 位置能力（严格依赖）
`IPositionProvider.TryGetPositionXZ(EcsEntityId id, out Vector2 positionXZ)`
- 形状过滤、距离评分等规则都依赖此能力
- **严格策略**：如果查询中包含 `RequiresPosition=true` 的候选提供者、评分器、选择器或规则，但 `SearchContext` 未提供 `IPositionProvider`，则直接返回空结果。

### 稳定键（确定性）
`IEntityKeyProvider.GetKey(EcsEntityId id) -> ulong`
- 用于同分/同权重时稳定排序
- 未来如存在标识复用，可扩展为 `(id, version)` 编码成稳定键

## 形状系统（解析器）

### 基础形状规则
- `CircleShapeRule`、`OrientedRectShapeRule`、`SectorShapeRule`

### 解析器化（应对复杂需求）
把“坐标系”和“参数”解耦：

- 参考系：`IShapeFrameResolver2D -> ShapeFrame2D(Origin, Forward, Right)`
- 参数：
  - 矩形：`IRectParamResolver2D`
  - 圆形：`ICircleParamResolver2D`
  - 扇形：`ISectorParamResolver2D`

对应组合规则：
- `ResolvedOrientedRectRule2D`
- `ResolvedCircleRule2D`
- `ResolvedSectorRule2D`

典型复杂需求覆盖：
- **偏移**：`OffsetFrameResolver2D(inner, offsetLocal)`
- **朝向来自两个实体**：`EntityToEntityFrameResolver2D(source, target, useMidPointAsOrigin)`
- **动态长度/半径来自两实体距离**：`RectLengthFromEntityDistanceResolver2D` / `CircleRadiusFromEntityDistanceResolver2D`

### 数据驱动解析器（来自上下文数据）

当“来源不是实体”（例如技能落点、鼠标点、外部系统给定点或动态参数）时，可使用数据驱动解析器从 `SearchContext` 的数据中读取：

- `DataFrameResolver2D(originKey, forwardKey)`：从上下文数据读取 `Vector2 origin/forward`
- `DataRectParamsResolver2D(widthKey, lengthKey)`：从上下文数据读取矩形宽/长
- `DataCircleParamsResolver2D(radiusKey)`：从上下文数据读取圆半径
- `DataSectorParamsResolver2D(radiusKey, halfAngleDegKey)`：从上下文数据读取扇形半径/半角

## 与 EntityManager 联动（索引候选）

当候选来自框架层 `BattleEntityManager` 的索引（内部通常是 `HashSet<int>`）时：
- 使用 `IEntityIdCollectionIndex.ForEach(key, ref consumer)` 以避免接口枚举带来的分配
- `EntityManager/KeyedEntityIndexAdapter<TKey>` 提供了将 `IKeyedEntityIndex<TKey, int>` 适配为 `IEntityIdCollectionIndex` 的示例

## Entitas 集成点

### 位置能力
- `Entitas/EntitasActorTransformPositionProvider`：
  - 通过 `EntitasActorIdLookup` 找到 `ActorEntity`
  - 读取 `ActorEntity.transform.Value.Position`，输出 XZ

### 输出类型
- `Entitas/EntitasUnitFacadeMapper`：把 `EcsEntityId -> IUnitFacade`（依赖 `IUnitResolver`）

## 包外扩展建议

- 新候选源：实现 `ICandidateProvider`，从业务索引、实体系统分组、空间划分、服务器快照等来源推送候选。
- 新过滤条件：实现 `ITargetRule`，通过 `SearchContext` 读取阵营、状态、标签、可见性、仇恨等服务。
- 新评分策略：实现 `ITargetScorer`，支持最近、血量最低、威胁最高、仇恨最高、稳定随机等排序。
- 新选择策略：实现 `ITargetSelector` 或 `IStreamingHitSelector`，支持前若干个结果、加权随机、分组取样、优先级桶等。
- 查询目录：实现 `ITargetQueryFactory` 并注册到 `TargetQueryDatabase`，让技能、智能体或触发器只通过查询标识和上下文发起查询。

## 当前值得继续优化的点

- 补齐候选提供者组合器与 `IVisitedSet` 的实际运行时实现，支撑并集、交集、差集、去重查询。
- 引入空间索引候选提供者（网格、四叉树、层次包围盒、EntityManager 索引适配），让候选生成从全量扫描升级为范围查询。
- 增加 `SearchQuery` 描述化/序列化能力，支持配置表、技能模板和热更新层生成查询。
- 增加完整编辑器测试包，覆盖池化归还、流式前若干个结果、查询目录、位置能力缺失严格返回空等路径。
- 统一文档中尚未落地的形状解析器、矩形规则、候选提供者组合器命名，避免设计文档超前于运行时实现。

## 选择器（排序/选择策略）

- `TopKByScoreSelector`：全量排序后取前若干个结果
- `OnlineTopKByScoreSelector`：不排序全量命中，仅维护前若干个结果（复杂度 `O(N*K)`）
- `Selectors/StreamingTopKByScoreSelector`：**流式前若干个结果选择**，引擎在枚举候选时直接提交命中，不构建命中列表（高频最优）

## 通用规则与评分器

规则：
- `ExcludeEntityRule`
- `RequireValidIdRule`
- `RequireHasPositionRule`
- `BlacklistRule`（依赖 `IActorIdSet`）
- `WhitelistRule`（依赖 `IActorIdSet`）

评分器：
- `DistanceToEntityScorer2D`（最近优先：返回负距离平方）
- `DistanceToFrameOriginScorer2D`
- `SeededHashRandomScorer`（可控随机：种子由 `SearchContext` 上下文数据决定）

## 可选统计（默认不启用）

为方便性能/逻辑排查，框架层提供轻量统计钩子：

- `ISearchStats`：`OnCandidate/OnHit/OnResult`
- `SearchStats`：一个简单实现（记录候选数/命中数/结果数）

用法：在 `SearchContext` 注入 `ISearchStats`，引擎会自动在一次查询中更新统计数据。

## 候选提供者组合器与去重（支持多重条件候选）

当需要表达“多阵营/多类型/多来源”的候选组合时，推荐使用框架层提供的候选提供者组合器，避免业务侧重复实现集合运算。

组合器：
- `ConcatCandidateProvider`：按顺序串联多个候选提供者（不去重）
- `UnionDistinctCandidateProvider`：并集 + 去重（依赖 `IVisitedSet`）
- `IntersectCandidateProvider`：交集（依赖 `IVisitedSet`）
- `ExceptCandidateProvider`：差集（依赖 `IVisitedSet`）

去重/集合运算依赖：
- `IVisitedSet`：零分配的“访问标记”服务
- 默认实现：`Visited/VersionedVisitedSet`（版本号递增，避免清理复杂度 `O(n)`）

使用方式：
- 在 `SearchContext` 注入 `IVisitedSet`（例如 `ctx.SetService<IVisitedSet>(new VersionedVisitedSet())`）
- 使用 `UnionDistinctCandidateProvider` / `IntersectCandidateProvider` / `ExceptCandidateProvider` 时会自动调用 `visited.Next()` 开启一次新的标记周期

