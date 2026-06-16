#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotViewModelMapper
    {
        private readonly List<ShooterViewEntityChange> _entityChanges = new List<ShooterViewEntityChange>();
        private readonly List<ShooterViewEntityKey> _removedEntities = new List<ShooterViewEntityKey>();
        private readonly List<ShooterViewTransformComponentChange> _transformChanges = new List<ShooterViewTransformComponentChange>();
        private readonly List<ShooterViewHealthComponentChange> _healthChanges = new List<ShooterViewHealthComponentChange>();
        private readonly List<ShooterViewScoreComponentChange> _scoreChanges = new List<ShooterViewScoreComponentChange>();
        private readonly List<ShooterViewProjectileLifetimeComponentChange> _projectileLifetimeChanges = new List<ShooterViewProjectileLifetimeComponentChange>();
        private readonly List<ShooterEventSnapshot> _events = new List<ShooterEventSnapshot>();
        private ulong _nextSequence;

        public ShooterSnapshotViewBatch Map(in ShooterStateSnapshotPayload snapshot)
        {
            return Map(in snapshot, ShooterViewBatchSource.LocalPrediction);
        }

        public ShooterSnapshotViewBatch Map(in ShooterStateSnapshotPayload snapshot, ShooterViewBatchSource source)
        {
            BeginSnapshot();

            if (snapshot.Players != null)
            {
                for (int i = 0; i < snapshot.Players.Length; i++)
                {
                    var player = snapshot.Players[i];
                    var key = new ShooterViewEntityKey(ShooterViewEntityKind.Player, player.PlayerId);
                    AddEntity(key, 0, player.Alive);
                    AddTransform(key, player.X, player.Y, player.AimX, player.AimY, 0f, 0f);
                    AddHealth(key, player.Hp);
                    AddScore(key, player.Score);
                }
            }

            if (snapshot.Bullets != null)
            {
                for (int i = 0; i < snapshot.Bullets.Length; i++)
                {
                    var bullet = snapshot.Bullets[i];
                    var key = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, bullet.BulletId);
                    var alive = bullet.RemainingFrames > 0;
                    AddEntity(key, bullet.OwnerPlayerId, alive);
                    AddTransform(key, bullet.X, bullet.Y, bullet.VelocityX, bullet.VelocityY, bullet.VelocityX, bullet.VelocityY);
                    AddProjectileLifetime(key, bullet.RemainingFrames);
                }
            }

            if (snapshot.Events != null)
            {
                _events.AddRange(snapshot.Events);
            }

            return CompleteSnapshot(
                0UL,
                snapshot.Frame,
                ShooterViewSnapshotKind.Full,
                source);
        }

        public ShooterSnapshotViewBatch Map(in ShooterGatewaySnapshot snapshot)
        {
            return Map(in snapshot, controlledPlayerId: -1);
        }

        public ShooterSnapshotViewBatch Map(in ShooterGatewaySnapshot snapshot, int controlledPlayerId)
        {
            if (snapshot.PackedSnapshot.HasValue)
            {
                var packed = snapshot.PackedSnapshot.Value;
                return MapPackedSnapshot(snapshot.WorldId, in packed, ShooterViewBatchSource.AuthoritativeCorrection, controlledPlayerId);
            }

            BeginSnapshot();

            var actors = snapshot.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                var key = new ShooterViewEntityKey(ShooterViewEntityKind.Player, actor.ActorId);
                AddEntity(key, 0, actor.Hp > 0f);
                AddHealth(key, ToDisplayHp(actor.Hp));

                if (actor.ActorId == controlledPlayerId)
                {
                    continue;
                }

                AddTransform(key, actor.X, actor.Y, 0f, 1f, actor.VelocityX, actor.VelocityY);
            }

            return CompleteSnapshot(
                snapshot.WorldId,
                snapshot.Frame,
                snapshot.IsFullSnapshot ? ShooterViewSnapshotKind.Full : ShooterViewSnapshotKind.Delta,
                ShooterViewBatchSource.JoinOrReconnect);
        }
 
        public ShooterSnapshotViewBatch Map(in ShooterPackedSnapshotPayload snapshot)
        {
            return MapPackedSnapshot(0UL, in snapshot, ShooterViewBatchSource.AuthoritativeCorrection, controlledPlayerId: -1);
        }

        private ShooterSnapshotViewBatch MapPackedSnapshot(
            ulong worldId,
            in ShooterPackedSnapshotPayload snapshot,
            ShooterViewBatchSource source,
            int controlledPlayerId)
        {
            BeginSnapshot();

            var componentChunks = snapshot.ComponentChunks;
            if (componentChunks != null)
            {
                for (int i = 0; i < componentChunks.Length; i++)
                {
                    var chunk = componentChunks[i];
                    ApplyPackedComponentChunk(in chunk, controlledPlayerId);
                }
            }

            var snapshotKind = (snapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Full) != 0
                ? ShooterViewSnapshotKind.Full
                : ShooterViewSnapshotKind.Delta;

            return CompleteSnapshot(
                worldId,
                snapshot.Frame,
                snapshotKind,
                source);
        }

        private void ApplyPackedComponentChunk(in ShooterPackedComponentChunk chunk, int controlledPlayerId)
        {
            switch (chunk.ComponentKind)
            {
                case ShooterPackedComponentKinds.EntityLifecycle:
                    ApplyPackedLifecycleComponents(in chunk);
                    break;
                case ShooterPackedComponentKinds.Transform:
                    ApplyPackedTransformComponents(in chunk, controlledPlayerId);
                    break;
                case ShooterPackedComponentKinds.Health:
                    ApplyPackedHealthComponents(in chunk);
                    break;
                case ShooterPackedComponentKinds.Score:
                    ApplyPackedScoreComponents(in chunk);
                    break;
                case ShooterPackedComponentKinds.ProjectileLifetime:
                    ApplyPackedProjectileLifetimeComponents(in chunk);
                    break;
            }
        }

        private void ApplyPackedLifecycleComponents(in ShooterPackedComponentChunk chunk)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                var key = CreateViewEntityKey(chunk.EntityKind, entityId);
                if (!key.HasValue) continue;

                var flags = GetByte(chunk.Flags, i);
                AddEntity(key.Value, GetInt(chunk.OwnerIds, i), (flags & ShooterPackedEntityFlags.Alive) != 0);
            }
        }

        private void ApplyPackedTransformComponents(in ShooterPackedComponentChunk chunk, int controlledPlayerId)
        {
            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var entityId = GetInt(chunk.EntityIds, i);
                if (entityId <= 0) continue;

                var key = CreateViewEntityKey(chunk.EntityKind, entityId);
                if (!key.HasValue) continue;
                if (IsControlledPlayerTransform(key.Value, controlledPlayerId)) continue;

                AddTransform(
                    key.Value,
                    GetFloat(chunk.ValueX, i),
                    GetFloat(chunk.ValueY, i),
                    GetFloat(chunk.ValueZ, i, 1f),
                    GetFloat(chunk.ValueW, i),
                    GetPackedPairValue(chunk.Aux, i, 0),
                    GetPackedPairValue(chunk.Aux, i, 1));
            }
        }

        private void ApplyPackedHealthComponents(in ShooterPackedComponentChunk chunk)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var playerId = GetInt(chunk.EntityIds, i);
                if (playerId <= 0) continue;

                AddHealth(new ShooterViewEntityKey(ShooterViewEntityKind.Player, playerId), GetInt(chunk.IntValues, i));
            }
        }

        private void ApplyPackedScoreComponents(in ShooterPackedComponentChunk chunk)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Player)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var playerId = GetInt(chunk.EntityIds, i);
                if (playerId <= 0) continue;

                AddScore(new ShooterViewEntityKey(ShooterViewEntityKind.Player, playerId), GetInt(chunk.IntValues, i));
            }
        }

        private void ApplyPackedProjectileLifetimeComponents(in ShooterPackedComponentChunk chunk)
        {
            if (chunk.EntityKind != ShooterPackedEntityKinds.Projectile)
            {
                return;
            }

            var count = Math.Max(0, chunk.Count);
            for (int i = 0; i < count; i++)
            {
                var bulletId = GetInt(chunk.EntityIds, i);
                if (bulletId <= 0) continue;

                AddProjectileLifetime(new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, bulletId), GetInt(chunk.IntValues, i));
            }
        }

        private void BeginSnapshot()
        {
            _entityChanges.Clear();
            _removedEntities.Clear();
            _transformChanges.Clear();
            _healthChanges.Clear();
            _scoreChanges.Clear();
            _projectileLifetimeChanges.Clear();
            _events.Clear();
        }

        private void AddEntity(ShooterViewEntityKey key, int ownerEntityId, bool alive)
        {
            _entityChanges.Add(new ShooterViewEntityChange(key, ownerEntityId, alive));
        }

        private void AddTransform(
            ShooterViewEntityKey key,
            float x,
            float y,
            float facingX,
            float facingY,
            float velocityX,
            float velocityY)
        {
            _transformChanges.Add(new ShooterViewTransformComponentChange(key, x, y, facingX, facingY, velocityX, velocityY));
        }

        private void AddHealth(ShooterViewEntityKey key, int hp)
        {
            _healthChanges.Add(new ShooterViewHealthComponentChange(key, hp));
        }

        private void AddScore(ShooterViewEntityKey key, int score)
        {
            _scoreChanges.Add(new ShooterViewScoreComponentChange(key, score));
        }

        private void AddProjectileLifetime(ShooterViewEntityKey key, int remainingFrames)
        {
            _projectileLifetimeChanges.Add(new ShooterViewProjectileLifetimeComponentChange(key, remainingFrames));
        }

        private ShooterSnapshotViewBatch CompleteSnapshot(
            ulong worldId,
            int frame,
            ShooterViewSnapshotKind snapshotKind,
            ShooterViewBatchSource source)
        {
            return new ShooterSnapshotViewBatch(
                worldId,
                frame,
                ++_nextSequence,
                snapshotKind,
                source,
                _entityChanges.ToArray(),
                _removedEntities.ToArray(),
                _transformChanges.ToArray(),
                _healthChanges.ToArray(),
                _scoreChanges.ToArray(),
                _projectileLifetimeChanges.ToArray(),
                _events.ToArray());
        }

        private static bool IsControlledPlayerTransform(ShooterViewEntityKey key, int controlledPlayerId)
        {
            return controlledPlayerId > 0 && key.Kind == ShooterViewEntityKind.Player && key.EntityId == controlledPlayerId;
        }

        private static ShooterViewEntityKey? CreateViewEntityKey(int entityKind, int entityId)
        {
            switch (entityKind)
            {
                case ShooterPackedEntityKinds.Player:
                    return new ShooterViewEntityKey(ShooterViewEntityKind.Player, entityId);
                case ShooterPackedEntityKinds.Projectile:
                    return new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, entityId);
                default:
                    return null;
            }
        }

        private static int ToDisplayHp(float hp)
        {
            if (hp <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(hp);
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

        private static float GetPackedPairValue(int[] values, int index, int slot)
        {
            return GetInt(values, (index * 2) + slot) / 10000f;
        }
    }
}
