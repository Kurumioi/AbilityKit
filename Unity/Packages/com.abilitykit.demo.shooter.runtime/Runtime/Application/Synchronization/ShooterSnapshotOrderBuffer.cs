using System;
using Svelto.DataStructures;
using Svelto.ECS.Internal;

namespace AbilityKit.Demo.Shooter.Runtime
{
    internal sealed class ShooterSnapshotOrderBuffer
    {
        private int[] _order = Array.Empty<int>();

        public int[] CreateIndexOrder(int count)
        {
            EnsureCapacity(count);
            for (var i = 0; i < count; i++)
            {
                _order[i] = i;
            }

            return _order;
        }

        public int[] CreateSortedPlayerOrder(NB<ShooterSveltoPlayerComponent> players, int count)
        {
            var order = CreateIndexOrder(count);
            SortPlayers(order, players, count);
            return order;
        }

        public int[] CreateSortedProjectileOrder(NB<ShooterSveltoProjectileComponent> bullets, int count)
        {
            var order = CreateIndexOrder(count);
            SortProjectiles(order, bullets, count);
            return order;
        }

        public int[] CreateSortedEnemyOrder(NativeEntityIDs ids, int count)
        {
            var order = CreateIndexOrder(count);
            SortEnemies(order, ids, count);
            return order;
        }

        private void EnsureCapacity(int count)
        {
            if (_order.Length >= count)
            {
                return;
            }

            var newCapacity = _order.Length == 0 ? 16 : _order.Length;
            while (newCapacity < count)
            {
                newCapacity = checked(newCapacity * 2);
            }

            _order = new int[newCapacity];
        }

        private static void SortPlayers(int[] order, NB<ShooterSveltoPlayerComponent> players, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var item = order[i];
                var itemKey = players[item].PlayerId;
                var j = i - 1;
                while (j >= 0 && players[order[j]].PlayerId > itemKey)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = item;
            }
        }

        private static void SortProjectiles(int[] order, NB<ShooterSveltoProjectileComponent> bullets, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var item = order[i];
                var itemKey = bullets[item].BulletId;
                var j = i - 1;
                while (j >= 0 && bullets[order[j]].BulletId > itemKey)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = item;
            }
        }

        private static void SortEnemies(int[] order, NativeEntityIDs ids, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var item = order[i];
                var itemKey = ids[item];
                var j = i - 1;
                while (j >= 0 && ids[order[j]] > itemKey)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = item;
            }
        }
    }
}
