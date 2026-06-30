#nullable enable

using System;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Shooter 客户端同步链路组装选项。
    /// 将同步模型、快照解码器与插值配置集中到一个稳定参数对象中，
    /// 便于 PlayMode、Gateway 与验收测试在同一客户端装配入口上切换多种同步方案。
    /// </summary>
    public readonly struct ShooterClientSyncAssemblyOptions
    {
        public ShooterClientSyncAssemblyOptions(
            NetworkSyncModel syncModel,
            ShooterGatewaySnapshotDecoder? decoder = null,
            InterpolationConfig? interpolationConfig = null)
            : this(NetworkSyncProfileRegistry.Resolve(syncModel), decoder, interpolationConfig)
        {
        }

        public ShooterClientSyncAssemblyOptions(
            in NetworkSyncProfile syncProfile,
            ShooterGatewaySnapshotDecoder? decoder = null,
            InterpolationConfig? interpolationConfig = null)
        {
            SyncProfile = syncProfile;
            Decoder = decoder;
            InterpolationConfig = interpolationConfig;
        }

        public static ShooterClientSyncAssemblyOptions Default => ForModel(ShooterClientSyncControllerFactory.DefaultSyncModel);

        public static ShooterClientSyncAssemblyOptions ForModel(NetworkSyncModel syncModel)
        {
            return new ShooterClientSyncAssemblyOptions(syncModel);
        }

        public static ShooterClientSyncAssemblyOptions ForProfile(in NetworkSyncProfile syncProfile)
        {
            return new ShooterClientSyncAssemblyOptions(in syncProfile);
        }

        public NetworkSyncProfile SyncProfile { get; }

        public NetworkSyncModel SyncModel => SyncProfile.CompatibilityModel;

        public ShooterGatewaySnapshotDecoder? Decoder { get; }

        public InterpolationConfig? InterpolationConfig { get; }

        public ShooterClientSyncAssemblyOptions WithDecoder(ShooterGatewaySnapshotDecoder? decoder)
        {
            return new ShooterClientSyncAssemblyOptions(SyncProfile, decoder, InterpolationConfig);
        }

        public ShooterClientSyncAssemblyOptions WithInterpolationConfig(InterpolationConfig? interpolationConfig)
        {
            return new ShooterClientSyncAssemblyOptions(SyncProfile, Decoder, interpolationConfig);
        }

        public ShooterClientSyncAssemblyOptions WithSyncModel(NetworkSyncModel syncModel)
        {
            return new ShooterClientSyncAssemblyOptions(syncModel, Decoder, InterpolationConfig);
        }

        public ShooterClientSyncAssemblyOptions WithSyncProfile(in NetworkSyncProfile syncProfile)
        {
            return new ShooterClientSyncAssemblyOptions(in syncProfile, Decoder, InterpolationConfig);
        }
    }
}
