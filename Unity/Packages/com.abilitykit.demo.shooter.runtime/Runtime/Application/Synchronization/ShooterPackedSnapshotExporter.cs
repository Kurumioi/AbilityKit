using System;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterPackedSnapshotExporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly IShooterStateHashProvider _stateHashProvider;

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
                _entities.PlayerCount + _entities.ProjectileCount,
                Array.Empty<byte>(),
                componentChunks);
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

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
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

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
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

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
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
            if (_entities.ProjectileCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Transform, ShooterPackedEntityKinds.Projectile);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
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

        private ShooterPackedComponentChunk ExportPlayerHealthChunk()
        {
            if (_entities.PlayerCount == 0)
            {
                return ShooterPackedComponentChunk.Empty(ShooterPackedComponentKinds.Health, ShooterPackedEntityKinds.Player);
            }

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
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

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
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

            var entityIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
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
