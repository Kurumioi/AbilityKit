using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterPackedSnapshotImporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;

        public ShooterPackedSnapshotImporter(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public bool Import(in ShooterPackedSnapshotPayload snapshot)
        {
            if (snapshot.Version <= 0)
            {
                return false;
            }

            var isDelta = (snapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Delta) != 0;
            if (!isDelta)
            {
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

        private static void ImportLifecycleComponentChunk(
            in ShooterPackedComponentChunk chunk,
            Dictionary<int, ShooterSveltoPlayerComponent> players,
            Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                var flags = ShooterPackedSnapshotChunkCodec.GetByte(chunk.Flags, i);
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
                        OwnerPlayerId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.OwnerIds, i)
                    };
                }
            }
        }

        private static void ImportTransformComponentChunk(
            in ShooterPackedComponentChunk chunk,
            Dictionary<int, ShooterSveltoPlayerComponent> players,
            Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                if (chunk.EntityKind == ShooterPackedEntityKinds.Player && players.TryGetValue(entityId, out var player))
                {
                    player.X = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueX, i);
                    player.Y = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueY, i);
                    player.AimX = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueZ, i, 1f);
                    player.AimY = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueW, i);
                    players[entityId] = player;
                }
                else if (chunk.EntityKind == ShooterPackedEntityKinds.Projectile && projectiles.TryGetValue(entityId, out var projectile))
                {
                    projectile.X = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueX, i);
                    projectile.Y = ShooterPackedSnapshotChunkCodec.GetFloat(chunk.ValueY, i);
                    projectile.VelocityX = ShooterPackedSnapshotChunkCodec.GetPackedPairValue(chunk.Aux, i, 0);
                    projectile.VelocityY = ShooterPackedSnapshotChunkCodec.GetPackedPairValue(chunk.Aux, i, 1);
                    projectiles[entityId] = projectile;
                }
            }
        }

        private static void ImportHealthComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoPlayerComponent> players)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !players.TryGetValue(entityId, out var player)) continue;

                player.Hp = ShooterPackedSnapshotChunkCodec.GetInt(chunk.IntValues, i, player.Hp);
                players[entityId] = player;
            }
        }

        private static void ImportScoreComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoPlayerComponent> players)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !players.TryGetValue(entityId, out var player)) continue;

                player.Score = ShooterPackedSnapshotChunkCodec.GetInt(chunk.IntValues, i, player.Score);
                players[entityId] = player;
            }
        }

        private static void ImportProjectileLifetimeComponentChunk(in ShooterPackedComponentChunk chunk, Dictionary<int, ShooterSveltoProjectileComponent> projectiles)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Projectile)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = ShooterPackedSnapshotChunkCodec.GetInt(chunk.EntityIds, i);
                if (entityId <= 0 || !projectiles.TryGetValue(entityId, out var projectile)) continue;

                projectile.RemainingFrames = ShooterPackedSnapshotChunkCodec.GetInt(chunk.IntValues, i, projectile.RemainingFrames);
                projectiles[entityId] = projectile;
            }
        }
    }
}
