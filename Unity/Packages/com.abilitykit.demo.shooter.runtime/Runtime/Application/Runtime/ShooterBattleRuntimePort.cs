using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterBattleRuntimePort), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterBattleRuntimePort), WorldLifetime.Singleton)]
    public sealed class ShooterBattleRuntimePort : IShooterBattleRuntimePort
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterBattleSimulation _simulation;
        private readonly IShooterEntityManager _entities;

        public ShooterBattleRuntimePort()
            : this(CreateDefaultEntityManager())
        {
        }

        private ShooterBattleRuntimePort(IShooterEntityManager entities)
            : this(CreateState(entities))
        {
        }

        private ShooterBattleRuntimePort(ShooterBattleState state)
            : this(state, new ShooterBattleSimulation(state), state.Entities)
        {
        }

        public ShooterBattleRuntimePort(ShooterBattleState state, IShooterBattleSimulation simulation, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
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
            var playerIds = CopyAndSort(_entities.PlayerIds);
            var players = new ShooterPlayerSnapshot[playerIds.Length];
            for (int i = 0; i < playerIds.Length; i++)
            {
                _entities.TryGetPlayer(playerIds[i], out var p);
                players[i] = new ShooterPlayerSnapshot(p.PlayerId, p.X, p.Y, p.AimX, p.AimY, p.Hp, p.Score, p.Alive);
            }

            var bulletIds = CopyAndSort(_entities.ProjectileIds);
            var bullets = new ShooterBulletSnapshot[bulletIds.Length];
            for (int i = 0; i < bulletIds.Length; i++)
            {
                _entities.TryGetProjectile(bulletIds[i], out var b);
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

                var playerIds = CopyAndSort(_entities.PlayerIds);
                for (int i = 0; i < playerIds.Length; i++)
                {
                    _entities.TryGetPlayer(playerIds[i], out var player);
                    hash = Hash(hash, player.PlayerId);
                    hash = Hash(hash, Quantize(player.X));
                    hash = Hash(hash, Quantize(player.Y));
                    hash = Hash(hash, Quantize(player.AimX));
                    hash = Hash(hash, Quantize(player.AimY));
                    hash = Hash(hash, player.Hp);
                    hash = Hash(hash, player.Score);
                    hash = Hash(hash, player.Alive ? 1 : 0);
                }

                var bulletIds = CopyAndSort(_entities.ProjectileIds);
                for (int i = 0; i < bulletIds.Length; i++)
                {
                    _entities.TryGetProjectile(bulletIds[i], out var bullet);
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
            var componentChunks = ExportComponentChunks();

            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                worldId,
                CurrentFrame,
                CurrentFrame,
                CreateSnapshotFlags(isFullSnapshot, authorityOverride),
                ComputeStateHash(),
                _entities.PlayerCount + _entities.ProjectileCount,
                Array.Empty<byte>(),
                componentChunks);
        }

        public bool ImportPackedSnapshot(in ShooterPackedSnapshotPayload snapshot)
        {
            if (snapshot.Version <= 0)
            {
                return false;
            }

            var isDelta = (snapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Delta) != 0;

            if (!isDelta)
            {
                // Full (key-frame) snapshot: reset world state before importing.
                _state.Reset(default);
            }

            _state.CurrentFrame = snapshot.Frame;

            var componentChunks = snapshot.ComponentChunks;
            if (componentChunks == null || componentChunks.Length == 0)
            {
                return snapshot.EntityCount == 0;
            }

            ImportComponentChunks(componentChunks, isDelta);

            _state.IsStarted = _entities.PlayerCount > 0;
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

        private ShooterPackedComponentChunk[] ExportComponentChunks()
        {
            return new[]
            {
                ExportPlayerLifecycleChunk(),
                ExportProjectileLifecycleChunk(),
                ExportPlayerTransformChunk(),
                ExportProjectileTransformChunk(),
                ExportPlayerHealthChunk(),
                ExportPlayerScoreChunk(),
                ExportProjectileLifetimeChunk()
            };
        }

        private ShooterPackedComponentChunk ExportPlayerLifecycleChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Player);
            }

            var entityIds = CopyAndSort(_entities.PlayerIds);
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                flags[i] = (byte)ShooterPackedEntityFlags.Player;
                if (player.Alive)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }

                ownerIds[i] = player.PlayerId;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                ownerIds,
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportProjectileLifecycleChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = CopyAndSort(_entities.ProjectileIds);
            var flags = new byte[entityIds.Length];
            var ownerIds = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                flags[i] = (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile);
                ownerIds[i] = bullet.OwnerPlayerId;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                ownerIds,
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerTransformChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Player);
            }

            var entityIds = CopyAndSort(_entities.PlayerIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;

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
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportProjectileTransformChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = CopyAndSort(_entities.ProjectileIds);
            var posX = new float[entityIds.Length];
            var posY = new float[entityIds.Length];
            var velX = new float[entityIds.Length];
            var velY = new float[entityIds.Length];
            var facingX = new float[entityIds.Length];
            var facingY = new float[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                velX[i] = bullet.VelocityX;
                velY[i] = bullet.VelocityY;
                var dirX = bullet.VelocityX;
                var dirY = bullet.VelocityY;
                Normalize(ref dirX, ref dirY);
                facingX[i] = dirX;
                facingY[i] = dirY;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportPlayerHealthChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Player);
            }

            var entityIds = CopyAndSort(_entities.PlayerIds);
            var hp = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                hp[i] = player.Hp;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Health,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                hp,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerScoreChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Score, ShooterPackedEntityKinds.Player);
            }

            var entityIds = CopyAndSort(_entities.PlayerIds);
            var scores = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetPlayer(entityIds[i], out var player);
                scores[i] = player.Score;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Score,
                ShooterPackedEntityKinds.Player,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                scores,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportProjectileLifetimeChunk()
        {
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = CopyAndSort(_entities.ProjectileIds);
            var remainingFrames = new int[entityIds.Length];
            for (int i = 0; i < entityIds.Length; i++)
            {
                _entities.TryGetProjectile(entityIds[i], out var bullet);
                remainingFrames[i] = bullet.RemainingFrames;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.ProjectileLifetime,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                remainingFrames,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private void ImportComponentChunks(ShooterPackedComponentChunk[] componentChunks, bool isDelta = false)
        {
            var players = new Dictionary<int, ShooterSveltoPlayerComponent>();
            var projectiles = new Dictionary<int, ShooterSveltoProjectileComponent>();

            for (int i = 0; i < componentChunks.Length; i++)
            {
                var chunk = componentChunks[i];
                switch (chunk.ComponentKind)
                {
                    case ShooterPackedComponentKinds.EntityLifecycle:
                        ImportLifecycleComponentChunk(in chunk, players, projectiles);
                        break;
                    case ShooterPackedComponentKinds.Transform:
                        ImportTransformComponentChunk(in chunk, players, projectiles);
                        break;
                    case ShooterPackedComponentKinds.Health:
                        ImportHealthComponentChunk(in chunk, players);
                        break;
                    case ShooterPackedComponentKinds.Score:
                        ImportScoreComponentChunk(in chunk, players);
                        break;
                    case ShooterPackedComponentKinds.ProjectileLifetime:
                        ImportProjectileLifetimeComponentChunk(in chunk, projectiles);
                        break;
                }
            }

            foreach (var player in players.Values)
            {
                var value = player;
                if (isDelta && _entities.HasPlayer(value.PlayerId))
                {
                    _entities.SetPlayer(in value);
                }
                else
                {
                    _entities.AddPlayer(in value);
                }
            }

            foreach (var projectile in projectiles.Values)
            {
                var value = projectile;
                if (isDelta && _entities.HasProjectile(value.BulletId))
                {
                    _entities.SetProjectile(in value);
                }
                else
                {
                    _entities.AddProjectile(in value);
                }
                _state.AdvanceBulletIdPast(value.BulletId);
            }
        }

        private void ImportLifecycleComponentChunk(
            in ShooterPackedComponentChunk chunk,
            Dictionary<int, ShooterSveltoPlayerComponent> players,
            Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                var flags = GetByte(chunk.Flags, i);
                if (chunk.EntityKind == ShooterPackedEntityKinds.Player)
                {
                    players[entityId] = new ShooterSveltoPlayerComponent
                    {
                        PlayerId = entityId,
                        AimX = 1f,
                        Hp = ShooterGameplay.DefaultPlayerHp,
                        Alive = (flags & ShooterPackedEntityFlags.Alive) != 0
                    };
                }
                else if (chunk.EntityKind == ShooterPackedEntityKinds.Projectile)
                {
                    projectiles[entityId] = new ShooterSveltoProjectileComponent
                    {
                        BulletId = entityId,
                        OwnerPlayerId = GetInt(chunk.OwnerIds, i)
                    };
                }
            }
        }

        private void ImportTransformComponentChunk(
            in ShooterPackedComponentChunk chunk,
            Dictionary<int, ShooterSveltoPlayerComponent> players,
            Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                if (chunk.EntityKind == ShooterPackedEntityKinds.Player && players.TryGetValue(entityId, out var player))
                {
                    player.X = GetFloat(chunk.ValueX, i);
                    player.Y = GetFloat(chunk.ValueY, i);
                    player.AimX = GetFloat(chunk.ValueZ, i, 1f);
                    player.AimY = GetFloat(chunk.ValueW, i);
                    players[entityId] = player;
                }
                else if (chunk.EntityKind == ShooterPackedEntityKinds.Projectile && projectiles.TryGetValue(entityId, out var projectile))
                {
                    projectile.X = GetFloat(chunk.ValueX, i);
                    projectile.Y = GetFloat(chunk.ValueY, i);
                    projectile.VelocityX = GetPackedPairValue(chunk.Aux, i, 0);
                    projectile.VelocityY = GetPackedPairValue(chunk.Aux, i, 1);
                    projectiles[entityId] = projectile;
                }
            }
        }

        private void ImportHealthComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoPlayerComponent> players)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !players.TryGetValue(entityId, out var player)) continue;

                player.Hp = GetInt(chunk.IntValues, i, player.Hp);
                players[entityId] = player;
            }
        }

        private void ImportScoreComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoPlayerComponent> players)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !players.TryGetValue(entityId, out var player)) continue;

                player.Score = GetInt(chunk.IntValues, i, player.Score);
                players[entityId] = player;
            }
        }

        private void ImportProjectileLifetimeComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Projectile)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !projectiles.TryGetValue(entityId, out var projectile)) continue;

                projectile.RemainingFrames = GetInt(chunk.IntValues, i, projectile.RemainingFrames);
                projectiles[entityId] = projectile;
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

        private static int GetInt(int[] values, int index, int fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static float GetFloat(float[] values, int index, float fallback = 0f)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static byte GetByte(byte[] values, int index, byte fallback = 0)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static int[] PackPairValues(float[] left, float[] right)
        {
            var count = Math.Max(left?.Length ?? 0, right?.Length ?? 0);
            if (count == 0)
            {
                return Array.Empty<int>();
            }

            var values = new int[count * 2];
            for (int i = 0; i < count; i++)
            {
                values[(i * 2)] = Quantize(GetFloat(left, i));
                values[(i * 2) + 1] = Quantize(GetFloat(right, i));
            }

            return values;
        }

        private static float GetPackedPairValue(int[] values, int index, int slot)
        {
            return GetInt(values, (index * 2) + slot) / 10000f;
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

        private static int[] CopyAndSort(IReadOnlyCollection<int> ids)
        {
            var sorted = new int[ids.Count];
            var index = 0;
            foreach (var id in ids)
            {
                sorted[index++] = id;
            }

            Array.Sort(sorted);
            return sorted;
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
