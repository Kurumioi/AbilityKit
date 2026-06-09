using System;
using System.Collections.Generic;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterSveltoEcsEntityStore : IShooterEcsEntityStore, IShooterEcsEntityStoreSynchronization
    {
        private readonly Dictionary<int, ShooterEcsPlayerEntity> _players = new Dictionary<int, ShooterEcsPlayerEntity>();
        private readonly List<ShooterEcsProjectileEntity> _projectiles = new List<ShooterEcsProjectileEntity>(32);
        private readonly HashSet<int> _submittedPlayers = new HashSet<int>();
        private readonly HashSet<int> _submittedProjectiles = new HashSet<int>();
        private readonly HashSet<int> _currentProjectiles = new HashSet<int>();
        private readonly List<int> _staleIds = new List<int>(32);
        private readonly ISveltoWorldContext _context;

        public ShooterSveltoEcsEntityStore(ISveltoWorldContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IDictionary<int, ShooterEcsPlayerEntity> Players => _players;

        public IList<ShooterEcsProjectileEntity> Projectiles => _projectiles;

        public void Clear()
        {
            _players.Clear();
            _projectiles.Clear();
            ClearSveltoGroups();
        }

        public void SyncToEcs()
        {
            var removed = RemoveStalePlayers();
            removed |= RemoveStaleProjectiles();
            if (removed)
            {
                _context.SubmitEntities();
            }

            var built = BuildMissingPlayers();
            built |= BuildMissingProjectiles();
            if (built)
            {
                _context.SubmitEntities();
            }

            UpdateSubmittedPlayers();
            UpdateSubmittedProjectiles();
        }

        private void ClearSveltoGroups()
        {
            var hasPlayers = _context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.Players);
            var hasProjectiles = _context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.Projectiles);
            if (!hasPlayers && !hasProjectiles)
            {
                _submittedPlayers.Clear();
                _submittedProjectiles.Clear();
                _currentProjectiles.Clear();
                _staleIds.Clear();
                return;
            }

            if (hasPlayers)
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.Players);
            }

            if (hasProjectiles)
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.Projectiles);
            }

            _context.SubmitEntities();
            _submittedPlayers.Clear();
            _submittedProjectiles.Clear();
            _currentProjectiles.Clear();
            _staleIds.Clear();
        }

        private bool RemoveStalePlayers()
        {
            _staleIds.Clear();
            foreach (var playerId in _submittedPlayers)
            {
                if (!_players.ContainsKey(playerId))
                {
                    _staleIds.Add(playerId);
                }
            }

            var removed = _staleIds.Count > 0;
            for (int i = 0; i < _staleIds.Count; i++)
            {
                var playerId = _staleIds[i];
                _context.EntityFunctions.RemoveEntity<ShooterSveltoPlayerDescriptor>((uint)playerId, ShooterSveltoGroups.Players);
                _submittedPlayers.Remove(playerId);
            }

            return removed;
        }

        private bool RemoveStaleProjectiles()
        {
            _currentProjectiles.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                _currentProjectiles.Add(_projectiles[i].BulletId);
            }

            _staleIds.Clear();
            foreach (var bulletId in _submittedProjectiles)
            {
                if (!_currentProjectiles.Contains(bulletId))
                {
                    _staleIds.Add(bulletId);
                }
            }

            var removed = _staleIds.Count > 0;
            for (int i = 0; i < _staleIds.Count; i++)
            {
                var bulletId = _staleIds[i];
                _context.EntityFunctions.RemoveEntity<ShooterSveltoProjectileDescriptor>((uint)bulletId, ShooterSveltoGroups.Projectiles);
                _submittedProjectiles.Remove(bulletId);
            }

            return removed;
        }

        private bool BuildMissingPlayers()
        {
            var built = false;
            foreach (var kv in _players)
            {
                var player = kv.Value;
                if (_submittedPlayers.Contains(player.PlayerId))
                {
                    continue;
                }

                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoPlayerDescriptor>((uint)player.PlayerId, ShooterSveltoGroups.Players);
                initializer.Init(ToComponent(player));
                _submittedPlayers.Add(player.PlayerId);
                built = true;
            }

            return built;
        }

        private bool BuildMissingProjectiles()
        {
            var built = false;
            for (int i = 0; i < _projectiles.Count; i++)
            {
                var projectile = _projectiles[i];
                if (_submittedProjectiles.Contains(projectile.BulletId))
                {
                    continue;
                }

                var initializer = _context.EntityFactory.BuildEntity<ShooterSveltoProjectileDescriptor>((uint)projectile.BulletId, ShooterSveltoGroups.Projectiles);
                initializer.Init(ToComponent(projectile));
                _submittedProjectiles.Add(projectile.BulletId);
                built = true;
            }

            return built;
        }

        private void UpdateSubmittedPlayers()
        {
            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players, out var mapper))
            {
                return;
            }

            foreach (var kv in _players)
            {
                var player = kv.Value;
                if (!_submittedPlayers.Contains(player.PlayerId))
                {
                    continue;
                }

                mapper.Entity((uint)player.PlayerId) = ToComponent(player);
            }
        }

        private void UpdateSubmittedProjectiles()
        {
            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles, out var mapper))
            {
                return;
            }

            for (int i = 0; i < _projectiles.Count; i++)
            {
                var projectile = _projectiles[i];
                if (!_submittedProjectiles.Contains(projectile.BulletId))
                {
                    continue;
                }

                mapper.Entity((uint)projectile.BulletId) = ToComponent(projectile);
            }
        }

        private static ShooterSveltoPlayerComponent ToComponent(ShooterEcsPlayerEntity player)
        {
            return new ShooterSveltoPlayerComponent
            {
                PlayerId = player.PlayerId,
                X = player.X,
                Y = player.Y,
                AimX = player.AimX,
                AimY = player.AimY,
                Hp = player.Hp,
                Score = player.Score,
                Alive = player.Alive
            };
        }

        private static ShooterSveltoProjectileComponent ToComponent(ShooterEcsProjectileEntity projectile)
        {
            return new ShooterSveltoProjectileComponent
            {
                BulletId = projectile.BulletId,
                OwnerPlayerId = projectile.OwnerPlayerId,
                X = projectile.X,
                Y = projectile.Y,
                VelocityX = projectile.VelocityX,
                VelocityY = projectile.VelocityY,
                RemainingFrames = projectile.RemainingFrames
            };
        }
    }
}
