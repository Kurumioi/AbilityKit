#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.World.Svelto;

namespace AbilityKit.Demo.Shooter.Runtime
{
    [WorldService(typeof(ShooterEntityManager), WorldLifetime.Singleton)]
    [WorldService(typeof(IShooterEntityManager), WorldLifetime.Singleton)]
    public sealed class ShooterEntityManager : IShooterEntityManager
    {
        private readonly ISveltoWorldContext _context;
        private readonly ShooterEntityLimitOptions _limits;
        private readonly HashSet<int> _playerIds = new HashSet<int>();
        private readonly HashSet<int> _projectileIds = new HashSet<int>();
        private int _structuralChangeDepth;
        private bool _hasPendingStructuralChanges;

        public ShooterEntityManager(ISveltoWorldContext context)
            : this(context, ShooterEntityLimitOptions.Default)
        {
        }

        public ShooterEntityManager(ISveltoWorldContext context, ShooterEntityLimitOptions limits)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _limits = limits;
        }

        public ISveltoWorldContext SveltoContext => _context;

        public int MaxEntityCount => _limits.MaxEntityCount;

        public int PlayerCount => _playerIds.Count;

        public int ProjectileCount => _projectileIds.Count;

        public IReadOnlyCollection<int> PlayerIds => _playerIds;

        public IReadOnlyCollection<int> ProjectileIds => _projectileIds;

        public void Clear()
        {
            var removed = false;
            if (_context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.Players))
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.Players);
                removed = true;
            }

            if (_context.EntitiesDB.ExistsAndIsNotEmpty(ShooterSveltoGroups.Projectiles))
            {
                _context.EntityFunctions.RemoveEntitiesFromGroup(ShooterSveltoGroups.Projectiles);
                removed = true;
            }

            _playerIds.Clear();
            _projectileIds.Clear();

            if (removed)
            {
                SubmitStructuralChanges();
            }
        }

        public void BeginStructuralChanges()
        {
            _structuralChangeDepth++;
        }

        public void EndStructuralChanges()
        {
            if (_structuralChangeDepth <= 0)
            {
                _structuralChangeDepth = 0;
                SubmitStructuralChanges();
                return;
            }

            _structuralChangeDepth--;
            if (_structuralChangeDepth == 0 && _hasPendingStructuralChanges)
            {
                SubmitStructuralChanges();
            }
        }

        public void SubmitStructuralChanges()
        {
            if (_structuralChangeDepth > 0)
            {
                _hasPendingStructuralChanges = true;
                return;
            }

            _hasPendingStructuralChanges = false;
            _context.SubmitEntities();
        }

        public bool HasPlayer(int playerId)
        {
            return _playerIds.Contains(playerId);
        }

        public bool TryGetPlayer(int playerId, out ShooterSveltoPlayerComponent player)
        {
            player = default;
            if (!_playerIds.Contains(playerId))
            {
                return false;
            }

            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players, out var mapper))
            {
                return false;
            }

            player = mapper.Entity((uint)playerId);
            return true;
        }

        public void AddPlayer(in ShooterSveltoPlayerComponent player)
        {
            if (player.PlayerId <= 0)
            {
                return;
            }

            if (_playerIds.Contains(player.PlayerId))
            {
                SetPlayer(in player);
                return;
            }

            if (IsEntityBudgetFull())
            {
                return;
            }

            ShooterSveltoEntityLayout.BuildPlayer(_context, in player);
            _playerIds.Add(player.PlayerId);
            SubmitStructuralChanges();
        }

        public void SetPlayer(in ShooterSveltoPlayerComponent player)
        {
            if (player.PlayerId <= 0)
            {
                return;
            }

            if (!_playerIds.Contains(player.PlayerId))
            {
                AddPlayer(in player);
                return;
            }

            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players, out var mapper))
            {
                return;
            }

            mapper.Entity((uint)player.PlayerId) = player;
        }

        public void RemovePlayer(int playerId)
        {
            if (!_playerIds.Remove(playerId))
            {
                return;
            }

            _context.EntityFunctions.RemoveEntity<ShooterSveltoPlayerDescriptor>((uint)playerId, ShooterSveltoGroups.Players);
            SubmitStructuralChanges();
        }

        public bool HasProjectile(int bulletId)
        {
            return _projectileIds.Contains(bulletId);
        }

        public bool TryGetProjectile(int bulletId, out ShooterSveltoProjectileComponent projectile)
        {
            projectile = default;
            if (!_projectileIds.Contains(bulletId))
            {
                return false;
            }

            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles, out var mapper))
            {
                return false;
            }

            projectile = mapper.Entity((uint)bulletId);
            return true;
        }

        public void AddProjectile(in ShooterSveltoProjectileComponent projectile)
        {
            if (projectile.BulletId <= 0)
            {
                return;
            }

            if (_projectileIds.Contains(projectile.BulletId))
            {
                SetProjectile(in projectile);
                return;
            }

            if (IsEntityBudgetFull())
            {
                return;
            }

            ShooterSveltoEntityLayout.BuildProjectile(_context, in projectile);
            _projectileIds.Add(projectile.BulletId);
            SubmitStructuralChanges();
        }

        public void SetProjectile(in ShooterSveltoProjectileComponent projectile)
        {
            if (projectile.BulletId <= 0)
            {
                return;
            }

            if (!_projectileIds.Contains(projectile.BulletId))
            {
                AddProjectile(in projectile);
                return;
            }

            if (!_context.EntitiesDB.TryQueryMappedEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles, out var mapper))
            {
                return;
            }

            mapper.Entity((uint)projectile.BulletId) = projectile;
        }

        public void RemoveProjectile(int bulletId)
        {
            if (!_projectileIds.Remove(bulletId))
            {
                return;
            }

            _context.EntityFunctions.RemoveEntity<ShooterSveltoProjectileDescriptor>((uint)bulletId, ShooterSveltoGroups.Projectiles);
            SubmitStructuralChanges();
        }

        private bool IsEntityBudgetFull()
        {
            return PlayerCount + ProjectileCount >= MaxEntityCount;
        }
    }
}
