using System;
using System.Collections.Generic;
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
    public delegate ISyncAdapter SyncAdapterCreator(IWorld world, in SessionConfig config);

    public interface ISyncAdapterFactory
    {
        ISyncAdapter Create(IWorld world, in SessionConfig config);
    }

    public sealed class DefaultSyncAdapterFactory : ISyncAdapterFactory
    {
        private readonly Dictionary<Core.SyncMode, SyncAdapterCreator> _creators = new Dictionary<Core.SyncMode, SyncAdapterCreator>();

        public DefaultSyncAdapterFactory()
        {
            Register(Core.SyncMode.Lockstep, (IWorld world, in SessionConfig config) => new LocalSyncAdapter(world, config));
            Register(Core.SyncMode.SnapshotAuthority, (IWorld world, in SessionConfig config) => new RemoteSyncAdapter(world, config));
            Register(Core.SyncMode.StateSync, (IWorld world, in SessionConfig config) => new RemoteSyncAdapter(world, config));
            Register(Core.SyncMode.Hybrid, (IWorld world, in SessionConfig config) => new HybridSyncAdapter(world, config));
        }

        public DefaultSyncAdapterFactory Register(Core.SyncMode mode, SyncAdapterCreator creator)
        {
            if (creator == null)
            {
                throw new ArgumentNullException(nameof(creator));
            }

            _creators[mode] = creator;
            return this;
        }

        public ISyncAdapter Create(IWorld world, in SessionConfig config)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world), "World cannot be null");
            }

            var mode = config.HostMode == HostMode.Local ? Core.SyncMode.Lockstep : config.SyncMode;
            if (_creators.TryGetValue(mode, out var creator))
            {
                return creator(world, in config);
            }

            return new LocalSyncAdapter(world, config);
        }
    }

    public static class SyncAdapterFactory
    {
        private static ISyncAdapterFactory _defaultFactory = new DefaultSyncAdapterFactory();

        public static ISyncAdapterFactory DefaultFactory => _defaultFactory;

        public static void SetDefaultFactory(ISyncAdapterFactory factory)
        {
            _defaultFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public static void ResetDefaultFactory()
        {
            _defaultFactory = new DefaultSyncAdapterFactory();
        }

        /// <summary>
        /// Create a sync adapter based on sync mode.
        /// </summary>
        public static ISyncAdapter Create(IWorld world, in SessionConfig config)
        {
            return _defaultFactory.Create(world, in config);
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
