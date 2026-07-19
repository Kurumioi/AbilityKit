using System;
using System.Collections.Generic;

namespace AbilityKit.Combat.Collision
{
    /// <summary>
    /// 基于网格的空间划分广相算法
    /// </summary>
    public sealed class GridBroadphase : IBroadphase
    {
        private readonly float _cellSize;
        private readonly int _poolSize;
        private readonly Dictionary<int, CellEntry> _colliderToCell;
        private readonly Dictionary<long, List<int>> _cells;
        private readonly List<int> _tmpResults;

        private struct CellEntry
        {
            public int CellX;
            public int CellY;
            public int CellZ;

            public bool IsValid;
        }

        public GridBroadphase(float cellSize = 4f, int poolSize = 1024)
        {
            _cellSize = cellSize;
            _poolSize = poolSize;
            _colliderToCell = new Dictionary<int, CellEntry>(poolSize);
            _cells = new Dictionary<long, List<int>>(poolSize);
            _tmpResults = new List<int>(64);
        }

        public void Clear()
        {
            _colliderToCell.Clear();
            _cells.Clear();
        }

        public void Update(int colliderId, in Core.Mathematics.Aabb worldAabb)
        {
            var center = worldAabb.Center;
            var extents = worldAabb.Extents * 0.5f;

            var minCX = WorldToCell(center.X - extents.X);
            var minCY = WorldToCell(center.Y - extents.Y);
            var minCZ = WorldToCell(center.Z - extents.Z);
            var maxCX = WorldToCell(center.X + extents.X);
            var maxCY = WorldToCell(center.Y + extents.Y);
            var maxCZ = WorldToCell(center.Z + extents.Z);

            bool exists = _colliderToCell.TryGetValue(colliderId, out var oldEntry);

            if (exists && oldEntry.IsValid)
            {
                if (oldEntry.CellX == minCX && oldEntry.CellY == minCY && oldEntry.CellZ == minCZ)
                {
                    return;
                }

                var oldKey = GetCellKey(oldEntry.CellX, oldEntry.CellY, oldEntry.CellZ);
                if (_cells.TryGetValue(oldKey, out var oldList))
                {
                    oldList.Remove(colliderId);
                    if (oldList.Count == 0)
                        _cells.Remove(oldKey);
                }
            }

            for (var cx = minCX; cx <= maxCX; cx++)
            {
                for (var cy = minCY; cy <= maxCY; cy++)
                {
                    for (var cz = minCZ; cz <= maxCZ; cz++)
                    {
                        var key = GetCellKey(cx, cy, cz);
                        if (!_cells.TryGetValue(key, out var list))
                        {
                            list = new List<int>(4);
                            _cells[key] = list;
                        }
                        list.Add(colliderId);
                    }
                }
            }

            _colliderToCell[colliderId] = new CellEntry
            {
                CellX = minCX,
                CellY = minCY,
                CellZ = minCZ,
                IsValid = true
            };
        }

        public void Remove(int colliderId)
        {
            if (!_colliderToCell.TryGetValue(colliderId, out var entry) || !entry.IsValid)
                return;

            var key = GetCellKey(entry.CellX, entry.CellY, entry.CellZ);
            if (_cells.TryGetValue(key, out var list))
            {
                list.Remove(colliderId);
                if (list.Count == 0)
                    _cells.Remove(key);
            }

            _colliderToCell.Remove(colliderId);
        }

        public int Query(in Core.Mathematics.Aabb queryAabb, int[] results, int maxResults)
        {
            if (results == null || maxResults <= 0)
                return 0;

            var center = queryAabb.Center;
            var extents = queryAabb.Extents * 0.5f;

            var minCX = WorldToCell(center.X - extents.X);
            var minCY = WorldToCell(center.Y - extents.Y);
            var minCZ = WorldToCell(center.Z - extents.Z);
            var maxCX = WorldToCell(center.X + extents.X);
            var maxCY = WorldToCell(center.Y + extents.Y);
            var maxCZ = WorldToCell(center.Z + extents.Z);

            _tmpResults.Clear();

            for (var cx = minCX; cx <= maxCX; cx++)
            {
                for (var cy = minCY; cy <= maxCY; cy++)
                {
                    for (var cz = minCZ; cz <= maxCZ; cz++)
                    {
                        var key = GetCellKey(cx, cy, cz);
                        if (_cells.TryGetValue(key, out var list))
                        {
                            for (var i = 0; i < list.Count; i++)
                            {
                                var id = list[i];
                                if (!_tmpResults.Contains(id))
                                    _tmpResults.Add(id);
                            }
                        }
                    }
                }
            }

            var count = 0;
            for (var i = 0; i < _tmpResults.Count && count < maxResults; i++)
            {
                results[count++] = _tmpResults[i];
            }

            return count;
        }

        private int WorldToCell(float worldCoord)
        {
            return (int)System.Math.Floor(worldCoord / _cellSize);
        }

        private static long GetCellKey(int cx, int cy, int cz)
        {
            return ((long)cx << 42) | ((long)cy << 21) | (long)cz;
        }
    }
}
