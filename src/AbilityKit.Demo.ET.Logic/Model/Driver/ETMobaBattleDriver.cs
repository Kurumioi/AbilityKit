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
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;

namespace ET.Logic
{
    /// <summary>
    /// ET battle host component and facade for the MOBA Runtime world.
    ///
    /// Responsibilities:
    /// - Host the AbilityKit world and ET-side lifecycle state.
    /// - Route input through handlers and Runtime input ports.
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

        // ============== Sync Adapter (for Coordinator) ==============

        public IETBattleSyncAdapter SyncAdapter { get; set; }

        // ============== Battle Logic IO Ports ==============

        private IMobaBattleInputPort _inputPort;
        public IMobaBattleInputPort InputPort
        {
            get => _inputPort;
            set => _inputPort = value;
        }

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

        // ============== IBattleDriver Compatibility Surface ==============
        // These members keep the legacy demo-facing IBattleDriver contract alive.
        // New battle logic should go through Runtime ports/services instead of
        // adding direct rule implementations here.

        public void CreateActor(int actorId, int characterId, int teamId, float x, float y, float z) { }
        public ActorTransformData? GetActorTransform(int actorId) => null;
        public IReadOnlyList<ActorTransformData> GetAllActorTransforms() => null;
        public IReadOnlyList<int> GetAliveActorIds() => null;
        public float GetActorAttribute(int actorId, ActorAttributeType attributeType) => 0;
        public void SetActorAttribute(int actorId, ActorAttributeType attributeType, float value) { }
        public float ModifyActorAttribute(int actorId, ActorAttributeType attributeType, float delta) => 0;
        public bool IsActorDead(int actorId) => false;
        public void MarkActorDead(int actorId, int killerId) { }
        public void MoveActor(int actorId, float targetX, float targetZ) { }
        public bool CanCastSkill(int actorId, int slot) => false;
        public bool CastSkill(int actorId, int slot, float targetX, float targetZ) => false;
        public bool CastSkillOnTarget(int actorId, int slot, int targetActorId) => false;
        public float GetSkillCooldown(int actorId, int slot) => 0;
        public bool IsSkillReady(int actorId, int slot) => false;
        public int AddBuff(int actorId, int casterId, int buffId) => -1;
        public void RemoveBuff(int actorId, int buffInstanceId) { }
        public int GetBuffStack(int actorId, int buffId) => 0;
        public IReadOnlyList<int> FindActorsInRange(float x, float z, float radius, int teamFilter = -1) => null;
        public int FindNearestActor(float x, float z, float radius, int teamFilter = -1) => -1;
        public float ApplyDamage(int attackerId, int targetId, float damage, int damageType) => 0;
        public float ApplyHeal(int healerId, int targetId, float heal) => 0;

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

        public void OnAllPlayersReady(List<ETPlayerSpawnData> players)
        {
            PlayerSpawnData.Clear();
            if (players != null)
            {
                PlayerSpawnData.AddRange(players);
            }

            ETBattleEnterGameCoordinator.Trigger(this);
            // Note: OnBattleStart is called by StartBattle in DemoProcessComponentSystem.
            // Demo test fixtures must be enabled by the demo/test entry, not by the formal battle driver.
        }
    }
}
