using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Demo.Shooter.View.PlayMode;
using AbilityKit.Network.Runtime;
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
}
