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

        public int TimeLimitFrames { get; private set; }

        public int RemainingTimeFrames => TimeLimitFrames <= 0 ? 0 : Math.Max(0, TimeLimitFrames - CurrentFrame);

        public bool IsTimeLimited => TimeLimitFrames > 0;

        public bool IsTimeExpired => IsTimeLimited && CurrentFrame >= TimeLimitFrames;

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
            TimeLimitFrames = 0;
            MatchState = ShooterBattleMatchState.NotStarted;
        }

        public void SetTimeLimitFrames(int frames)
        {
            TimeLimitFrames = frames < 0 ? 0 : frames;
        }

        public void RestoreSnapshotMetadata(
            ShooterBattleMatchState matchState,
            int matchCompletedFrame,
            int defeatedEnemies,
            int victoryTargetDefeats,
            int timeLimitFrames)
        {
            MatchState = matchState;
            MatchCompletedFrame = matchCompletedFrame < 0 ? 0 : matchCompletedFrame;
            DefeatedEnemies = defeatedEnemies < 0 ? 0 : defeatedEnemies;
            VictoryTargetDefeats = victoryTargetDefeats < 1 ? 1 : victoryTargetDefeats;
            SetTimeLimitFrames(timeLimitFrames);
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

            if (resultState != ShooterBattleMatchState.Victory && resultState != ShooterBattleMatchState.Defeat && resultState != ShooterBattleMatchState.Ended)
            {
                throw new ArgumentOutOfRangeException(nameof(resultState), resultState, "Match result must be Victory, Defeat, or Ended.");
            }

            MatchState = resultState;
            MatchCompletedFrame = CurrentFrame;
            Events.Add(CreateMatchResultEvent(resultState));
            return true;
        }

        public ShooterMatchResultSnapshot GetMatchResult()
        {
            if (MatchState != ShooterBattleMatchState.Victory && MatchState != ShooterBattleMatchState.Defeat && MatchState != ShooterBattleMatchState.Ended)
            {
                return ShooterMatchResultSnapshot.NotCompleted(
                    CurrentFrame,
                    DefeatedEnemies,
                    VictoryTargetDefeats,
                    TimeLimitFrames,
                    RemainingTimeFrames);
            }

            return new ShooterMatchResultSnapshot(
                MatchState,
                MatchCompletedFrame,
                isFinal: true,
                isVictory: MatchState == ShooterBattleMatchState.Victory,
                DefeatedEnemies,
                VictoryTargetDefeats,
                TimeLimitFrames,
                RemainingTimeFrames);
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
                CreateMatchResultEventType(resultState),
                sourcePlayerId: 0,
                targetPlayerId: 0,
                bulletId: 0,
                x: 0f,
                y: 0f,
                value: CreateMatchResultEventValue(resultState));
        }

        private int CreateMatchResultEventValue(ShooterBattleMatchState resultState)
        {
            return resultState switch
            {
                ShooterBattleMatchState.Victory => DefeatedEnemies,
                ShooterBattleMatchState.Defeat => 0,
                ShooterBattleMatchState.Ended => RemainingTimeFrames,
                _ => throw new ArgumentOutOfRangeException(nameof(resultState), resultState, "Unsupported match result state.")
            };
        }

        private static ShooterEventType CreateMatchResultEventType(ShooterBattleMatchState resultState)
        {
            return resultState switch
            {
                ShooterBattleMatchState.Victory => ShooterEventType.MatchVictory,
                ShooterBattleMatchState.Defeat => ShooterEventType.MatchDefeat,
                ShooterBattleMatchState.Ended => ShooterEventType.MatchEnded,
                _ => throw new ArgumentOutOfRangeException(nameof(resultState), resultState, "Unsupported match result state.")
            };
        }
    }
}
