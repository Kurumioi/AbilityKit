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
    /// A selectable synchronization mode for acceptance validation. Wraps a
    /// <see cref="NetworkSyncModel"/> with a human-friendly label so a Unity shell can
    /// present the supported sync strategies without knowing the framework internals.
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

        /// <summary>The framework-neutral sync model this option selects.</summary>
        public NetworkSyncModel Model { get; }

        /// <summary>Label shown in the Unity acceptance shell.</summary>
        public string DisplayName { get; }

        /// <summary>
        /// True when the Shooter client has a landed controller for this model. The Unity shell
        /// can grey out unimplemented options; calling <see cref="ShooterAcceptanceLab"/> with an
        /// unimplemented model throws.
        /// </summary>
        public bool Implemented { get; }

        /// <summary>The full framework sync profile (playback/snapshot/validation policies) for this model.</summary>
        public NetworkSyncProfile Profile => NetworkSyncProfiles.FromCompatibilityModel(Model);
    }

    /// <summary>
    /// A selectable simulated network environment for acceptance validation. Wraps a
    /// <see cref="NetworkConditionProfile"/> preset with a stable id and label.
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

        /// <summary>Stable identifier, suitable for persistence or command-line selection.</summary>
        public string Id { get; }

        /// <summary>Label shown in the Unity acceptance shell.</summary>
        public string DisplayName { get; }

        /// <summary>The simulated network condition this option applies.</summary>
        public NetworkConditionProfile Profile { get; }
    }

    /// <summary>
    /// The fixed menu of sync modes and network environments offered for Shooter acceptance.
    /// The Unity shell binds directly to these lists so the selectable surface stays in sync with
    /// what the pure-C# layer can actually build and run.
    /// </summary>
    public static class ShooterAcceptanceCatalog
    {
        /// <summary>Synchronization modes offered for acceptance. Only implemented modes are runnable.</summary>
        public static IReadOnlyList<ShooterAcceptanceSyncOption> SyncModes { get; } = new[]
        {
            new ShooterAcceptanceSyncOption(NetworkSyncModel.PredictRollback, "Predict + Rollback", implemented: true),
            new ShooterAcceptanceSyncOption(NetworkSyncModel.AuthoritativeInterpolation, "Authoritative Interpolation", implemented: true),
            new ShooterAcceptanceSyncOption(NetworkSyncModel.HybridHeroPrediction, "Hybrid (Predict + Interpolation)", implemented: true)
        };

        /// <summary>Simulated network environments, ordered from ideal baseline to stress case.</summary>
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
    /// Per-entity divergence between the client predicted world and the authoritative world at a
    /// given frame. The Unity shell renders both worlds side by side and can highlight entities
    /// whose <see cref="Distance"/> exceeds a tolerance to make prediction error visible.
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

        /// <summary>Euclidean distance between the predicted and authoritative position.</summary>
        public double Distance { get; }
    }

    /// <summary>
    /// A snapshot of how far the client predicted world has drifted from the authoritative world.
    /// Produced by <see cref="ShooterAcceptanceSession.CompareWorlds"/>; the Unity shell reads
    /// <see cref="MaxDistance"/> / <see cref="Divergences"/> to drive a divergence overlay.
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

        /// <summary>Largest per-entity positional divergence observed this comparison.</summary>
        public double MaxDistance { get; }
    }

    /// <summary>
    /// A fully assembled, runnable Shooter acceptance session: runtime port, presentation facade,
    /// the chosen sync controller and a demo-harness carrier, all started and ready to step.
    /// The same object is driven headlessly from xUnit and visually from a Unity shell; the shell
    /// reads <see cref="Runtime"/> / <see cref="Presentation"/> to render the validated state.
    /// When <see cref="HasAuthoritativeWorld"/> is set an independent authoritative simulation runs
    /// alongside the client so the two worlds can be rendered and compared side by side.
    /// </summary>
    public sealed class ShooterAcceptanceSession
    {
        public const int DefaultStepCount = 120;

        private NetworkConditionProfile _networkProfile;
        private string _networkName;

        internal ShooterAcceptanceSession(
            ShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            IShooterClientSyncController controller,
            ISyncDemoCarrier carrier,
            NetworkSyncModel syncModel,
            NetworkConditionProfile networkProfile,
            string networkName,
            ShooterBattleRuntimePort? authoritativeWorld,
            ShooterPresentationFacade? authoritativePresentation)
        {
            Runtime = runtime;
            Presentation = presentation;
            Controller = controller;
            Carrier = carrier;
            SyncModel = syncModel;
            _networkProfile = networkProfile;
            _networkName = networkName;
            AuthoritativeWorld = authoritativeWorld;
            AuthoritativePresentation = authoritativePresentation;
        }

        /// <summary>Deterministic battle runtime the controller advances each step (client predicted world).</summary>
        public ShooterBattleRuntimePort Runtime { get; }

        /// <summary>Presentation facade the Unity shell observes to render the client predicted session.</summary>
        public ShooterPresentationFacade Presentation { get; }

        /// <summary>The selected sync controller (predict-rollback, interpolation, ...).</summary>
        public IShooterClientSyncController Controller { get; }

        /// <summary>Carrier bridging the controller to the framework demo harness.</summary>
        public ISyncDemoCarrier Carrier { get; }

        public NetworkSyncModel SyncModel { get; }

        /// <summary>
        /// The currently active network environment. Mutated by <see cref="ApplyNetwork"/> so the
        /// Unity shell can tune latency/loss/jitter live while the session keeps stepping.
        /// </summary>
        public NetworkConditionProfile NetworkProfile => _networkProfile;

        /// <summary>Human-friendly label of the selected network environment.</summary>
        public string NetworkName => _networkName;

        /// <summary>True when an independent authoritative world is running for side-by-side comparison.</summary>
        public bool HasAuthoritativeWorld => AuthoritativeWorld != null;

        /// <summary>
        /// Optional independent authoritative simulation (no prediction, pure advance). Null when
        /// comparison mode is disabled at startup. The Unity shell renders this as the "ground truth" world.
        /// </summary>
        public ShooterBattleRuntimePort? AuthoritativeWorld { get; }

        /// <summary>
        /// Optional presentation facade projecting the authoritative world. Null when comparison
        /// mode is disabled. Lets the shell reuse the existing view binder for the second world.
        /// </summary>
        public ShooterPresentationFacade? AuthoritativePresentation { get; }

        /// <summary>
        /// Live-tunes the network environment without rebuilding the session. The next
        /// <see cref="Run"/> / step uses the new profile. Accepts either a catalog preset or an
        /// ad-hoc <see cref="NetworkConditionProfile"/> built from runtime sliders.
        /// </summary>
        public void ApplyNetwork(NetworkConditionProfile profile, string? displayName = null)
        {
            _networkProfile = profile;
            _networkName = string.IsNullOrWhiteSpace(displayName) ? DescribeNetwork(profile) : displayName!;
        }

        /// <summary>
        /// Steps the session through the framework demo harness under the current network profile
        /// and returns the four-state run result with metrics. When an authoritative world is
        /// attached it is advanced in lockstep so the two stay frame-aligned for comparison.
        /// Reusing <see cref="DemoHarnessRunner"/> means the acceptance path exercises the exact
        /// machinery already covered by the headless test suite.
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

            // Keep the authoritative world frame-aligned with the client world after the run so a
            // subsequent CompareWorlds reflects the same step horizon.
            AdvanceAuthoritativeWorld(result.Metrics.StepsRun, deltaSeconds);

            return result;
        }

        /// <summary>
        /// Advances the authoritative world one tick. Use this when the Unity shell drives the
        /// session frame-by-frame (via Carrier.Step / Controller.Tick) and wants the authoritative
        /// world to stay aligned. No-op when comparison mode is disabled.
        /// </summary>
        public void TickAuthoritativeWorld(float deltaSeconds)
        {
            AdvanceAuthoritativeWorld(1, deltaSeconds);
        }

        /// <summary>
        /// Compares the client predicted world against the authoritative world and returns the
        /// per-entity positional divergences. Returns an empty comparison when comparison mode is
        /// disabled. The Unity shell uses this to highlight prediction error visually.
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

        private void AdvanceAuthoritativeWorld(int stepCount, float deltaSeconds)
        {
            if (AuthoritativeWorld == null || stepCount <= 0)
            {
                return;
            }

            for (var i = 0; i < stepCount; i++)
            {
                AuthoritativeWorld.Tick(deltaSeconds);
            }

            // Project the authoritative world through its own presentation facade so the Unity
            // shell can bind the second world with the same view pipeline as the client world.
            if (AuthoritativePresentation != null)
            {
                var authoritySnapshot = AuthoritativeWorld.GetSnapshot();
                AuthoritativePresentation.ApplyLocalPredictionSnapshot(in authoritySnapshot);
            }
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
    /// One-call factory for Shooter acceptance sessions. Given a sync mode and a network
    /// environment it assembles the full pure-C# stack (runtime + presentation + controller +
    /// carrier), starts the match and hands back a runnable <see cref="ShooterAcceptanceSession"/>.
    /// This is the single seam the Unity acceptance shell depends on: pick a mode, pick a network,
    /// optionally enable the authoritative comparison world, call Create, then step and observe.
    /// </summary>
    public static class ShooterAcceptanceLab
    {
        public const int DefaultTickRate = 30;

        /// <summary>
        /// Builds a runnable session for an explicit sync model and network profile.
        /// </summary>
        /// <param name="syncModel">The sync strategy to validate. Must be an implemented model.</param>
        /// <param name="networkProfile">The simulated network environment.</param>
        /// <param name="networkName">Optional label for the network; defaults to the profile latency.</param>
        /// <param name="tickRate">Simulation tick rate; also written into the start payload.</param>
        /// <param name="players">Optional roster; defaults to two spawned players.</param>
        /// <param name="matchId">Optional match id; defaults to an acceptance id derived from the model.</param>
        /// <param name="randomSeed">Deterministic seed for the battle runtime.</param>
        /// <param name="interpolationConfig">Optional config for the interpolation model.</param>
        /// <param name="enableAuthoritativeWorld">
        /// When true, an independent authoritative simulation is started alongside the client world
        /// so the Unity shell can render and compare both. Defaults to false (client world only).
        /// </param>
        /// <param name="networkStats">Optional live network stats source surfaced to the harness.</param>
        /// <param name="remoteJitter">Optional live remote-jitter source.</param>
        /// <param name="acceptedHits">Optional accepted-hit counter source.</param>
        /// <param name="rejectedHits">Optional rejected-hit counter source.</param>
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

            var runtime = new ShooterBattleRuntimePort();
            var presentation = new ShooterPresentationFacade();
            var controller = ShooterClientSyncControllerFactory.Create(
                syncModel, runtime, presentation, tickRate, decoder: null, gateway: null, interpolationConfig);

            var start = BuildStartPayload(matchId, tickRate, randomSeed, players, syncModel);
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

            ShooterBattleRuntimePort? authoritativeWorld = null;
            ShooterPresentationFacade? authoritativePresentation = null;
            if (enableAuthoritativeWorld)
            {
                authoritativeWorld = new ShooterBattleRuntimePort();
                authoritativePresentation = new ShooterPresentationFacade();
                authoritativeWorld.StartGame(in start);
                var initialSnapshot = authoritativeWorld.GetSnapshot();
                authoritativePresentation.ApplyLocalPredictionSnapshot(in initialSnapshot);
            }

            return new ShooterAcceptanceSession(
                runtime,
                presentation,
                controller,
                carrier,
                syncModel,
                networkProfile,
                string.IsNullOrWhiteSpace(networkName) ? DescribeNetwork(networkProfile) : networkName!,
                authoritativeWorld,
                authoritativePresentation);
        }

        /// <summary>
        /// Catalog-driven overload: build a session straight from selected menu options.
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
        /// Runs every implemented sync mode against every catalogued network environment and
        /// returns the four-state batch result with aggregated summary. This is the headless
        /// equivalent of clicking through the whole Unity acceptance matrix.
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
                    var session = Create(sync, network);
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
