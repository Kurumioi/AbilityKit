using System;
using AbilityKit.Protocol.Shooter;
using AbilityKit.World.Svelto;
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
            var (playerComponents, _, playerCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
            var playerOrder = CreateIndexOrder(playerCount);
            Array.Sort(playerOrder, (left, right) => playerComponents[left].PlayerId.CompareTo(playerComponents[right].PlayerId));
            var players = new ShooterPlayerSnapshot[playerCount];
            for (int i = 0; i < playerOrder.Length; i++)
            {
                var player = playerComponents[playerOrder[i]];
                players[i] = new ShooterPlayerSnapshot(player.PlayerId, player.X, player.Y, player.AimX, player.AimY, player.Hp, player.Score, player.Alive);
            }

            var (projectileComponents, _, projectileCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles);
            var projectileOrder = CreateIndexOrder(projectileCount);
            Array.Sort(projectileOrder, (left, right) => projectileComponents[left].BulletId.CompareTo(projectileComponents[right].BulletId));
            var bullets = new ShooterBulletSnapshot[projectileCount];
            for (int i = 0; i < projectileOrder.Length; i++)
            {
                var bullet = projectileComponents[projectileOrder[i]];
                bullets[i] = new ShooterBulletSnapshot(bullet.BulletId, bullet.OwnerPlayerId, bullet.X, bullet.Y, bullet.VelocityX, bullet.VelocityY, bullet.RemainingFrames);
            }

            return new ShooterStateSnapshotPayload(_state.CurrentFrame, players, bullets, _state.Events.ToArray());
        }
        private static int[] CreateIndexOrder(int count)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++)
            {
                order[i] = i;
            }

            return order;
        }
    }
}
