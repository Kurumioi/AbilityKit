using System;
using System.Collections.Generic;
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
        private readonly ShooterSnapshotOrderBuffer _orderBuffer = new();
        private readonly HashSet<int> _lastExportedProjectileIds = new HashSet<int>();
        private readonly HashSet<int> _lastExportedEnemyIds = new HashSet<int>();

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
            var componentChunks = ExportComponentChunks(isFullSnapshot);

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

        private ShooterPackedComponentChunk[] ExportComponentChunks(bool isFullSnapshot)
        {
            return new[]
            {
                ExportRuntimeMetadataChunk(),
                ExportPlayerLifecycleChunk(),
                ExportProjectileLifecycleChunk(isFullSnapshot),
                ExportEnemyLifecycleChunk(isFullSnapshot),
                ExportPlayerTransformChunk(),
                ExportProjectileTransformChunk(),
                ExportEnemyTransformChunk(),
                ExportPlayerHealthChunk(),
                ExportEnemyHealthChunk(),
                ExportPlayerScoreChunk(),
                ExportProjectileLifetimeChunk()
            };
        }

        private ShooterPackedComponentChunk ExportRuntimeMetadataChunk()
        {
            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.RuntimeMetadata,
                0,
                1,
                Array.Empty<int>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                new[]
                {
                    (int)_state.MatchState,
                    _state.MatchCompletedFrame,
                    _state.DefeatedEnemies,
                    _state.VictoryTargetDefeats,
                    _state.TimeLimitFrames,
                    _state.RemainingTimeFrames
                },
                Array.Empty<byte>(),
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        private ShooterPackedComponentChunk ExportPlayerLifecycleChunk()
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Player);
            }

            var order = _orderBuffer.CreateSortedPlayerOrder(players, count);
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

        private ShooterPackedComponentChunk ExportProjectileLifecycleChunk(bool isFullSnapshot)
        {
            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var count);
            var order = count > 0 ? _orderBuffer.CreateSortedProjectileOrder(bullets, count) : Array.Empty<int>();
            var currentProjectileIds = new HashSet<int>();
            var despawnedProjectileIds = isFullSnapshot ? Array.Empty<int>() : CollectDespawnedProjectiles(bullets, order, count, currentProjectileIds);
            var totalCount = count + despawnedProjectileIds.Length;
            if (totalCount == 0)
            {
                _lastExportedProjectileIds.Clear();
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = new int[totalCount];
            var flags = new byte[totalCount];
            var ownerIds = new int[totalCount];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
                flags[i] = (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile);
                ownerIds[i] = bullet.OwnerPlayerId;
                currentProjectileIds.Add(bullet.BulletId);
            }

            for (int i = 0; i < despawnedProjectileIds.Length; i++)
            {
                var targetIndex = count + i;
                entityIds[targetIndex] = despawnedProjectileIds[i];
                flags[targetIndex] = (byte)(ShooterPackedEntityFlags.Projectile | ShooterPackedEntityFlags.Despawned);
            }

            ReplaceLastExportedProjectiles(currentProjectileIds);

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

        private int[] CollectDespawnedProjectiles(
            NB<ShooterSveltoProjectileComponent> bullets,
            int[] order,
            int count,
            HashSet<int> currentProjectileIds)
        {
            for (int i = 0; i < count; i++)
            {
                currentProjectileIds.Add(bullets[order[i]].BulletId);
            }

            if (_lastExportedProjectileIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var despawned = new List<int>();
            foreach (var projectileId in _lastExportedProjectileIds)
            {
                if (!currentProjectileIds.Contains(projectileId))
                {
                    despawned.Add(projectileId);
                }
            }

            despawned.Sort();
            return despawned.ToArray();
        }

        private void ReplaceLastExportedProjectiles(HashSet<int> currentProjectileIds)
        {
            _lastExportedProjectileIds.Clear();
            foreach (var projectileId in currentProjectileIds)
            {
                _lastExportedProjectileIds.Add(projectileId);
            }
        }

        private ShooterPackedComponentChunk ExportEnemyLifecycleChunk(bool isFullSnapshot)
        {
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> _, out NB<ShooterSveltoHealthComponent> healths, out NativeEntityIDs ids, out var count);
            var order = count > 0 ? _orderBuffer.CreateSortedEnemyOrder(ids, count) : Array.Empty<int>();
            var currentEnemyIds = new HashSet<int>();
            var despawnedEnemyIds = isFullSnapshot ? Array.Empty<int>() : CollectDespawnedEnemies(ids, order, count, currentEnemyIds);
            var totalCount = count + despawnedEnemyIds.Length;
            if (totalCount == 0)
            {
                _lastExportedEnemyIds.Clear();
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Enemy);
            }

            var entityIds = new int[totalCount];
            var flags = new byte[totalCount];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                var enemyId = (int)ids[sourceIndex];
                entityIds[i] = enemyId;
                flags[i] = (byte)ShooterPackedEntityFlags.Enemy;
                if (healths[sourceIndex].Alive != 0)
                {
                    flags[i] |= ShooterPackedEntityFlags.Alive;
                }

                currentEnemyIds.Add(enemyId);
            }

            for (int i = 0; i < despawnedEnemyIds.Length; i++)
            {
                var targetIndex = count + i;
                entityIds[targetIndex] = despawnedEnemyIds[i];
                flags[targetIndex] = (byte)(ShooterPackedEntityFlags.Enemy | ShooterPackedEntityFlags.Despawned);
            }

            ReplaceLastExportedEnemies(currentEnemyIds);

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

        private int[] CollectDespawnedEnemies(
            NativeEntityIDs ids,
            int[] order,
            int count,
            HashSet<int> currentEnemyIds)
        {
            for (int i = 0; i < count; i++)
            {
                currentEnemyIds.Add((int)ids[order[i]]);
            }

            if (_lastExportedEnemyIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var despawned = new List<int>();
            foreach (var enemyId in _lastExportedEnemyIds)
            {
                if (!currentEnemyIds.Contains(enemyId))
                {
                    despawned.Add(enemyId);
                }
            }

            despawned.Sort();
            return despawned.ToArray();
        }

        private void ReplaceLastExportedEnemies(HashSet<int> currentEnemyIds)
        {
            _lastExportedEnemyIds.Clear();
            foreach (var enemyId in currentEnemyIds)
            {
                _lastExportedEnemyIds.Add(enemyId);
            }
        }

        private ShooterPackedComponentChunk ExportPlayerTransformChunk()
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Player);
            }

            var order = _orderBuffer.CreateSortedPlayerOrder(players, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            var packedVelocity = new int[count * 2];
            for (int i = 0; i < count; i++)
            {
                var player = players[order[i]];
                entityIds[i] = player.PlayerId;
                posX[i] = player.X;
                posY[i] = player.Y;
                facingX[i] = player.AimX;
                facingY[i] = player.AimY;

                var velocityX = 0f;
                var velocityY = 0f;
                if (_state.InputBuffer.TryGetLatestCommand(player.PlayerId, out var command))
                {
                    var moveX = command.MoveX;
                    var moveY = command.MoveY;
                    if (ShooterBattleMath.Normalize(ref moveX, ref moveY) > 0f)
                    {
                        velocityX = moveX * _rules.PlayerSpeed;
                        velocityY = moveY * _rules.PlayerSpeed;
                    }
                }

                ShooterPackedSnapshotChunkCodec.SetPackedPairValue(packedVelocity, i, velocityX, velocityY);
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
                packedVelocity);
        }

        private ShooterPackedComponentChunk ExportProjectileTransformChunk()
        {
            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Projectile);
            }

            var order = _orderBuffer.CreateSortedProjectileOrder(bullets, count);
            var entityIds = new int[count];
            var posX = new float[count];
            var posY = new float[count];
            var facingX = new float[count];
            var facingY = new float[count];
            var packedVelocity = new int[count * 2];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
                posX[i] = bullet.X;
                posY[i] = bullet.Y;
                ShooterPackedSnapshotChunkCodec.SetPackedPairValue(packedVelocity, i, bullet.VelocityX, bullet.VelocityY);
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
                packedVelocity);
        }

        private ShooterPackedComponentChunk ExportEnemyTransformChunk()
        {
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> transforms, out NB<ShooterSveltoHealthComponent> _, out NativeEntityIDs ids, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Enemy);
            }

            var order = _orderBuffer.CreateSortedEnemyOrder(ids, count);
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
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Player);
            }

            var order = _orderBuffer.CreateSortedPlayerOrder(players, count);
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
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> _, out NB<ShooterSveltoHealthComponent> healths, out NativeEntityIDs ids, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Enemy);
            }

            var order = _orderBuffer.CreateSortedEnemyOrder(ids, count);
            var entityIds = new int[count];
            var hp = new int[count];
            var maxHp = new int[count];
            for (int i = 0; i < count; i++)
            {
                var sourceIndex = order[i];
                entityIds[i] = (int)ids[sourceIndex];
                hp[i] = healths[sourceIndex].Current;
                maxHp[i] = healths[sourceIndex].Max;
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
                maxHp);
        }

        private ShooterPackedComponentChunk ExportPlayerScoreChunk()
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Score, ShooterPackedEntityKinds.Player);
            }

            var order = _orderBuffer.CreateSortedPlayerOrder(players, count);
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
            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var count);
            if (count == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
            }

            var order = _orderBuffer.CreateSortedProjectileOrder(bullets, count);
            var entityIds = new int[count];
            var remainingFrames = new int[count];
            var penetrationRemaining = new int[count];
            var explosionRadius = new float[count];
            var explosionDamage = new float[count];
            for (int i = 0; i < count; i++)
            {
                var bullet = bullets[order[i]];
                entityIds[i] = bullet.BulletId;
                remainingFrames[i] = bullet.RemainingFrames;
                penetrationRemaining[i] = bullet.PenetrationRemaining;
                explosionRadius[i] = bullet.ExplosionRadius;
                explosionDamage[i] = bullet.ExplosionDamage;
            }

            return new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.ProjectileLifetime,
                ShooterPackedEntityKinds.Projectile,
                entityIds.Length,
                entityIds,
                explosionRadius,
                explosionDamage,
                Array.Empty<float>(),
                Array.Empty<float>(),
                remainingFrames,
                Array.Empty<byte>(),
                Array.Empty<int>(),
                penetrationRemaining);
        }

        private int CountEnemies()
        {
            return _context.EntitiesDB.Count<ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
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
