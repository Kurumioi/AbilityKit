using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientSyncControllerFactoryTests
{
    [Theory]
    [InlineData(NetworkSyncModel.Unspecified)]
    [InlineData(NetworkSyncModel.PredictRollback)]
    public void DefaultRegistryCreatesPredictRollbackController(NetworkSyncModel syncModel)
    {
        var controller = ShooterClientSyncControllerFactory.Create(
            syncModel,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null);

        Assert.IsType<ShooterClientPredictRollbackSyncController>(controller);
        Assert.Equal(NetworkSyncModel.PredictRollback, controller.SyncModel);
    }

    [Fact]
    public void DefaultRegistryCreatesHybridHeroPredictionController()
    {
        var controller = ShooterClientSyncControllerFactory.Create(
            NetworkSyncModel.HybridHeroPrediction,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null);

        Assert.IsType<ShooterClientHybridHeroPredictionSyncController>(controller);
        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, controller.SyncModel);
        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, ((AbilityKit.Network.Runtime.Sync.IClientSyncStrategy<AbilityKit.Protocol.Shooter.ShooterPlayerCommand, ShooterRemoteSnapshotSample>)controller).SyncModel);
    }

    [Fact]
    public void DefaultRegistryCreatesControllerFromSyncProfile()
    {
        var controller = ShooterClientSyncControllerFactory.Create(
            NetworkSyncProfiles.HybridHeroPrediction,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null);

        Assert.IsType<ShooterClientHybridHeroPredictionSyncController>(controller);
        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, controller.SyncModel);
    }

    [Theory]
    [InlineData(NetworkSyncModel.AuthoritativeInterpolation)]
    [InlineData(NetworkSyncModel.BatchStateSync)]
    [InlineData(NetworkSyncModel.MassBattleLodSync)]
    public void DefaultRegistryCreatesAuthoritativeInterpolationBasedControllers(NetworkSyncModel syncModel)
    {
        var config = new InterpolationConfig(
            ticksPerSecond: 1000L,
            interpolationDelayTicks: 250L,
            bufferCapacity: 8,
            catchUpRate: 0d);

        var controller = ShooterClientSyncControllerFactory.Create(
            syncModel,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null,
            interpolationConfig: config);

        Assert.IsType<ShooterClientAuthoritativeInterpolationSyncController>(controller);
        Assert.Equal(syncModel, controller.SyncModel);
    }

    [Fact]
    public void DefaultRegistryRejectsUnregisteredSyncModel()
    {
        Assert.Throws<System.NotSupportedException>(() => ShooterClientSyncControllerFactory.Create(
            NetworkSyncModel.Lockstep,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegisterOverridesProfileBuilderUntilReset(bool useLegacyModelRegistration)
    {
        try
        {
            var builderCalled = false;
            var lockstepProfile = NetworkSyncProfiles.Lockstep;
            ShooterClientSyncControllerFactory.ShooterClientSyncControllerBuilder builder =
                (in ShooterClientSyncControllerFactoryContext context) =>
                {
                    builderCalled = true;
                    Assert.Equal(lockstepProfile, context.SyncProfile);
                    Assert.Equal(NetworkSyncModel.Lockstep, context.SyncModel);
                    Assert.Equal(45, context.TickRate);
                    Assert.NotNull(context.Runtime);
                    Assert.NotNull(context.Presentation);
                    return new ShooterClientPredictRollbackSyncController(
                        context.Runtime,
                        context.Presentation,
                        context.TickRate,
                        context.Decoder,
                        context.Gateway);
                };

            if (useLegacyModelRegistration)
            {
                ShooterClientSyncControllerFactory.Register(NetworkSyncModel.Lockstep, builder);
            }
            else
            {
                ShooterClientSyncControllerFactory.Register(lockstepProfile, builder);
            }

            var controller = ShooterClientSyncControllerFactory.Create(
                lockstepProfile,
                new ShooterBattleRuntimePort(),
                new ShooterPresentationFacade(),
                tickRate: 45,
                decoder: null,
                gateway: null);

            Assert.True(builderCalled);
            Assert.IsType<ShooterClientPredictRollbackSyncController>(controller);
        }
        finally
        {
            ShooterClientSyncControllerFactory.ResetToDefaults();
        }

        Assert.Throws<System.NotSupportedException>(() => ShooterClientSyncControllerFactory.Create(
            NetworkSyncProfiles.Lockstep,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null));
    }
}
