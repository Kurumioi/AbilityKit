# com.abilitykit.world.networkfragments：网络帧数据层

## 目标
本包提供帧数据包结构和远程帧缓冲区，与具体网络传输解耦。

## 核心类型

| 类型 | 命名空间 | 说明 |
|---|---|---|
| `ISnapshotEnvelope` | `AbilityKit.Ability.Host` | 快照信封接口 |
| `FramePacket` | `AbilityKit.Ability.Host` | 帧数据包 |
| `RemoteFrameBuffer<T>` | `AbilityKit.Ability.Host` | 通用帧缓冲区（实现 `IRemoteFrameSource/Sink`） |
| `RemoteFrameAggregator` | `AbilityKit.Ability.Host` | 输入/快照聚合器 |
| `RemoteSnapshotFrame` | `AbilityKit.Ability.Host` | 快照帧聚合结果 |
| `RemoteInputFrame` | `AbilityKit.Ability.Host` | 输入帧聚合结果 |

## 目录结构

```
com.abilitykit.world.networkfragments/Runtime/
└── Frames/
    ├── ISnapshotEnvelope.cs       # 快照信封接口
    ├── FramePacket.cs            # 帧数据包 + SnapshotProviderDrain
    ├── RemoteFrameBuffer.cs       # 通用帧缓冲区
    ├── RemoteFrameAggregator.cs   # 帧聚合器
    ├── RemoteSnapshotFrame.cs     # 快照帧聚合结果
    └── RemoteInputFrame.cs       # 输入帧聚合结果
```

## 依赖关系

```
core
 ├── Network.Runtime
 ├── framesync
 ├── host              (依赖 Network.Runtime, framesync)
 │     └── WorldStateSnapshot (opCode + payload)
 └── networkfragments  (依赖 host, framesync, Network.Runtime)
       └── Frames: FramePacket, ISnapshotEnvelope, RemoteBuffer, Aggregator
```

## 适配器位置

`FramePacketNetAdapter`（处理帧数据 + 快照分发的适配器）放置在 `com.abilitykit.host.extension` 包的 `Session/` 子目录下，因为它需要同时引用：

- `networkfragments`（提供 `FramePacket`、`ISnapshotEnvelope`）
- `snapshot`（提供 `FrameSnapshotDispatcher`）
- `host`（提供 `IWorldStateSnapshotProvider`）

## 非目标
- 不处理 socket/kcp/websocket 连接
- 不定义网络协议格式
- 不负责序列化/反序列化
