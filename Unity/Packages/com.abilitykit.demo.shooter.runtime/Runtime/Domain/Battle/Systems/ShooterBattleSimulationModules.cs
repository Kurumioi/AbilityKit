#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterPlayerCommandBattleModule
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly ShooterCombatEventBuffer _events;
        private readonly ISveltoWorldContext _context;

        public ShooterPlayerCommandBattleModule(ShooterBattleState state, IShooterEntityManager entities, IShooterBattleRules rules, ShooterCombatEventBuffer events)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _context = _entities.SveltoContext;
        }

        public void Tick(float deltaTime)
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            for (var i = 0; i < count; i++)
            {
                if (!players[i].Alive)
                {
                    continue;
                }

                _state.InputBuffer.TryGetLatestCommand(players[i].PlayerId, out var command);
                var moveLength = ShooterBattleMath.Normalize(ref command.MoveX, ref command.MoveY);
                if (moveLength > 0f)
                {
                    players[i].X += command.MoveX * _rules.PlayerSpeed * deltaTime;
                    players[i].Y += command.MoveY * _rules.PlayerSpeed * deltaTime;
                }

                var aimLength = ShooterBattleMath.Normalize(ref command.AimX, ref command.AimY);
                if (aimLength > 0f)
                {
                    players[i].AimX = command.AimX;
                    players[i].AimY = command.AimY;
                }

                if (command.Fire)
                {
                    SpawnBullet(in players[i]);
                    _state.InputBuffer.TryConsumeLatestFire(_state.CurrentFrame, players[i].PlayerId, out _);
                }
            }
        }

        private void SpawnBullet(in ShooterSveltoPlayerComponent player)
        {
            var bullet = new ShooterSveltoProjectileComponent
            {
                BulletId = _state.AllocateBulletId(),
                OwnerPlayerId = player.PlayerId,
                X = player.X + player.AimX * 0.5f,
                Y = player.Y + player.AimY * 0.5f,
                VelocityX = player.AimX * _rules.BulletSpeed,
                VelocityY = player.AimY * _rules.BulletSpeed,
                RemainingFrames = _rules.BulletLifeFrames
            };

            _entities.AddProjectile(in bullet);
            _events.AddFire(player.PlayerId, bullet.BulletId, bullet.X, bullet.Y);
        }
    }

    internal sealed class ShooterProjectileCombatBattleModule
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly ShooterCombatEventBuffer _events;
        private readonly List<int> _projectileRemovalBuffer = new(32);
        private readonly List<ShooterPendingEnemyHit> _pendingEnemyHits = new(32);
        private readonly ShooterSpatialHitIndex _enemyHitIndex;
        private readonly ShooterSpatialPlayerHitIndex _playerHitIndex;
        private readonly ISveltoWorldContext _context;

        public ShooterProjectileCombatBattleModule(ShooterBattleState state, IShooterEntityManager entities, IShooterBattleRules rules, ShooterCombatEventBuffer events)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _context = _entities.SveltoContext;
            var hitCellSize = Math.Max(_rules.HitRadius * 2f, 1f);
            _enemyHitIndex = new ShooterSpatialHitIndex(hitCellSize);
            _playerHitIndex = new ShooterSpatialPlayerHitIndex(hitCellSize);
        }

        public void Tick(float deltaTime)
        {
            _projectileRemovalBuffer.Clear();
            _pendingEnemyHits.Clear();

            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var count);
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var playerCount);
            _playerHitIndex.Rebuild(players, playerCount);
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            _enemyHitIndex.Rebuild(enemyTransforms, enemyHealths, enemyCount);

            for (var i = count - 1; i >= 0; i--)
            {
                bullets[i].X += bullets[i].VelocityX * deltaTime;
                bullets[i].Y += bullets[i].VelocityY * deltaTime;
                bullets[i].RemainingFrames--;

                if (TryCollectPlayerHit(in bullets[i], players, out var targetIndex))
                {
                    ApplyPlayerHit(in bullets[i], players, playerCount, targetIndex);
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                    continue;
                }

                if (TryCollectEnemyHit(in bullets[i], enemyTransforms, enemyHealths, enemyIds, out var enemyHit))
                {
                    _pendingEnemyHits.Add(enemyHit);
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                    continue;
                }

                if (bullets[i].RemainingFrames <= 0)
                {
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                }
            }

            ResolveEnemyHits(enemyTransforms, enemyHealths, players, playerCount);
            for (var i = 0; i < _projectileRemovalBuffer.Count; i++)
            {
                _entities.RemoveProjectile(_projectileRemovalBuffer[i]);
            }
        }

        private bool TryCollectPlayerHit(in ShooterSveltoProjectileComponent bullet, NB<ShooterSveltoPlayerComponent> players, out int targetIndex)
        {
            return _playerHitIndex.TryFindFirstHit(bullet.X, bullet.Y, _rules.HitRadius, bullet.OwnerPlayerId, players, out targetIndex);
        }

        private void ApplyPlayerHit(in ShooterSveltoProjectileComponent bullet, NB<ShooterSveltoPlayerComponent> players, int playerCount, int targetIndex)
        {
            players[targetIndex].Hp = Math.Max(0, players[targetIndex].Hp - _rules.HitDamage);
            if (players[targetIndex].Hp == 0)
            {
                players[targetIndex].Alive = false;
            }

            IncrementPlayerScore(bullet.OwnerPlayerId, players, playerCount);
            var target = players[targetIndex];
            _events.AddPlayerHit(bullet.OwnerPlayerId, target.PlayerId, bullet.BulletId, target.X, target.Y, _rules.HitDamage);
        }

        private bool TryCollectEnemyHit(
            in ShooterSveltoProjectileComponent bullet,
            NB<ShooterSveltoTransformComponent> transforms,
            NB<ShooterSveltoHealthComponent> healths,
            NativeEntityIDs ids,
            out ShooterPendingEnemyHit hit)
        {
            if (_enemyHitIndex.TryFindFirstHit(bullet.X, bullet.Y, _rules.HitRadius, transforms, healths, ids, out var targetIndex, out var enemyId))
            {
                hit = new ShooterPendingEnemyHit(bullet.BulletId, bullet.OwnerPlayerId, targetIndex, enemyId);
                return true;
            }

            hit = default;
            return false;
        }

        private void ResolveEnemyHits(
            NB<ShooterSveltoTransformComponent> enemyTransforms,
            NB<ShooterSveltoHealthComponent> enemyHealths,
            NB<ShooterSveltoPlayerComponent> players,
            int playerCount)
        {
            for (var i = 0; i < _pendingEnemyHits.Count; i++)
            {
                var hit = _pendingEnemyHits[i];
                if (enemyHealths[hit.EnemyIndex].Alive == 0)
                {
                    continue;
                }

                enemyHealths[hit.EnemyIndex].Current = Math.Max(0, enemyHealths[hit.EnemyIndex].Current - _rules.HitDamage);
                var defeated = enemyHealths[hit.EnemyIndex].Current == 0;
                if (defeated)
                {
                    enemyHealths[hit.EnemyIndex].Alive = 0;
                    _state.DefeatedEnemies++;
                    IncrementPlayerScore(hit.OwnerPlayerId, players, playerCount);
                }

                _events.AddEnemyHit(hit.OwnerPlayerId, hit.EnemyId, hit.BulletId, enemyTransforms[hit.EnemyIndex].X, enemyTransforms[hit.EnemyIndex].Y, _rules.HitDamage);
            }
        }

        private static void IncrementPlayerScore(int playerId, NB<ShooterSveltoPlayerComponent> players, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    players[i].Score++;
                    return;
                }
            }
        }

        private readonly struct ShooterPendingEnemyHit
        {
            public ShooterPendingEnemyHit(int bulletId, int ownerPlayerId, int enemyIndex, uint enemyId)
            {
                BulletId = bulletId;
                OwnerPlayerId = ownerPlayerId;
                EnemyIndex = enemyIndex;
                EnemyId = enemyId;
            }

            public int BulletId { get; }
            public int OwnerPlayerId { get; }
            public int EnemyIndex { get; }
            public uint EnemyId { get; }
        }
    }
}
