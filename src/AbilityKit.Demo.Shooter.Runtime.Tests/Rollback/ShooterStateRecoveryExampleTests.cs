using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Rollback;

public sealed class ShooterStateRecoveryExampleTests
{
    [Fact]
    public void CaptureRollbackCheckpointExportsProviderPayloadAndDiagnostics()
    {
        var runtime = CreateStartedRuntime();
        Assert.True(runtime.Tick(1f / 30f));
        var recovery = new ShooterStateRecoveryExample(runtime, 7001ul);

        var capture = recovery.CaptureRollbackCheckpoint(runtime.CurrentFrame);

        Assert.NotEmpty(capture.Payload);
        Assert.True(capture.Diagnostics.Success);
        Assert.Equal(ShooterStateRecoveryOperation.Capture, capture.Diagnostics.Operation);
        Assert.Equal(ShooterStateRecoveryScenarios.Rollback, capture.Diagnostics.Scenario);
        Assert.Equal(ShooterPackedSnapshotRollbackProvider.DefaultKey, capture.Diagnostics.ProviderKey);
        Assert.Equal(runtime.CurrentFrame, capture.Diagnostics.Frame);
        Assert.Equal(capture.Payload.Length, capture.Diagnostics.PayloadBytes);
        Assert.Equal(capture.Diagnostics.StateHashBefore, capture.Diagnostics.StateHashAfter);
        Assert.False(capture.Diagnostics.StateHashChanged);
        Assert.Equal(capture.Diagnostics, recovery.LastDiagnostics);
    }

    [Fact]
    public void RestoreReplayCheckpointImportsCapturedPayloadAndReportsHashChange()
    {
        var runtime = CreateStartedRuntime();
        Assert.True(runtime.Tick(1f / 30f));
        var recovery = new ShooterStateRecoveryExample(runtime, 7002ul);
        var capture = recovery.CaptureReplayCheckpoint(runtime.CurrentFrame);
        var capturedFrame = runtime.CurrentFrame;
        var capturedHash = runtime.ComputeStateHash();

        runtime.SubmitInput(runtime.CurrentFrame, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));
        Assert.NotEqual(capturedHash, runtime.ComputeStateHash());

        var restored = recovery.RestoreReplayCheckpoint(capturedFrame, capture.Payload);

        Assert.True(restored.Success);
        Assert.Equal(ShooterStateRecoveryOperation.Restore, restored.Operation);
        Assert.Equal(ShooterStateRecoveryScenarios.Replay, restored.Scenario);
        Assert.Equal(capturedFrame, restored.Frame);
        Assert.Equal(capture.Payload.Length, restored.PayloadBytes);
        Assert.True(restored.StateHashChanged);
        Assert.Equal(capturedHash, runtime.ComputeStateHash());
        Assert.Equal(capturedFrame, runtime.CurrentFrame);
        Assert.Equal(restored, recovery.LastDiagnostics);
    }

    [Fact]
    public void RestoreRewindCheckpointRejectsEmptyPayloadWithoutChangingState()
    {
        var runtime = CreateStartedRuntime();
        Assert.True(runtime.Tick(1f / 30f));
        var recovery = new ShooterStateRecoveryExample(runtime, 7003ul);
        var beforeHash = runtime.ComputeStateHash();
        var beforeFrame = runtime.CurrentFrame;

        var restored = recovery.RestoreRewindCheckpoint(beforeFrame, System.Array.Empty<byte>());

        Assert.False(restored.Success);
        Assert.Equal(ShooterStateRecoveryOperation.Restore, restored.Operation);
        Assert.Equal(ShooterStateRecoveryScenarios.Rewind, restored.Scenario);
        Assert.Equal(0, restored.PayloadBytes);
        Assert.False(restored.StateHashChanged);
        Assert.Equal(beforeHash, runtime.ComputeStateHash());
        Assert.Equal(beforeFrame, runtime.CurrentFrame);
    }

    private static ShooterBattleRuntimePort CreateStartedRuntime()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "state-recovery",
            30,
            7000,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });
        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, false) });
        return runtime;
    }
}
