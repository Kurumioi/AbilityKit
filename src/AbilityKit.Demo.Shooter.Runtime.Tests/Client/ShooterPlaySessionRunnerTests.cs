using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Demo.Shooter.View.PlayMode;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.LagCompensation;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class ShooterPlaySessionRunnerTests
{
    [Fact]
    public void FiveHundredMsLatencyDoesNotPullControlledPlayerBackAfterStopping()
    {
        const int tickRate = 30;
        const int controlledPlayerId = 1;
        var moveRightTicks = tickRate;
        var observeTicks = tickRate;
        var inputs = new List<ShooterHostFrameInput>(moveRightTicks + observeTicks);
        for (var i = 0; i < moveRightTicks; i++)
        {
            inputs.Add(new ShooterHostFrameInput(1f, 0f, 1f, 0f, false));
        }

        for (var i = 0; i < observeTicks; i++)
        {
            inputs.Add(new ShooterHostFrameInput(0f, 0f, 1f, 0f, false));
        }

        var input = new ScriptedInputSource(inputs.ToArray());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate,
            playerCount: 2,
            randomSeed: 33,
            controlledPlayerId,
            enableAuthoritativeWorld: true,
            latencyMs: 500,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: "500ms pull-back smoke"));

        var key = new ShooterViewEntityKey(ShooterViewEntityKind.Player, controlledPlayerId);
        float? previousObservedX = null;
        float? stoppedBaselineX = null;
        var maxPullBackAfterStop = 0f;
        var maxDriftAfterStop = 0f;
        for (var tick = 0; tick < moveRightTicks + observeTicks; tick++)
        {
            runner.Tick(1f / tickRate);
            Assert.NotEmpty(view.Frames);
            Assert.True(TryGetTransformX(view.Frames[^1].ClientBatch, key, out var x));

            if (tick == moveRightTicks)
            {
                stoppedBaselineX = x;
            }

            if (tick >= moveRightTicks && previousObservedX.HasValue)
            {
                maxPullBackAfterStop = Math.Max(maxPullBackAfterStop, previousObservedX.Value - x);
                if (stoppedBaselineX.HasValue)
                {
                    maxDriftAfterStop = Math.Max(maxDriftAfterStop, Math.Abs(x - stoppedBaselineX.Value));
                }
            }

            previousObservedX = x;
        }

        Assert.True(maxPullBackAfterStop <= 0.001f, $"Controlled player was pulled back by {maxPullBackAfterStop:0.0000} units after input stopped under 500ms latency.");
        Assert.True(maxDriftAfterStop <= 0.001f, $"Controlled player drifted by {maxDriftAfterStop:0.0000} units after input stopped under 500ms latency.");
    }

    [Fact]
    public void RepeatedMovementUnderFiveHundredMsLatencyKeepsControlledPlayerReasonablyAlignedAfterDrain()
    {
        const int tickRate = 30;
        const int controlledPlayerId = 1;
        var inputs = new List<ShooterHostFrameInput>();
        AddRepeatedMovement(inputs, repeats: 3, activeTicks: 10, restTicks: 5);
        AddIdle(inputs, tickRate * 2);

        var input = new ScriptedInputSource(inputs.ToArray());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate,
            playerCount: 2,
            randomSeed: 41,
            controlledPlayerId,
            enableAuthoritativeWorld: true,
            latencyMs: 500,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: "500ms repeated movement alignment smoke"));

        for (var tick = 0; tick < inputs.Count; tick++)
        {
            runner.Tick(1f / tickRate);
        }

        var comparison = runner.Session!.CompareWorlds();
        var controlled = Assert.Single(comparison.Divergences, d => d.PlayerId == controlledPlayerId);
        Assert.True(controlled.Distance <= 0.05d, $"Controlled player diverged from authority by {controlled.Distance:0.0000} after repeated movement and latency drain. client=({controlled.ClientX:0.0000},{controlled.ClientY:0.0000}) authority=({controlled.AuthorityX:0.0000},{controlled.AuthorityY:0.0000})");
    }

    [Fact]
    public void StartAlignsRuntimeOptionsWithSelectedGameplayScenario()
    {
        var input = new ScriptedInputSource(Array.Empty<ShooterHostFrameInput>());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        var options = ShooterPlayModeSessionOptions.FromTemplate(
            ShooterAcceptanceCatalog.GetSyncTemplate("predict-rollback-authority"),
            ShooterSveltoGameplayScenarioCatalog.WaveSurvival);

        runner.Start(options);

        Assert.Equal(ShooterSveltoGameplayScenarioCatalog.WaveSurvival.Id, runner.Options.GameplayScenario.Id);
        Assert.Equal(30, runner.Options.TickRate);
        Assert.Equal(ShooterSveltoGameplayScenarioCatalog.WaveSurvival.ShooterCount, runner.Options.PlayerCount);
    }

    [Fact]
    public void TickAdvancesLocalTimeAnchorAndProjectsItToDiagnostics()
    {
        const int tickRate = 20;
        var input = new ScriptedInputSource(Array.Empty<ShooterHostFrameInput>());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate,
            playerCount: 2,
            randomSeed: 42,
            controlledPlayerId: 1,
            enableAuthoritativeWorld: true,
            latencyMs: 0,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: "local anchor smoke"));

        var actualTickRate = runner.Options.TickRate;
        runner.Tick(1f / actualTickRate);
        runner.Tick(1f / actualTickRate);

        var frame = Assert.Single(view.Frames, f => f.LocalTimeAnchor.LocalFrame == 1);
        Assert.Equal(2, runner.StepCount);
        Assert.Equal(1, runner.LastLocalTimeAnchor.LocalFrame);
        Assert.Equal(1L, runner.LastLocalTimeAnchor.TimelineTicks);
        Assert.Equal(1d / actualTickRate, runner.LastLocalTimeAnchor.ElapsedSeconds, precision: 6);

        var diagnostics = ShooterHostDiagnosticsProjector.ProjectFromFrame(in frame, previousTotalEvents: 0);
        Assert.Equal(frame.LocalTimeAnchor, diagnostics.LocalTimeAnchor);
    }

    [Fact]
    public void FireUnderFiveHundredMsLatencySpawnsPredictedAndAuthoritativeBulletsAtSameOrigin()
    {
        const int tickRate = 30;
        const int controlledPlayerId = 1;
        var inputs = new List<ShooterHostFrameInput>();
        AddRepeatedMovement(inputs, repeats: 2, activeTicks: 10, restTicks: 5);
        var fireFrameIndex = inputs.Count;
        inputs.Add(new ShooterHostFrameInput(0f, 0f, 1f, 0f, true));
        AddIdle(inputs, tickRate * 2);

        var input = new ScriptedInputSource(inputs.ToArray());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate,
            playerCount: 2,
            randomSeed: 43,
            controlledPlayerId,
            enableAuthoritativeWorld: true,
            latencyMs: 500,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: "500ms fire origin smoke"));

        ShooterEventSnapshot? predictedFire = null;
        ShooterEventSnapshot? authoritativeFire = null;
        ShooterSveltoPlayerComponent? predictedFirePlayer = null;
        ShooterSveltoPlayerComponent? authoritativeFirePlayer = null;
        for (var tick = 0; tick < inputs.Count; tick++)
        {
            runner.Tick(1f / tickRate);
            if (tick == fireFrameIndex)
            {
                predictedFirePlayer = TryGetPlayer(runner.Session!.Runtime.GetSnapshot(), controlledPlayerId, out var localPlayer)
                    ? localPlayer
                    : null;
                authoritativeFirePlayer = runner.Session!.AuthoritativeWorld != null && TryGetPlayer(runner.Session.AuthoritativeWorld.GetSnapshot(), controlledPlayerId, out var authorityPlayer)
                    ? authorityPlayer
                    : null;
            }

            predictedFire ??= TryGetFireEvent(runner.Session!.Runtime.GetSnapshot(), controlledPlayerId, out var localFire)
                ? localFire
                : null;
            authoritativeFire ??= runner.Session!.AuthoritativeWorld != null && TryGetFireEvent(runner.Session.AuthoritativeWorld.GetSnapshot(), controlledPlayerId, out var authorityFire)
                ? authorityFire
                : null;
        }

        var predictedFirePlayerValue = Assert.NotNull(predictedFirePlayer);
        var authoritativeFirePlayerValue = Assert.NotNull(authoritativeFirePlayer);
        var predicted = Assert.IsType<ShooterEventSnapshot>(predictedFire);
        var authoritative = Assert.IsType<ShooterEventSnapshot>(authoritativeFire);
        var dx = predicted.X - authoritative.X;
        var dy = predicted.Y - authoritative.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var playerDx = predictedFirePlayerValue.X - authoritativeFirePlayerValue.X;
        var playerDy = predictedFirePlayerValue.Y - authoritativeFirePlayerValue.Y;
        var playerDistance = Math.Sqrt(playerDx * playerDx + playerDy * playerDy);
        Assert.True(distance <= 0.05d, $"Predicted and authoritative bullet origins diverged by {distance:0.0000}. predicted=({predicted.X:0.0000},{predicted.Y:0.0000}) authority=({authoritative.X:0.0000},{authoritative.Y:0.0000}); fire-frame player predicted=({predictedFirePlayerValue.X:0.0000},{predictedFirePlayerValue.Y:0.0000}) authority=({authoritativeFirePlayerValue.X:0.0000},{authoritativeFirePlayerValue.Y:0.0000}) playerDistance={playerDistance:0.0000}");
    }

    [Fact]
    public void LagCompensationEvaluationIsProjectedToHostDiagnostics()
    {
        const int tickRate = 30;
        var input = new ScriptedInputSource(Array.Empty<ShooterHostFrameInput>());
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate,
            playerCount: 2,
            randomSeed: 44,
            controlledPlayerId: 1,
            enableAuthoritativeWorld: true,
            latencyMs: 0,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 1f,
            networkName: "lag compensation diagnostics smoke"));
        runner.Tick(1f / tickRate);
        var shot = new ShooterLagCompensationShot(
            shooterPlayerId: 1,
            originX: 0f,
            originY: 0f,
            directionX: 1f,
            directionY: 0f,
            maxDistance: 10f,
            rewindFrame: 1,
            serverReceiveFrame: 1);

        var accepted = runner.Session!.TryEvaluateLagCompensationShot(in shot, out var evaluation);
        runner.Tick(1f / tickRate);

        Assert.True(accepted);
        Assert.Equal(LagCompensationResultReason.Hit, evaluation.Reason);
        var frame = Assert.Single(view.Frames, f => f.LagCompensationEvaluation.HasValue);
        var frameEvaluation = Assert.IsType<ShooterLagCompensationEvaluation>(frame.LagCompensationEvaluation);
        Assert.Equal(evaluation, frameEvaluation);
        var diagnostics = ShooterHostDiagnosticsProjector.ProjectFromFrame(in frame, previousTotalEvents: 0);
        Assert.Equal(evaluation, diagnostics.LagCompensationEvaluation);
    }

    [Fact]
    public void PureStateRecoveryDiagnosticsAreProjectedFromHostFrame()
    {
        var frame = new ShooterHostPresentationFrame(
            ShooterSnapshotViewBatch.Empty,
            ShooterSnapshotViewBatch.Empty,
            false,
            controlledPlayerId: 1,
            worldScale: 1f,
            carrierNetworkStats: null,
            lastCarrierSnapshotApplyResult: ShooterSnapshotApplyResult.PureStateBaselineResyncNeeded,
            lastCarrierTimeAnchor: default,
            localTimeAnchor: default,
            lagCompensationTelemetry: null,
            lagCompensationEvaluation: null,
            remoteLatencyCompensationDiagnostics: default,
            crossLayerDiagnostics: new ShooterCrossLayerDiagnostics(
                frameworkPacketCount: 3,
                frameworkDispatchedSnapshotCount: 2,
                frameworkPackedSnapshotCount: 1,
                frameworkPureStateSnapshotCount: 1,
                lastFrameworkFrame: 18,
                lastFrameworkPayloadOpCode: 12002,
                lastFrameworkWorldId: "shooter:7001",
                hasSnapshotApplyResult: true,
                snapshotApplyResult: ShooterSnapshotApplyResult.PureStateBaselineResyncNeeded,
                hasRemoteLatencyResult: false,
                remoteInputDelayFrames: 0,
                remoteAuthoritativeFrameGap: 0,
                needsPureStateBaselineResync: true,
                lastPureStateAppliedFrame: 12,
                lastPureStateResyncFrame: 18),
            pureStateSyncDiagnostics: new ShooterPureStateSyncDiagnostics(
                ShooterPureStateSnapshotApplyResult.NeedsFullBaselineResync,
                sourceFrame: 18,
                sourceSnapshotKind: ShooterPureStateSnapshotKinds.Delta,
                sourceEntityCount: 2,
                sourceVisibilityHintCount: 1,
                sourceBaselineFrame: 12,
                sourceBaselineHash: 0x1234u,
                sourceStateHash: 0x5678u,
                sourceServerTick: 99L,
                appliedFrame: 12,
                appliedStateHash: 0x1234u,
                needsFullBaselineResync: true,
                lastResyncReason: ShooterPureStateResyncReason.BaselineMismatch,
                lastResyncFrame: 18,
                lastResyncStateHash: 0x5678u,
                lastIgnoredFrame: -1),
            needsPureStateBaselineResync: true,
            lastPureStateResyncReason: ShooterPureStateResyncReason.BaselineMismatch,
            lastPureStateAppliedFrame: 12,
            lastPureStateAppliedStateHash: 0x1234u,
            lastPureStateResyncFrame: 18,
            lastPureStateResyncStateHash: 0x5678u);

        var diagnostics = ShooterHostDiagnosticsProjector.ProjectFromFrame(in frame, previousTotalEvents: 0);

        Assert.True(diagnostics.NeedsPureStateBaselineResync);
        Assert.Equal(3, diagnostics.CrossLayerDiagnostics.FrameworkPacketCount);
        Assert.Equal(2, diagnostics.CrossLayerDiagnostics.FrameworkDispatchedSnapshotCount);
        Assert.Equal(1, diagnostics.CrossLayerDiagnostics.FrameworkPackedSnapshotCount);
        Assert.Equal(1, diagnostics.CrossLayerDiagnostics.FrameworkPureStateSnapshotCount);
        Assert.Equal(18, diagnostics.CrossLayerDiagnostics.LastFrameworkFrame);
        Assert.Equal(12002, diagnostics.CrossLayerDiagnostics.LastFrameworkPayloadOpCode);
        Assert.Equal("shooter:7001", diagnostics.CrossLayerDiagnostics.LastFrameworkWorldId);
        Assert.True(diagnostics.CrossLayerDiagnostics.HasSnapshotApplyResult);
        Assert.Equal(ShooterSnapshotApplyResult.PureStateBaselineResyncNeeded, diagnostics.CrossLayerDiagnostics.SnapshotApplyResult);
        Assert.True(diagnostics.CrossLayerDiagnostics.NeedsPureStateBaselineResync);
        Assert.Equal(12, diagnostics.CrossLayerDiagnostics.LastPureStateAppliedFrame);
        Assert.Equal(18, diagnostics.CrossLayerDiagnostics.LastPureStateResyncFrame);
        Assert.Equal(ShooterPureStateResyncReason.BaselineMismatch, diagnostics.LastPureStateResyncReason);
        Assert.Equal(12, diagnostics.LastPureStateAppliedFrame);
        Assert.Equal(0x1234u, diagnostics.LastPureStateAppliedStateHash);
        Assert.Equal(18, diagnostics.LastPureStateResyncFrame);
        Assert.Equal(0x5678u, diagnostics.LastPureStateResyncStateHash);
        Assert.Equal(ShooterPureStateSnapshotApplyResult.NeedsFullBaselineResync, diagnostics.PureStateSyncDiagnostics.LastApplyResult);
        Assert.Equal(18, diagnostics.PureStateSyncDiagnostics.SourceFrame);
        Assert.Equal(ShooterPureStateSnapshotKinds.Delta, diagnostics.PureStateSyncDiagnostics.SourceSnapshotKind);
        Assert.Equal(2, diagnostics.PureStateSyncDiagnostics.SourceEntityCount);
        Assert.Equal(1, diagnostics.PureStateSyncDiagnostics.SourceVisibilityHintCount);
        Assert.Equal(12, diagnostics.PureStateSyncDiagnostics.SourceBaselineFrame);
        Assert.Equal(0x1234u, diagnostics.PureStateSyncDiagnostics.SourceBaselineHash);
        Assert.Equal(0x5678u, diagnostics.PureStateSyncDiagnostics.SourceStateHash);
        Assert.Equal(99L, diagnostics.PureStateSyncDiagnostics.SourceServerTick);
        Assert.Equal(12, diagnostics.PureStateSyncDiagnostics.AppliedFrame);
        Assert.Equal(0x1234u, diagnostics.PureStateSyncDiagnostics.AppliedStateHash);
        Assert.True(diagnostics.PureStateSyncDiagnostics.NeedsFullBaselineResync);
        Assert.Equal(ShooterPureStateResyncReason.BaselineMismatch, diagnostics.PureStateSyncDiagnostics.LastResyncReason);
        Assert.Equal(18, diagnostics.PureStateSyncDiagnostics.LastResyncFrame);
        Assert.Equal(0x5678u, diagnostics.PureStateSyncDiagnostics.LastResyncStateHash);
        Assert.True(diagnostics.PureStateSyncDiagnostics.HasSourceSnapshot);
        Assert.False(diagnostics.PureStateSyncDiagnostics.AppliedPresentation);
    }

    [Fact]
    public void RemoteLatencyCompensationDiagnosticsAreProjectedFromHostFrame()
    {
        var remoteInput = new ShooterClientGatewayInputSubmitResult(
            new ShooterClientInputSubmitResult(1, 10, default),
            new ShooterGatewayBattleInputResult(
                success: false,
                acceptedFrame: 12,
                message: "Input frame is too far ahead.",
                currentFrame: 8,
                status: "RejectedTooFarFuture",
                shouldResync: true,
                serverTicks: 987654321L));
        var remoteDiagnostics = ShooterRemoteLatencyCompensationDiagnostics.FromGatewayInput(
            in remoteInput,
            hasPendingInput: true,
            hasQueuedInput: false,
            submittedCount: 5,
            queuedCount: 2,
            replacedCount: 1,
            completedCount: 4,
            failedCount: 0,
            resyncRequestedCount: 1);
        var frame = new ShooterHostPresentationFrame(
            ShooterSnapshotViewBatch.Empty,
            ShooterSnapshotViewBatch.Empty,
            false,
            controlledPlayerId: 1,
            worldScale: 1f,
            carrierNetworkStats: null,
            lastCarrierSnapshotApplyResult: ShooterSnapshotApplyResult.Ignored,
            lastCarrierTimeAnchor: default,
            localTimeAnchor: default,
            lagCompensationTelemetry: null,
            lagCompensationEvaluation: null,
            remoteLatencyCompensationDiagnostics: remoteDiagnostics,
            crossLayerDiagnostics: ShooterCrossLayerDiagnostics.From(
                default,
                ShooterSnapshotApplyResult.Ignored,
                remoteDiagnostics,
                needsPureStateBaselineResync: false,
                lastPureStateAppliedFrame: 0,
                lastPureStateResyncFrame: 0),
            pureStateSyncDiagnostics: default,
            needsPureStateBaselineResync: false,
            lastPureStateResyncReason: ShooterPureStateResyncReason.None,
            lastPureStateAppliedFrame: 0,
            lastPureStateAppliedStateHash: 0,
            lastPureStateResyncFrame: 0,
            lastPureStateResyncStateHash: 0);

        var diagnostics = ShooterHostDiagnosticsProjector.ProjectFromFrame(in frame, previousTotalEvents: 0);

        Assert.True(diagnostics.RemoteLatencyCompensationDiagnostics.HasResult);
        Assert.True(diagnostics.CrossLayerDiagnostics.HasSnapshotApplyResult);
        Assert.True(diagnostics.CrossLayerDiagnostics.HasRemoteLatencyResult);
        Assert.Equal(ShooterSnapshotApplyResult.Ignored, diagnostics.CrossLayerDiagnostics.SnapshotApplyResult);
        Assert.Equal(2, diagnostics.CrossLayerDiagnostics.RemoteInputDelayFrames);
        Assert.Equal(-2, diagnostics.CrossLayerDiagnostics.RemoteAuthoritativeFrameGap);
        Assert.Equal(10, diagnostics.RemoteLatencyCompensationDiagnostics.RequestedFrame);
        Assert.Equal(12, diagnostics.RemoteLatencyCompensationDiagnostics.AcceptedFrame);
        Assert.Equal(8, diagnostics.RemoteLatencyCompensationDiagnostics.AuthoritativeFrame);
        Assert.Equal(2, diagnostics.RemoteLatencyCompensationDiagnostics.InputDelayFrames);
        Assert.Equal(-2, diagnostics.RemoteLatencyCompensationDiagnostics.AuthoritativeFrameGap);
        Assert.True(diagnostics.RemoteLatencyCompensationDiagnostics.ShouldResync);
        Assert.Equal("RejectedTooFarFuture", diagnostics.RemoteLatencyCompensationDiagnostics.Status);
        Assert.Equal(987654321L, diagnostics.RemoteLatencyCompensationDiagnostics.ServerTicks);
        Assert.True(diagnostics.RemoteLatencyCompensationDiagnostics.HasPendingInput);
        Assert.False(diagnostics.RemoteLatencyCompensationDiagnostics.HasQueuedInput);
        Assert.Equal(5, diagnostics.RemoteLatencyCompensationDiagnostics.SubmittedCount);
        Assert.Equal(1, diagnostics.RemoteLatencyCompensationDiagnostics.ResyncRequestedCount);
    }

    private static bool TryGetTransformX(in ShooterSnapshotViewBatch batch, ShooterViewEntityKey key, out float x)
    {
        var transforms = batch.TransformChanges;
        for (var i = 0; i < transforms.Count; i++)
        {
            var transform = transforms[i];
            if (transform.Key.Equals(key))
            {
                x = transform.X;
                return true;
            }
        }

        x = 0f;
        return false;
    }

    private static bool TryGetFireEvent(in ShooterStateSnapshotPayload snapshot, int sourcePlayerId, out ShooterEventSnapshot fire)
    {
        var events = snapshot.Events;
        for (var i = 0; i < events.Length; i++)
        {
            var candidate = events[i];
            if (candidate.EventType == (int)ShooterEventType.Fire && candidate.SourcePlayerId == sourcePlayerId)
            {
                fire = candidate;
                return true;
            }
        }

        fire = default;
        return false;
    }

    private static bool TryGetPlayer(in ShooterStateSnapshotPayload snapshot, int playerId, out ShooterSveltoPlayerComponent player)
    {
        var players = snapshot.Players;
        for (var i = 0; i < players.Length; i++)
        {
            var candidate = players[i];
            if (candidate.PlayerId != playerId)
            {
                continue;
            }

            player = new ShooterSveltoPlayerComponent
            {
                PlayerId = candidate.PlayerId,
                X = candidate.X,
                Y = candidate.Y,
                Hp = candidate.Hp,
                Score = candidate.Score,
                Alive = candidate.Alive
            };
            return true;
        }

        player = default;
        return false;
    }

    private static void AddRepeatedMovement(List<ShooterHostFrameInput> inputs, int repeats, int activeTicks, int restTicks)
    {
        for (var repeat = 0; repeat < repeats; repeat++)
        {
            AddInput(inputs, activeTicks, repeat % 2 == 0 ? 1f : -1f, 0f, 1f, 0f, false);
            AddIdle(inputs, restTicks);
        }
    }

    private static void AddIdle(List<ShooterHostFrameInput> inputs, int ticks)
    {
        AddInput(inputs, ticks, 0f, 0f, 1f, 0f, false);
    }

    private static void AddInput(List<ShooterHostFrameInput> inputs, int ticks, float moveX, float moveY, float aimX, float aimY, bool fire)
    {
        for (var i = 0; i < ticks; i++)
        {
            inputs.Add(new ShooterHostFrameInput(moveX, moveY, aimX, aimY, fire));
        }
    }
    private sealed class ScriptedInputSource : IShooterPlayInputSource
    {
        private readonly ShooterHostFrameInput[] _inputs;
        private int _index;

        public ScriptedInputSource(ShooterHostFrameInput[] inputs)
        {
            _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        }

        public ShooterPlayFrameInput ReadInput(int controlledPlayerId)
        {
            if (_index >= _inputs.Length)
            {
                return new ShooterPlayFrameInput(0f, 0f, 1f, 0f, false);
            }

            return new ShooterPlayFrameInput(_inputs[_index++]);
        }
    }

    private sealed class RecordingViewSink : IShooterPlayViewSink
    {
        private readonly List<ShooterHostPresentationFrame> _frames = new();

        public IReadOnlyList<ShooterHostPresentationFrame> Frames => _frames;

        public void Render(in ShooterHostPresentationFrame frame)
        {
            _frames.Add(frame);
        }

        public void Clear()
        {
            _frames.Clear();
        }
    }

    [Fact]
    public void RecordingViewSinkCollectsFramesAndCanBeCleared()
    {
        var sink = new RecordingViewSink();
        var frame = new ShooterHostPresentationFrame(
            ShooterSnapshotViewBatch.Empty,
            ShooterSnapshotViewBatch.Empty,
            false,
            controlledPlayerId: 7,
            worldScale: 1.25f,
            carrierNetworkStats: null,
            lastCarrierSnapshotApplyResult: ShooterSnapshotApplyResult.Ignored,
            lastCarrierTimeAnchor: default,
            localTimeAnchor: default,
            lagCompensationTelemetry: null,
            lagCompensationEvaluation: null,
            remoteLatencyCompensationDiagnostics: default,
            crossLayerDiagnostics: default,
            pureStateSyncDiagnostics: default,
            needsPureStateBaselineResync: false,
            lastPureStateResyncReason: ShooterPureStateResyncReason.None,
            lastPureStateAppliedFrame: 0,
            lastPureStateAppliedStateHash: 0,
            lastPureStateResyncFrame: 0,
            lastPureStateResyncStateHash: 0);

        sink.Render(in frame);

        var recordedFrame = Assert.Single(sink.Frames);
        Assert.Equal(7, recordedFrame.ControlledPlayerId);
        Assert.Equal(1.25f, recordedFrame.WorldScale);

        sink.Clear();

        Assert.Empty(sink.Frames);
    }
}
