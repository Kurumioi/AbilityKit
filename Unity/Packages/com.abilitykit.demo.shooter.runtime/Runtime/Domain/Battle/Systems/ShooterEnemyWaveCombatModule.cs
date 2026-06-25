#nullable enable

using System;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterEnemyWaveCombatModule
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ShooterCombatEventBuffer _events;
        private readonly ShooterSpatialTargetIndex _targetIndex = new();
        private readonly ISveltoWorldContext _context;
        private readonly int _attackIntervalFrames;
        private readonly int _attackDamage;

        public ShooterEnemyWaveCombatModule(ShooterBattleState state, IShooterEntityManager entities)
            : this(state, entities, ShooterEnemyWaveOptions.DefaultEnabled)
        {
        }

        public ShooterEnemyWaveCombatModule(ShooterBattleState state, IShooterEntityManager entities, ShooterEnemyWaveOptions options)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _events = new ShooterCombatEventBuffer(_state);
            _context = _entities.SveltoContext;
            var activeOptions = options ?? ShooterEnemyWaveOptions.DefaultEnabled;
            _attackIntervalFrames = activeOptions.EnemyAttackIntervalFrames;
            _attackDamage = activeOptions.EnemyAttackDamage;
        }

        public void Tick()
        {
            if (_state.CurrentFrame % _attackIntervalFrames != 0 || _entities.PlayerCount == 0 || _entities.EnemyCount == 0)
            {
                return;
            }

            _targetIndex.Rebuild(_context, _state.CurrentFrame);
            var (transforms, healths, ids, count) = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            for (var i = 0; i < count; i++)
            {
                if (healths[i].Alive == 0)
                {
                    continue;
                }

                if (!_targetIndex.TryGetLivePlayerByPosition(transforms[i].X, transforms[i].Y, out var player))
                {
                    continue;
                }

                player.Hp = Math.Max(0, player.Hp - _attackDamage);
                if (player.Hp == 0)
                {
                    player.Alive = false;
                }

                _entities.SetPlayer(in player);
                _events.AddEnemyAttack(ids[i], player.PlayerId, transforms[i].X, transforms[i].Y, _attackDamage);
            }
        }
    }
}
