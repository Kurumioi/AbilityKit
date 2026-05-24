using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// View Event Sink Interface
    /// 
    /// Design Principles:
    /// - Framework defines the contract, application interprets the data
    /// - Frame snapshot contains all data for a frame
    /// - Simple lifecycle events are framework-defined
    /// - Business-specific events are passed as raw data for application to interpret
    ///
    /// This interface is optional. If not set, the coordinator still works but
    /// applications need to query states manually via IBattleDriverHost.
    /// </summary>
    public interface IViewEventSink
    {
        // ============== Frame Snapshot Events ==============

        /// <summary>
        /// Called when entering game snapshot (initial state)
        /// Contains all entities and their initial states
        /// 
        /// Application should:
        /// - Create all visual representations for entities in the snapshot
        /// - Initialize UI elements based on config
        /// - Cache EntityState data for later interpolation
        /// </summary>
        /// <param name="snapshot">Frame snapshot with all entity states</param>
        void OnEnterGameSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// Called when actor transform snapshot arrives
        /// Contains position/rotation changes for actors
        /// 
        /// Application should:
        /// - Update visual positions
        /// - Queue interpolation data for smooth rendering
        /// </summary>
        /// <param name="snapshot">Frame snapshot with transform data</param>
        void OnActorTransformSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// Called when damage event snapshot arrives
        /// Contains damage information for display
        /// 
        /// Application should:
        /// - Show damage numbers
        /// - Play damage effects
        /// - Update HP bars
        /// </summary>
        /// <param name="snapshot">Frame snapshot with damage data</param>
        void OnDamageEventSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// Called when frame sync completes
        /// All snapshot events have been processed
        /// 
        /// Application should:
        /// - Flush pending visual updates
        /// - Start interpolation for next frame
        /// </summary>
        /// <param name="frame">Completed frame number</param>
        void OnFrameSyncComplete(int frame);

        // ============== Lifecycle Events ==============

        /// <summary>
        /// Called when battle starts
        /// </summary>
        /// <param name="frame">Starting frame number</param>
        void OnBattleStart(int frame);

        /// <summary>
        /// Called when battle ends
        /// </summary>
        /// <param name="frame">Ending frame number</param>
        /// <param name="winTeamId">Winning team ID (0 for draw)</param>
        void OnBattleEnd(int frame, int winTeamId);

        // ============== Extension Events ==============

        /// <summary>
        /// Called for custom events (skill casts, buffs, etc.)
        /// The eventType string allows applications to filter and handle custom data
        /// 
        /// Common event types (application-defined):
        /// - "SkillCast" - skill was cast
        /// - "BuffApply" - buff was applied
        /// - "BuffRemove" - buff was removed
        /// - "ProjectileSpawn" - projectile created
        /// - "ProjectileHit" - projectile hit target
        /// 
        /// Application should:
        /// - Switch on eventType to handle specific events
        /// - Deserialize customData based on eventType
        /// </summary>
        /// <param name="eventType">Application-defined event type identifier</param>
        /// <param name="entityId">Primary entity ID related to this event</param>
        /// <param name="customData">Event-specific data (format determined by application)</param>
        void OnCustomEvent(string eventType, int entityId, byte[] customData);
    }
}
