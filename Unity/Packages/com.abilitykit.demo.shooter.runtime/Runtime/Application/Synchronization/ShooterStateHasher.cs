using System;
using AbilityKit.World.Svelto;
using Svelto.ECS;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterStateHasher
    {
        private readonly ShooterBattleState _state;
        private readonly IShooterEntityManager _entities;
        private readonly ISveltoWorldContext _context;

        public ShooterStateHasher(ShooterBattleState state, IShooterEntityManager entities)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _context = _entities.SveltoContext;
        }

        public uint Compute()
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Hash(hash, _state.CurrentFrame);

                var (players, _, playerCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoPlayerComponent>(ShooterSveltoGroups.Players);
                var playerOrder = CreateIndexOrder(playerCount);
                Array.Sort(playerOrder, (left, right) => players[left].PlayerId.CompareTo(players[right].PlayerId));
                for (int i = 0; i < playerOrder.Length; i++)
                {
                    var player = players[playerOrder[i]];
                    hash = Hash(hash, player.PlayerId);
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.X));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.Y));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.AimX));
                    hash = Hash(hash, ShooterRuntimeSnapshotUtility.Quantize(player.AimY));
                    hash = Hash(hash, player.Hp);
                    hash = Hash(hash, player.Score);
                    hash = Hash(hash, player.Alive ? 1 : 0);
                }

                var (bullets, _, bulletCount) = _context.EntitiesDB.QueryEntities<ShooterSveltoProjectileComponent>(ShooterSveltoGroups.Projectiles);
                var bulletOrder = CreateIndexOrder(bulletCount);
                Array.Sort(bulletOrder, (left, right) => bullets[left].BulletId.CompareTo(bullets[right].BulletId));
                for (int i = 0; i < bulletOrder.Length; i++)
                {
                    var bullet = bullets[bulletOrder[i]];
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

        private static int[] CreateIndexOrder(int count)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++)
            {
                order[i] = i;
            }

            return order;
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
