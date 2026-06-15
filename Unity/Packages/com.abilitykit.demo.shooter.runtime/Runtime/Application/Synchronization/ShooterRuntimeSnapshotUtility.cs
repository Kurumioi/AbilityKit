using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public static class ShooterRuntimeSnapshotUtility
    {
        public static int[] CopyAndSort(IReadOnlyCollection<int> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var sorted = new int[ids.Count];
            var index = 0;
            foreach (var id in ids)
            {
                sorted[index++] = id;
            }

            Array.Sort(sorted);
            return sorted;
        }

        public static int Quantize(float value)
        {
            return (int)Math.Round(value * 10000f);
        }
    }
}
