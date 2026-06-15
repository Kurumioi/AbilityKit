using System;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterStateHasher
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;

        public ShooterStateHasher(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public uint Compute()
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Hash(hash, _state.CurrentFrame);

                var playerIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.PlayerIds);
                for (int i = 0; i < playerIds.Length; i++)
                {
                    _entities.TryGetPlayer(playerIds[i], out var player);
                    hash = Hash(hash, player.PlayerId);
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.X));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.Y));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.AimX));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.AimY));
                    hash = Hash(hash, player.Hp);
                    hash = Hash(hash, player.Score);
                    hash = Hash(hash, player.Alive ? 1 : 0);
                }

                var bulletIds = ShooterRuntimeSnapshotUtility.CopyAndSort(_entities.ProjectileIds);
                for (int i = 0; i < bulletIds.Length; i++)
                {
                    _entities.TryGetProjectile(bulletIds[i], out var bullet);
                    hash = Hash(hash, bullet.BulletId);
                    hash = Hash(hash, bullet.OwnerPlayerId);
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(bullet.X));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(bullet.Y));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(bullet.VelocityX));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(bullet.VelocityY));
                    hash = Hash(hash, bullet.RemainingFrames);
                }

                return hash;
            }
        }

        private static uint Hash(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 16777619u;
                return hash;
            }
        }
    }
}
