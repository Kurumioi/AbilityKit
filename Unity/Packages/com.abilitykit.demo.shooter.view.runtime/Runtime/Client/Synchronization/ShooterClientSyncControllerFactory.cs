#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Creates the <see cref="IShooterClientSyncController"/> matching a requested
    /// <see cref="NetworkSyncModel"/>. This is the single entry point where the Shooter
    /// session chooses its synchronization strategy. New models (authoritative interpolation,
    /// batch state sync, etc.) plug in here without touching the session facade.
    /// </summary>
    public static class ShooterClientSyncControllerFactory
    {
        public const NetworkSyncModel DefaultSyncModel = NetworkSyncModel.PredictRollback;

        private static readonly object SyncRoot = new();
        private static Dictionary<NetworkSyncModel, ShooterClientSyncControllerBuilder> _builders = CreateDefaultBuilders();

        public delegate IShooterClientSyncController ShooterClientSyncControllerBuilder(
            in ShooterClientSyncControllerFactoryContext context);

        public static void Register(
            NetworkSyncModel syncModel,
            ShooterClientSyncControllerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            lock (SyncRoot)
            {
                var builders = new Dictionary<NetworkSyncModel, ShooterClientSyncControllerBuilder>(_builders)
                {
                    [syncModel] = builder
                };
                _builders = builders;
            }
        }

        public static void ResetToDefaults()
        {
            lock (SyncRoot)
            {
                _builders = CreateDefaultBuilders();
            }
        }

        public static IShooterClientSyncController Create(
            NetworkSyncModel syncModel,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway)
        {
            return Create(syncModel, runtime, presentation, tickRate, decoder, gateway, interpolationConfig: null);
        }

        /// <summary>
        /// Creates a sync controller, optionally supplying an
        /// <see cref="InterpolationConfig"/> for the
        /// <see cref="NetworkSyncModel.AuthoritativeInterpolation"/> model. The config is ignored by
        /// models that do not interpolate (e.g. predict rollback); when omitted the interpolation model
        /// falls back to <see cref="InterpolationConfig.Default"/>.
        /// </summary>
        public static IShooterClientSyncController Create(
            NetworkSyncModel syncModel,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway,
            InterpolationConfig? interpolationConfig)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));

            var builders = _builders;
            if (!builders.TryGetValue(syncModel, out var builder))
            {
                throw new NotSupportedException(
                    $"Shooter client sync model '{syncModel}' is not implemented yet.");
            }

            var context = new ShooterClientSyncControllerFactoryContext(
                runtime,
                presentation,
                tickRate,
                decoder,
                gateway,
                interpolationConfig);
            return builder(in context);
        }

        private static Dictionary<NetworkSyncModel, ShooterClientSyncControllerBuilder> CreateDefaultBuilders()
        {
            var builders = new Dictionary<NetworkSyncModel, ShooterClientSyncControllerBuilder>
            {
                [NetworkSyncModel.Unspecified] = CreatePredictRollbackController,
                [NetworkSyncModel.PredictRollback] = CreatePredictRollbackController,
                [NetworkSyncModel.AuthoritativeInterpolation] = CreateAuthoritativeInterpolationController,
                [NetworkSyncModel.HybridHeroPrediction] = CreateHybridHeroPredictionController
            };
            return builders;
        }

        private static IShooterClientSyncController CreatePredictRollbackController(
            in ShooterClientSyncControllerFactoryContext context)
        {
            return new ShooterClientPredictRollbackSyncController(
                context.Runtime,
                context.Presentation,
                context.TickRate,
                context.Decoder,
                context.Gateway);
        }

        private static IShooterClientSyncController CreateAuthoritativeInterpolationController(
            in ShooterClientSyncControllerFactoryContext context)
        {
            return new ShooterClientAuthoritativeInterpolationSyncController(
                context.Runtime,
                context.Presentation,
                context.TickRate,
                context.Decoder,
                context.Gateway,
                context.InterpolationConfig ?? InterpolationConfig.Default);
        }

        private static IShooterClientSyncController CreateHybridHeroPredictionController(
            in ShooterClientSyncControllerFactoryContext context)
        {
            return new ShooterClientHybridHeroPredictionSyncController(
                context.Runtime,
                context.Presentation,
                context.TickRate,
                context.Decoder,
                context.Gateway,
                context.InterpolationConfig ?? InterpolationConfig.Default);
        }
    }

    public readonly struct ShooterClientSyncControllerFactoryContext
    {
        public ShooterClientSyncControllerFactoryContext(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway,
            InterpolationConfig? interpolationConfig)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            TickRate = tickRate;
            Decoder = decoder;
            Gateway = gateway;
            InterpolationConfig = interpolationConfig;
        }

        public IShooterBattleRuntimePort Runtime { get; }

        public ShooterPresentationFacade Presentation { get; }

        public int TickRate { get; }

        public ShooterGatewaySnapshotDecoder? Decoder { get; }

        public IShooterRoomGatewayClient? Gateway { get; }

        public InterpolationConfig? InterpolationConfig { get; }
    }
}
