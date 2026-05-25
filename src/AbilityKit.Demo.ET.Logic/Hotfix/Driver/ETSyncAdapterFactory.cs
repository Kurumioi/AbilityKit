using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Sync Adapter Factory
    ///
    /// Design:
    /// - Creates appropriate sync adapter based on configuration
    /// - Centralizes adapter instantiation logic
    /// - Returns strongly typed interfaces for mode-specific behavior
    /// </summary>
    public static class ETSyncAdapterFactory
    {
        // ============== Factory Methods ==============

        /// <summary>
        /// Create a sync adapter based on sync mode
        /// </summary>
        public static IETBattleSyncAdapter Create(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            IETBattleSyncAdapter adapter = plan.SyncMode switch
            {
                SyncMode.Lockstep => CreateFrameSyncAdapter(driver, plan),
                SyncMode.SnapshotAuthority => CreateStateSyncAdapter(driver, plan),
                SyncMode.StateSync => CreateStateSyncAdapter(driver, plan),
                SyncMode.Hybrid => CreateHybridSyncAdapter(driver, plan),
                _ => CreateFrameSyncAdapter(driver, plan)
            };

            Log.Info($"[ETSyncAdapterFactory] Created {adapter.GetType().Name} for SyncMode={plan.SyncMode}");

            return adapter;
        }

        /// <summary>
        /// Create a local sync adapter (Lockstep mode)
        /// </summary>
        public static IETLocalSyncAdapter CreateLocalSyncAdapter(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            var adapter = new ETLocalSyncAdapter();
            adapter.Initialize(driver, plan);
            return adapter;
        }

        /// <summary>
        /// Create a remote sync adapter (Server authority mode)
        /// </summary>
        public static IETRemoteSyncAdapter CreateRemoteSyncAdapter(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            var adapter = new ETStateSyncAdapter();
            adapter.Initialize(driver, plan);
            return adapter;
        }

        /// <summary>
        /// Create a hybrid sync adapter (Client prediction mode)
        /// </summary>
        public static IETPredictionSyncAdapter CreateHybridSyncAdapter(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            var adapter = new ETHybridSyncAdapter();
            adapter.Initialize(driver, plan);
            return adapter;
        }

        // ============== Type-Safe Factory Methods ==============

        /// <summary>
        /// Create FrameSync adapter (Lockstep mode)
        /// </summary>
        public static IETLocalSyncAdapter CreateFrameSyncAdapter(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            return CreateLocalSyncAdapter(driver, plan);
        }

        /// <summary>
        /// Create StateSync adapter (Server authority mode)
        /// </summary>
        public static IETRemoteSyncAdapter CreateStateSyncAdapter(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            return CreateRemoteSyncAdapter(driver, plan);
        }

        // ============== Utility Methods ==============

        /// <summary>
        /// Get sync adapter type name from mode
        /// </summary>
        public static string GetAdapterTypeName(SyncMode mode)
        {
            return mode switch
            {
                SyncMode.Lockstep => "LocalSyncAdapter",
                SyncMode.SnapshotAuthority => nameof(ETStateSyncAdapter),
                SyncMode.StateSync => nameof(ETStateSyncAdapter),
                SyncMode.Hybrid => nameof(ETHybridSyncAdapter),
                _ => "LocalSyncAdapter"
            };
        }

        /// <summary>
        /// Check if sync adapter requires network connection
        /// </summary>
        public static bool RequiresNetwork(SyncMode mode)
        {
            return mode switch
            {
                SyncMode.Lockstep => false,
                SyncMode.SnapshotAuthority => true,
                SyncMode.StateSync => true,
                SyncMode.Hybrid => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if sync adapter supports client prediction
        /// </summary>
        public static bool SupportsPrediction(SyncMode mode)
        {
            return mode switch
            {
                SyncMode.Hybrid => true,
                _ => false
            };
        }

        /// <summary>
        /// Get the appropriate adapter interface for a given sync mode
        /// Returns the most specific interface available
        /// </summary>
        public static Type GetAdapterInterfaceType(SyncMode mode)
        {
            return mode switch
            {
                SyncMode.Lockstep => typeof(IETLocalSyncAdapter),
                SyncMode.SnapshotAuthority => typeof(IETRemoteSyncAdapter),
                SyncMode.StateSync => typeof(IETRemoteSyncAdapter),
                SyncMode.Hybrid => typeof(IETPredictionSyncAdapter),
                _ => typeof(IETBattleSyncAdapter)
            };
        }
    }
}
