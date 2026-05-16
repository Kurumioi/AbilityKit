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
using AbilityKit.Demo.Moba.Console.Battle.Sync;
using AbilityKit.Demo.Moba.Console.Battle.Sync.View;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Services;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Demo.Moba.Console.Replay;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Console.MobaCore;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console
{
    /// <summary>
    /// Console ???
    /// ?? [WorldService] ??????????
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
        private MobaConfigDatabase _mobaConfig;
        private readonly BattleServices _battleServices;
        private readonly ConsoleSkillExecutor _skillExecutor;
        private readonly RecordConfig _recordConfig;

        // 同步适配器（支持帧同步/状态同步切换）
        private IBattleSyncAdapter? _syncAdapter;
        private ConsoleViewBinder? _viewBinder;

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

        public ConsoleBattleBootstrapper(BattleStartConfig? config, MobaConfigDatabase? mobaConfig)
            : this(config, mobaConfig, null, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, MobaConfigDatabase? mobaConfig, IEnumerable<IWorldModule>? additionalModules)
            : this(config, mobaConfig, additionalModules, null)
        {
        }

        public ConsoleBattleBootstrapper(BattleStartConfig? config, MobaConfigDatabase? mobaConfig, IEnumerable<IWorldModule>? additionalModules, RecordConfig? recordConfig)
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

            // ??????
            _battleServices = new BattleServices((BattleViewServices)_battleView);
            _skillExecutor = new ConsoleSkillExecutor(_mobaConfig, _battleServices);

            _syncFeature = new ConsoleSyncFeature();
            _inputFeature = new ConsoleInputFeature();
            _hudFeature = new ConsoleHudFeature();
            _inputHandler = new ConsoleInputHandler(_inputFeature, _hudFeature, _flow, Platform.Input);

            // SetServices ???????????????
            _inputFeature.SetServices(_skillExecutor, _battleServices);

            // ???????????????????
            _hudFeature.SetBattleView(_battleView);

            _flow.Events.PhaseEntered += OnPhaseEntered;

            // 创建同步适配器（根据配置选择帧同步或状态同步）
            _syncAdapter = SyncAdapterFactory.Create(_context, _config);
            _syncAdapter.Initialize(_context, _config);

            // 创建 View 绑定器
            _viewBinder = new ConsoleViewBinder();
            _viewBinder.TickRate = _config.TickRate;
            _viewBinder.BackTimeSeconds = (float)(1.0 / _config.TickRate);

            Log.Config($"Config loaded: {_config.Name}, TickRate: {_config.TickRate}, SyncMode: {_config.SyncMode}");
            Log.Config($"MobaConfig: {_mobaConfig.CharacterCount} characters, {_mobaConfig.SkillCount} skills, {_mobaConfig.ProjectileCount} projectiles");
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ctor - Exit");
        }

        public IConsoleBattleView BattleView => _battleView;
        public IBattleFlow Flow => _flow;
        public ConsoleBattleContext Context => _context;
        public bool IsRunning => _running;
        public IReadOnlyList<IWorldModule> Modules => _modules;

        /// <summary>
        /// ?? Moba ??
        /// </summary>
        public MobaConfigDatabase MobaConfig => _mobaConfig;

        /// <summary>
        /// ??????
        /// </summary>
        public BattleServices BattleServices => _battleServices;

        /// <summary>
        /// ???????
        /// </summary>
        public ConsoleSkillExecutor SkillExecutor => _skillExecutor;

        /// <summary>
        /// ??????
        /// </summary>
        public BattleStartPlan Build()
        {
            return _config.BuildPlan();
        }

        /// <summary>
        /// ?? IBattleStartConfigProvider? ???
        /// </summary>
        BattleStartPlan IBattleStartConfigProvider.BuildPlan() => Build();

        public void Initialize()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Initialize - Entry");
            Log.System($"Initializing... Plan: {_context.Plan}");

            ConfigureWorld();
            _context.InitializeEcsWorld();

            LogBattleConfig();
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Initialize - Exit");
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
                Log.System($"MobaConfig resolved from DI: {_mobaConfig.CharacterCount} characters, {_mobaConfig.SkillCount} skills");
            }

            // ??? moba.core ??
            InitializeMobaCore();

            Log.Trace("[TRACE] ConsoleBattleBootstrapper.ConfigureWorld - Exit");
        }

        private void InitializeMobaCore()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.InitializeMobaCore - Entry");
            Log.System("Initializing MobaCore...");

            // ???? MobaActorRegistry????????????
            MobaActorRegistry actorRegistry = null;
            try
            {
                actorRegistry = _worldResolver.Resolve<MobaActorRegistry>();
            }
            catch (Exception ex)
            {
                Log.Trace($"[TRACE] MobaActorRegistry resolve failed (expected in standalone mode): {ex.Message}");
            }

            if (actorRegistry != null)
            {
                Log.System("MobaActorRegistry resolved");
                Log.Trace("[TRACE] InitializeMobaCore - ActorRegistry found");
                _context.MobaCore.ActorRegistry = actorRegistry;
            }
            else
            {
                Log.System("MobaActorRegistry not found (using ConsoleSkillExecutor instead)");
                Log.Trace("[TRACE] InitializeMobaCore - ActorRegistry NOT found, using fallback");
            }

            // ???? SkillPipelineLibrary
            IMobaSkillPipelineLibrary skillPipelineLib = null;
            try
            {
                skillPipelineLib = _worldResolver.Resolve<IMobaSkillPipelineLibrary>();
            }
            catch (Exception ex)
            {
                Log.Trace($"[TRACE] IMobaSkillPipelineLibrary resolve failed: {ex.Message}");
            }

            if (skillPipelineLib != null)
            {
                Log.System("IMobaSkillPipelineLibrary resolved");
                Log.Trace("[TRACE] InitializeMobaCore - SkillPipelineLibrary found");
                _context.MobaCore.SkillPipelineLibrary = skillPipelineLib;
            }
            else
            {
                Log.System("IMobaSkillPipelineLibrary not found (using ConsoleSkillExecutor)");
                Log.Trace("[TRACE] InitializeMobaCore - SkillPipelineLibrary NOT found");
            }

            _context.MobaCore.WorldServices = _worldResolver;

            if (_context.MobaCore.ActorRegistry != null)
            {
                var mobaSkillExecutor = new MobaCoreSkillExecutor(_worldResolver, _context.MobaCore.ActorRegistry);
                _context.MobaCore.SkillExecutor = mobaSkillExecutor;

                mobaSkillExecutor.OnSkillCast += (actorId, skillId, slot, targetId) =>
                {
                    Log.Skill($"[MobaCore] Skill cast: Actor#{actorId} Skill#{skillId} Slot{slot} -> Target#{targetId}");
                };

                mobaSkillExecutor.OnDamageDealt += (sourceId, targetId, damage, skillId) =>
                {
                    Log.Damage($"[MobaCore] Damage: Actor#{sourceId} dealt {damage:F0} to Actor#{targetId} with Skill#{skillId}");
                };

                Log.System("MobaCoreSkillExecutor initialized");
                Log.Trace("[TRACE] InitializeMobaCore - MobaCoreSkillExecutor initialized");
            }
            else
            {
                Log.System("MobaCoreSkillExecutor skipped (using ConsoleSkillExecutor instead)");
            }

            Log.System("MobaCore initialized (standalone mode)");
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.InitializeMobaCore - Exit");
        }

        private void LogBattleConfig()
        {
            Log.Config("=== Battle Configuration ===");
            Log.Config($"  World: {_context.Plan.WorldId} ({_context.Plan.WorldType})");
            Log.Config($"  Sync: {_context.Plan.SyncMode}, TickRate: {_context.Plan.TickRate}");
            Log.Config($"  Max Players: {_context.Plan.MaxPlayerCount}");
            Log.Config($"  Debug: {_context.Plan.EnableDebug}");
            Log.Config($"  Input Delay: {_context.Plan.InputDelayFrames} frames");

            if (_mobaConfig.CharacterCount > 0)
            {
                Log.Config("  Characters:");
                foreach (var c in _mobaConfig.GetAllCharacters())
                {
                    var attrs = _mobaConfig.GetCharacterAttributes(c);
                    var hp = attrs?.Hp ?? 0;
                    var atk = attrs?.PhysicsAttack ?? 0;
                    var def = attrs?.PhysicsDefense ?? 0;
                    Log.Config($"    - {c.Name} (HP:{hp:F0}, ATK:{atk:F0}, DEF:{def:F0}, TemplateId:{c.AttributeTemplateId})");
                }
            }

            Log.Config("============================");
        }

        public void Start()
        {
            Log.Trace("[TRACE] ConsoleBattleBootstrapper.Start - Entry");
            Log.System("Starting...");

            _syncFeature.OnAttach(_context);
            _inputFeature.OnAttach(_context);
            _hudFeature.OnAttach(_context);
            _inputHandler.Start();
            _flow.Start();

            // 如果是状态同步模式，连接到服务器
            if (_syncAdapter is StateSyncAdapter stateSync && _config.SyncMode == BattleSyncMode.SnapshotAuthority)
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
            _hudFeature.Tick(_context, (float)elapsed);
            _battleView.Tick((float)elapsed);

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
                CreateCharacter(
                    actorId: HashPlayerId(player.PlayerId),
                    name: player.Name,
                    characterId: player.HeroId,
                    hp: 500,
                    maxHp: 500,
                    x: player.PositionX,
                    y: player.PositionY,
                    z: player.PositionZ);
                return;
            }

            var attrs = _mobaConfig.GetCharacterAttributes(charConfig);
            CreateCharacter(
                actorId: HashPlayerId(player.PlayerId),
                name: charConfig.Name,
                characterId: charConfig.Id,
                hp: attrs?.Hp ?? 500,
                maxHp: attrs?.MaxHp ?? 500,
                x: player.PositionX,
                y: player.PositionY,
                z: player.PositionZ);

            Log.Battle($"Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1})");
        }

        private static int HashPlayerId(string playerId)
        {
            return DeterministicHash.StringToActorId(playerId);
        }

        public void RegisterDemoEntities()
        {
            CreateCharacter(1, "Warrior", 1001, 800, 800, 0, 0, 0);
            CreateCharacter(2, "Archer", 1002, 600, 600, 10, 0, 0);
            CreateCharacter(3, "Mage", 1003, 500, 500, -10, 0, 0);
            CreateCharacter(101, "Minion_A1", 2001, 300, 300, 20, 0, 0);
            CreateCharacter(102, "Minion_A2", 2001, 300, 300, 22, 0, 2);
            CreateCharacter(103, "Minion_A3", 2002, 250, 250, 21, 0, 1);

            Log.Entity($"Registered demo entities: 3 heroes, 3 minions");
            Log.Entity($"ECS World alive count: {_context.EcsWorld.AliveCount}");
        }

        public void CreateCharacter(int actorId, string name, int characterId, float hp, float maxHp, float x, float y, float z)
        {
            Log.Trace($"[TRACE] CreateCharacter - Actor#{actorId} ({name}), CharacterId:{characterId}, HP:{hp:F0}, Pos:({x:F1},{y:F1},{z:F1})");
            var netId = new BattleNetId(actorId);
            var e = _context.EntityFactory.CreateCharacter(netId, characterId);

            if (e.TryGetRef(out BattleTransformComponent t))
            {
                t.Position = new AbilityKit.Core.Math.Vec3(x, y, z);
            }

            if (e.TryGetRef(out BattleCharacterComponent c))
            {
                c.Hp = hp;
                c.HpMax = maxHp;
                c.TeamId = 1;
            }

            _battleView.RegisterEntity(actorId, name, "Character", hp, maxHp, x, y, z);

            // ???????
            _battleServices.RegisterActor(new ActorInfo(actorId, name)
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
                TeamId = 1
            });

            Log.Entity($"Created: #{actorId} {name} (CharId:{characterId})");
        }

        public void SimulateDamage(int targetId, float damage)
        {
            var netId = new BattleNetId(targetId);
            if (_context.EntityLookup.TryResolve(_context.EcsWorld, netId, out var e))
            {
                if (e.TryGetRef(out BattleCharacterComponent c))
                {
                    var newHp = Math.Max(0, c.Hp - damage);
                    c.Hp = newHp;
                    _battleView.UpdateEntityHp(targetId, newHp, c.HpMax);
                    _battleView.ShowFloatingText(targetId, $"-{damage:F0}", false);
                    Log.Damage($"Actor#{targetId} takes -{damage:F0} damage (HP: {newHp:F0}/{c.HpMax:F0})");
                }
            }
        }

        public EC.IEntity CreateProjectile(int actorId, int ownerId, int templateId = 0)
        {
            var netId = new BattleNetId(actorId);
            var ownerNetId = new BattleNetId(ownerId);
            var e = _context.EntityFactory.CreateProjectile(netId, ownerNetId, templateId);
            Log.Entity($"Created projectile: #{actorId} from #{ownerId}");
            return e;
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
            Log.System($"  ActorCount: {_battleServices.ActorCount}");
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
            _inputHandler?.Dispose();
            _hudFeature?.OnDetach(_context);
            _inputFeature?.OnDetach(_context);
            _syncFeature?.OnDetach(_context);
            _viewBinder?.Dispose();
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
