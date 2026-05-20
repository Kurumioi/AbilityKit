using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Components;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Battle.Input;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Battle.Sync;
using AbilityKit.Demo.Moba.Console.Battle.Features;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Presentation;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.Replay;
using AbilityKit.Demo.Moba.Console.Simulation;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Console.AutoTest;
using AbilityKit.Demo.Moba.Config.Core;
using BattleConfig = AbilityKit.Demo.Moba.Console.Battle.Config;
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
using ShareFrameSnapshotDispatcher = AbilityKit.Demo.Moba.Share.FrameSnapshotDispatcher;
using ShareActorSpawnData = AbilityKit.Demo.Moba.Share.ActorSpawnData;
using EC = AbilityKit.World.ECS;
using Bootstrap = AbilityKit.Demo.Moba.Console.Bootstrap;
using ECSEntities = AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;
using ECSComp = AbilityKit.Demo.Moba.Console.Battle.ECS;
using ViewBinderNamespace = AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console
{
    /// <summary>
    /// Console Battle bootstrapper
    /// Orchestrates initialization and lifecycle of all presentation layer components
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
        private readonly BattleConfig.BattleStartConfig _config;
        private readonly RecordConfig _recordConfig;
        private MobaConfigDatabase _mobaConfig;

        private ViewActorAdapter? _viewActorAdapter;
        private IBattleSyncAdapter? _syncAdapter;
        private ViewBinderNamespace.IConsoleViewBinder? _viewBinder;

        private ShareFrameSnapshotDispatcher? _snapshotDispatcher;
        private ConsoleBattleViewEventSink? _shareViewEventSink;
        private ShareReplayRecorder? _replayRecorder;
        private ShareReplayPlayer? _replayPlayer;

        private AutoTestInputFeature? _autoTestInput;

        private bool _disposed;
        private bool _running;
        private DateTime _lastTick;
        private double _totalTime;
        private IWorldResolver _worldResolver;

        public PlatformComponents Platform { get; }
        public IBattleSyncAdapter? SyncAdapter => _syncAdapter;
        public ViewBinderNamespace.IConsoleViewBinder? ViewBinder => _viewBinder;
        public IConsoleBattleView BattleView => _battleView;
        public IBattleFlow Flow => _flow;
        public ConsoleBattleContext Context => _context;
        public bool IsRunning => _running;
        public IReadOnlyList<IWorldModule> Modules => _modules;
        public ViewActorAdapter? ViewActorAdapter => _viewActorAdapter;
        public ShareReplayRecorder? ReplayRecorder => _replayRecorder;
        public ShareReplayPlayer? ReplayPlayer => _replayPlayer;

        BattleConfig.BattleStartConfig IBattleStartConfigProvider.Config => _config;

        public ConsoleBattleBootstrapper(BattleConfig.BattleStartConfig? config = null, MobaConfigDatabase? mobaConfig = null,
            IEnumerable<IWorldModule>? additionalModules = null, RecordConfig? recordConfig = null)
        {
            _recordConfig = recordConfig ?? new RecordConfig();
            _modules = new List<IWorldModule> { new Bootstrap.ConsoleConfigModule() };

            if (additionalModules != null)
            {
                _modules.AddRange(additionalModules);
            }

            _config = config ?? Bootstrap.ConsoleConfigLoader.LoadBattleStartConfig();
            _mobaConfig = mobaConfig;

            Platform = new PlatformComponents();
            _context = new ConsoleBattleContext { Plan = _config.BuildPlan() };
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

            _hudFeature.SetBattleView(_battleView);
            _flow.Events.PhaseEntered += OnPhaseEntered;

            _syncAdapter = SyncAdapterFactory.Create(_context, _config);

            _viewBinder = new ViewBinderNamespace.ConsoleViewBinder
            {
                TickRate = _config.TickRate,
                BackTimeSeconds = (float)(1.0 / _config.TickRate)
            };

            InitializeShareComponents();

            Log.Config($"Config loaded: {_config.Name}, TickRate: {_config.TickRate}, SyncMode: {_config.SyncMode}");
        }

        private void InitializeShareComponents()
        {
            _snapshotDispatcher = new ShareFrameSnapshotDispatcher();
            _shareViewEventSink = new ConsoleBattleViewEventSink(_battleView, _config.PlayerId);

            _replayRecorder = new ShareReplayRecorder();
            _replayRecorder.SetSnapshotInterval(30);

            _replayPlayer = new ShareReplayPlayer();

            Log.System("[Share] Share components initialized");
        }

        public bool StartRecording(string replayId = null) => _replayRecorder?.StartRecording(replayId) ?? false;

        public string StopRecording() => _replayRecorder?.StopRecording() ?? string.Empty;

        public bool StartReplay(string filePath) => _replayPlayer?.LoadReplay(filePath) ?? false;

        public void SetAutoTestInput(AutoTestInputFeature? autoInput)
        {
            _autoTestInput = autoInput;
            if (autoInput != null)
            {
                autoInput.OnAttach(_context);
            }
            else
            {
                _autoTestInput?.OnDetach(_context);
            }
        }

        public BattleConfig.BattleStartPlan Build() => _config.BuildPlan();
        BattleConfig.BattleStartPlan IBattleStartConfigProvider.BuildPlan() => Build();

        public void Initialize()
        {
            Log.System($"Initializing... Plan: {_context.Plan}");
            ConfigureWorld();
            _context.InitializeEcsWorld();
            InitializeShareSubscriptions();
            LogBattleConfig();
        }

        private void InitializeShareSubscriptions()
        {
            if (_snapshotDispatcher == null || _shareViewEventSink == null)
            {
                Log.Warn("[Share] SnapshotDispatcher or ViewEventSink not initialized");
                return;
            }

            var dispatcher = _snapshotDispatcher;

            dispatcher.Subscribe(ShareMobaOpCode.EnterGameSnapshot, (int frame, ShareEnterGameData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, enterGame: data);
                _shareViewEventSink.OnEnterGameSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.ActorSpawnSnapshot, (int frame, ShareActorSpawnData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, actorSpawns: data);
                _shareViewEventSink.OnActorSpawnSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.ActorTransformSnapshot, (int frame, ShareActorTransformData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, actorTransforms: data);
                _shareViewEventSink.OnActorTransformSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.ProjectileEventSnapshot, (int frame, ShareProjectileEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, projectileEvents: data);
                _shareViewEventSink.OnProjectileEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.AreaEventSnapshot, (int frame, ShareAreaEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, areaEvents: data);
                _shareViewEventSink.OnAreaEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.DamageEventSnapshot, (int frame, ShareDamageEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, damageEvents: data);
                _shareViewEventSink.OnDamageEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(ShareMobaOpCode.StateHashSnapshot, (int frame, ShareStateHashData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, stateHash: data);
                _shareViewEventSink.OnStateHashSnapshot(in snapshotData);
            });

            Log.System("[Share] Snapshot subscriptions initialized");
        }

        private void ConfigureWorld()
        {
            var builder = new WorldContainerBuilder();

            foreach (var module in _modules)
            {
                module.Configure(builder);
            }

            var container = builder.Build();
            _worldResolver = container;

            if (_mobaConfig == null)
            {
                _mobaConfig = container.Resolve<MobaConfigDatabase>();
                Log.System($"MobaConfig resolved: {_mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.CharacterMO>().Count} characters");
            }

            var effectService = new ConsoleEffectExecutionService();
        }

        private void CreateBattleSession()
        {
            var inputSink = new Bootstrap.DirectCallInputSink();
            _inputFeature.SetInputSink(inputSink);
            Log.System($"[Bootstrapper] DirectCall InputSink initialized");
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
                    Log.Config($"    - {c.Name} (HP:{hp:F0}, ATK:{atk:F0}, DEF:{def:F0})");
                }
            }

            Log.Config("============================");
        }

        public void Start()
        {
            Log.System("Starting...");

            // 设置 BattleFlow 的 Context
            _flow.SetBattleContext(_context);

            // 配置 InMatchPhase 的 Features
            var inMatchPhase = _flow.GetInMatchPhase();
            if (inMatchPhase != null)
            {
                inMatchPhase.ConfigureFeatures(features =>
                {
                    features.AddConsoleFeature(_syncFeature);
                    features.AddConsoleFeature(_inputFeature);
                    features.AddConsoleFeature(_hudFeature);
                });
            }

            CreateBattleSession();

            // 不再手动 Attach Features，由 PhaseHost 管理
            // _syncFeature.OnAttach(_context);
            // _inputFeature.OnAttach(_context);
            // _hudFeature.OnAttach(_context);

            _inputHandler.Start();
            _flow.Start();

            if (_syncAdapter is StateSyncAdapter stateSync && _config.SyncMode == ShareSyncMode.SnapshotAuthority)
            {
                Log.Sync($"[Bootstrapper] StateSync mode, connecting to server...");
                if (_config.Network != null)
                {
                    stateSync.Connect();
                }
                else
                {
                    stateSync.Connect(host: "localhost", port: 4000, roomId: _config.WorldId, playerId: _config.PlayerId);
                }
            }

            _lastTick = DateTime.Now;
            _running = true;
        }

        public void Stop()
        {
            Log.System("Stopping...");
            _running = false;
            _flow.Stop();
        }

        public void Tick(float deltaTime = 0.033f)
        {
            if (!_running) return;

            var now = DateTime.Now;
            var elapsed = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            _totalTime += elapsed;
            _context.LogicTimeSeconds = _totalTime;
            _context.LastFrame++;

            // 由 PhaseHost -> InMatchPhase -> FeatureHost 管理 Features
            _flow.Tick((float)elapsed);

            // Features 现在由 FeatureHost.Tick() 自动管理
            // _syncFeature.Tick(_context, (float)elapsed);
            // _inputFeature.Tick(_context, (float)elapsed);
            // _autoTestInput?.Tick(_context, (float)elapsed);
            // _hudFeature.Tick(_context, (float)elapsed);

            _battleView.Tick((float)elapsed);
            _syncAdapter?.Tick((float)elapsed);

            if (_viewBinder != null)
            {
                var snapshots = _syncAdapter?.GetAllActorStates() ?? Array.Empty<ActorStateSnapshot>();
                foreach (var snapshot in snapshots)
                {
                    _viewBinder.SyncActor(snapshot.ActorId, snapshot, _syncAdapter?.LogicTimeSeconds ?? _totalTime);
                }
                _viewBinder.TickRender((float)elapsed, _syncAdapter?.LogicTimeSeconds ?? _totalTime);
            }

            if (_replayRecorder?.IsRecording == true && _replayRecorder.ShouldRecordSnapshot())
            {
                RecordCurrentSnapshot();
            }
        }

        private void RecordCurrentSnapshot()
        {
            if (_replayRecorder == null) return;

            try
            {
                var snapshotJson = System.Text.Json.JsonSerializer.Serialize(
                    new { Frame = _context.LastFrame, Actors = Array.Empty<object>() });
                var snapshotData = System.Text.Encoding.UTF8.GetBytes(snapshotJson);
                _replayRecorder.RecordSnapshot(_context.LastFrame, snapshotData);
            }
            catch (Exception ex)
            {
                Log.Warn($"[Bootstrapper] Failed to record snapshot: {ex.Message}");
            }
        }

        public void TransitionTo(string phaseName)
        {
            Log.System($"Transitioning to: {phaseName}");
            _flow.TransitionTo(phaseName);
        }

        public void SetupBattle()
        {
            Log.System("Setting up battle...");

            TransitionTo("Connect");
            TransitionTo("CreateOrJoinWorld");
            TransitionTo("LoadAssets");
            TransitionTo("InMatch");

            RegisterEntitiesFromConfig();
            RegisterLocalPlayer();
            _hudFeature.RenderHud();
        }

        private void RegisterLocalPlayer()
        {
            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = DeterministicHash.StringToActorId(localPlayer.PlayerId);
                Log.Battle($"[Bootstrapper] LocalPlayer: {localPlayer.Name} (ActorId: {_context.LocalActorId})");
            }
            else
            {
                _context.LocalActorId = 1;
                Log.Battle($"[Bootstrapper] Using default LocalActorId: {_context.LocalActorId}");
            }
        }

        private void RegisterEntitiesFromConfig()
        {
            Log.Battle($"Setting up battle with {_config.Players.Count} players...");

            int index = 0;
            foreach (var player in _config.Players)
            {
                CreateCharacterFromPlayer(player);
            }

            Log.Entity($"Registered entities: {_context.EcsWorld.AliveCount} total");
        }

        private void CreateCharacterFromPlayer(PlayerConfig player)
        {
            float physicsAttack = 10f;
            float physicsDefense = 0f;
            float moveSpeed = 5f;

            if (_mobaConfig.TryGetCharacter(player.HeroId, out var charConfig) && charConfig != null)
            {
                if (_mobaConfig.TryGetAttributeTemplate(charConfig.AttributeTemplateId, out var attrs) && attrs != null)
                {
                    physicsAttack = attrs.PhysicsAttack;
                    physicsDefense = attrs.PhysicsDefense;
                    moveSpeed = attrs.MoveSpeed;
                }

                CreateEntityForView(
                    actorId: DeterministicHash.StringToActorId(player.PlayerId),
                    name: charConfig.Name,
                    characterId: charConfig.Id,
                    hp: attrs?.Hp ?? 500,
                    maxHp: attrs?.MaxHp ?? 500,
                    x: player.PositionX,
                    y: player.PositionY,
                    z: player.PositionZ,
                    teamId: player.TeamId,
                    physicsAttack: physicsAttack,
                    physicsDefense: physicsDefense,
                    moveSpeed: moveSpeed);

                Log.Battle($"Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1})");
            }
            else
            {
                Log.Warn($"Character config not found for HeroId: {player.HeroId}, using defaults");
                CreateEntityForView(
                    actorId: DeterministicHash.StringToActorId(player.PlayerId),
                    name: player.Name,
                    characterId: player.HeroId,
                    hp: 500, maxHp: 500,
                    x: player.PositionX, y: player.PositionY, z: player.PositionZ,
                    teamId: player.TeamId,
                    physicsAttack: physicsAttack,
                    physicsDefense: physicsDefense,
                    moveSpeed: moveSpeed);
            }
        }

        private void CreateEntityForView(int actorId, string name, int characterId, float hp, float maxHp,
            float x, float y, float z, int teamId = 1, float physicsAttack = 10f, float physicsDefense = 0f, float moveSpeed = 5f)
        {
            var entity = _context.EntityFactory.CreateCharacter(actorId, entityCode: characterId);

            DispatchActorSpawnEvent(actorId, characterId, name, x, y, z, teamId, hp, maxHp);
            Log.Entity($"Created: #{actorId} {name} (CharId:{characterId}, Team:{teamId})");
        }

        private void DispatchActorSpawnEvent(int actorId, int characterId, string name, float x, float y, float z, int teamId, float hp, float maxHp)
        {
            if (_snapshotDispatcher == null)
            {
                Log.Warn("[Bootstrapper] Cannot dispatch ActorSpawn: _snapshotDispatcher is null");
                return;
            }

            var spawnData = new ShareActorSpawnData(actorId, characterId, name, x, y, z, 0f, 1f, teamId, maxHp, hp);
            _snapshotDispatcher.DispatchActorSpawn(_context.LastFrame, new[] { spawnData });
        }

        public void ShowHud() => _hudFeature.RenderHud();

        /// <summary>
        /// Register demo entities for automated testing
        /// </summary>
        public void RegisterDemoEntities()
        {
            CreateEntityForView(1, "Warrior", 1001, 800, 800, 0, 0, 0, physicsAttack: 15f, physicsDefense: 5f, moveSpeed: 4f);
            CreateEntityForView(2, "Archer", 1002, 600, 600, 10, 0, 0, physicsAttack: 20f, physicsDefense: 2f, moveSpeed: 5f);
            CreateEntityForView(3, "Mage", 1003, 500, 500, -10, 0, 0, physicsAttack: 25f, physicsDefense: 1f, moveSpeed: 4.5f);
            CreateEntityForView(101, "Minion_A1", 2001, 300, 300, 20, 0, 0, physicsAttack: 8f, physicsDefense: 1f, moveSpeed: 3f);
            CreateEntityForView(102, "Minion_A2", 2001, 300, 300, 22, 0, 2, physicsAttack: 8f, physicsDefense: 1f, moveSpeed: 3f);
            CreateEntityForView(103, "Minion_A3", 2002, 250, 250, 21, 0, 1, physicsAttack: 6f, physicsDefense: 0.5f, moveSpeed: 3f);

            Log.Entity($"Registered demo entities: 3 heroes, 3 minions");
            Log.Entity($"ECS World alive count: {_context.EcsWorld.AliveCount}");
        }

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
        }

        private void OnPhaseEntered(string phaseName)
        {
            Log.Debug($"Phase entered: {phaseName}");

            if (phaseName == "InMatch")
            {
                _context.State = BattleState.InMatch;
                _context.IsInitialized = true;
                Log.Battle("Battle started!");
                PrintWorldStatus();
            }
            else if (phaseName == "End")
            {
                Log.Battle("Battle ended!");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Log.System("Disposing...");

            _inputHandler?.Dispose();

            // Features 现在由 FeatureHost 管理，不需要手动 Detach
            // _hudFeature?.OnDetach(_context);
            // _inputFeature?.OnDetach(_context);
            // _syncFeature?.OnDetach(_context);

            _viewBinder?.Dispose();
            _snapshotDispatcher?.Dispose();
            _shareViewEventSink?.Dispose();
            _replayRecorder?.Dispose();
            _replayPlayer?.Dispose();
            _syncAdapter?.Dispose();
            _flow?.Dispose();
            _battleView?.Dispose();
            _context?.Dispose();

            Log.System("Disposed.");
        }
    }

    public sealed class PlatformComponents
    {
        public IOutput Output { get; }
        public IInputSource Input { get; }
        public IRenderer Renderer { get; }
        public ILogSink LogSink { get; }

        public PlatformComponents(IOutput? output = null, IInputSource? input = null,
            IRenderer? renderer = null, ILogSink? logSink = null)
        {
            Output = output ?? new Platform.Console_.ConsoleOutput();
            Input = input ?? new Platform.Console_.ConsoleInputSource();
            Renderer = renderer ?? new Platform.Console_.ConsoleRenderer(80, 40);
            LogSink = logSink ?? new ConsoleLogSink(Output);
        }
    }

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
