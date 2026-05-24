using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Share;
using EC = AbilityKit.World.ECS;

namespace ET.Logic
{
    /// <summary>
    /// ET version of moba.core battle driver (Pure Data Component)
    ///
    /// Responsibilities:
    /// - Integrate AbilityKit.Host.Extension framework
    /// - Manage snapshot dispatching
    /// - Host World for moba.core services
    ///
    /// Design:
    /// - This Component stores data
    /// - All business logic is in ETMobaBattleDriverSystem
    /// - Lifecycle methods (Awake/Update/Destroy) are handled by the System
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy
    {
        // ============== Core (World Management) ==============

        public IWorldManager WorldManager { get; set; }
        public HostRuntime HostRuntime { get; set; }
        public IWorld World { get; set; }

        // ============== View Sink ==============

        public IBattleViewEventSink ViewSink { get; set; }

        // ============== Config Loader ==============

        public ITextAssetLoader TextAssetLoader { get; set; }
        public ETConfigLoaderService ConfigLoader { get; set; }

        // ============== Player Spawn Data ==============

        public List<ETPlayerSpawnData> PlayerSpawnData { get; set; } = new();

        // ============== Snapshot Dispatcher ==============

        public FrameSnapshotDispatcher SnapshotDispatcher { get; set; }

        // ============== Sync Adapter (for Coordinator) ==============

        public IETBattleSyncAdapter SyncAdapter { get; set; }

        // ============== State ==============

        public BattleStartPlan Plan { get; set; }
        public int CurrentFrame { get; set; }
        public double LogicTimeSeconds { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }
        public double LastTickTime { get; set; }

        // ============== Lifecycle Methods (Empty - Handled by System) ==============

        public void Awake()
        {
        }

        public void Update(ETMobaBattleDriver self)
        {
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
        }
    }
}
