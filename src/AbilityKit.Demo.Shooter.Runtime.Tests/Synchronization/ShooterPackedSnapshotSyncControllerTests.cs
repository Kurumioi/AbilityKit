using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterPackedSnapshotSyncControllerTests
{
    [Fact]
    public void PackedSnapshotSyncControllerOverwritesLocalRuntimeAndViewModel()
    {
        var source = new ShooterBattleRuntimePort();
        var sourceStart = new ShooterStartGamePayload(
            "source",
            30,
            901,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 4f, 0f)
            });
        Assert.True(source.StartGame(in sourceStart));
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) });
        Assert.True(source.Tick(1f / 30f));

        var target = new ShooterBattleRuntimePort();
        var targetStart = new ShooterStartGamePayload(
            "target",
            30,
            902,
            new[]
            {
                new ShooterStartPlayer(9, "Other", -5f, -5f)
            });
        Assert.True(target.StartGame(in targetStart));
        target.SubmitInput(0, new[] { new ShooterPlayerCommand(9, 0f, 1f, 0f, 1f, false) });
        Assert.True(target.Tick(1f / 30f));
        Assert.NotEqual(source.ComputeStateHash(), target.ComputeStateHash());

        var packed = source.ExportPackedSnapshot(777ul, isFullSnapshot: true, authorityOverride: true);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = packed.WorldId,
            Frame = packed.Frame,
            Timestamp = 456.5,
            IsFullSnapshot = true,
            Actors = null,
            PayloadOpCode = ShooterOpCodes.Snapshot.PackedState,
            Payload = ShooterPackedSnapshotCodec.Serialize(in packed)
        };
        var gatewayPayload = WireRoomGatewayBinary.Serialize(in wire);
        var presentation = new ShooterPresentationFacade();
        var controller = new ShooterPackedSnapshotSyncController(target, presentation);

        var result = controller.TryApplyGatewayPush(RoomGatewayOpCodes.SnapshotPushed, gatewayPayload);

        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, result);
        Assert.Equal(source.CurrentFrame, target.CurrentFrame);
        Assert.Equal(source.ComputeStateHash(), target.ComputeStateHash());
        Assert.Equal(packed.Frame, controller.LastAppliedFrame);
        Assert.Equal(packed.StateHash, controller.LastAppliedStateHash);
        Assert.Equal(packed.Frame, presentation.ViewModel.Frame);
        Assert.Equal(2, presentation.ViewModel.Players.Count);
        Assert.Single(presentation.ViewModel.Bullets);
        Assert.True(presentation.ViewModel.Players.ContainsKey(1));
        Assert.True(presentation.ViewModel.Players.ContainsKey(2));
    }
}
