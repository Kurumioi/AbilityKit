using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Sync Adapter Factory
    ///
    /// Design:
    /// - Creates appropriate sync adapter based on configuration
    /// - Centralized adapter instantiation logic
    /// - Returns strongly typed interfaces for mode-specific behavior
    /// </summary>
    public static class SyncAdapterFactory
    {
        /// <summary>
        /// Create a sync adapter based on sync mode
        /// </summary>
        public static ISyncAdapter Create(IWorld world, in SessionConfig config)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world), "World cannot be null");
            }

            if (config.HostMode == HostMode.Local)
            {
                return CreateLocalSyncAdapter(world, config);
            }

            return config.SyncMode switch
            {
                Core.SyncMode.Lockstep => CreateLocalSyncAdapter(world, config),
                Core.SyncMode.SnapshotAuthority => CreateRemoteSyncAdapter(world, config),
                Core.SyncMode.StateSync => CreateRemoteSyncAdapter(world, config),
                Core.SyncMode.Hybrid => CreateHybridSyncAdapter(world, config),
                _ => CreateLocalSyncAdapter(world, config)
            };
        }

        /// <summary>
        /// Create a local sync adapter (Lockstep mode)
        /// </summary>
        public static ILocalSyncAdapter CreateLocalSyncAdapter(IWorld world, in SessionConfig config)
        {
            return new LocalSyncAdapter(world, config);
        }

        /// <summary>
        /// Create a remote sync adapter (Server authority mode)
        /// </summary>
        public static IRemoteSyncAdapter CreateRemoteSyncAdapter(IWorld world, in SessionConfig config)
        {
            return new RemoteSyncAdapter(world, config);
        }

        /// <summary>
        /// Create a hybrid sync adapter (Client prediction mode)
        /// </summary>
        public static IPredictionSyncAdapter CreateHybridSyncAdapter(IWorld world, in SessionConfig config)
        {
            return new HybridSyncAdapter(world, config);
        }

        // ============== Utility Methods ==============

        /// <summary>
        /// Get sync adapter type name from mode
        /// </summary>
        public static string GetAdapterTypeName(Core.SyncMode mode)
        {
            return mode switch
            {
                Core.SyncMode.Lockstep => nameof(LocalSyncAdapter),
                Core.SyncMode.SnapshotAuthority => nameof(RemoteSyncAdapter),
                Core.SyncMode.StateSync => nameof(RemoteSyncAdapter),
                Core.SyncMode.Hybrid => nameof(HybridSyncAdapter),
                _ => nameof(LocalSyncAdapter)
            };
        }

        /// <summary>
        /// Check if sync adapter requires network connection
        /// </summary>
        public static bool RequiresNetwork(Core.SyncMode mode)
        {
            return mode switch
            {
                Core.SyncMode.Lockstep => false,
                Core.SyncMode.SnapshotAuthority => true,
                Core.SyncMode.StateSync => true,
                Core.SyncMode.Hybrid => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if sync adapter supports client prediction
        /// </summary>
        public static bool SupportsPrediction(Core.SyncMode mode)
        {
            return mode == Core.SyncMode.Hybrid;
        }
    }
}
