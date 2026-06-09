using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Protocol.Shooter;
using ShooterBulletState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsProjectileEntity;
using ShooterPlayerState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsPlayerEntity;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterBattleSimulation _simulation;
        private readonly IShooterSveltoWorld? _sveltoWorld;

        public ShooterBattleRuntimePort()
            : this(new ShooterManagedEcsEntityStore())
        {
        }

        public ShooterBattleRuntimePort(IShooterEcsEntityStore entityStore)
            : this(CreateState(entityStore))
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state)
            : this(state, new ShooterBattleSimulation(state), null)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterSveltoWorld? sveltoWorld)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _sveltoWorld = sveltoWorld;
        }

        private IDictionary<int, ShooterPlayerState> _players => _state.Players;

        private IList<ShooterBulletState> _bullets => _state.Bullets;

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
                if (player.PlayerId <= 0 || _players.ContainsKey(player.PlayerId)) continue;

                _players[player.PlayerId] = new ShooterPlayerState
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
            }

            _state.IsStarted = _players.Count > 0;
            SyncEntityStore();
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
                if (!_players.ContainsKey(command.PlayerId)) continue;

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
            SyncEntityStore();
            return true;
        }

        public ShooterStateSnapshotPayload GetSnapshot()
        {
            var players = new ShooterPlayerSnapshot[_players.Count];
            var index = 0;
            foreach (var kv in _players)
            {
                var p = kv.Value;
                players[index++] = new ShooterPlayerSnapshot(p.PlayerId, p.X, p.Y, p.AimX, p.AimY, p.Hp, p.Score, p.Alive);
            }

            var bullets = new ShooterBulletSnapshot[_bullets.Count];
            for (int i = 0; i < _bullets.Count; i++)
            {
                var b = _bullets[i];
                bullets[i] = new ShooterBulletSnapshot(b.BulletId, b.OwnerPlayerId, b.X, b.Y, b.VelocityX, b.VelocityY, b.RemainingFrames);
            }

            return new ShooterStateSnapshotPayload(CurrentFrame, players, bullets, _state.Events.ToArray());
        }

        public uint ComputeStateHash()
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Hash(hash, CurrentFrame);

                var playerIds = new int[_players.Count];
                var playerIndex = 0;
                foreach (var kv in _players)
                {
                    playerIds[playerIndex++] = kv.Key;
                }

                Array.Sort(playerIds);
                for (int i = 0; i < playerIds.Length; i++)
                {
                    var player = _players[playerIds[i]];
                    hash = Hash(hash, player.PlayerId);
                    hash = Hash(hash, Quantize(player.X));
                    hash = Hash(hash, Quantize(player.Y));
                    hash = Hash(hash, Quantize(player.AimX));
                    hash = Hash(hash, Quantize(player.AimY));
                    hash = Hash(hash, player.Hp);
                    hash = Hash(hash, player.Score);
                    hash = Hash(hash, player.Alive ? 1 : 0);
                }

                var bullets = _bullets.ToArray();
                Array.Sort(bullets, CompareBulletsById);
                for (int i = 0; i < bullets.Length; i++)
                {
                    var bullet = bullets[i];
                    hash = Hash(hash, bullet.BulletId);
                    hash = Hash(hash, bullet.OwnerPlayerId);
                    hash = Hash(hash, Quantize(bullet.X));
                    hash = Hash(hash, Quantize(bullet.Y));
                    hash = Hash(hash, Quantize(bullet.VelocityX));
                    hash = Hash(hash, Quantize(bullet.VelocityY));
                    hash = Hash(hash, bullet.RemainingFrames);
                }

                return hash;
            }
        }

        public ShooterPackedSnapshotPayload ExportPackedSnapshot(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var chunks = new[]
            {
                ExportPlayerChunk(),
                ExportProjectileChunk()
            };

            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                worldId,
                CurrentFrame,
                CurrentFrame,
                CreateSnapshotFlags(isFullSnapshot, authorityOverride),
                ComputeStateHash(),
                _players.Count + _bullets.Count,
                chunks,
                Array.Empty<byte>());
        }

        public bool ImportPackedSnapshot(in ShooterPackedSnapshotPayload snapshot)
        {
            if (snapshot.Version <= 0 || snapshot.Chunks == null)
            {
                return false;
            }

            _state.Reset(default);
            _state.CurrentFrame = snapshot.Frame;

            for (int i = 0; i < snapshot.Chunks.Length; i++)
            {
                var chunk = snapshot.Chunks[i];
                switch (chunk.EntityKind)
                {
                    case ShooterPackedEntityKinds.Player:
                        ImportPlayerChunk(in chunk);
                        break;
                    case ShooterPackedEntityKinds.Projectile:
                        ImportProjectileChunk(in chunk);
                        break;
                }
            }

            _state.IsStarted = _players.Count > 0;
            SyncEntityStore();
            return _state.IsStarted || snapshot.EntityCount == 0;
        }

        public byte[] ExportPackedSnapshotBytes(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var snapshot = ExportPackedSnapshot(worldId, isFullSnapshot, authorityOverride);
            return ShooterPackedSnapshotCodec.Serialize(in snapshot);
        }

        public bool ImportPackedSnapshotBytes(byte[] payload)
        {
            var snapshot = ShooterPackedSnapshotCodec.Deserialize(payload);
            return ImportPackedSnapshot(in snapshot);
        }

        private void SyncEntityStore()
        {
            _sveltoWorld?.SyncEntities();
        }

        private ShooterPackedEntityChunk ExportPlayerChunk()
        {
            if (_players.Count == 0)
            {
                return ShooterPackedEntityChunk.Empty(ShooterPackedEntityKinds.Player);
            }

            var entityIds = new int[_players.Count];
            var index = 0;
            foreach (var kv in _players)
            {
                entityIds[index++] = kv.Key;
            }

            Array.Sort(entityIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            var hp = new short[entityIds.Length];
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            var aux = new int[entityIds.Length];

            for (int i = 0; i < entityIds.Length; i++)
            {
                var player = _players[entityIds[i]];
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;
                hp[i] = ClampToShort(player.Hp);
                flags[i] = (byte)(ShooterPackedEntityFlags.Player | ShooterPackedEntityFlags.DirtyTransform | ShooterPackedEntityFlags.DirtyStats);
                if (player.Alive)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }

                if (_state.LatestCommands.TryGetValue(player.PlayerId, out var command))
                {
                    var moveX = command.MoveX;
                    var moveY = command.MoveY;
                    if (Normalize(ref moveX, ref moveY) > 0f)
                    {
                        velX[i] = moveX * ShooterBattleTuning.PlayerSpeed;
                        velY[i] = moveY * ShooterBattleTuning.PlayerSpeed;
                    }
                }

                ownerIds[i] = player.PlayerId;
                aux[i] = player.Score;
            }

            return new ShooterPackedEntityChunk(ShooterPackedEntityKinds.Player, entityIds.Length, entityIds, posX, posY, velX, velY, facingX, facingY, hp, flags, ownerIds, aux);
        }

        private ShooterPackedEntityChunk ExportProjectileChunk()
        {
            if (_bullets.Count == 0)
            {
                return ShooterPackedEntityChunk.Empty(ShooterPackedEntityKinds.Projectile);
            }

            var bullets = _bullets.ToArray();
            Array.Sort(bullets, CompareBulletsById);
            var entityIds = new int[bullets.Length];
            var posX = new float[bullets.Length];
            var posY = new float[bullets.Length];
            var velX = new float[bullets.Length];
            var velY = new float[bullets.Length];
            var facingX = new float[bullets.Length];
            var facingY = new float[bullets.Length];
            var hp = new short[bullets.Length];
            var flags = new byte[bullets.Length];
            var ownerIds = new int[bullets.Length];
            var aux = new int[bullets.Length];

            for (int i = 0; i < bullets.Length; i++)
            {
                var bullet = bullets[i];
                entityIds[i] = bullet.BulletId;
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                velX[i] = bullet.VelocityX;
                velY[i] = bullet.VelocityY;
                var dirX = bullet.VelocityX;
                var dirY = bullet.VelocityY;
                Normalize(ref dirX, ref dirY);
                facingX[i] = dirX;
                facingY[i] = dirY;
                hp[i] = ClampToShort(bullet.RemainingFrames);
                flags[i] = (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile | ShooterPackedEntityFlags.DirtyTransform);
                ownerIds[i] = bullet.OwnerPlayerId;
                aux[i] = bullet.RemainingFrames;
            }

            return new ShooterPackedEntityChunk(ShooterPackedEntityKinds.Projectile, bullets.Length, entityIds, posX, posY, velX, velY, facingX, facingY, hp, flags, ownerIds, aux);
        }

        private void ImportPlayerChunk(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var playerId = GetInt(chunk.EntityIds, i);
                if (playerId <= 0) continue;

                var flags = GetByte(chunk.Flags, i);
                _players[playerId] = new ShooterPlayerState
                {
                    PlayerId = playerId,
                    X = GetFloat(chunk.PosX, i),
                    Y = GetFloat(chunk.PosY, i),
                    AimX = GetFloat(chunk.FacingX, i, 1f),
                    AimY = GetFloat(chunk.FacingY, i),
                    Hp = GetShort(chunk.Hp, i),
                    Score = GetInt(chunk.Aux, i),
                    Alive = (flags & ShooterPackedEntityFlags.Alive) != 0
                };
            }
        }

        private void ImportProjectileChunk(in ShooterPackedEntityChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var bulletId = GetInt(chunk.EntityIds, i);
                if (bulletId <= 0) continue;

                var remainingFrames = GetInt(chunk.Aux, i, GetShort(chunk.Hp, i));
                _bullets.Add(new ShooterBulletState
                {
                    BulletId = bulletId,
                    OwnerPlayerId = GetInt(chunk.OwnerIds, i),
                    X = GetFloat(chunk.PosX, i),
                    Y = GetFloat(chunk.PosY, i),
                    VelocityX = GetFloat(chunk.VelX, i),
                    VelocityY = GetFloat(chunk.VelY, i),
                    RemainingFrames = remainingFrames
                });

                _state.AdvanceBulletIdPast(bulletId);
            }
        }

        private static uint CreateSnapshotFlags(bool isFullSnapshot, bool authorityOverride)
        {
            var flags = isFullSnapshot ? ShooterPackedSnapshotFlags.Full : ShooterPackedSnapshotFlags.Delta;
            if (isFullSnapshot)
            {
                flags |= ShooterPackedSnapshotFlags.KeyFrame;
            }

            if (authorityOverride)
            {
                flags |= ShooterPackedSnapshotFlags.AuthorityOverride;
            }

            return flags;
        }

        private static int CompareBulletsById(ShooterBulletState left, ShooterBulletState right)
        {
            return left.BulletId.CompareTo(right.BulletId);
        }

        private static short ClampToShort(int value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)value;
        }

        private static int GetInt(int[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static float GetFloat(float[] values, int index, float fallback = 0f)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int GetShort(short[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static byte GetByte(byte[] values, int index, byte fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int Quantize(float value)
        {
            return (int)Math.Round(value * 10000f);
        }

        private static uint Hash(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
                return hash;
            }
        }

        private static float Normalize(ref float x, ref float y)
        {
            return ShooterBattleMath.Normalize(ref x, ref y);
        }

        private static ShooterBattleState CreateState(IShooterEcsEntityStore entityStore)
        {
            return new ShooterBattleState(entityStore);
        }

    }
}
