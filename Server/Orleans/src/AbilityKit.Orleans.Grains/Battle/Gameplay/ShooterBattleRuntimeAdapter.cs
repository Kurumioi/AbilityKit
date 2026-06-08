using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal sealed class ShooterBattleRuntimeAdapter : IBattleRuntimeAdapter
{
    public string RoomType => ShooterGameplay.RoomType;

    public IBattleRuntimeSession CreateSession(string battleId)
    {
        return new ShooterBattleRuntimeSession(battleId);
    }

    private sealed class ShooterBattleRuntimeSession : IBattleRuntimeSession
    {
        private readonly string _battleId;
        private readonly IShooterBattleRuntimePort _runtime = new ShooterBattleRuntimePort();
        private ulong _worldId;

        public ShooterBattleRuntimeSession(string battleId)
        {
            _battleId = battleId ?? string.Empty;
        }

        public BattleRuntimeStartResult Start(BattleInitParams initParams)
        {
            if (initParams is null)
            {
                return BattleRuntimeStartResult.Fail("Battle init params are missing.");
            }

            _worldId = initParams.WorldId;
            var players = BuildStartPlayers(initParams.Players);
            var start = new ShooterStartGamePayload(
                _battleId,
                initParams.TickRate > 0 ? initParams.TickRate : ShooterGameplay.DefaultTickRate,
                initParams.RandomSeed,
                players);

            return _runtime.StartGame(in start)
                ? BattleRuntimeStartResult.Success()
                : BattleRuntimeStartResult.Fail("Shooter runtime rejected start spec.");
        }

        public int SubmitInputs(int frame, IReadOnlyList<BattleInputItem> inputs)
        {
            if (inputs == null || inputs.Count == 0)
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
            return _runtime.Tick(deltaTime);
        }

        public BattleSnapshot? GetSnapshot(int frame)
        {
            var snapshot = _runtime.GetSnapshot();
            return new BattleSnapshot
            {
                Frame = snapshot.Frame,
                Actors = CreateActorSnapshots(in snapshot)
            };
        }

        public StateSyncPush CreateStateSyncPush(ulong worldId, int frame, bool isFullSnapshot)
        {
            var snapshot = _runtime.GetSnapshot();
            return new StateSyncPush
            {
                WorldId = worldId == 0 ? _worldId : worldId,
                Frame = snapshot.Frame,
                Timestamp = DateTime.UtcNow.Ticks,
                Actors = CreateActorSnapshots(in snapshot),
                IsFullSnapshot = isFullSnapshot
            };
        }

        public void Dispose()
        {
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
                var angle = players.Count <= 1 ? 0 : (Math.PI * 2.0 * i / players.Count);
                var spawnX = Math.Abs(player.PosX) > 0.0001f ? player.PosX : (float)Math.Cos(angle) * 3f;
                var spawnY = Math.Abs(player.PosZ) > 0.0001f ? player.PosZ : (float)Math.Sin(angle) * 3f;
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
