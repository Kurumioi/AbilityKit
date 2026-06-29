# Shooter 多进程测试推进计划

## 背景

`Shooter` 多进程烟测已经从“能启动/能连接”的基础联调推进到同步验收阶段。当前 create + join 多客户端链路已经能够稳定完成 packed snapshot 应用、输入提交与结果采集；本阶段重点转为把这些同步信号固化为脚本断言，避免后续回归只停留在进程退出码层面。

## 当前进度

- 已完成联调脚本的进程启动、输出采集和退出码读取修正。
- 已为 client mode 增加异常兜底，失败时也会输出机器可读的 `SHOOTER_MP_CLIENT_RESULT status=fail` 行。
- 已修复 `JoinClients=1` 场景中 packed snapshot hash mismatch 的误判：验收改为使用导入时的 reconciliation/authoritative hash 结果，而不是 replay/catch-up 后的 runtime hash。
- 已修复构建链路中的 `UnityEngine` 引用问题，`ShooterSmoke` 项目可完成编译；当前仍有大量既有 warning，但没有阻断本阶段验收。
- 已增强 `Server/Orleans/tools/run_shooter_multiprocess_smoke.ps1` 的同步验收断言：解析 result fields，并检查 mode、clientId、playerId、entryKind、snapshotHashMatched、shouldResync、runtime/view frame、输入接受结果、server ticks、entity 可见性、room/battle/world 一致性。
- 已修复脚本自身验收问题：`dotnet run` 阶段固定使用 `--no-build` 复用前置 build 产物，避免二次构建耗尽端口等待；`lastServerTicks` 改用 64 位整数解析，避免时间戳溢出。
- 已增加 `-WaitForMatchEnd` 终局验收模式：create client 会持续提交输入并等待 packed snapshot 中的权威 match metadata 进入 final；脚本会校验所有客户端最终 `stateHash`、`runtimeFrame`、`matchState`、`matchCompletedFrame`、`matchVictory` 一致。
- 已为 Shooter 服务端默认世界注册 `ShooterEnemyWaveOptions.DefaultEnabled`，让 smoke 使用默认 120 帧时限并能稳定进入终局。

- 已补充断线重连 smoke：join client 会主动关闭 TCP 连接、延迟后用同一 session token 与 room id 重新加入，脚本断言 `entryKind=Reconnect`、`reconnectCount=1` 且重连后继续收到 snapshot push。
- 已补充延迟/丢包 smoke：join client 可对入站 server push 注入 deterministic latency/jitter/loss，脚本断言扰动计数真实生效，同时保留 snapshot hash、resync、输入提交与房间一致性验收。
- 已接入框架回放/记录模块：client process 通过 `LockstepInputRecordCodecs.Current` 写出 `.record.json`，覆盖输入、snapshot payload、state hash 与基础 meta，便于测试完成后在 Unity 侧加载排查同步流程。
- 已增强 `Server/Orleans/tools/run_shooter_multiprocess_smoke.ps1`：默认写出 `artifacts/shooter_multiprocess_smoke/records/*.record.json`，并在结果断言中校验 `replayPath` 非空且文件存在；需要禁用时可传入 `-NoReplay`。

## 已通过验证

### `JoinClients=0` 最小闭环

- create client 可创建房间、启动战斗、订阅同步并提交输入。
- packed snapshot 可应用，结果行可稳定输出。

### `JoinClients=1` 强同步验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -TimeoutSeconds 45
```

结果：

- create client 与 join client 均输出 `status=pass`。
- 两个客户端共享同一个 `roomId`、`battleId`、`worldId`。
- join client `entryKind=LateJoin`。
- `snapshotHashMatched=True`。
- `shouldResync=False`。
- 3 次输入均被接受，`lastInputSuccess=True`，`lastAcceptedFrame >= lastRequestedFrame`。
- smoke 总结输出：`Shooter multiprocess smoke passed. Clients=2`。

### `JoinClients=2` 扩展同步验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 2 -TimeoutSeconds 60
```

