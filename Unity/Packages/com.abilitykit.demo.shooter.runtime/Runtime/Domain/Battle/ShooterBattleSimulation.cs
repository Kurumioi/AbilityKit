#nullable enable

using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

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
        private readonly ShooterPlayerCommandBattleModule _players;
        private readonly ShooterProjectileCombatBattleModule _projectiles;

        public ShooterBattleSimulation(ShooterBattleState state)
            : this(state, ShooterBattleRules.Default)
        {
        }

        public ShooterBattleSimulation(ShooterBattleState state, IShooterBattleRules rules)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            _entities = _state.Entities;
            var events = new ShooterCombatEventBuffer(_state);
            _players = new ShooterPlayerCommandBattleModule(_state, _entities, rules, events);
            _projectiles = new ShooterProjectileCombatBattleModule(_state, _entities, rules, events);
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
                _players.Tick(deltaTime);
            }
            finally
            {
                _entities.EndStructuralChanges();
            }

            _entities.BeginStructuralChanges();
            try
            {
                _projectiles.Tick(deltaTime);
            }
            finally
            {
                _entities.EndStructuralChanges();
            }
        }
    }
}
