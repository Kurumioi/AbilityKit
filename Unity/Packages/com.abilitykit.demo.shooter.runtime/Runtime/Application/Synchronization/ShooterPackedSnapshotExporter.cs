using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterPackedSnapshotExporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly IShooterStateHashProvider _stateHashProvider;
        private readonly ISveltoWorldContext _context;

        public ShooterPackedSnapshotExporter(
            ShooterBattleState state,
            IShooterEntityManager entities,
            IShooterBattleRules rules,
            IShooterStateHashProvider stateHashProvider)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _stateHashProvider = stateHashProvider ?? throw new ArgumentNullException(nameof(stateHashProvider));
            _context = _entities.SveltoContext;
        }

        public ShooterPackedSnapshotPayload Export(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false)
        {
            var componentChunks = ExportComponentChunks();

            return new ShooterPackedSnapshotPayload(
                ShooterPackedSnapshotCodec.CurrentVersion,
                worldId,
                _state.CurrentFrame,
                _state.CurrentFrame,
                CreateSnapshotFlags(isFullSnapshot, authorityOverride),
                _stateHashProvider.ComputeStateHash(),
                _entities.PlayerCount + _entities.ProjectileCount + CountEnemies(),
                Array.Empty<byte>(),
                componentChunks);
        }

        private ShooterPackedComponentChunk[] ExportComponentChunks()
        {
            return new[]
            {
                ExportPlayerLifecycleChunk(),
                ExportProjectileLifecycleChunk(),
                ExportEnemyLifecycleChunk(),
                ExportPlayerTransformChunk(),
                ExportProjectileTransformChunk(),
                ExportEnemyTransformChunk(),
                ExportPlayerHealthChunk(),
                ExportEnemyHealthChunk(),
                ExportPlayerScoreChunk(),
                ExportProjectileLifetimeChunk()
            };
        }

        private ShooterPackedComponentChunk ExportPlayerLifecycleChunk()
        {
            var (players, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Player);
            }

            var order = CreateSortedPlayerOrder(players, count);
            var entityIds = new int[count];
            var flags = new byte[count];
            var ownerIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                var player = players[order[i]];
                entityIds[i] = player.PlayerId;
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
            var (bullets, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
            }

            var order = CreateSortedProjectileOrder(bullets, count);
            var entityIds = new int[count];
            var flags = new byte[count];
            var ownerIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
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

        private ShooterPackedComponentChunk ExportEnemyLifecycleChunk()
        {
            var (_, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var flags = new byte[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                flags[i] = (byte)ShooterPackedEntityFlags.Enemy;
                if (healths[sourceIndex].Alive != 0)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Enemy,
                entityIds.Length,
                entityIds,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<int>(),
                flags,
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerTransformChunk()
        {
            var (players, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Player);
            }

            var order = CreateSortedPlayerOrder(players, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var velX = new float[count];
            var velY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            for (int i = 0; i < count; i++)
            {
                var player = players[order[i]];
                entityIds[i] = player.PlayerId;
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;

                if (_state.InputBuffer.TryGetLatestCommand(player.PlayerId, out var command))
                {
                    var moveX = command.MoveX;
                    var moveY = command.MoveY;
                    if (ShooterBattleMath.Normalize(ref moveX, ref moveY) > 0f)
                    {
                        velX[i] = moveX * _rules.PlayerSpeed;
                        velY[i] = moveY * _rules.PlayerSpeed;
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
                ShooterPackedSnapshotChunkCodec.PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportProjectileTransformChunk()
        {
            var (bullets, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Projectile);
            }

            var order = CreateSortedProjectileOrder(bullets, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var velX = new float[count];
            var velY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                velX[i] = bullet.VelocityX;
                velY[i] = bullet.VelocityY;
                var dirX = bullet.VelocityX;
                var dirY = bullet.VelocityY;
                ShooterBattleMath.Normalize(ref dirX, ref dirY);
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
                ShooterPackedSnapshotChunkCodec.PackPairValues(velX, velY));
        }

        private ShooterPackedComponentChunk ExportEnemyTransformChunk()
        {
            var (transforms, _, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                posX[i] = transforms[sourceIndex].X;
                posY[i] = transforms[sourceIndex].Y;
                facingX[i] = transforms[sourceIndex].DirectionX;
                facingY[i] = transforms[sourceIndex].DirectionY;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Enemy,
                entityIds.Length,
                entityIds,
                posX,
                posY,
                facingX,
                facingY,
                Array.Empty<int>(),
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerHealthChunk()
        {
            var (players, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Player);
            }

            var order = CreateSortedPlayerOrder(players, count);
            var entityIds = new int[count];
            var hp = new int[count];
            for (int i = 0; i < count; i++)
            {
                var player = players[order[i]];
                entityIds[i] = player.PlayerId;
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

        private ShooterPackedComponentChunk ExportEnemyHealthChunk()
        {
            var (_, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Enemy);
            }

            var order = CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var hp = new int[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                hp[i] = healths[sourceIndex].Current;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Health,
                ShooterPackedEntityKinds.Enemy,
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
            var (players, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Score, ShooterPackedEntityKinds.Player);
            }

            var order = CreateSortedPlayerOrder(players, count);
            var entityIds = new int[count];
            var scores = new int[count];
            for (int i = 0; i < count; i++)
            {
                var player = players[order[i]];
                entityIds[i] = player.PlayerId;
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
            var (bullets, _, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
            }

            var order = CreateSortedProjectileOrder(bullets, count);
            var entityIds = new int[count];
            var remainingFrames = new int[count];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
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

        private int CountEnemies()
        {
            return _context.EntitiesDB.Count<ShooterSveltoHealthComponent>(ShooterSveltoGroups.GameplayTargets);
        }

        private static int[] CreateSortedPlayerOrder(NB<ShooterSveltoPlayerComponent> players, int count)
        {
            var order = CreateIndexOrder(count);
            Array.Sort(order, (left, right) => players[left].PlayerId.CompareTo(players[right].PlayerId));
            return order;
        }

        private static int[] CreateSortedProjectileOrder(NB<ShooterSveltoProjectileComponent> bullets, int count)
        {
            var order = CreateIndexOrder(count);
            Array.Sort(order, (left, right) => bullets[left].BulletId.CompareTo(bullets[right].BulletId));
            return order;
        }

        private static int[] CreateSortedEnemyOrder(NativeEntityIDs ids, int count)
        {
            var order = CreateIndexOrder(count);
            Array.Sort(order, (left, right) => ids[left].CompareTo(ids[right]));
            return order;
        }

        private static int[] CreateIndexOrder(int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            return order;
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
    }
}
