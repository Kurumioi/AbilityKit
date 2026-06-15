using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Shooter;
using IWorld = AbilityKit.Ability.World.Abstractions.IWorld;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal sealed class ShooterBattleRuntimeAdapter : IBattleRuntimeAdapter
{
    private readonly ServerMobaWorldManager _worldManager;

    public ShooterBattleRuntimeAdapter(ServerMobaWorldManager worldManager)
    {
        _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
    }

    public string RoomType => ShooterGameplay.RoomType;

    public IBattleRuntimeSession CreateSession(string battleId)
    {
        return new ShooterBattleRuntimeSession(battleId, _worldManager);
    }

    private sealed class ShooterBattleRuntimeSession : IBattleRuntimeSession
    {
        private readonly string _battleId;
        private readonly ServerMobaWorldManager _worldManager;
        private IWorld? _battleWorld;
        private IShooterBattleRuntimePort? _runtime;
        private ulong _worldId;

        public ShooterBattleRuntimeSession(string battleId, ServerMobaWorldManager worldManager)
        {
            _battleId = battleId ?? string.Empty;
            _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        }

        public BattleRuntimeStartResult Start(BattleInitParams initParams)
        {
            if (initParams is null)
            {
                return BattleRuntimeStartResult.Fail("Battle init params are missing.");
            }

            _worldId = initParams.WorldId;
            _battleWorld = _worldManager.CreateBattleWorld(_battleId, ShooterGameplay.WorldType, initParams.TickRate);
            if (_battleWorld == null)
            {
                return BattleRuntimeStartResult.Fail("Shooter battle world creation returned null.");
            }

            if (!_battleWorld.Services.TryResolve<IShooterBattleRuntimePort>(out _runtime) || _runtime == null)
            {
                return BattleRuntimeStartResult.Fail("IShooterBattleRuntimePort not resolved from Shooter logic world.");
            }

            var tickRate = initParams.TickRate > 0 ? initParams.TickRate : ShooterGameplay.DefaultTickRate;
            var players = BuildStartPlayers(initParams.Players);
            var anchor = initParams.WorldStartAnchor;
            var start = new ShooterStartGamePayload(
                _battleId,
                tickRate,
                initParams.RandomSeed,
                players,
                _worldId,
                anchor?.StartServerTicks ?? 0L,
                anchor?.ServerTickFrequency ?? 0L,
                anchor?.StartFrame ?? 0,
                anchor?.FixedDeltaSeconds ?? 0d);

            return _runtime.StartGame(in start)
                ? BattleRuntimeStartResult.Success()
                : BattleRuntimeStartResult.Fail("Shooter runtime rejected start spec.");
        }

        public int SubmitInputs(int frame, IReadOnlyList<BattleInputItem> inputs)
        {
            if (inputs == null || inputs.Count == 0 || _runtime == null)
            {
                return 0;
            }

            var commands = new List<ShooterPlayerCommand>(inputs.Count);
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input == null) continue;

                if (input.OpCode == ShooterOpCodes.Input.PlayerCommand)
                {
                    commands.AddRange(ShooterInputCodec.Deserialize(input.Payload ?? Array.Empty<byte>()));
                    continue;
                }

                commands.Add(CreateFallbackCommand(input));
            }

            return _runtime.SubmitInput(frame, commands.ToArray());
        }

        public bool Tick(int frame, int tickRate, float deltaTime)
        {
            if (_battleWorld == null || _runtime == null)
            {
                return false;
            }

            _battleWorld.Tick(deltaTime);
            return _runtime.CurrentFrame >= frame;
        }

        public BattleSnapshot? GetSnapshot(int frame)
        {
            if (_runtime == null)
            {
                return null;
            }

            var snapshot = _runtime.GetSnapshot();
            return new BattleSnapshot
            {
                Frame = snapshot.Frame,
                Actors = CreateActorSnapshots(in snapshot)
            };
        }

        public StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot)
        {
            var resolvedWorldId = worldId == 0 ? _worldId : worldId;
            var snapshot = _runtime?.GetSnapshot() ?? default;
            var packed = _runtime?.ExportPackedSnapshot(resolvedWorldId, isFullSnapshot, authorityOverride: isFullSnapshot) ?? default;
            return new StateSyncPush
            {
                WorldId = resolvedWorldId,
                Frame = packed.Frame,
                Timestamp = DateTime.UtcNow.Ticks,
                Actors = CreateActorSnapshots(in snapshot),
                IsFullSnapshot = isFullSnapshot,
                PayloadOpCode = isFullSnapshot ? ShooterOpCodes.Snapshot.PackedState : ShooterOpCodes.Snapshot.PackedStateDelta,
                Payload = ShooterPackedSnapshotCodec.Serialize(in packed)
            };
        }

        public void Dispose()
        {
            _worldManager.DestroyBattleWorld(_battleId);
            _battleWorld = null;
            _runtime = null;
        }

        private static ShooterStartPlayer[] BuildStartPlayers(IReadOnlyList<PlayerInitInfo>? players)
        {
            if (players == null || players.Count == 0)
            {
                return Array.Empty<ShooterStartPlayer>();
            }

            var result = new ShooterStartPlayer[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var playerId = player.PlayerId == 0 ? i + 1 : (int)player.PlayerId;
                var spawnX = player.PosX;
                var spawnY = player.PosZ;
                result[i] = new ShooterStartPlayer(
                    playerId,
                    playerId.ToString(),
                    spawnX,
                    spawnY);
            }

            return result;
        }

        private static List<ActorSnapshot> CreateActorSnapshots(in ShooterStateSnapshotPayload snapshot)
        {
            var actors = new List<ActorSnapshot>(snapshot.Players?.Length ?? 0);
            if (snapshot.Players == null)
            {
                return actors;
            }

            for (int i = 0; i < snapshot.Players.Length; i++)
            {
                var player = snapshot.Players[i];
                actors.Add(new ActorSnapshot
                {
                    ActorId = player.PlayerId,
                    X = player.X,
                    Y = 0f,
                    Z = player.Y,
                    Rotation = 0f,
                    VelocityX = 0f,
                    VelocityZ = 0f,
                    Hp = player.Hp,
                    HpMax = ShooterGameplay.DefaultPlayerHp,
                    TeamId = 1
                });
            }

            return actors;
        }

        private static ShooterPlayerCommand CreateFallbackCommand(BattleInputItem input)
        {
            var playerId = (int)input.PlayerId;
            var fire = input.OpCode != 0;
            return new ShooterPlayerCommand(playerId, 0f, 0f, 1f, 0f, fire);
        }
    }
}
