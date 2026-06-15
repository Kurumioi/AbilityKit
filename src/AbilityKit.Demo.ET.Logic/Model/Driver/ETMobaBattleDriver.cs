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
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using ET.AbilityKit.Demo.ET.Share;
using ActorKind = ET.AbilityKit.Demo.ET.Share.ActorKind;
using AbilityKit.Core.Mathematics;
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
    public class ETMobaBattleDriver : Entity, IAwake, IUpdate, IDestroy
    {
        // ============== Runtime Driver State ==============

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
            set => _viewSink = value;
        }

        // ============== Config Access ==============

        public ITextAssetLoader TextAssetLoader { get; set; }

        // ============== Player Spawn Data ==============

        public List<ETPlayerSpawnData> PlayerSpawnData { get; set; } = new List<ETPlayerSpawnData>();

        // ============== Snapshot Dispatcher ==============

        public FrameSnapshotDispatcher SnapshotDispatcher { get; set; }
        public ActorSpawnData[] LastActorSpawnSnapshot { get; private set; } = Array.Empty<ActorSpawnData>();
        public ActorTransformData[] LastActorTransformSnapshot { get; private set; } = Array.Empty<ActorTransformData>();
        public StateHashData LastStateHashSnapshot { get; private set; }

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

        private BattleFramePipeline _framePipeline;

        // ============== Lifecycle Methods ==============

        public void Awake()
        {
            EnsureSnapshotHandlersRegistered();
        }

        public void EnsureSnapshotHandlersRegistered()
        {
            HandlerRegistry.RegisterSnapshotHandlers(this);
        }

        public void Update(ETMobaBattleDriver self)
        {
        }

        public void OnDestroy(ETMobaBattleDriver self)
        {
        }

        // ============== Runtime Driver Methods ==============

        public void Initialize(in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            if (viewSink == null)
            {
                throw new ArgumentNullException(nameof(viewSink));
            }

            Plan = plan;
            RuntimeGameStarted = false;
            ViewSink = viewSink;
            TickRate = plan.TickRate > 0 ? plan.TickRate : 30;

            try
            {
                SnapshotDispatcher = new FrameSnapshotDispatcher();

                var worldType = BattleWorldTypes.Battle;
                var creator = LogicWorldRegistry.GetCreator(worldType);
                if (creator == null)
                {
                    throw new InvalidOperationException($"No LogicWorldCreator registered for world type '{worldType}'");
                }

                creator.CreateAndInitialize(this, in plan);

                CurrentFrame = 0;
                LogicTimeSeconds = 0;
                LastTickTime = 0;
                IsRunning = false;

                Log.Info($"[ETMobaBattleDriver] Initialized: TickRate={TickRate}, WorldId={Plan.WorldId}, World={World?.Id}");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                Log.Error($"[ETMobaBattleDriver] Initialize failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] Initialize failed: {ex.GetType().Name} - {ex.Message}");
                throw new InvalidOperationException("Failed to initialize battle driver due to unexpected error", ex);
            }
        }

        public void Start()
        {
            IMobaBattleRuntimePort runtime = null;
            if (World?.Services == null ||
                !World.Services.TryResolve(out runtime) ||
                runtime == null ||
                !runtime.Status.IsReadyForBattleLoop)
            {
                var status = runtime != null ? runtime.Status.ToString() : "runtime port missing";
                Log.Error($"[ETMobaBattleDriver] IMobaBattleRuntimePort is not ready: {status}");
                throw new InvalidOperationException("IMobaBattleRuntimePort must be ready for battle loop");
            }

            IsRunning = true;
            LastTickTime = 0;
            CurrentFrame = 0;
            LogicTimeSeconds = 0;
        }

        public void Stop()
        {
            IsRunning = false;
            RuntimeGameStarted = false;
            Log.Info("[ETMobaBattleDriver] Stopped");
        }

        public void Destroy()
        {
            SnapshotDispatcher = null;
            Units.Clear();
            World = null;
            HostRuntime = null;
            WorldManager = null;
            ViewSink = null;
            _framePipeline = null;

            RuntimeGameStarted = false;
            IsRunning = false;
            CurrentFrame = 0;
            LogicTimeSeconds = 0;
            LastTickTime = 0;

            SnapshotHandlers?.Clear();
            Log.Info("[ETMobaBattleDriver] Destroyed");
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            _framePipeline ??= new BattleFramePipeline();
            LogicTimeSeconds += deltaTime;
            LastTickTime = LogicTimeSeconds;
            _framePipeline.Tick(this, deltaTime);
        }

        // ============== Snapshot Handling ==============

        public void HandleSnapshot(in FrameSnapshotData snapshot)
        {
            DispatchFrameSnapshot(in snapshot);

            foreach (var handler in SnapshotHandlers)
            {
                if (handler.CanHandle(in snapshot))
                {
                    handler.Handle(this, in snapshot);
                }
            }
        }

        private void DispatchFrameSnapshot(in FrameSnapshotData snapshot)
        {
            var dispatcher = SnapshotDispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (snapshot.EnterGame.HasValue)
            {
                dispatcher.DispatchEnterGame(snapshot.FrameIndex, snapshot.EnterGame);
            }

            if (snapshot.ActorTransforms != null && snapshot.ActorTransforms.Count > 0)
            {
                var actorTransforms = ToArray(snapshot.ActorTransforms);
                LastActorTransformSnapshot = actorTransforms;
                dispatcher.DispatchActorTransform(snapshot.FrameIndex, actorTransforms);
            }

            if (snapshot.ProjectileEvents != null && snapshot.ProjectileEvents.Count > 0)
            {
                dispatcher.DispatchProjectileEvent(snapshot.FrameIndex, ToArray(snapshot.ProjectileEvents));
            }

            if (snapshot.AreaEvents != null && snapshot.AreaEvents.Count > 0)
            {
                dispatcher.DispatchAreaEvent(snapshot.FrameIndex, ToArray(snapshot.AreaEvents));
            }

            if (snapshot.DamageEvents != null && snapshot.DamageEvents.Count > 0)
            {
                dispatcher.DispatchDamageEvent(snapshot.FrameIndex, ToArray(snapshot.DamageEvents));
            }

            if (snapshot.StateHash.HasValue)
            {
                LastStateHashSnapshot = snapshot.StateHash;
                dispatcher.DispatchStateHash(snapshot.FrameIndex, snapshot.StateHash);
            }

            if (snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0)
            {
                var actorSpawns = ToArray(snapshot.ActorSpawns);
                LastActorSpawnSnapshot = actorSpawns;
                dispatcher.DispatchActorSpawn(snapshot.FrameIndex, actorSpawns);
            }
        }

        private static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<T>();
            }

            if (source is T[] array)
            {
                return array;
            }

            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
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
