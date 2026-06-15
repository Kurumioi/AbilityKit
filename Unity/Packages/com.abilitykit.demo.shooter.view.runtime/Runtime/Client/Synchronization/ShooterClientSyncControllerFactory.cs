#nullable enable

using System;
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

            switch (syncModel)
            {
                case NetworkSyncModel.Unspecified:
                case NetworkSyncModel.PredictRollback:
                case NetworkSyncModel.HybridHeroPrediction:
                    // Hybrid falls back to predict-rollback for all entities; per-entity playback
                    // split (local rollback + remote interpolation) is not yet implemented.
                    return new ShooterClientPredictRollbackSyncController(runtime, presentation, tickRate, decoder, gateway);
                case NetworkSyncModel.AuthoritativeInterpolation:
                    return new ShooterClientAuthoritativeInterpolationSyncController(
                        runtime, presentation, tickRate, decoder, gateway,
                        interpolationConfig ?? InterpolationConfig.Default);
                default:
                    throw new NotSupportedException(
                        $"Shooter client sync model '{syncModel}' is not implemented yet.");
            }
        }
    }
}
