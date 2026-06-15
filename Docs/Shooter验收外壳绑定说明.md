# Shooter 验收外壳绑定说明（Unity 薄层）

本说明描述 Unity 端如何作为「验收外壳」绑定到纯 C# 验收抽象 [`ShooterAcceptanceLab`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)。核心同步逻辑与多模式/多网络环境的组合已由 xUnit 全量覆盖，Unity 端只负责「选择 + 驱动 + 可视化」，不持有任何同步规则。

## 1. 抽象边界

纯 C# 层（`AbilityKit.Demo.Shooter.View` 命名空间，asmdef↔csproj 双投影）暴露以下入口：

- [`ShooterAcceptanceCatalog`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)：可选同步模式 `SyncModes` 与网络环境 `NetworkEnvironments` 两个只读目录，UI 直接绑定。
- [`ShooterAcceptanceLab.Create(...)`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)：一键装配 runtime + presentation + controller + carrier 并已 `StartGame`，返回可运行的 `ShooterAcceptanceSession`。支持 `enableAuthoritativeWorld` 勾选对比模式。
- [`ShooterAcceptanceSession`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)：暴露 `Runtime`/`Presentation` 供渲染，`Run(...)` 走框架 `DemoHarnessRunner` 返回四态结果与指标；`ApplyNetwork(...)` 运行时改网络；`CompareWorlds()` 计算预测/权威差异。

Unity 端**只依赖以上三者**，不引用任何 `ShooterClient*SyncController` 具体类型。

## 2. 下拉菜单绑定

```csharp
// 同步模式下拉：未实现的模式置灰（Implemented == false）。
foreach (var mode in ShooterAcceptanceCatalog.SyncModes)
{
    AddDropdownOption(mode.DisplayName, enabled: mode.Implemented, payload: mode);
}

// 网络环境下拉：从理想基线到压力场景，已按序排列。
foreach (var env in ShooterAcceptanceCatalog.NetworkEnvironments)
{
    AddDropdownOption(env.DisplayName, enabled: true, payload: env);
}
```

## 3. 点击「开始验收」

```csharp
var sync = _selectedSyncOption;       // ShooterAcceptanceSyncOption
var network = _selectedNetworkOption; // ShooterAcceptanceNetworkOption

// enableAuthoritativeWorld: 启动时勾选「对比模式」，会额外启动一个独立权威 World。
_session = ShooterAcceptanceLab.Create(in sync, in network, enableAuthoritativeWorld: _compareToggle.isOn);
```

得到 `_session` 后，Unity 有两种驱动方式：

- 离线一键校验：调用 `_session.Run()`，把返回的 `DemoHarnessRunResult.Status`（Completed/Degraded/Unsupported/Failed）与 `Metrics` 直接打到验收面板。
- 逐帧可视化：每个 Unity 帧调用 `_session.Controller.Tick(dt)`，再从 `_session.Presentation.ViewModel` 读取实体位置驱动渲染；若开了对比模式，同步调用 `_session.TickAuthoritativeWorld(dt)` 保持权威 World 对齐。

## 4. 运行时调节网络环境（无需重建会话）

验收的核心诉求之一是「边跑边调网络」。会话启动后，UI 滑条/下拉可随时改网络：

```csharp
// 方式 A：切到目录里的另一个预设。
_session.ApplyNetwork(ShooterAcceptanceCatalog.NetworkEnvironments[3].Profile, "Cross Region");

// 方式 B：用滑条拼一个自定义网络（延迟/抖动/丢包/乱序/带宽）。
var tuned = new NetworkConditionProfile(
    baseLatencyMs: _latencySlider.Value,
    jitterMs: _jitterSlider.Value,
    packetLossRate: _lossSlider.Value,
    reorderRate: _reorderSlider.Value,
    bandwidthKbps: 0);
_session.ApplyNetwork(tuned, $"Tuned {_latencySlider.Value}ms");

// 下一次 Run() 或逐帧 Step 立即生效。
var result = _session.Run();
```

`NetworkProfile` 是会话上的可变属性，`ApplyNetwork` 只改值、不重建 controller，因此调节过程平滑无中断。

## 5. 多 World 对比模式（可选，启动勾选）

当 `enableAuthoritativeWorld: true` 时，会话额外持有：

- [`Session.AuthoritativeWorld`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)：一个独立 [`ShooterBattleRuntimePort`](../Unity/Packages/com.abilitykit.demo.shooter.runtime/Runtime/Application/Runtime/ShooterBattleRuntimePort.cs:12)，纯前向模拟、无预测/回滚，作为「地面真相」。
- [`Session.AuthoritativePresentation`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)：独立的 `ShooterPresentationFacade`，权威 World 的快照已投影进去，Unity 可用**同一套** `ShooterViewBinder` 渲染第二个视口。

Unity 渲染两个并排视口：

```csharp
// 左视口：客户端预测 World（带预测/回滚，受网络环境影响）。
_viewBinderLeft.Bind(_session.Presentation);

// 右视口：权威 World（地面真相，不受网络影响）。
if (_session.HasAuthoritativeWorld)
{
    _viewBinderRight.Bind(_session.AuthoritativePresentation);
}
```

### 差异高亮

```csharp
var comparison = _session.CompareWorlds();
// comparison.Divergences: 每个玩家的 (ClientX,ClientY) vs (AuthorityX,AuthorityY) 与 Distance
// comparison.MaxDistance: 当前最大偏差，可驱动顶部偏差条
foreach (var d in comparison.Divergences)
{
    if (d.Distance > _tolerance)
    {
        _overlay.Highlight(d.PlayerId, (float)d.Distance);
    }
}
```

`CompareWorlds()` 返回 [`ShooterWorldComparison`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)，内含每实体 [`ShooterWorldDivergence`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1)。预测误差越大、偏差越明显，直观展示回滚/预测在网络劣化下的表现。

> 对比模式默认关闭（`HasAuthoritativeWorld == false`），`CompareWorlds()` 返回空结果，不影响纯客户端演示。

## 6. 全矩阵冒烟

验收面板可提供「跑全部组合」按钮，直接调用：

```csharp
var results = ShooterAcceptanceLab.RunCatalogMatrix();
// results: 每个 (实现模式 × 网络环境) 一行四态结果，等价于手动点完整张验收矩阵。
```

该方法与 [`ShooterAcceptanceLabTests`](../src/AbilityKit.Demo.Shooter.Runtime.Tests/Client/ShooterAcceptanceLabTests.cs:1) 中 `RunCatalogMatrixCoversEveryImplementedModeAndNetwork` 用例同源，保证 Unity 看到的结论与 CI 一致。

## 7. 扩展新同步模式的接入点

新增模式时，纯 C# 层改两处即可，Unity 端零改动：

1. 在 [`ShooterClientSyncControllerFactory.Create`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterClientSyncControllerFactory.cs:37) 增加对应 `case`。
2. 在 [`ShooterAcceptanceCatalog.SyncModes`](../Unity/Packages/com.abilitykit.demo.shooter.view.runtime/Runtime/Client/Synchronization/ShooterAcceptanceLab.cs:1) 增加一项并置 `implemented: true`。

下拉菜单、Session 装配、矩阵冒烟、对比模式会自动纳入该模式。
