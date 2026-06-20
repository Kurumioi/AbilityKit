# Orleans Silo Affinity 与专用部署设计

## 问题背景
当前系统虽然可以依赖 Orleans 的自动分配把 grain 分布到多个 silo，但商业化游戏通常不会让所有逻辑完全随机漂移。对于房间、战斗、编队、匹配、中心会话等能力，往往会要求：

- 某些 grain 必须固定在同一台 silo 上，减少进程间调用成本
- 某些能力必须按业务域拆分成专用 silo，便于扩容与隔离
- 某些强耦合逻辑最好在同进程内完成，而不是跨 silo RPC
- 部署层面需要显式规划“区域 / 集群 / 进程 / 角色”边界

## 核心目标
1. 让 grain 归属策略可配置，而不是完全默认随机调度
2. 让同域高频调用尽量落在同进程或同 silo 内
3. 让不同能力域可以独立扩缩容和发布
4. 让运维、监控、故障隔离与部署策略一致

## 建议的分层
### 1. 逻辑域分层
把服务器能力按业务域划分，而不是按技术类型划分：

- `Session`：登录、令牌、账号状态
- `Matchmaking`：匹配与队列
- `Room`：房间生命周期、成员管理、准备、选角
- `Battle`：对局与战斗模拟
- `Sandbox`：演示/调试/压测专用逻辑
- `Directory`：目录、路由、实例发现

### 2. 部署角色分层
每个 silo 不再只是“一个 Orleans 节点”，而是有明确角色：

- `Gateway`：HTTP/WebSocket/TCP 入口
- `SessionSilo`：账号与会话中心
- `RoomSilo`：房间类 grain 专用
- `BattleSilo`：战斗类 grain 专用
- `SharedSilo`：低频共享逻辑或通用目录
- `SandboxSilo`：开发/验收/压测场景

### 3. Affinity 策略
为 grain 增加“亲和性”概念：

- `HardAffinity`：必须在指定角色或同组 silo 上
- `SoftAffinity`：优先同组，但可降级
- `CoLocated`：要求与某些 grain 同进程/同 silo
- `Exclusive`：单实例独占，例如全局目录、中心会话

## 设计建议
### Grain 身份分段
将 grain id 设计成可路由的结构，而不是纯字符串：

- `Room:{region}:{serverId}:{roomId}`
- `Battle:{battleId}`
- `Session:Global`
- `Directory:{region}:{serverId}`

这样可以让 placement / director 根据前缀识别所属域。

### 目录服务不直接承载业务
`Directory` 只负责：

- 哪个实例负责哪个域
- 哪个 room/battle 应该落在哪个 silo 组
- 故障时如何重定向

它不应该直接执行房间或战斗逻辑。

### 同进程协作优先
对于高频交互的逻辑，比如：

- 房间创建后马上做成员绑定
- 战斗开始前的房间状态快照
- 准备/选角/开始战斗的状态流转

应优先保证它们位于同一 role 或同一 placement group，这样可以显著降低跨进程调用开销。

## Orleans 侧可落地的机制
### Placement Director / Strategy
可以考虑为不同 grain 类型配置不同 placement 策略：

- 按 `GrainType` / `GrainInterface` 选择 silo
- 按 region / serverId / roomId hash 固定落点
- 对某些域做“粘性调度”

### Cluster Membership + 自定义分组
维护一个内部的 logical silo group：

- `room-group-a`
- `battle-group-a`
- `session-group`

每个组内再由 Orleans 自动平衡，但不同组之间保持隔离。

### Placement Hint / Metadata
在 grain 定义中增加元数据：

- `PreferredGroup`
- `CoLocateWith`
- `IsExclusive`
- `SupportsMigration`

这些 metadata 可以作为未来 placement 与运维决策的输入。

## 推荐的现实落地顺序
1. 先定义部署角色和逻辑域
2. 再把 grain id 结构化
3. 再做 placement metadata
4. 再实现按角色/分组的 silo 启动参数
5. 最后才考虑更细的亲和性和迁移策略

## 对当前项目的直接建议
- `SessionGrain` 保持中心化或单组部署
- `RoomGrain` 按 `region/serverId` 固定到同一房间组
- `BattleGrain` 与其依赖房间逻辑尽量共组或同役
- `DirectoryGrain` 只做路由与实例索引，不承载重业务
- `Gateway` 不直接负责业务状态，只做编排与转发

## 结论
商业化游戏服务端不应该把 Orleans 只当作“自动负载均衡器”，而是把它当作“可编排的分布式执行层”。真正重要的是提前设计：

- 哪些逻辑必须同进程
- 哪些逻辑必须同 silo 组
- 哪些逻辑必须独占实例
- 哪些逻辑允许跨 silo

这样才能把性能、维护性和发布策略统一起来。