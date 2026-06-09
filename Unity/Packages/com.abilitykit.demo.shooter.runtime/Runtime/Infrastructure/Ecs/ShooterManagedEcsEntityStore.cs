using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterManagedEcsEntityStore : IShooterEcsEntityStore
    {
        private readonly Dictionary<int, ShooterEcsPlayerEntity> _players = new Dictionary<int, ShooterEcsPlayerEntity>();
        private readonly List<ShooterEcsProjectileEntity> _projectiles = new List<ShooterEcsProjectileEntity>(32);

        public IDictionary<int, ShooterEcsPlayerEntity> Players => _players;

        public IList<ShooterEcsProjectileEntity> Projectiles => _projectiles;

        public void Clear()
        {
            _players.Clear();
            _projectiles.Clear();
        }
    }
}