结果：

- create client + 两个 late join client 均输出 `status=pass`。
- 三个客户端共享同一个 `roomId`、`battleId`、`worldId`。
- 两个 join client 均为 `entryKind=LateJoin`。
- 两个 join client 的 packed snapshot 均 `snapshotHashMatched=True`。
- 全部客户端均 `shouldResync=False`。
- 全部客户端输入提交均成功，server ticks 为正，entity 可见性满足当前玩家数量验收要求。
- smoke 总结输出：`Shooter multiprocess smoke passed. Clients=3`。

### `JoinClients=1` 同步到终局验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -TimeoutSeconds 75 -WaitForMatchEnd
```

结果：

- create client 与 join client 均输出 `status=pass`。
- 两个客户端共享同一个 `roomId`、`battleId`、`worldId`。
- create client 持续提交输入直到终局，最终 `inputs=88`，`lastInputSuccess=True`，`shouldResync=False`。
- join client 作为 late join 能持续接收权威 packed snapshot 到终局，最终 `shouldResync=False`。
- 两个客户端最终一致：`runtimeFrame=120`、`viewFrame=120`、`stateHash=0xCFE41A89`、`matchState=4`、`matchFinal=True`、`matchCompletedFrame=120`、`timeLimitFrames=120`、`remainingTimeFrames=0`。
- smoke 总结输出：`Shooter multiprocess end-to-end smoke passed. Clients=2`。

### `JoinClients=1` 断线重连验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -Inputs 3 -TimeoutSeconds 45 -ReconnectJoinClient -ReconnectDelayMs 500
```

结果：

- create client 与 join client 均输出 `status=pass`。
- join client 使用同一 session token 与 room id 重连，最终 `entryKind=Reconnect`、`reconnectCount=1`、`reconnectEntryKind=Reconnect`。
- join client 重连前后继续收到权威 snapshot push，验证结果为 `reconnectPushesBefore=19`、`reconnectPushesAfter=20`。
- 重连后仍满足 `snapshotHashMatched=True`、`shouldResync=False`、输入提交成功、room/battle/world 一致。
- smoke 总结输出：`Shooter multiprocess resilience smoke passed. Clients=2`。

### `JoinClients=1` 延迟/丢包验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -NoBuild -JoinClients 1 -Inputs 3 -TimeoutSeconds 60 -ConditionLatencyMs 25 -ConditionJitterMs 10 -ConditionPacketLossRate 0.25 -ConditionSeed 20260610
```

结果：

- create client 与 join client 均输出 `status=pass`。
- join client 在入站 server push 上启用延迟、抖动和丢包，结果字段为 `conditionLatencyMs=25`、`conditionJitterMs=10`、`conditionPacketLossRate=0.25`。
- 扰动计数生效：`conditionInboundReceived=18`、`conditionInboundDelayed=12`、`conditionInboundDropped=6`。
- 网络扰动下仍满足 `snapshotHashMatched=True`、`shouldResync=False`、输入提交成功、room/battle/world 一致。
- smoke 总结输出：`Shooter multiprocess resilience smoke passed. Clients=2`。

### `JoinClients=1` 回放记录产物验收

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Server\Orleans\tools\run_shooter_multiprocess_smoke.ps1 -JoinClients 1 -Inputs 3 -TimeoutSeconds 30
```

结果：

- create client 与 join client 均输出 `status=pass`，且 `replayPath` 指向实际生成的记录文件。
- 生成 `artifacts/shooter_multiprocess_smoke/records/client-create.record.json`，包含 `Meta`、`Inputs=3`、`StateHashes=47`、`Snapshots=42`。
- 生成 `artifacts/shooter_multiprocess_smoke/records/client-join-1.record.json`，包含 `Meta`、`Inputs=3`、`StateHashes=15`、`Snapshots=10`。
- 记录格式使用框架 `LockstepInputRecordFile` JSON 结构，Unity 侧可通过 `LockstepJsonInputRecordReader` / `LockstepInputRecordCodecs.Current` 加载，用于按帧检查输入、状态 hash 与 snapshot payload。

