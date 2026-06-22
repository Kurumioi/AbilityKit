#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterBattleSimulation
    {
        void Tick(float deltaTime);
    }

    [WorldService(typeof(ShooterBattleSimulation), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterBattleSimulation), WorldLifetime.Singleton)]
    public sealed class ShooterBattleSimulation : IShooterBattleSimulation
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly IShooterBattleRules _rules;
        private readonly List<int> _projectileRemovalBuffer = new List<int>(32);
        private readonly ISveltoWorldContext _context;

        public ShooterBattleSimulation(ShooterBattleState state)
            : this(state, ShooterBattleRules.Default)
        {
        }

        public ShooterBattleSimulation(ShooterBattleState state, IShooterBattleRules rules)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _entities = _state.Entities;
            _context = _entities.SveltoContext;
        }

        public void Tick(float deltaTime)
        {
            if (_state.MatchState != ShooterBattleMatchState.Running)
            {
                return;
            }

            _entities.BeginStructuralChanges();
            try
            {
                TickPlayers(deltaTime);
            }
            finally
            {
                _entities.EndStructuralChanges();
            }

            _entities.BeginStructuralChanges();
            try
            {
                TickBullets(deltaTime);
            }
            finally
            {
                _entities.EndStructuralChanges();
            }
        }

        private void TickPlayers(float deltaTime)
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var count);
            for (int i = 0; i < count; i++)
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

        private void TickBullets(float deltaTime)
        {
            _projectileRemovalBuffer.Clear();
            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> bullets, out _, out var count);
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> players, out _, out var playerCount);
            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out NativeEntityIDs enemyIds, out var enemyCount);
            for (int i = count - 1; i >= 0; i--)
            {
                bullets[i].X += bullets[i].VelocityX * deltaTime;
                bullets[i].Y += bullets[i].VelocityY * deltaTime;
                bullets[i].RemainingFrames--;

                if (TryHitPlayer(in bullets[i], players, playerCount, out var target))
                {
                    IncrementPlayerScore(bullets[i].OwnerPlayerId, players, playerCount);
                    _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, bullets[i].OwnerPlayerId, target.PlayerId, bullets[i].BulletId, target.X, target.Y, _rules.HitDamage));
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                    continue;
                }

                if (TryHitEnemy(in bullets[i], enemyTransforms, enemyHealths, enemyIds, enemyCount, out var enemyId, out var enemyX, out var enemyY, out var defeated))
                {
                    if (defeated)
                    {
                        _state.DefeatedEnemies++;
                        IncrementPlayerScore(bullets[i].OwnerPlayerId, players, playerCount);
                    }

                    _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Hit, bullets[i].OwnerPlayerId, -(int)enemyId, bullets[i].BulletId, enemyX, enemyY, _rules.HitDamage));
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                    continue;
                }

                if (bullets[i].RemainingFrames <= 0)
                {
                    _projectileRemovalBuffer.Add(bullets[i].BulletId);
                }
            }

            for (int i = 0; i < _projectileRemovalBuffer.Count; i++)
            {
                _entities.RemoveProjectile(_projectileRemovalBuffer[i]);
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
            _state.Events.Add(new ShooterEventSnapshot(ShooterEventType.Fire, player.PlayerId, 0, bullet.BulletId, bullet.X, bullet.Y, 0));
        }

        private bool TryHitPlayer(in ShooterSveltoProjectileComponent bullet, NB<ShooterSveltoPlayerComponent> players, int count, out ShooterSveltoPlayerComponent target)
        {
            for (int i = 0; i < count; i++)
            {
                if (!players[i].Alive || players[i].PlayerId == bullet.OwnerPlayerId)
                {
                    continue;
                }

                var dx = players[i].X - bullet.X;
                var dy = players[i].Y - bullet.Y;
                if (dx * dx + dy * dy > _rules.HitRadius * _rules.HitRadius)
                {
                    continue;
                }

                players[i].Hp = Math.Max(0, players[i].Hp - _rules.HitDamage);
                if (players[i].Hp == 0)
                {
                    players[i].Alive = false;
                }

                target = players[i];
                return true;
            }

            target = default;
            return false;
        }

        private static void IncrementPlayerScore(int playerId, NB<ShooterSveltoPlayerComponent> players, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    players[i].Score++;
                    return;
                }
            }
        }

        private bool TryHitEnemy(
            in ShooterSveltoProjectileComponent bullet,
            NB<ShooterSveltoTransformComponent> transforms,
            NB<ShooterSveltoHealthComponent> healths,
            NativeEntityIDs ids,
            int count,
            out uint enemyId,
            out float enemyX,
            out float enemyY,
            out bool defeated)
        {
            for (int i = 0; i < count; i++)
            {
                if (healths[i].Alive == 0)
                {
                    continue;
                }

                var dx = transforms[i].X - bullet.X;
                var dy = transforms[i].Y - bullet.Y;
                if (dx * dx + dy * dy > _rules.HitRadius * _rules.HitRadius)
                {
                    continue;
                }

                healths[i].Current = Math.Max(0, healths[i].Current - _rules.HitDamage);
                defeated = healths[i].Current == 0;
                if (defeated)
                {
                    healths[i].Alive = 0;
                }

                enemyId = ids[i];
                enemyX = transforms[i].X;
                enemyY = transforms[i].Y;
                return true;
            }

            enemyId = 0u;
            enemyX = 0f;
            enemyY = 0f;
            defeated = false;
            return false;
        }

    }
}
