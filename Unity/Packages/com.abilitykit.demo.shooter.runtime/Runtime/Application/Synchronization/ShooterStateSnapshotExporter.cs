using System;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterStateSnapshotExporter
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;

        public ShooterStateSnapshotExporter(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public ShooterStateSnapshotPayload Export()
        {
            var playerIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
            var players = new ShooterPlayerSnapshot[playerIds.Length];
            for (int i = 0; i < playerIds.Length; i++)
            {
                _entities.TryGetPlayer(playerIds[i], out var player);
                players[i] = new ShooterPlayerSnapshot(player.PlayerId, player.X, player.Y, player.AimX, player.AimY, player.Hp, player.Score, player.Alive);
            }

            var bulletIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
            var bullets = new ShooterBulletSnapshot[bulletIds.Length];
            for (int i = 0; i < bulletIds.Length; i++)
            {
                _entities.TryGetProjectile(bulletIds[i], out var bullet);
                bullets[i] = new ShooterBulletSnapshot(bullet.BulletId, bullet.OwnerPlayerId, bullet.X, bullet.Y, bullet.VelocityX, bullet.VelocityY, bullet.RemainingFrames);
            }

            return new ShooterStateSnapshotPayload(_state.CurrentFrame, players, bullets, _state.Events.ToArray());
        }
    }
}