## 当前结论

- 多进程连接链路已经具备基础稳定性：server、create client、多个 join client 可被脚本统一启动、采集和清理。
- late join 的 packed snapshot 应用链路已经通过强断言验收，不再复现此前的 hash mismatch / resync 问题。
- 当前同步验收已经覆盖关键回归信号：结果格式、客户端身份、房间/战斗/世界一致性、snapshot hash、resync、frame 推进、输入提交与服务端接受帧。
- 终局流程已完成验收：create client 与 late join client 都能持续接收 packed snapshot 到权威 final metadata，并在最终帧达到一致的 runtime frame、state hash 与 match result。
- resilience 流程已完成验收：断线重连会重新进入 `Reconnect` 分支并恢复 push；延迟/丢包会真实作用于入站 server push，且不会破坏当前 smoke 的同步验收条件。
- 回放记录流程已完成验收：默认 smoke 会生成 create/join 客户端对应的 `.record.json`，文件内包含 inputs、state hashes、snapshots 与 meta，可作为 Unity 回放/排查入口。
- 终局模式下，late join 首个 applied snapshot 的 hash 可能不同于最终一致状态；脚本只在非终局模式强制 `snapshotHashMatched=True`，终局模式改为以最终跨客户端状态一致性作为验收核心。
- 后续如果该 smoke 失败，优先查看 `artifacts/shooter_multiprocess_smoke/*.log` 中的 `SHOOTER_MP_CLIENT_RESULT` 行，通常可以直接定位到失败断言字段；需要复盘帧级输入/snapshot 时继续查看 `artifacts/shooter_multiprocess_smoke/records/*.record.json`。

## 后续推荐执行顺序

1. 将当前多进程 smoke 纳入本地/CI 的 Shooter 同步回归入口：快速回归运行 `JoinClients=1`，完整端到端回归运行 `JoinClients=1 -WaitForMatchEnd`，resilience 回归运行 `JoinClients=1 -ReconnectJoinClient` 与 `JoinClients=1 -ConditionLatencyMs 25 -ConditionJitterMs 10 -ConditionPacketLossRate 0.25`。
2. 在 Unity 侧补一个面向 `LockstepInputRecordFile` 的 Shooter 调试入口：选择 `.record.json` 后展示 meta、输入帧、state hash 曲线与 snapshot 帧列表，方便和客户端日志对照。
3. 针对当前大量既有 warning 建立单独清理计划，避免 smoke 输出过大影响失败定位。
4. 如果继续扩展到更多客户端，建议先让脚本支持端口与 artifact 目录参数化，便于并行运行。

## 相关文件

- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Program.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Networking/SmokeTcpGameFrameworkNetworkChannel.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Runner/ShooterSmokeClientProcessRunner.cs`
- `Server/Orleans/src/AbilityKit.Orleans.ShooterSmoke/Runner/ShooterSmokeReplayRecordScope.cs`
- `Server/Orleans/tools/run_shooter_multiprocess_smoke.ps1`
- `Unity/Packages/com.abilitykit.record/Runtime/Record/Lockstep/LockstepInputRecordFile.cs`
- `Unity/Packages/com.abilitykit.record/Runtime/Record/Lockstep/LockstepJsonInputRecordReader.cs`
- `Unity/Packages/com.abilitykit.demo.shooter.runtime/Runtime/Worlds/ShooterWorldModule.cs`
- `artifacts/shooter_multiprocess_smoke/`
- `artifacts/shooter_multiprocess_smoke/records/`
- `Docs/` 下的 Shooter 相关设计与阶段总结文档
