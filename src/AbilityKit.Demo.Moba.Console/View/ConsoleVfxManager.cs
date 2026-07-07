using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console VFX 条目。
    /// </summary>
    public sealed class ConsoleVfxEntry
    {
        public int VfxId { get; set; }
        public int OwnerActorId { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Console VFX 数据库（占位实现）。
    /// </summary>
    public sealed class ConsoleVfxDatabase
    {
        private readonly Dictionary<int, string> _vfxNames = new();

        public ConsoleVfxDatabase()
        {
            _vfxNames[1] = "[HIT_EFFECT]";
            _vfxNames[2] = "[SKILL_EFFECT]";
            _vfxNames[3] = "[DEATH_EFFECT]";
            _vfxNames[4] = "[HEAL_EFFECT]";
            _vfxNames[5] = "[BUFF_EFFECT]";
            _vfxNames[1001] = "[PROJECTILE]";
            _vfxNames[1002] = "[ARROW]";
            _vfxNames[1003] = "[FIREBALL]";
        }

        public string GetVfxName(int vfxId)
        {
            if (_vfxNames.TryGetValue(vfxId, out var name))
            {
                return name;
            }
            return $"[VFX_{vfxId}]";
        }

        public bool HasVfx(int vfxId) => _vfxNames.ContainsKey(vfxId);
    }

    /// <summary>
    /// Console VFX 管理器（占位实现）。
    /// 管理 Console 环境中的 VFX 生命周期。
    /// </summary>
    public sealed class ConsoleVfxManager : IDisposable
    {
        private readonly ConsoleVfxDatabase _database;
        private readonly Dictionary<int, ConsoleVfxEntry> _activeVfx = new();
        private readonly List<int> _toRemove = new();
        private int _nextVfxId = 1;
        private bool _disposed;
        private double _logicTimeSeconds;

        public ConsoleVfxDatabase Database => _database;
        public int ActiveCount => _activeVfx.Count;

        public ConsoleVfxManager(ConsoleVfxDatabase? database = null)
        {
            _database = database ?? new ConsoleVfxDatabase();
        }

        /// <summary>
        /// 更新用于 VFX 生命周期的逻辑时间。
        /// </summary>
        public void SetLogicTime(double seconds)
        {
            _logicTimeSeconds = seconds;
        }

        /// <summary>
        /// 创建 VFX。
        /// </summary>
        public int CreateVfx(int vfxTemplateId, int ownerActorId, float x, float y, float z, float duration = 2f)
        {
            if (_disposed)
            {
                Platform.Log.Error("[VfxManager] Cannot create VFX: disposed");
                return -1;
            }

            var vfxId = _nextVfxId++;
            var vfxName = _database.GetVfxName(vfxTemplateId);

            var entry = new ConsoleVfxEntry
            {
                VfxId = vfxId,
                OwnerActorId = ownerActorId,
                StartTime = (float)_logicTimeSeconds,
                Duration = duration,
                X = x,
                Y = y,
                Z = z,
                IsActive = true
            };

            _activeVfx[vfxId] = entry;
            Platform.Log.View($"[VfxManager] Created {vfxName} (id:{vfxId}) at ({x:F1}, {y:F1}, {z:F1})");

            return vfxId;
        }

        /// <summary>
        /// 同步 VFX 跟随位置。
        /// </summary>
        public void SyncFollow(int vfxId, float x, float y, float z)
        {
            if (_disposed) return;

            if (_activeVfx.TryGetValue(vfxId, out var entry))
            {
                entry.X = x;
                entry.Y = y;
                entry.Z = z;
            }
        }

        /// <summary>
        /// 销毁 VFX。
        /// </summary>
        public void DestroyVfx(int vfxId)
        {
            if (_disposed) return;

            if (_activeVfx.Remove(vfxId))
            {
                Platform.Log.View($"[VfxManager] Destroyed VFX {vfxId}");
            }
        }

        /// <summary>
        /// 更新 VFX（Console 占位实现）。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_disposed) return;

            var currentTime = (float)_logicTimeSeconds;
            _toRemove.Clear();

            foreach (var kvp in _activeVfx)
            {
                var entry = kvp.Value;
                var age = currentTime - entry.StartTime;

                if (age > entry.Duration)
                {
                    entry.IsActive = false;
                    _toRemove.Add(kvp.Key);
                }
            }

            foreach (var vfxId in _toRemove)
            {
                _activeVfx.Remove(vfxId);
                Platform.Log.View($"[VfxManager] VFX {vfxId} expired");
            }
        }

        /// <summary>
        /// 清理所有 VFX。
        /// </summary>
        public void Clear()
        {
            if (_disposed) return;

            var count = _activeVfx.Count;
            _activeVfx.Clear();
            Platform.Log.View($"[VfxManager] Cleared {count} VFX");
        }

        /// <summary>
        /// 获取所有活动 VFX。
        /// </summary>
        public IEnumerable<ConsoleVfxEntry> GetActiveVfx()
        {
            return _activeVfx.Values;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Clear();
            Platform.Log.View("[VfxManager] Disposed");
        }
    }
}
