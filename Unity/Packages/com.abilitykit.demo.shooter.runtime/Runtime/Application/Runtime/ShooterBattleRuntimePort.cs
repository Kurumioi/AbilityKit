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
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterBattleSimulation _simulation;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly ShooterStateSnapshotExporter _snapshotExporter;
        private readonly ShooterStateHasher _stateHasher;
        private readonly ShooterPackedSnapshotExporter _packedSnapshotExporter;
        private readonly ShooterPackedSnapshotImporter _packedSnapshotImporter;
        private readonly ShooterPackedSnapshotBytesCodec _bytesCodec;

        public ShooterBattleRuntimePort()
            : this(CreateDefaultEntityManager())
        {
        }

        private ShooterBattleRuntimePort(IShooterEntityManager entities)
            : this(CreateState(entities))
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state)
            : this(state, ShooterBattleRules.Default)
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleRules rules)
            : this(state, new ShooterBattleSimulation(state, rules), state.Entities, rules)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities)
            : this(state, simulation, entities, ShooterBattleRules.Default)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities, IShooterBattleRules rules)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _snapshotExporter = new ShooterStateSnapshotExporter(_state, _entities);
            _stateHasher = new ShooterStateHasher(_state, _entities);
            _packedSnapshotExporter = new ShooterPackedSnapshotExporter(_state, _entities, _rules, this);
            _packedSnapshotImporter = new ShooterPackedSnapshotImporter(_state, _entities);
            _bytesCodec = new ShooterPackedSnapshotBytesCodec();
        }

        public bool IsStarted => _state.IsStarted;

        public int CurrentFrame => _state.CurrentFrame;

        public ShooterStartGamePayload StartSpec => _state.StartSpec;

        public bool StartGame(in ShooterStartGamePayload spec)
        {
            _state.Reset(in spec);

            var players = spec.Players ?? Array.Empty<ShooterStartPlayer>();
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

            _state.IsStarted = _entities.PlayerCount > 0;
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

                _state.LatestCommands[command.PlayerId] = command;
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

            _state.CurrentFrame++;
            _state.Events.Clear();

            _simulation.Tick(deltaTime);
            return true;
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

        public byte[] ExportPackedSnapshotBytes(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            return _bytesCodec.Export(this, worldId, isFullSnapshot, authorityOverride);
        }

        public bool ImportPackedSnapshotBytes(byte[] payload)
        {
            return _bytesCodec.Import(this, payload);
        }

        private static IShooterEntityManager CreateDefaultEntityManager()
        {
            return new ShooterEntityManager(new SveltoWorldContext());
        }

        private static ShooterBattleState CreateState(IShooterEntityManager entities)
        {
            return new ShooterBattleState(entities);
        }
    }
}
