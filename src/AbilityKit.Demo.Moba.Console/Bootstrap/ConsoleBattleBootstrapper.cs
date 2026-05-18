using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Components;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Bootstrap;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Battle.Snapshot;
using AbilityKit.Demo.Moba.Console.Battle.Sync;
using AbilityKit.Demo.Moba.Console.Battle.Sync.View;
using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Presentation;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.Replay;
using AbilityKit.Demo.Moba.Console.Simulation;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Console.AutoTest;
using AbilityKit.Demo.Moba.Config.Core;
using ShareBattleStartPlan = AbilityKit.Demo.Moba.Share.BattleStartPlan;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;
using ShareFrameSnapshotData = AbilityKit.Demo.Moba.Share.FrameSnapshotData;
using ShareSnapshotType = AbilityKit.Demo.Moba.Share.SnapshotType;
using ShareEnterGameData = AbilityKit.Demo.Moba.Share.EnterGameData;
using ShareActorTransformData = AbilityKit.Demo.Moba.Share.ActorTransformData;
using ShareProjectileEventData = AbilityKit.Demo.Moba.Share.ProjectileEventData;
using ShareAreaEventData = AbilityKit.Demo.Moba.Share.AreaEventData;
using ShareDamageEventData = AbilityKit.Demo.Moba.Share.DamageEventData;
using ShareStateHashData = AbilityKit.Demo.Moba.Share.StateHashData;
using ShareMobaOpCode = AbilityKit.Demo.Moba.Share.MobaOpCode;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console
{
    /// <summary>
    /// Console 战斗启动器（纯表现层）
    ///
    /// 职责边界：
    /// - ✅ 组装和初始化各层组件
    /// - ✅ 管理生命周期
    /// - ✅ 转发流程控制命令
    /// - ❌ 不执行战斗逻辑（由 Simulation 层处理）
    /// - ❌ 不计算伤害（由 Simulation 层处理）
    /// - ❌ 不管理实体状态（由 ECS 组件处理）
    /// </summary>
    public sealed class ConsoleBattleBootstrapper : IBattleBootstrapper, IBattleStartConfigProvider
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleFlow _flow;
        private readonly IConsoleBattleView _battleView;
        private readonly ConsoleSyncFeature _syncFeature;
        private readonly ConsoleInputFeature _inputFeature;
        private readonly ConsoleHudFeature _hudFeature;
        private readonly ConsoleInputHandler _inputHandler;
        private readonly List<IWorldModule> _modules;
        private readonly BattleStartConfig _config;
        private AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase _mobaConfig;

        // 表现层数据适配器（从 Simulation 层读取只读数据）
        private ViewActorAdapter? _viewActorAdapter;
        private readonly RecordConfig _recordConfig;

        // 同步适配器（支持帧同步/状态同步切换）
        private IBattleSyncAdapter? _syncAdapter;
        private ConsoleViewBinder? _viewBinder;

        // 输入转发表层（表现层与模拟层解耦）
        private IConsoleInputSink? _inputSink;

        // Share 模块组件
        private ConsoleFrameSnapshotDispatcher? _snapshotDispatcher;
        private ConsoleBattleViewEventSink? _shareViewEventSink;
        private ShareReplayRecorder? _replayRecorder;
        private ShareReplayPlayer? _replayPlayer;

        // 自动测试输入（可选，由 AutoTestRunner 管理）
        private AutoTestInputFeature? _autoTestInput;

        // 逻辑层会话（模拟逻辑层）
        private SimulatedBattleSession? _battleSession;

        private bool _disposed;
        private bool _running;
        private DateTime _lastTick;
        private double _totalTime;

        public PlatformComponents Platform { get; }

        /// <summary>
        /// 同步适配器（帧同步/状态同步）
        /// </summary>
        public IBattleSyncAdapter? SyncAdapter => _syncAdapter;

        /// <summary>
        /// View 绑定器（用于视图插值）
        /// </summary>
        public ConsoleViewBinder? ViewBinder => _viewBinder;

        /// <summary>
        /// ?? IBattleStartConfigProvider? ???
        /// </summary>
        BattleStartConfig IBattleStartConfigProvider.Config => _config;

        public ConsoleBattleBootstrapper() : this(null, null, null, null)
        {
        }

        public ConsoleBattleBootstrapper(RecordConfig? recordConfig)
            : this(null, null, null, recordConfig)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase? mobaConfig)
            : this(config, mobaConfig, null, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase? mobaConfig, IEnumerable<IWorldModule>? additionalModules)
            : this(config, mobaConfig, additionalModules, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase? mobaConfig, IEnumerable<IWorldModule>? additionalModules, RecordConfig? recordConfig)
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ctor - Entry");

            _recordConfig = recordConfig ?? new RecordConfig();

            _modules = new List<IWorldModule>();

            // 使用 ConsoleConfigModule 统一管理配置加载
            _modules.Add(new ConsoleConfigModule());

            // 添加额外的模块
            if (additionalModules != null)
            {
                _modules.AddRange(additionalModules);
            }

            // ????
            _config = config ?? ConsoleConfigLoader.LoadBattleStartConfig();

            // _mobaConfig 通过 DI 在 ConfigureWorld 中解析
            _mobaConfig = mobaConfig;

            Platform = new PlatformComponents();

            _context = new ConsoleBattleContext();
            _context.Plan = _config.BuildPlan();

            _flow = new BattleFlow();

            _battleView = new ConsoleBattleView(
                new ConsoleEntityDisplayService(),
                new ConsoleFloatingTextSystem(),
                new ConsoleAreaViewSystem(),
                new ConsoleProjectileDisplayService(),
                Platform.Renderer);

            _syncFeature = new ConsoleSyncFeature();
            _inputFeature = new ConsoleInputFeature();
            _hudFeature = new ConsoleHudFeature();
            _inputHandler = new ConsoleInputHandler(_inputFeature, _hudFeature, _flow, Platform.Input);

            // SetServices 在 ConfigureWorld 中调用（在 SkillExecutor 创建后）

            // ???????????????????
            _hudFeature.SetBattleView(_battleView);

            _flow.Events.PhaseEntered += OnPhaseEntered;

            // 创建同步适配器（根据配置选择帧同步或状态同步）
            // 注意：Session 在 SetupBattle 后才创建，所以先创建不带 Session 的适配器
            _syncAdapter = SyncAdapterFactory.Create(_context, _config);

            // 创建 View 绑定器
            _viewBinder = new ConsoleViewBinder();
            _viewBinder.TickRate = _config.TickRate;
            _viewBinder.BackTimeSeconds = (float)(1.0 / _config.TickRate);

            // 初始化 Share 模块组件
            InitializeShareComponents();

            Log.Config($"Config loaded: {_config.Name}, TickRate: {_config.TickRate}, SyncMode: {_config.SyncMode}");
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ctor - Exit");
        }

        /// <summary>
        /// 初始化 Share 模块组件
        /// </summary>
        private void InitializeShareComponents()
        {
            Log.Trace("[TRACE] InitializeShareComponents - Entry");

            // 创建快照分发器
            _snapshotDispatcher = new ConsoleFrameSnapshotDispatcher();
            Log.System("[Share] ConsoleFrameSnapshotDispatcher created");

            // 创建 Share 视图事件接收器
            _shareViewEventSink = new ConsoleBattleViewEventSink(_battleView, _config.PlayerId);
            Log.System("[Share] ConsoleBattleViewEventSink created");

            // 创建回放录制器
            _replayRecorder = new ShareReplayRecorder();
            _replayRecorder.SetSnapshotInterval(30); // 每 30 帧记录一次快照
            Log.System("[Share] ShareReplayRecorder created");

            // 创建回放播放器
            _replayPlayer = new ShareReplayPlayer();
            Log.System("[Share] ShareReplayPlayer created");

            Log.Trace("[TRACE] InitializeShareComponents - Exit");
        }

        public IConsoleBattleView BattleView => _battleView;
        public IBattleFlow Flow => _flow;
        public ConsoleBattleContext Context => _context;
        public bool IsRunning => _running;
        public IReadOnlyList<IWorldModule> Modules => _modules;

        /// <summary>
        /// ?? Moba ??
        /// </summary>
        public AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase MobaConfig => _mobaConfig;

        /// <summary>
        /// 表现层角色数据适配器
        /// </summary>
        public ViewActorAdapter? ViewActorAdapter => _viewActorAdapter;

        /// <summary>
        /// 设置自动测试输入特征
        /// </summary>
        /// <summary>
        /// 回放录制器
        /// </summary>
        public ShareReplayRecorder? ReplayRecorder => _replayRecorder;

        /// <summary>
        /// 回放播放器
        /// </summary>
        public ShareReplayPlayer? ReplayPlayer => _replayPlayer;

        /// <summary>
        /// 开始录制回放
        /// </summary>
        public bool StartRecording(string replayId = null)
        {
            if (_replayRecorder == null) return false;
            return _replayRecorder.StartRecording(replayId);
        }

        /// <summary>
        /// 停止录制并保存
        /// </summary>
        public string StopRecording()
        {
            return _replayRecorder?.StopRecording() ?? string.Empty;
        }

        /// <summary>
        /// 开始回放
        /// </summary>
        public bool StartReplay(string filePath)
        {
            if (_replayPlayer == null) return false;
            return _replayPlayer.LoadReplay(filePath);
        }

        public void SetAutoTestInput(AutoTestInputFeature? autoInput)
        {
            _autoTestInput = autoInput;
            if (autoInput != null)
            {
                autoInput.OnAttach(_context);
            }
            else if (_autoTestInput != null)
            {
                _autoTestInput.OnDetach(_context);
            }
        }

        /// <summary>
        /// 构建战斗计划
        /// </summary>
        public Battle.BattleStartPlan Build()
        {
            return _config.BuildPlan();
        }

        /// <summary>
        /// ?? IBattleStartConfigProvider? ???
        /// </summary>
        Battle.BattleStartPlan IBattleStartConfigProvider.BuildPlan() => Build();

        public void Initialize()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Initialize - Entry");
            Log.System($"Initializing... Plan: {_context.Plan}");

            ConfigureWorld();
            _context.InitializeEcsWorld();

            // 设置 Share 模块的订阅关系
            InitializeShareSubscriptions();

            LogBattleConfig();
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Initialize - Exit");
        }

        /// <summary>
        /// 初始化 Share 模块的订阅关系
        /// </summary>
        private void InitializeShareSubscriptions()
        {
            if (_snapshotDispatcher == null || _shareViewEventSink == null)
            {
                Log.Warn("[Share] SnapshotDispatcher or ViewEventSink not initialized");
                return;
            }

            var dispatcher = _snapshotDispatcher.Dispatcher;

            // 订阅进入游戏事件
            dispatcher.Subscribe(ShareMobaOpCode.EnterGameSnapshot, (int frame, ShareEnterGameData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, enterGame: data);
                _shareViewEventSink.OnEnterGameSnapshot(in snapshotData);
            });

            // 订阅角色变换事件
            dispatcher.Subscribe(ShareMobaOpCode.ActorTransformSnapshot, (int frame, ShareActorTransformData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, actorTransforms: data);
                _shareViewEventSink.OnActorTransformSnapshot(in snapshotData);
            });

            // 订阅弹道事件
            dispatcher.Subscribe(ShareMobaOpCode.ProjectileEventSnapshot, (int frame, ShareProjectileEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, projectileEvents: data);
                _shareViewEventSink.OnProjectileEventSnapshot(in snapshotData);
            });

            // 订阅区域事件
            dispatcher.Subscribe(ShareMobaOpCode.AreaEventSnapshot, (int frame, ShareAreaEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, areaEvents: data);
                _shareViewEventSink.OnAreaEventSnapshot(in snapshotData);
            });

            // 订阅伤害事件
            dispatcher.Subscribe(ShareMobaOpCode.DamageEventSnapshot, (int frame, ShareDamageEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, damageEvents: data);
                _shareViewEventSink.OnDamageEventSnapshot(in snapshotData);
            });

            // 订阅状态哈希
            dispatcher.Subscribe(ShareMobaOpCode.StateHashSnapshot, (int frame, ShareStateHashData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, stateHash: data);
                _shareViewEventSink.OnStateHashSnapshot(in snapshotData);
            });

            Log.System("[Share] Snapshot subscriptions initialized");
        }

        private IWorldResolver _worldResolver;

        private void ConfigureWorld()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ConfigureWorld - Entry");
            var builder = new WorldContainerBuilder();

            foreach (var module in _modules)
            {
                Log.Debug($"Configuring module: {module.GetType().Name}");
                module.Configure(builder);
            }

            var container = builder.Build();
            _worldResolver = container;

            // 从 DI 容器解析 MobaConfig（由 ConsoleConfigModule 注册）
            if (_mobaConfig == null)
            {
                _mobaConfig = container.Resolve<MobaConfigDatabase>();
                Log.System($"MobaConfig resolved from DI: {_mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.CharacterMO>().Count} characters, {_mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.SkillMO>().Count} skills");
            }

            // 创建表现层服务
            var effectService = new ConsoleEffectExecutionService();

            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ConfigureWorld - Exit");
        }

        private void CreateBattleSession()
        {
            Log.Trace("[TRACE] CreateBattleSession - Entry");

            // 1. 创建 ConsoleMobaConfigDatabase（用于技能配置）
            var configDb = new ConsoleMobaConfigDatabase(new ConsoleTextAssetLoader());
            configDb.LoadFromResources();
            Log.System($"[Bootstrapper] ConsoleMobaConfigDatabase loaded: {configDb.SkillCount} skills, {configDb.SkillLevelTableCount} level tables");

            // 2. 创建模拟逻辑层会话（会在内部创建 ConsoleActorRepository）
            _battleSession = new SimulatedBattleSession(_context.EcsWorld, _context.LocalActorId, null);

            // 3. 创建配置驱动的技能执行器（依赖 ConsoleActorRepository）
            var skillExecutor = new ConsoleSkillExecutor(configDb, _battleSession.ActorRepository);
            skillExecutor.Initialize();

            // 将 skillExecutor 注入到 Session
            _battleSession.SetSkillExecutor(skillExecutor);
            _battleSession.Initialize();

            // 4. 创建表现层数据适配器（从 Simulation 层读取只读数据）
            _viewActorAdapter = new ViewActorAdapter(_battleSession.ActorRepository);

            // 5. 连接逻辑层到快照分发器（表现层负责连接）
            if (_snapshotDispatcher != null)
            {
                _battleSession.SetSnapshotDispatcher(_snapshotDispatcher);
                Log.System($"[Bootstrapper] Session connected to SnapshotDispatcher");
            }

            // 6. 将 Session 注入到 SyncAdapter（用于获取角色状态）
            if (_syncAdapter is FrameSyncAdapter frameSync)
            {
                frameSync.Initialize(_context, _config, _battleSession);
            }

            // 7. 创建输入转发表层（直接调用模式）
            _inputSink = new DirectCallInputSink(_battleSession);

            // 7. 将 Sink 注入到 InputFeature
            _inputFeature.SetInputSink(_inputSink);

            Log.System($"[Bootstrapper] SimulatedBattleSession with ConsoleSkillExecutor created and connected");
            Log.Trace("[TRACE] CreateBattleSession - Exit");
        }

        private void LogBattleConfig()
        {
            Log.Config("=== Battle Configuration ===");
            Log.Config($"  World: {_context.Plan.WorldId} ({_context.Plan.WorldType})");
            Log.Config($"  Sync: {_context.Plan.SyncMode}, TickRate: {_context.Plan.TickRate}");
            Log.Config($"  Max Players: {_context.Plan.MaxPlayerCount}");
            Log.Config($"  Debug: {_context.Plan.EnableDebug}");
            Log.Config($"  Input Delay: {_context.Plan.InputDelayFrames} frames");

            if (_mobaConfig.TryGetCharacter(0, out var firstChar) && firstChar != null)
            {
                Log.Config("  Characters:");
                foreach (var c in _mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.CharacterMO>().All())
                {
                    var hp = 0;
                    var atk = 0;
                    var def = 0;
                    if (_mobaConfig.TryGetAttributeTemplate(c.AttributeTemplateId, out var attrs) && attrs != null)
                    {
                        hp = attrs.Hp;
                        atk = attrs.PhysicsAttack;
                        def = attrs.PhysicsDefense;
                    }
                    Log.Config($"    - {c.Name} (HP:{hp:F0}, ATK:{atk:F0}, DEF:{def:F0}, TemplateId:{c.AttributeTemplateId})");
                }
            }

            Log.Config("============================");
        }

        public void Start()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Start - Entry");
            Log.System("Starting...");

            // 创建逻辑层会话
            CreateBattleSession();

            _syncFeature.OnAttach(_context);
            _inputFeature.OnAttach(_context);
            _hudFeature.OnAttach(_context);
            _inputHandler.Start();
            _flow.Start();

            // 如果是状态同步模式，连接到服务器
            if (_syncAdapter is StateSyncAdapter stateSync && _config.SyncMode == ShareSyncMode.SnapshotAuthority)
            {
                Log.Sync($"[Bootstrapper] Connecting to server in StateSync mode...");
                if (_config.Network != null)
                {
                    stateSync.Connect();
                }
                else
                {
                    // 使用命令行参数或默认值
                    stateSync.Connect(
                        host: "localhost",
                        port: 4000,
                        roomId: _config.WorldId,
                        playerId: _config.PlayerId);
                }
            }

            _lastTick = DateTime.Now;
            _running = true;
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Start - Exit, Running=true");
        }

        public void Stop()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Stop - Entry");
            Log.System("Stopping...");
            _running = false;
            _flow.Stop();
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Stop - Exit");
        }

        public void Tick(float deltaTime = 0.033f)
        {
            Log.Trace($"[TRACE] ConsoleBattleBootstrapper.Tick - Entry (Running:{_running})");
            if (!_running) return;

            var now = DateTime.Now;
            var elapsed = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            _totalTime += elapsed;
            _context.LogicTimeSeconds = _totalTime;
            _context.LastFrame++;

            Log.Trace($"[TRACE] Tick - Frame:{_context.LastFrame}, Time:{_totalTime:F2}s");

            _flow.Tick((float)elapsed);
            _syncFeature.Tick(_context, (float)elapsed);
            _inputFeature.Tick(_context, (float)elapsed);
            _autoTestInput?.Tick(_context, (float)elapsed);
            _hudFeature.Tick(_context, (float)elapsed);
            _battleView.Tick((float)elapsed);

            // 更新逻辑层会话（处理冷却等）
            _battleSession?.Step((float)elapsed);

            // 更新同步适配器
            _syncAdapter?.Tick((float)elapsed);

            // 更新 View 绑定器（用于状态同步模式下的视图插值）
            if (_viewBinder != null)
            {
                var snapshots = _syncAdapter?.GetAllActorStates() ?? Array.Empty<ActorStateSnapshot>();
                foreach (var snapshot in snapshots)
                {
                    _viewBinder.SyncActor(snapshot.ActorId, snapshot, _syncAdapter?.LogicTimeSeconds ?? _totalTime);
                }
                _viewBinder.TickRender((float)elapsed, _syncAdapter?.LogicTimeSeconds ?? _totalTime);
            }

            // 更新 Share 模块快照分发器（推进帧计数）
            _snapshotDispatcher?.SetFrame(_context.LastFrame);

            // 更新回放录制器
            if (_replayRecorder?.IsRecording == true && _replayRecorder.ShouldRecordSnapshot())
            {
                var snapshotData = _snapshotDispatcher?.SerializeCurrentSnapshot() ?? Array.Empty<byte>();
                if (snapshotData.Length > 0)
                {
                    _replayRecorder.RecordSnapshot(_context.LastFrame, snapshotData);
                    Log.Trace($"[Bootstrapper] Recorded snapshot at frame {_context.LastFrame}");
                }
            }

            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Tick - Exit");
        }

        public void TransitionTo(string phaseName)
        {
            Log.Trace($"[TRACE] ConsoleBattleBootstrapper.TransitionTo({phaseName})");
            Log.System($"Transitioning to: {phaseName}");
            _flow.TransitionTo(phaseName);
        }

        public void SetupBattle()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.SetupBattle - Entry");
            Log.System("Setting up battle...");

            TransitionTo("Connect");
            TransitionTo("CreateOrJoinWorld");
            TransitionTo("LoadAssets");
            TransitionTo("InMatch");

            Log.Trace("[TRACE] SetupBattle - Phases transitioned, registering entities...");
            RegisterEntitiesFromConfig();
            RegisterLocalPlayer();
            _hudFeature.RenderHud();

            Log.Trace("[TRACE] ConsoleBattleBootstrapper.SetupBattle - Exit");
        }

        private void RegisterLocalPlayer()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.RegisterLocalPlayer - Entry");
            // Set local player to first player's actor ID
            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = HashPlayerId(localPlayer.PlayerId);
                Log.Battle($"[Bootstrapper] LocalPlayer: {localPlayer.Name} (ActorId: {_context.LocalActorId})");
                Log.Trace($"[TRACE] RegisterLocalPlayer - Player:{localPlayer.Name}, ActorId:{_context.LocalActorId}");
            }
            else
            {
                // Fallback to demo entity
                _context.LocalActorId = 1;
                Log.Battle($"[Bootstrapper] Using default LocalActorId: {_context.LocalActorId}");
                Log.Trace("[TRACE] RegisterLocalPlayer - Using default ActorId:1");
            }

            Log.Battle($"[Bootstrapper] LocalActorId set to: {_context.LocalActorId}");
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.RegisterLocalPlayer - Exit");
        }

        private void RegisterEntitiesFromConfig()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.RegisterEntitiesFromConfig - Entry");
            Log.Battle($"Setting up battle with {_config.Players.Count} players...");
            Log.Trace($"[TRACE] RegisterEntities - PlayerCount:{_config.Players.Count}");

            int index = 0;
            foreach (var player in _config.Players)
            {
                Log.Trace($"[TRACE] RegisterEntities - Creating character {++index}: {player.Name}");
                CreateCharacterFromPlayer(player);
            }

            Log.Entity($"Registered entities: {_context.EcsWorld.AliveCount} total");
            Log.Trace($"[TRACE] RegisterEntitiesFromConfig - Exit, ECS AliveCount:{_context.EcsWorld.AliveCount}");
        }

        private void CreateCharacterFromPlayer(PlayerConfig player)
        {
            if (!_mobaConfig.TryGetCharacter(player.HeroId, out var charConfig))
            {
                Log.Warn($"Character config not found for HeroId: {player.HeroId}, using defaults");
                CreateEntityForView(
                    actorId: HashPlayerId(player.PlayerId),
                    name: player.Name,
                    characterId: player.HeroId,
                    hp: 500,
                    maxHp: 500,
                    x: player.PositionX,
                    y: player.PositionY,
                    z: player.PositionZ,
                    teamId: player.TeamId);
                return;
            }

            var attrs = _mobaConfig.TryGetAttributeTemplate(charConfig.AttributeTemplateId, out var attrMo) ? attrMo : null;
            CreateEntityForView(
                actorId: HashPlayerId(player.PlayerId),
                name: charConfig.Name,
                characterId: charConfig.Id,
                hp: attrs?.Hp ?? 500,
                maxHp: attrs?.MaxHp ?? 500,
                x: player.PositionX,
                y: player.PositionY,
                z: player.PositionZ,
                teamId: player.TeamId);

            Log.Battle($"Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1})");
        }

        private static int HashPlayerId(string playerId)
        {
            return DeterministicHash.StringToActorId(playerId);
        }

        public void RegisterDemoEntities()
        {
            CreateEntityForView(1, "Warrior", 1001, 800, 800, 0, 0, 0);
            CreateEntityForView(2, "Archer", 1002, 600, 600, 10, 0, 0);
            CreateEntityForView(3, "Mage", 1003, 500, 500, -10, 0, 0);
            CreateEntityForView(101, "Minion_A1", 2001, 300, 300, 20, 0, 0);
            CreateEntityForView(102, "Minion_A2", 2001, 300, 300, 22, 0, 2);
            CreateEntityForView(103, "Minion_A3", 2002, 250, 250, 21, 0, 1);

            Log.Entity($"Registered demo entities: 3 heroes, 3 minions");
            Log.Entity($"ECS World alive count: {_context.EcsWorld.AliveCount}");
        }

        /// <summary>
        /// 为视图层创建实体（纯表现层）
        /// 只负责在视图层注册显示数据，不涉及逻辑层实体创建
        /// </summary>
        private void CreateEntityForView(int actorId, string name, int characterId, float hp, float maxHp, float x, float y, float z, int teamId = 1)
        {
            Log.Trace($"[TRACE] CreateEntityForView - Actor#{actorId} ({name}), HP:{hp:F0}, Pos:({x:F1},{y:F1},{z:F1}), Team:{teamId}");

            // 注册到视图层
            _battleView.RegisterEntity(actorId, name, "Character", hp, maxHp, x, y, z);

            // 注册到 Simulation 层数据仓库
            _battleSession?.ActorRepository.RegisterActor(new ActorState(actorId, name)
            {
                CharacterId = characterId,
                X = x,
                Y = y,
                Z = z,
                Hp = hp,
                HpMax = maxHp,
                PhysicsAttack = 10f,
                PhysicsDefense = 0f,
                MoveSpeed = 5f,
                TeamId = teamId
            });

            Log.Entity($"Created: #{actorId} {name} (CharId:{characterId}, Team:{teamId})");
        }

        public void ShowHud() => _hudFeature.RenderHud();

        public void PrintWorldStatus()
        {
            var world = _context.EcsWorld;
            if (world == null)
            {
                Log.System("ECS World: not initialized");
                return;
            }

            Log.System($"ECS World Status:");
            Log.System($"  Alive entities: {world.AliveCount}");
            Log.System($"  Phase: {_flow.CurrentPhase}");
            Log.System($"  Frame: {_context.LastFrame}");
            Log.System($"  ActorCount: {_battleSession?.ActorRepository.ActorCount ?? 0}");
        }

        private void OnPhaseEntered(string phaseName)
        {
            Log.Trace($"[TRACE] ConsoleBattleBootstrapper.OnPhaseEntered({phaseName})");
            Log.Debug($"Phase entered: {phaseName}");

            if (phaseName == "InMatch")
            {
                _context.State = BattleState.InMatch;
                _context.IsInitialized = true;
                Log.Battle("Battle started!");
                Log.Trace("[TRACE] OnPhaseEntered - Entered InMatch, Context updated");
                PrintWorldStatus();
            }
            else if (phaseName == "End")
            {
                Log.Battle("Battle ended!");
            }
        }

        public void Dispose()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Dispose - Entry");
            if (_disposed) return;
            _disposed = true;

            Log.System("Disposing...");
            _battleSession?.Dispose();
            _inputHandler?.Dispose();
            _hudFeature?.OnDetach(_context);
            _inputFeature?.OnDetach(_context);
            _syncFeature?.OnDetach(_context);
            _viewBinder?.Dispose();

            // 清理 Share 模块组件
            _snapshotDispatcher?.Dispose();
            _shareViewEventSink?.Dispose();
            _replayRecorder?.Dispose();
            _replayPlayer?.Dispose();

            _syncAdapter?.Dispose();
            _flow?.Dispose();
            _battleView?.Dispose();
            _context?.Dispose();
            Log.System("Disposed.");
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Dispose - Exit");
        }
    }

    /// <summary>
    /// Platform components container
    /// Contains all platform-related abstractions for swapping implementations
    /// </summary>
    public sealed class PlatformComponents
    {
        public IOutput Output { get; }
        public IInputSource Input { get; }
        public IRenderer Renderer { get; }
        public ILogSink LogSink { get; }

        public PlatformComponents() : this(null, null, null, null)
        {
        }

        public PlatformComponents(
            IOutput? output,
            IInputSource? input,
            IRenderer? renderer,
            ILogSink? logSink)
        {
            Output = output ?? new Platform.Console_.ConsoleOutput();
            Input = input ?? new Platform.Console_.ConsoleInputSource();
            Renderer = renderer ?? new Platform.Console_.ConsoleRenderer(80, 40);
            LogSink = logSink ?? new ConsoleLogSink(Output);
        }

        public PlatformComponents(IOutput output, IInputSource input, IRenderer renderer)
            : this(output, input, renderer, null)
        {
        }
    }

    /// <summary>
    /// Console ?? Sink ??
    /// ?????????
    /// </summary>
    public sealed class ConsoleLogSink : ILogSink
    {
        private readonly IOutput _output;

        public string Name => "ConsoleLogSink";

        public ConsoleLogSink(IOutput output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void Log(OutputChannel channel, string message)
        {
            _output.Write(channel, message);
        }
    }
}
