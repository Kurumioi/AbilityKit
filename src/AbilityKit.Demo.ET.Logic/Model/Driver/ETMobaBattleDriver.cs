using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;
using ET.AbilityKit.Demo.ET.Share;
using ActorKind = ET.AbilityKit.Demo.ET.Share.ActorKind;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;

namespace ET.Logic
{
    /// <summary>
    /// ET battle host component and facade for the MOBA Runtime world.
    ///
    /// Responsibilities:
    /// - Host the AbilityKit world and ET-side lifecycle state.
    /// - Route input, startup, and snapshots through the Runtime port.
    /// - Dispatch snapshots and view events back to ET presentation.
    ///
    /// Boundary:
    /// - Keep combat rules in MOBA Runtime services/systems.
    /// - Keep ET glue in handlers, coordinators, and world modules.
    /// - Do not expand this component as a direct rules implementation surface.
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy, IBattleDriver
    {
        // ============== IBattleDriver Implementation ==============

        public int CurrentFrame { get; set; }
        public double LogicTimeSeconds { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }
        public IBattleViewEventSink ViewEventSink { get; set; }
        public BattleStartPlan Plan { get; set; }
        public bool RuntimeGameStarted { get; set; }

        // ============== Core (World Management) ==============

        public IWorldManager WorldManager { get; set; }
        public HostRuntime HostRuntime { get; set; }
        public IWorld World { get; set; }

        // ============== View Sink (for ETBridge) ==============

        private IBattleViewEventSink _viewSink;
        public IBattleViewEventSink ViewSink
        {
            get => _viewSink;
            set => _viewSink = value ?? throw new ArgumentNullException(nameof(ViewSink));
        }

        // ============== Config Loader ==============

        public ITextAssetLoader TextAssetLoader { get; set; }
        public ETConfigLoaderService ConfigLoader { get; set; }

        // ============== Player Spawn Data ==============

        public List<ETPlayerSpawnData> PlayerSpawnData { get; set; } = new List<ETPlayerSpawnData>();

        // ============== Snapshot Dispatcher ==============

        public FrameSnapshotDispatcher SnapshotDispatcher { get; set; }

        // ============== Entity Registry (moba.core integration) ==============

        /// <summary>
        /// ActorId -> ETUnit 映射（用于 moba.core 实体跟踪）
        /// 由 EnterGameHandler 在创建实体时填充
        /// </summary>
        public Dictionary<int, ETUnit> Units { get; } = new Dictionary<int, ETUnit>();

        // ============== State ==============

        public double LastTickTime { get; set; }

        // ============== Handler Collections ==============

        /// <summary>
        /// 快照处理器列表
        /// </summary>
        public List<ISnapshotHandler> SnapshotHandlers { get; set; } = new List<ISnapshotHandler>();

        /// <summary>
        /// 生命周期处理器列表
        /// </summary>
        public List<ILifecycleHandler> LifecycleHandlers { get; set; } = new List<ILifecycleHandler>();

        // ============== IBattleDriver Explicit Implementation ==============

        IBattleViewEventSink IBattleDriver.ViewEventSink
        {
            get => ViewSink;
            set => ViewSink = value;
        }

        // ============== Lifecycle Methods (Empty - Handled by Handlers) ==============

        public void Awake()
        {
            // 注册所有处理器
            HandlerRegistry.RegisterAll(this);
        }

        public void Update(ETMobaBattleDriver self)
        {
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
        }

        // ============== IBattleDriver Methods ==============

        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            ETBattleLifecycleDispatcher.Initialize(this, in plan, viewSink);
        }

        public void Start()
        {
            ETBattleLifecycleDispatcher.Start(this);
        }

        public void Stop()
        {
            ETBattleLifecycleDispatcher.Stop(this);
        }

        public void Destroy()
        {
            ETBattleLifecycleDispatcher.Destroy(this);

            // 清理处理器
            SnapshotHandlers?.Clear();
            LifecycleHandlers?.Clear();
        }

        public void Tick(float deltaTime)
        {
            ETBattleLifecycleDispatcher.Tick(this, deltaTime);
        }

        // ============== Snapshot Handling ==============

        public void HandleSnapshot(in FrameSnapshotData snapshot)
        {
            foreach (var handler in SnapshotHandlers)
            {
                if (handler.CanHandle(in snapshot))
                {
                    handler.Handle(this, in snapshot);
                }
            }
        }

        // ============== Service Resolution ==============

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (World?.Services != null)
            {
                return World.Services.TryResolve(out service);
            }
            return false;
        }

        // ============== Additional Methods ==============

        public void StartBattle()
        {
            Start();
        }

        public void StopBattle()
        {
            Stop();
        }

        public bool OnAllPlayersReady(List<ETPlayerSpawnData> players)
        {
            if (RuntimeGameStarted)
            {
                return true;
            }

            PlayerSpawnData.Clear();
            if (players != null)
            {
                PlayerSpawnData.AddRange(players);
            }
 
            var started = ETBattleEnterGameCoordinator.Trigger(this);
            // Note: OnBattleStart is called by StartBattle in DemoProcessComponentSystem.
            // Demo test fixtures must be enabled by the demo/test entry, not by the formal battle driver.
            return started;
        }
    }
}
