using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterBattleState), WorldLifetime.Singleton)]
    public sealed class ShooterBattleState
    {
        private readonly IShooterEntityManager _entities;
        private int _nextBulletId = 1;

        public ShooterBattleState(IShooterEntityManager entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public IShooterEntityManager Entities => _entities;

        public ShooterInputFrameBuffer InputBuffer { get; } = new ShooterInputFrameBuffer();

        public IReadOnlyDictionary<int, ShooterPlayerCommand> LatestCommands => InputBuffer.LatestCommands;

        public List<ShooterEventSnapshot> Events { get; } = new List<ShooterEventSnapshot>(16);

        public ShooterBattleMatchState MatchState { get; private set; }

        public int MatchCompletedFrame { get; private set; }

        public int DefeatedEnemies { get; set; }

        public int VictoryTargetDefeats { get; set; } = 72;

        public bool IsStarted
        {
            get => MatchState == ShooterBattleMatchState.Running;
            set => MatchState = value ? ShooterBattleMatchState.Running : ShooterBattleMatchState.NotStarted;
        }

        public int CurrentFrame { get; set; }

        public ShooterStartGamePayload StartSpec { get; set; }

        public void Reset(in ShooterStartGamePayload spec)
        {
            _entities.Clear();
            InputBuffer.Clear();
            Events.Clear();
            _nextBulletId = 1;
            CurrentFrame = 0;
            MatchCompletedFrame = 0;
            StartSpec = spec;
            DefeatedEnemies = 0;
            VictoryTargetDefeats = 72;
            MatchState = ShooterBattleMatchState.NotStarted;
        }

        public void SetMatchRunning()
        {
            MatchState = ShooterBattleMatchState.Running;
            MatchCompletedFrame = 0;
        }

        public bool TryCompleteMatch(ShooterBattleMatchState resultState)
        {
            if (MatchState != ShooterBattleMatchState.Running)
            {
                return false;
            }

            if (resultState != ShooterBattleMatchState.Victory && resultState != ShooterBattleMatchState.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(resultState), resultState, "Match result must be Victory or Defeat.");
            }

            MatchState = resultState;
            MatchCompletedFrame = CurrentFrame;
            Events.Add(CreateMatchResultEvent(resultState));
            return true;
        }

        public ShooterMatchResultSnapshot GetMatchResult()
        {
            if (MatchState != ShooterBattleMatchState.Victory && MatchState != ShooterBattleMatchState.Defeat)
            {
                return ShooterMatchResultSnapshot.NotCompleted(CurrentFrame, DefeatedEnemies, VictoryTargetDefeats);
            }

            return new ShooterMatchResultSnapshot(
                MatchState,
                MatchCompletedFrame,
                isFinal: true,
                isVictory: MatchState == ShooterBattleMatchState.Victory,
                DefeatedEnemies,
                VictoryTargetDefeats);
        }

        public int AllocateBulletId()
        {
            return _nextBulletId++;
        }

        public void AdvanceBulletIdPast(int bulletId)
        {
            if (bulletId >= _nextBulletId)
            {
                _nextBulletId = bulletId + 1;
            }
        }
        private ShooterEventSnapshot CreateMatchResultEvent(ShooterBattleMatchState resultState)
        {
            return new ShooterEventSnapshot(
                resultState == ShooterBattleMatchState.Victory ? ShooterEventType.MatchVictory : ShooterEventType.MatchDefeat,
                sourcePlayerId: 0,
                targetPlayerId: 0,
                bulletId: 0,
                x: 0f,
                y: 0f,
                value: resultState == ShooterBattleMatchState.Victory ? DefeatedEnemies : 0);
        }
    }
}
