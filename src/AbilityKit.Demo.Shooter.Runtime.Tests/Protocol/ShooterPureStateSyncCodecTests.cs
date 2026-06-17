using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Protocol;

public sealed class ShooterPureStateSyncCodecTests
{
    [Fact]
    public void RoundTripPreservesPureStateSnapshotEnvelope()
    {
        var snapshot = new ShooterPureStateSnapshotPayload(
            ShooterPureStateSyncCodec.CurrentVersion,
            99ul,
            12,
            3456,
            ShooterPureStateSnapshotKinds.Delta,
            10,
            111u,
            222u,
            ShooterPureStateSyncSettings.Default,
            new[]
            {
                new ShooterPureStateEntityDelta(
                    7,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    ShooterPureStateDeltaKinds.Update,
                    1,
                    1200,
                    3400,
                    20,
                    -10,
                    0,
                    0,
                    12,
                    ShooterPureStateEntityFlags.Visible)
            },
            new[]
            {
                new ShooterPureStateVisibilityHint(
                    7,
                    ShooterPackedEntityKinds.Projectile,
                    ShooterPureStateEntityLayers.Combat,
                    ShooterPureStateEntityFlags.Visible,
                    100)
            });

        var bytes = ShooterPureStateSyncCodec.Serialize(in snapshot);
        var restored = ShooterPureStateSyncCodec.Deserialize(bytes);

        Assert.Equal(snapshot.WorldId, restored.WorldId);
        Assert.Equal(snapshot.Frame, restored.Frame);
        Assert.Equal(snapshot.SnapshotKind, restored.SnapshotKind);
        Assert.Equal(snapshot.BaselineFrame, restored.BaselineFrame);
        Assert.Equal(snapshot.Settings.MaxEntityCount, restored.Settings.MaxEntityCount);
        Assert.Single(restored.Entities);
        Assert.Single(restored.VisibilityHints);
        Assert.Equal(ShooterPureStateDeltaKinds.Update, restored.Entities[0].DeltaKind);
    }

    [Fact]
    public void EmptyPayloadUsesDefaultSettings()
    {
        var restored = ShooterPureStateSyncCodec.Deserialize(null!);

        Assert.Equal(ShooterPureStateSyncCodec.CurrentVersion, restored.Version);
        Assert.Equal(ShooterPureStateSnapshotKinds.FullBaseline, restored.SnapshotKind);
        Assert.Equal(10000, restored.Settings.MaxEntityCount);
        Assert.Empty(restored.Entities);
        Assert.Empty(restored.VisibilityHints);
    }
}
