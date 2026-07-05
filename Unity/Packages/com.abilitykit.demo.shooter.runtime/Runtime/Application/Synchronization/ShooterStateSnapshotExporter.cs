using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
using Svelto.DataStructures;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterStateSnapshotExporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ISveltoWorldContext _context;
        public ShooterStateSnapshotExporter(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _context = _entities.SveltoContext;
        }

        public ShooterStateSnapshotPayload Export()
        {
            var playerCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Players);
            playerCollection.Deconstruct(out NB<ShooterSveltoPlayerComponent> playerComponents, out _, out var playerCount);
            var players = new ShooterPlayerSnapshot[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                var player = playerComponents[i];
                players[i] = new ShooterPlayerSnapshot(player.PlayerId, player.X, player.Y, player.AimX, player.AimY, player.Hp, player.Score, player.Alive);
            }

            var projectileCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.Projectiles);
            projectileCollection.Deconstruct(out NB<ShooterSveltoProjectileComponent> projectileComponents, out _, out var projectileCount);
            var bullets = new ShooterBulletSnapshot[projectileCount];
            for (int i = 0; i < projectileCount; i++)
            {
                var bullet = projectileComponents[i];
                bullets[i] = new ShooterBulletSnapshot(bullet.BulletId, bullet.OwnerPlayerId, bullet.X, bullet.Y, bullet.VelocityX, bullet.VelocityY, bullet.RemainingFrames);
            }

            var enemyCollection = _context.EntitiesDB.QueryEntities<ShooterSveltoTransformComponent, ShooterSveltoHealthComponent>((ExclusiveGroupStruct)ShooterSveltoGroups.GameplayTargets);
            enemyCollection.Deconstruct(out NB<ShooterSveltoTransformComponent> enemyTransforms, out NB<ShooterSveltoHealthComponent> enemyHealths, out var enemyIds, out var enemyCount);
            var enemies = new ShooterEnemySnapshot[enemyCount];
            for (int i = 0; i < enemyCount; i++)
            {
                var transform = enemyTransforms[i];
                var health = enemyHealths[i];
                enemies[i] = new ShooterEnemySnapshot(
                    checked((int)enemyIds[i]),
                    transform.X,
                    transform.Y,
                    transform.DirectionX,
                    transform.DirectionY,
                    health.Current,
                    health.Max,
                    health.Alive != 0);
            }

            var events = _state.Events.Count == 0
                ? Array.Empty<ShooterEventSnapshot>()
                : _state.Events.ToArray();

            return new ShooterStateSnapshotPayload(
                _state.CurrentFrame,
                players,
                bullets,
                events,
                (int)_state.MatchState,
                _state.TimeLimitFrames,
                _state.RemainingTimeFrames,
                enemies);
        }
    }
}
