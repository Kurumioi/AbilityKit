using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterDeterministicReplayTests
{
    [Fact]
    public void ReplaySameInputScriptProducesSameFrameHashAndSnapshot()
    {
        var script = new[]
        {
            new ShooterReplayFrame(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, false) }),
            new ShooterReplayFrame(1, new[] { new ShooterPlayerCommand(2, -1f, 0f, -1f, 0f, false) }),
            new ShooterReplayFrame(2, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) }),
            new ShooterReplayFrame(3, new[] { new ShooterPlayerCommand(1, 0f, 1f, 0f, 1f, false) }),
            new ShooterReplayFrame(4, new[] { new ShooterPlayerCommand(2, 0f, 0f, -1f, 0f, true) }),
            new ShooterReplayFrame(5, new[] { new ShooterPlayerCommand(1, 0f, 0f, 0f, 1f, true) })
        };

        var first = RunReplay(script, totalFrames: 12);
        var second = RunReplay(script, totalFrames: 12);

        Assert.Equal(first.Frame, second.Frame);
        Assert.Equal(first.StateHash, second.StateHash);
        Assert.Equal(first.PlayerCount, second.PlayerCount);
        Assert.Equal(first.BulletCount, second.BulletCount);
        Assert.Equal(first.EntityCount, second.EntityCount);
        Assert.Equal(first.SnapshotStateHash, second.SnapshotStateHash);
    }

    [Fact]
    public void ReplayImportedSnapshotAndPendingInputsMatchesContinuousRun()
    {
        var script = new[]
        {
            new ShooterReplayFrame(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, false) }),
            new ShooterReplayFrame(1, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) }),
            new ShooterReplayFrame(2, new[] { new ShooterPlayerCommand(2, -1f, 0f, -1f, 0f, false) }),
            new ShooterReplayFrame(3, new[] { new ShooterPlayerCommand(2, 0f, 0f, -1f, 0f, true) }),
            new ShooterReplayFrame(4, new[] { new ShooterPlayerCommand(1, 0f, 1f, 0f, 1f, false) }),
            new ShooterReplayFrame(5, new[] { new ShooterPlayerCommand(2, 0f, -1f, 0f, -1f, false) })
        };

        var continuous = new ShooterBattleRuntimePort();
        var checkpointSource = new ShooterBattleRuntimePort();
        var replayed = new ShooterBattleRuntimePort();
        var start = CreateStartPayload();
        Assert.True(continuous.StartGame(in start));
        Assert.True(checkpointSource.StartGame(in start));

        RunFrames(continuous, script, startFrame: 0, endFrameExclusive: 12);
        RunFrames(checkpointSource, script, startFrame: 0, endFrameExclusive: 4);
        var checkpoint = checkpointSource.ExportPackedSnapshot(77ul, isFullSnapshot: true, authorityOverride: true);

        Assert.True(replayed.ImportPackedSnapshot(in checkpoint));
        RunFrames(replayed, script, startFrame: checkpoint.Frame, endFrameExclusive: 12);

        Assert.Equal(continuous.CurrentFrame, replayed.CurrentFrame);
        Assert.Equal(continuous.ComputeStateHash(), replayed.ComputeStateHash());
        Assert.Equal(continuous.ExportPackedSnapshot(77ul).EntityCount, replayed.ExportPackedSnapshot(77ul).EntityCount);
    }

    private static ShooterReplayResult RunReplay(IReadOnlyList<ShooterReplayFrame> script, int totalFrames)
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = CreateStartPayload();
        Assert.True(runtime.StartGame(in start));

        RunFrames(runtime, script, startFrame: 0, endFrameExclusive: totalFrames);

        var snapshot = runtime.GetSnapshot();
        var packed = runtime.ExportPackedSnapshot(77ul, isFullSnapshot: true, authorityOverride: true);
        return new ShooterReplayResult(
            runtime.CurrentFrame,
            runtime.ComputeStateHash(),
            snapshot.Players.Length,
            snapshot.Bullets.Length,
            packed.EntityCount,
            packed.StateHash);
    }

    private static void RunFrames(ShooterBattleRuntimePort runtime, IReadOnlyList<ShooterReplayFrame> script, int startFrame, int endFrameExclusive)
    {
        for (var frame = startFrame; frame < endFrameExclusive; frame++)
        {
            SubmitFrame(runtime, script, frame);
            Assert.True(runtime.Tick(1f / 30f));
        }
    }

    private static void SubmitFrame(ShooterBattleRuntimePort runtime, IReadOnlyList<ShooterReplayFrame> script, int frame)
    {
        for (var i = 0; i < script.Count; i++)
        {
            if (script[i].Frame != frame)
            {
                continue;
            }

            Assert.Equal(script[i].Commands.Length, runtime.SubmitInput(frame, script[i].Commands));
        }
    }

    private static ShooterStartGamePayload CreateStartPayload()
    {
        return new ShooterStartGamePayload(
            "deterministic-replay",
            30,
            2468,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });
    }

    private readonly record struct ShooterReplayFrame(int Frame, ShooterPlayerCommand[] Commands);

    private readonly record struct ShooterReplayResult(
        int Frame,
        uint StateHash,
        int PlayerCount,
        int BulletCount,
        int EntityCount,
        uint SnapshotStateHash);
}
