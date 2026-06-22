using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterBattleRuntimePort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterBattleRuntimePort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterGameStartPort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterInputPort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterSimulationClock), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterSnapshotReadPort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterStateHashProvider), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterPackedSnapshotPort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterPureStateSnapshotPort), WorldLifetime.Singleton)]
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterBattleSimulation _simulation;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly ShooterEnemyWaveOptions _enemyWaveOptions;
        private readonly ShooterStateSnapshotExporter _snapshotExporter;
        private readonly ShooterStateHasher _stateHasher;
        private readonly ShooterPackedSnapshotExporter _packedSnapshotExporter;
        private readonly ShooterPackedSnapshotImporter _packedSnapshotImporter;
        private readonly ShooterPackedSnapshotBytesCodec _bytesCodec;
        private readonly ShooterPureStateSnapshotExporter _pureStateSnapshotExporter;
        private readonly ShooterBotAiSystem _botAiSystem;
        private readonly ShooterBattleServiceContext _services;
        private readonly ShooterBattleSveltoStepEngine _battleStepEngine;

        public ShooterBattleRuntimePort()
            : this(ShooterEntityLimitOptions.Default)
        {
        }

        public ShooterBattleRuntimePort(ShooterEntityLimitOptions entityLimits)
            : this(entityLimits, ShooterEnemyWaveOptions.Disabled)
        {
        }

        public ShooterBattleRuntimePort(ShooterEntityLimitOptions entityLimits, ShooterEnemyWaveOptions enemyWaveOptions)
            : this(CreateDefaultEntityManager(entityLimits), enemyWaveOptions)
        {
        }

        private ShooterBattleRuntimePort(IShooterEntityManager entities, ShooterEnemyWaveOptions enemyWaveOptions)
            : this(CreateState(entities), enemyWaveOptions)
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state, ShooterEnemyWaveOptions enemyWaveOptions)
            : this(state, ShooterBattleRules.Default, enemyWaveOptions)
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleRules rules, ShooterEnemyWaveOptions enemyWaveOptions)
            : this(state, new ShooterBattleSimulation(state, rules), state.Entities, rules, enemyWaveOptions)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities)
            : this(state, simulation, entities, ShooterBattleRules.Default)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities, IShooterBattleRules rules)
            : this(state, simulation, entities, rules, ShooterEnemyWaveOptions.Disabled)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities, IShooterBattleRules rules, ShooterEnemyWaveOptions enemyWaveOptions)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _enemyWaveOptions = enemyWaveOptions ?? ShooterEnemyWaveOptions.Disabled;
            _snapshotExporter = new ShooterStateSnapshotExporter(_state, _entities);
            _stateHasher = new ShooterStateHasher(_state, _entities);
            _packedSnapshotExporter = new ShooterPackedSnapshotExporter(_state, _entities, _rules, this);
            _packedSnapshotImporter = new ShooterPackedSnapshotImporter(_state, _entities);
            _bytesCodec = new ShooterPackedSnapshotBytesCodec();
            _pureStateSnapshotExporter = new ShooterPureStateSnapshotExporter(_state, this, this, _entities);
            _botAiSystem = new ShooterBotAiSystem(_state, _entities);
            _services = CreateServiceContext(_enemyWaveOptions);
            _battleStepEngine = new ShooterBattlePipelineFactory().Create(_services);
            _services.EnginesRoot.AddEngine(_battleStepEngine);
        }

        public bool IsStarted => _state.IsStarted;

        public ShooterBattleMatchState MatchState => _state.MatchState;

        public ShooterMatchResultSnapshot MatchResult => _state.GetMatchResult();

        public int CurrentFrame => _state.CurrentFrame;

        public ShooterStartGamePayload StartSpec => _state.StartSpec;

        public bool StartGame(in ShooterStartGamePayload spec)
        {
            _state.Reset(in spec);
            _state.VictoryTargetDefeats = _enemyWaveOptions.VictoryTargetDefeats;
            _state.SetTimeLimitFrames(_enemyWaveOptions.DurationFrames);
            _botAiSystem.ClearBotAi();

            var players = spec.Players ?? Array.Empty<ShooterStartPlayer>();
            _entities.BeginStructuralChanges();
            try
            {
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (player.PlayerId <= 0 || _entities.HasPlayer(player.PlayerId)) continue;

                    var component = new ShooterSveltoPlayerComponent
                    {
                        PlayerId = player.PlayerId,
                        X = player.SpawnX,
                        Y = player.SpawnY,
                        AimX = 1f,
                        AimY = 0f,
                        Hp = ShooterGameplay.DefaultPlayerHp,
                        Score = 0,
                        Alive = true
                    };
                    _entities.AddPlayer(in component);
                }
            }
            finally
            {
                _entities.EndStructuralChanges();
            }

            if (_entities.PlayerCount > 0)
            {
                _state.SetMatchRunning();
            }

            return _state.IsStarted;
        }

        public int SubmitInput(int frame, ShooterPlayerCommand[] commands)
        {
            if (!_state.IsStarted || commands == null || commands.Length == 0)
            {
                return 0;
            }

            var accepted = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                if (!_entities.HasPlayer(command.PlayerId)) continue;

                _state.InputBuffer.SubmitCommand(frame, in command);
                accepted++;
            }

            return accepted;
        }

        public bool Tick(float deltaTime)
        {
            if (!_state.IsStarted)
            {
                return false;
            }

            _battleStepEngine.Step(in deltaTime);
            _state.InputBuffer.TrimBefore(Math.Max(0, _state.CurrentFrame - 120));
            return _state.IsStarted;
        }

        public ShooterStateSnapshotPayload GetSnapshot()
        {
            return _snapshotExporter.Export();
        }

        public uint ComputeStateHash()
        {
            return _stateHasher.Compute();
        }

        public ShooterPackedSnapshotPayload ExportPackedSnapshot(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            return _packedSnapshotExporter.Export(worldId, isFullSnapshot, authorityOverride);
        }

        public bool ImportPackedSnapshot(in ShooterPackedSnapshotPayload snapshot)
        {
            return _packedSnapshotImporter.Import(in snapshot);
        }

        public ShooterPureStateSnapshotPayload ExportPureStateSnapshot(
            ulong worldId,
            bool isFullBaseline = true,
            ShooterPureStateSyncSettings? settings = null,
            int baselineFrame = 0,
            uint baselineHash = 0,
            ShooterPureStateInterestScope? interestScope = null)
        {
            return _pureStateSnapshotExporter.Export(worldId, isFullBaseline, settings, baselineFrame, baselineHash, interestScope);
        }

        public bool TryGetPlayer(int playerId, out ShooterSveltoPlayerComponent player)
        {
            return _entities.TryGetPlayer(playerId, out player);
        }

        public void SetPlayer(in ShooterSveltoPlayerComponent player)
        {
            _entities.SetPlayer(in player);
        }

        public int BotAiCount => _botAiSystem.BotAiCount;

        public bool MountBotAi(in ShooterBotAiMountOptions options)
        {
            return _botAiSystem.MountBotAi(in options);
        }

        public bool UnmountBotAi(int playerId)
        {
            return _botAiSystem.UnmountBotAi(playerId);
        }

        public void ClearBotAi()
        {
            _botAiSystem.ClearBotAi();
        }

        public byte[] ExportPackedSnapshotBytes(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            return _bytesCodec.Export(this, worldId, isFullSnapshot, authorityOverride);
        }

        public bool ImportPackedSnapshotBytes(byte[] payload)
        {
            return _bytesCodec.Import(this, payload);
        }

        private ShooterBattleServiceContext CreateServiceContext(ShooterEnemyWaveOptions enemyWaveOptions)
        {
            return new ShooterBattleServiceContext(_entities.SveltoContext)
                .Add(_state)
                .Add(_entities)
                .Add(_rules)
                .Add(_simulation)
                .Add(_botAiSystem)
                .Add(enemyWaveOptions)
                .Add<IShooterSveltoWorld>(new ShooterSveltoWorld(_entities.SveltoContext))
                .Add<IShooterBotAiPort>(_botAiSystem)
                .Add<IShooterBattleRuntimePort>(this)
                .Add<IShooterGameStartPort>(this)
                .Add<IShooterInputPort>(this)
                .Add<IShooterSimulationClock>(this)
                .Add<IShooterSnapshotReadPort>(this)
                .Add<IShooterStateHashProvider>(this)
                .Add<IShooterPackedSnapshotPort>(this)
                .Add<IShooterPureStateSnapshotPort>(this);
        }

        private static IShooterEntityManager CreateDefaultEntityManager(ShooterEntityLimitOptions entityLimits)
        {
            return new ShooterEntityManager(new SveltoWorldContext(), entityLimits);
        }

        private static ShooterBattleState CreateState(IShooterEntityManager entities)
        {
            return new ShooterBattleState(entities);
        }
    }
}
