#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.DemoHarness;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// 验收验证可选择的同步模式。封装 <see cref="NetworkSyncModel"/> 与面向人的显示名，
    /// 让 Unity 外壳无需了解框架内部即可展示支持的同步策略。
    /// </summary>
    public readonly struct ShooterAcceptanceSyncOption
    {
        public ShooterAcceptanceSyncOption(NetworkSyncModel model, string displayName, bool implemented)
        {
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));

            Model = model;
            DisplayName = displayName;
            Implemented = implemented;
        }

        /// <summary>该选项选择的框架无关同步模型。</summary>
        public NetworkSyncModel Model { get; }

        /// <summary>显示在 Unity 验收外壳中的标签。</summary>
        public string DisplayName { get; }

        /// <summary>
        /// 当 Shooter 客户端已经落地该模型对应控制器时为 true。Unity 外壳可将未实现选项置灰；
        /// 使用未实现模型调用 <see cref="ShooterAcceptanceLab"/> 会抛出异常。
        /// </summary>
        public bool Implemented { get; }

        /// <summary>该模型对应的完整框架同步档案（播放、快照、校验策略）。</summary>
        public NetworkSyncProfile Profile => NetworkSyncProfiles.FromCompatibilityModel(Model);
    }

    /// <summary>
    /// 验收验证可选择的模拟网络环境。封装 <see cref="NetworkConditionProfile"/> 预设、稳定 id 与标签。
    /// </summary>
    public readonly struct ShooterAcceptanceNetworkOption
    {
        public ShooterAcceptanceNetworkOption(string id, string displayName, NetworkConditionProfile profile)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));

            Id = id;
            DisplayName = displayName;
            Profile = profile;
        }

        /// <summary>稳定标识，适合持久化或命令行选择。</summary>
        public string Id { get; }

        /// <summary>显示在 Unity 验收外壳中的标签。</summary>
        public string DisplayName { get; }

        /// <summary>该选项应用的模拟网络条件。</summary>
        public NetworkConditionProfile Profile { get; }
    }

    /// <summary>
    /// Shooter 验收提供的固定同步模式与网络环境菜单。Unity 外壳直接绑定这些列表，
    /// 确保可选项与纯 C# 层实际可构建、可运行的能力保持一致。
    /// </summary>
    public static class ShooterAcceptanceCatalog
    {
        /// <summary>验收提供的同步模式，只有已实现模式可运行。</summary>
        public static IReadOnlyList<ShooterAcceptanceSyncOption> SyncModes { get; } = new[]
        {
            new ShooterAcceptanceSyncOption(NetworkSyncModel.PredictRollback, "Predict + Rollback", implemented: true),
            new ShooterAcceptanceSyncOption(NetworkSyncModel.AuthoritativeInterpolation, "Authoritative Interpolation", implemented: true),
            new ShooterAcceptanceSyncOption(NetworkSyncModel.HybridHeroPrediction, "Hybrid (Predict + Interpolation)", implemented: true)
        };

        /// <summary>模拟网络环境，从理想基线到压力场景排序。</summary>
        public static IReadOnlyList<ShooterAcceptanceNetworkOption> NetworkEnvironments { get; } = new[]
        {
            new ShooterAcceptanceNetworkOption("ideal", "Ideal (0ms)", NetworkConditionProfile.Ideal),
            new ShooterAcceptanceNetworkOption("lan", "LAN (5ms)", NetworkConditionProfile.Lan),
            new ShooterAcceptanceNetworkOption("mobile4g", "Mobile 4G (60ms)", NetworkConditionProfile.Mobile4G),
            new ShooterAcceptanceNetworkOption("crossregion", "Cross Region (150ms)", NetworkConditionProfile.CrossRegion),
            new ShooterAcceptanceNetworkOption("poorwifi", "Poor WiFi (80ms, loss)", NetworkConditionProfile.PoorWifi),
            new ShooterAcceptanceNetworkOption("limitedbw", "Limited BW (128 Kbps)", NetworkConditionProfile.LimitedBandwidth)
        };
    }

    /// <summary>
    /// 指定帧下客户端预测世界与权威世界的逐实体偏差。Unity 外壳可并排渲染两个世界，
    /// 并高亮 <see cref="Distance"/> 超过容差的实体，让预测误差可视化。
    /// </summary>
    public readonly struct ShooterWorldDivergence
    {
        public ShooterWorldDivergence(int playerId, float clientX, float clientY, float authorityX, float authorityY)
        {
            PlayerId = playerId;
            ClientX = clientX;
            ClientY = clientY;
            AuthorityX = authorityX;
            AuthorityY = authorityY;
            Distance = Math.Sqrt(((clientX - authorityX) * (clientX - authorityX))
                                 + ((clientY - authorityY) * (clientY - authorityY)));
        }

        public int PlayerId { get; }

        public float ClientX { get; }

        public float ClientY { get; }

        public float AuthorityX { get; }

        public float AuthorityY { get; }

        /// <summary>预测位置与权威位置之间的欧氏距离。</summary>
        public double Distance { get; }
    }

    /// <summary>
    /// 客户端预测世界相对权威世界漂移程度的快照。由 <see cref="ShooterAcceptanceSession.CompareWorlds"/> 生成；
    /// Unity 外壳读取 <see cref="MaxDistance"/> / <see cref="Divergences"/> 来驱动偏差叠加显示。
    /// </summary>
    public readonly struct ShooterWorldComparison
    {
        public ShooterWorldComparison(int clientFrame, int authorityFrame, IReadOnlyList<ShooterWorldDivergence> divergences)
        {
            ClientFrame = clientFrame;
            AuthorityFrame = authorityFrame;
            Divergences = divergences;

            var max = 0d;
            for (var i = 0; i < divergences.Count; i++)
            {
                if (divergences[i].Distance > max)
                {
                    max = divergences[i].Distance;
                }
            }

            MaxDistance = max;
        }

        public int ClientFrame { get; }

        public int AuthorityFrame { get; }

        public IReadOnlyList<ShooterWorldDivergence> Divergences { get; }

        /// <summary>本次比较中观测到的最大逐实体位置偏差。</summary>
        public double MaxDistance { get; }
    }

    /// <summary>
    /// 已完整装配、可运行的 Shooter 验收会话：包含 runtime port、表现门面、已选择同步控制器与 demo-harness carrier，
    /// 并已启动到可推进状态。同一个对象既可由 xUnit 无头驱动，也可由 Unity 外壳可视化驱动；
    /// 外壳读取 <see cref="Runtime"/> / <see cref="Presentation"/> 渲染已验证状态。
    /// 当 <see cref="HasAuthoritativeWorld"/> 为 true 时，会并行运行独立权威模拟，便于两个世界并排渲染与比较。
    /// </summary>
    public sealed class ShooterAcceptanceSession : IDisposable
    {
        public const int DefaultStepCount = 120;

        private NetworkConditionProfile _networkProfile;
        private string _networkName;

        private readonly ShooterBattleWorldSession _runtimeWorld;
        private readonly ShooterBattleWorldSession? _authoritativeWorldSession;
        private ShooterAuthoritativeComparisonDriver? _authoritativeDriver;
        private bool _disposed;

        internal ShooterAcceptanceSession(
            ShooterBattleWorldSession runtimeWorld,
            ShooterPresentationFacade presentation,
            IShooterClientSyncController controller,
            ISyncDemoCarrier carrier,
            NetworkSyncModel syncModel,
            NetworkConditionProfile networkProfile,
            string networkName,
            ShooterBattleWorldSession? authoritativeWorldSession,
            ShooterPresentationFacade? authoritativePresentation,
            int networkSeed = 0)
        {
            _runtimeWorld = runtimeWorld ?? throw new ArgumentNullException(nameof(runtimeWorld));
            _authoritativeWorldSession = authoritativeWorldSession;
            Runtime = runtimeWorld.Runtime;
            Presentation = presentation;
            Controller = controller;
            Carrier = carrier;
            SyncModel = syncModel;
            _networkProfile = networkProfile;
            _networkName = networkName;
            AuthoritativeWorld = authoritativeWorldSession?.Runtime;
            AuthoritativePresentation = authoritativePresentation;
            if (AuthoritativeWorld != null)
            {
                _authoritativeDriver = new ShooterAuthoritativeComparisonDriver(
                    Controller,
                    AuthoritativeWorld,
                    AuthoritativePresentation,
                    _networkProfile,
                    networkSeed);
            }
        }

        /// <summary>控制器每步推进的确定性战斗运行时（客户端预测世界）。</summary>
        public ShooterBattleRuntimePort Runtime { get; }

        /// <summary>Unity 外壳用于渲染客户端预测会话的表现门面。</summary>
        public ShooterPresentationFacade Presentation { get; }

        /// <summary>当前选择的同步控制器（预测回滚、插值等）。</summary>
        public IShooterClientSyncController Controller { get; }

        /// <summary>将控制器桥接到框架 DemoHarness 的 Carrier。</summary>
        public ISyncDemoCarrier Carrier { get; }

        public NetworkSyncModel SyncModel { get; }

        /// <summary>
        /// 当前生效的网络环境。可通过 <see cref="ApplyNetwork"/> 修改，
        /// 让 Unity 外壳在会话持续推进时实时调节延迟、丢包与抖动。
        /// </summary>
        public NetworkConditionProfile NetworkProfile => _networkProfile;

        /// <summary>当前网络环境的可读标签。</summary>
        public string NetworkName => _networkName;

        /// <summary>存在用于并排比较的独立权威世界时为 true。</summary>
        public bool HasAuthoritativeWorld => AuthoritativeWorld != null;

        /// <summary>
        /// 可选的独立权威模拟（无预测，仅纯推进）。启动时未启用比较模式则为 null。
        /// Unity 外壳将其作为“真实基准”世界渲染。
        /// </summary>
        public ShooterBattleRuntimePort? AuthoritativeWorld { get; }

        /// <summary>
        /// 可选的权威世界表现门面。比较模式禁用时为 null，
        /// 让外壳可以复用现有 view binder 渲染第二个世界。
        /// </summary>
        public ShooterPresentationFacade? AuthoritativePresentation { get; }

        /// <summary>Carrier 侧网络中间件链路的实时统计。</summary>
        public NetworkConditioningStats? CarrierNetworkStats => _authoritativeDriver?.Stats;

        /// <summary>最近一次带网络条件的权威快照应用到控制器后的结果。</summary>
        public ShooterSnapshotApplyResult? LastCarrierSnapshotApplyResult => _authoritativeDriver?.LastApplyResult;

        /// <summary>最近一次通过 Carrier 链路发布权威快照时使用的时间锚点。</summary>
        public SyncTimeAnchor LastCarrierTimeAnchor => _authoritativeDriver?.LastCarrierTimeAnchor ?? default;

        /// <summary>从权威世界采集到的当前 LagComp 历史遥测。</summary>
        public ShooterLagCompensationTelemetry? LagCompensationTelemetry => _authoritativeDriver?.Telemetry;

        /// <summary>最近一次服务端回溯命中验证结果。</summary>
        public ShooterLagCompensationEvaluation? LastLagCompensationEvaluation => _authoritativeDriver?.LastLagCompensationEvaluation;

        /// <summary>
        /// Uses the authoritative world history to validate a client-reported shot against rewound
        /// player hitboxes. Returns false when comparison mode is disabled or the shot is rejected.
        /// </summary>
        public bool TryEvaluateLagCompensationShot(
            in ShooterLagCompensationShot shot,
            out ShooterLagCompensationEvaluation evaluation)
        {
            if (_authoritativeDriver == null)
            {
                evaluation = default;
                return false;
            }

            return _authoritativeDriver.TryEvaluateShot(in shot, out evaluation);
        }
 
        /// <summary>
        /// 在不重建会话的情况下实时调节网络环境。下一次 <see cref="Run"/> 或单步推进会使用新的 profile。
        /// 可接收目录预设，也可接收由运行时滑条构建的临时 <see cref="NetworkConditionProfile"/>。
        /// </summary>
        public void ApplyNetwork(NetworkConditionProfile profile, string? displayName = null)
        {
            _networkProfile = profile;
            _networkName = string.IsNullOrWhiteSpace(displayName) ? DescribeNetwork(profile) : displayName!;
            _authoritativeDriver?.ApplyNetwork(_networkProfile);
        }

        /// <summary>
        /// 在当前网络 profile 下通过框架 DemoHarness 推进会话，并返回包含指标的四态运行结果。
        /// 当存在权威世界时，它会按锁步推进，确保两个世界保持帧对齐以便比较。
        /// 复用 <see cref="DemoHarnessRunner"/> 意味着验收路径会覆盖无头测试套件已经验证过的同一套机制。
        /// </summary>
        public DemoHarnessRunResult Run(int stepCount = DefaultStepCount, float deltaSeconds = 1f / 30f, int seed = 0)
        {
            if (stepCount <= 0) throw new ArgumentOutOfRangeException(nameof(stepCount));
            if (deltaSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            var scenario = new DemoHarnessScenario(
                name: $"Shooter {SyncModel} @ {_networkName}",
                syncModel: SyncModel,
                networkProfile: _networkProfile,
                carrierName: Carrier.CarrierName,
                stepCount: stepCount,
                deltaSeconds: deltaSeconds,
                seed: seed);

            var runner = new DemoHarnessRunner();
            var result = runner.Run(in scenario, Carrier);

            // 让权威世界在批量运行后与客户端世界保持同一帧范围，便于后续 CompareWorlds 对齐比较。
            AdvanceAuthoritativeWorld(result.Metrics.StepsRun, deltaSeconds);

            return result;
        }

        /// <summary>
        /// 推进权威世界一个 tick。Unity 外壳逐帧驱动会话（通过 Carrier.Step / Controller.Tick）时，
        /// 可调用它保持权威世界对齐；比较模式禁用时该操作为空。
        /// </summary>
        public void TickAuthoritativeWorld(float deltaSeconds)
        {
            AdvanceAuthoritativeWorld(1, deltaSeconds);
        }

        /// <summary>
        /// 比较客户端预测世界与权威世界，并返回逐实体位置偏差。比较模式禁用时返回空比较结果。
        /// Unity 外壳使用它来可视化高亮预测误差。
        /// </summary>
        public ShooterWorldComparison CompareWorlds()
        {
            if (AuthoritativeWorld == null)
            {
                return new ShooterWorldComparison(Runtime.CurrentFrame, 0, Array.Empty<ShooterWorldDivergence>());
            }

            var clientSnapshot = Runtime.GetSnapshot();
            var authoritySnapshot = AuthoritativeWorld.GetSnapshot();

            var divergences = new List<ShooterWorldDivergence>();
            var authorityPlayers = IndexByPlayerId(authoritySnapshot.Players);
            for (var i = 0; i < clientSnapshot.Players.Length; i++)
            {
                var client = clientSnapshot.Players[i];
                if (authorityPlayers.TryGetValue(client.PlayerId, out var authority))
                {
                    divergences.Add(new ShooterWorldDivergence(
                        client.PlayerId, client.X, client.Y, authority.X, authority.Y));
                }
            }

            return new ShooterWorldComparison(
                clientSnapshot.Frame,
                authoritySnapshot.Frame,
                divergences);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _authoritativeDriver = null;
            _authoritativeWorldSession?.Dispose();
            _runtimeWorld.Dispose();
        }

        private void AdvanceAuthoritativeWorld(int stepCount, float deltaSeconds)
        {
            _authoritativeDriver?.Advance(stepCount, deltaSeconds);
        }

        private static Dictionary<int, ShooterPlayerSnapshot> IndexByPlayerId(ShooterPlayerSnapshot[] players)
        {
            var map = new Dictionary<int, ShooterPlayerSnapshot>(players.Length);
            for (var i = 0; i < players.Length; i++)
            {
                map[players[i].PlayerId] = players[i];
            }

            return map;
        }

        private static string DescribeNetwork(NetworkConditionProfile profile)
        {
            return $"{profile.BaseLatencyMs}ms/{profile.JitterMs}ms jitter";
        }
    }

    /// <summary>
    /// Shooter 验收会话的一站式工厂。给定同步模式与网络环境后，装配完整纯 C# 栈
    /// （runtime + presentation + controller + carrier）、启动对局，并返回可运行的 <see cref="ShooterAcceptanceSession"/>。
    /// 这是 Unity 验收外壳依赖的单一接缝：选择模式、选择网络、可选启用权威比较世界，然后调用 Create、推进并观察。
    /// </summary>
    public static class ShooterAcceptanceLab
    {
        public const int DefaultTickRate = 30;

        /// <summary>
        /// 根据显式同步模型与网络 profile 构建可运行会话。
        /// </summary>
        /// <param name="syncModel">要验证的同步策略，必须是已实现模型。</param>
        /// <param name="networkProfile">模拟网络环境。</param>
        /// <param name="networkName">可选网络标签；默认使用 profile 延迟描述。</param>
        /// <param name="tickRate">模拟 tick rate，也会写入 start payload。</param>
        /// <param name="players">可选玩家名单；默认生成两个玩家。</param>
        /// <param name="matchId">可选对局 id；默认从模型派生验收 id。</param>
        /// <param name="randomSeed">战斗运行时的确定性随机种子。</param>
        /// <param name="interpolationConfig">插值模型的可选配置。</param>
        /// <param name="enableAuthoritativeWorld">
        /// 为 true 时，在客户端世界旁启动独立权威模拟，让 Unity 外壳可以同时渲染与比较二者。默认 false（仅客户端世界）。
        /// </param>
        /// <param name="networkStats">可选的实时网络统计源，会暴露给 harness。</param>
        /// <param name="remoteJitter">可选的实时远端抖动源。</param>
        /// <param name="acceptedHits">可选的命中接受计数源。</param>
        /// <param name="rejectedHits">可选的命中拒绝计数源。</param>
        public static ShooterAcceptanceSession Create(
            NetworkSyncModel syncModel,
            NetworkConditionProfile networkProfile,
            string? networkName = null,
            int tickRate = DefaultTickRate,
            IReadOnlyList<ShooterStartPlayer>? players = null,
            string? matchId = null,
            int randomSeed = 0,
            InterpolationConfig? interpolationConfig = null,
            bool enableAuthoritativeWorld = false,
            Func<NetworkConditioningStats>? networkStats = null,
            Func<double>? remoteJitter = null,
            Func<long>? acceptedHits = null,
            Func<long>? rejectedHits = null)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));

            var start = BuildStartPayload(matchId, tickRate, randomSeed, players, syncModel);
            var worldHost = new ShooterWorldHost();
            var runtimeWorld = ShooterBattleWorldSession.Create($"{start.MatchId}-client", worldHost);
            ShooterBattleWorldSession? authoritativeWorldSession = null;

            try
            {
                var runtime = runtimeWorld.Runtime;
                var presentation = new ShooterPresentationFacade();
                var controller = ShooterClientSyncControllerFactory.Create(
                    syncModel, runtime, presentation, tickRate, decoder: null, gateway: null, interpolationConfig);

                if (!controller.StartGame(in start))
                {
                    throw new InvalidOperationException(
                        $"Shooter acceptance session failed to start the '{syncModel}' controller.");
                }

                ISyncDemoCarrier carrier = syncModel == NetworkSyncModel.AuthoritativeInterpolation
                    ? new ShooterInterpolationDemoHarnessCarrier(
                        controller, networkStats, remoteJitter, acceptedHits, rejectedHits)
                    : syncModel == NetworkSyncModel.HybridHeroPrediction
                        ? new ShooterHybridDemoHarnessCarrier(
                            controller, networkStats, remoteJitter, acceptedHits, rejectedHits)
                        : new ShooterDemoHarnessCarrier(
                            controller, networkStats, remoteJitter, acceptedHits, rejectedHits);

                ShooterPresentationFacade? authoritativePresentation = null;
                if (enableAuthoritativeWorld)
                {
                    authoritativeWorldSession = ShooterBattleWorldSession.Create($"{start.MatchId}-authority", worldHost);
                    authoritativePresentation = new ShooterPresentationFacade();
                    if (!authoritativeWorldSession.Runtime.StartGame(in start))
                    {
                        throw new InvalidOperationException(
                            $"Shooter acceptance session failed to start the '{syncModel}' authoritative world.");
                    }

                    var initialSnapshot = authoritativeWorldSession.Runtime.GetSnapshot();
                    authoritativePresentation.ApplyLocalPredictionSnapshot(in initialSnapshot);
                }

                return new ShooterAcceptanceSession(
                    runtimeWorld,
                    presentation,
                    controller,
                    carrier,
                    syncModel,
                    networkProfile,
                    string.IsNullOrWhiteSpace(networkName) ? DescribeNetwork(networkProfile) : networkName!,
                    authoritativeWorldSession,
                    authoritativePresentation,
                    randomSeed);
            }
            catch
            {
                authoritativeWorldSession?.Dispose();
                runtimeWorld.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 由目录菜单选项直接构建会话的重载。
        /// </summary>
        public static ShooterAcceptanceSession Create(
            in ShooterAcceptanceSyncOption sync,
            in ShooterAcceptanceNetworkOption network,
            int tickRate = DefaultTickRate,
            InterpolationConfig? interpolationConfig = null,
            bool enableAuthoritativeWorld = false)
        {
            if (!sync.Implemented)
            {
                throw new NotSupportedException(
                    $"Sync mode '{sync.DisplayName}' ({sync.Model}) is not implemented yet and cannot be run.");
            }

            return Create(
                sync.Model,
                network.Profile,
                network.DisplayName,
                tickRate,
                interpolationConfig: interpolationConfig,
                enableAuthoritativeWorld: enableAuthoritativeWorld);
        }

        /// <summary>
        /// 将每个已实现同步模式运行在每个目录化网络环境上，并返回包含聚合摘要的四态批量结果。
        /// 这是逐个点击完整 Unity 验收矩阵的无头等价路径。
        /// </summary>
        public static DemoHarnessBatchResult RunCatalogMatrix(
            int stepCount = ShooterAcceptanceSession.DefaultStepCount,
            float deltaSeconds = 1f / 30f,
            int seed = 0)
        {
            var results = new List<DemoHarnessRunResult>();
            foreach (var sync in ShooterAcceptanceCatalog.SyncModes)
            {
                if (!sync.Implemented)
                {
                    continue;
                }

                foreach (var network in ShooterAcceptanceCatalog.NetworkEnvironments)
                {
                    using var session = Create(sync, network);
                    results.Add(session.Run(stepCount, deltaSeconds, seed));
                }
            }

            return new DemoHarnessBatchResult(results.AsReadOnly());
        }

        private static ShooterStartGamePayload BuildStartPayload(
            string? matchId,
            int tickRate,
            int randomSeed,
            IReadOnlyList<ShooterStartPlayer>? players,
            NetworkSyncModel syncModel)
        {
            var id = string.IsNullOrWhiteSpace(matchId)
                ? $"acceptance-{syncModel}".ToLowerInvariant()
                : matchId!;

            var roster = (players == null || players.Count == 0)
                ? DefaultPlayers()
                : ToArray(players);

            return new ShooterStartGamePayload(id, tickRate, randomSeed, roster);
        }

        private static ShooterStartPlayer[] DefaultPlayers()
        {
            return new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 4f, 0f)
            };
        }

        private static ShooterStartPlayer[] ToArray(IReadOnlyList<ShooterStartPlayer> players)
        {
            var buffer = new ShooterStartPlayer[players.Count];
            for (var i = 0; i < players.Count; i++)
            {
                buffer[i] = players[i];
            }

            return buffer;
        }

        private static string DescribeNetwork(NetworkConditionProfile profile)
        {
            return $"{profile.BaseLatencyMs}ms/{profile.JitterMs}ms jitter";
        }
    }
}
