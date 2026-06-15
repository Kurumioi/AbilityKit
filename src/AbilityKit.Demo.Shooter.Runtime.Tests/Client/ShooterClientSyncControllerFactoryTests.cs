using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
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
    public void DefaultRegistryCreatesAuthoritativeInterpolationController()
    {
        var config = new InterpolationConfig(
            ticksPerSecond: 1000L,
            interpolationDelayTicks: 250L,
            bufferCapacity: 8,
            catchUpRate: 0d);

        var controller = ShooterClientSyncControllerFactory.Create(
            NetworkSyncModel.AuthoritativeInterpolation,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null,
            interpolationConfig: config);

        Assert.IsType<ShooterClientAuthoritativeInterpolationSyncController>(controller);
        Assert.Equal(NetworkSyncModel.AuthoritativeInterpolation, controller.SyncModel);
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

    [Fact]
    public void RegisterOverridesModelBuilderUntilReset()
    {
        try
        {
            var builderCalled = false;
            ShooterClientSyncControllerFactory.Register(
                NetworkSyncModel.Lockstep,
                (in ShooterClientSyncControllerFactoryContext context) =>
                {
                    builderCalled = true;
                    Assert.Equal(45, context.TickRate);
                    Assert.NotNull(context.Runtime);
                    Assert.NotNull(context.Presentation);
                    return new ShooterClientPredictRollbackSyncController(
                        context.Runtime,
                        context.Presentation,
                        context.TickRate,
                        context.Decoder,
                        context.Gateway);
                });

            var controller = ShooterClientSyncControllerFactory.Create(
                NetworkSyncModel.Lockstep,
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
            NetworkSyncModel.Lockstep,
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null));
    }
}
