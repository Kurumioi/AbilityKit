using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;
using ShooterBulletState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsProjectileEntity;
using ShooterPlayerState = AbilityKit.Demo.Shooter.Runtime.ShooterEcsPlayerEntity;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterBattleState
    {
        private readonly IShooterEcsEntityStore _entityStore;
        private int _nextBulletId = 1;

        public ShooterBattleState(IShooterEcsEntityStore entityStore)
        {
            _entityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
        }

        public IDictionary<int, ShooterPlayerState> Players => _entityStore.Players;

        public IList<ShooterBulletState> Bullets => _entityStore.Projectiles;

        public Dictionary<int, ShooterPlayerCommand> LatestCommands { get; } = new Dictionary<int, ShooterPlayerCommand>();

        public List<ShooterEventSnapshot> Events { get; } = new List<ShooterEventSnapshot>(16);

        public bool IsStarted { get; set; }

        public int CurrentFrame { get; set; }

        public ShooterStartGamePayload StartSpec { get; set; }

        public void Reset(in ShooterStartGamePayload spec)
        {
            _entityStore.Clear();
            LatestCommands.Clear();
            Events.Clear();
            _nextBulletId = 1;
            CurrentFrame = 0;
            StartSpec = spec;
            IsStarted = false;
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
    }
}
