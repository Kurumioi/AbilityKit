using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 视图对象类型
    /// </summary>
    public enum ConsoleViewObjectType
    {
        Character,
        Projectile,
        AreaEffect,
        Vfx,
        FloatingText
    }

    /// <summary>
    /// Console 视图对象条目
    /// </summary>
    public sealed class ConsoleViewObject
    {
        public int Id { get; set; }
        public ConsoleViewObjectType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public int TeamId { get; set; }
        public bool IsActive { get; set; } = true;
        public float CreatedTime { get; set; }
    }

    /// <summary>
    /// Console 视图工厂 (占位实现)
    /// 对标 Unity BattleViewFactory
    /// 负责创建和管理 Console 视图对象
    /// </summary>
    public sealed class ConsoleViewFactory : IDisposable
    {
        private readonly Dictionary<int, ConsoleViewObject> _characters = new();
        private readonly Dictionary<int, ConsoleViewObject> _projectiles = new();
        private readonly Dictionary<int, ConsoleViewObject> _areas = new();
        private readonly Dictionary<int, ConsoleViewObject> _vfx = new();
        private int _nextCharacterId = 1;
        private int _nextProjectileId = 1;
        private int _nextAreaId = 1;
        private int _nextVfxId = 1;
        private bool _disposed;
        private double _logicTimeSeconds;

        /// <summary>
        /// Update logic time for view object lifecycle
        /// </summary>
        public void SetLogicTime(double seconds)
        {
            _logicTimeSeconds = seconds;
        }

        /// <summary>
        /// 创建角色视图
        /// </summary>
        public ConsoleViewObject CreateCharacter(string name, int teamId, float x, float y, float z)
        {
            if (_disposed)
            {
                Platform.Log.Error("[ViewFactory] Cannot create character: disposed");
                return null;
            }

            var id = _nextCharacterId++;
            var obj = new ConsoleViewObject
            {
                Id = id,
                Type = ConsoleViewObjectType.Character,
                Name = name,
                TeamId = teamId,
                X = x,
                Y = y,
                Z = z,
                Rotation = 0f,
                Scale = 1f,
                IsActive = true,
                CreatedTime = (float)_logicTimeSeconds
            };

            _characters[id] = obj;
            Platform.Log.View($"[ViewFactory] Created Character #{id} ({name}) at ({x:F1}, {y:F1}, {z:F1})");

            return obj;
        }

        /// <summary>
        /// 创建投射物视图
        /// </summary>
        public ConsoleViewObject CreateProjectile(int templateId, float x, float y, float z)
        {
            if (_disposed)
            {
                Platform.Log.Error("[ViewFactory] Cannot create projectile: disposed");
                return null;
            }

            var id = _nextProjectileId++;
            var obj = new ConsoleViewObject
            {
                Id = id,
                Type = ConsoleViewObjectType.Projectile,
                Name = $"Projectile_{templateId}",
                X = x,
                Y = y,
                Z = z,
                Rotation = 0f,
                Scale = 1f,
                IsActive = true,
                CreatedTime = (float)_logicTimeSeconds
            };

            _projectiles[id] = obj;
            Platform.Log.View($"[ViewFactory] Created Projectile #{id} at ({x:F1}, {y:F1}, {z:F1})");

            return obj;
        }

        /// <summary>
        /// 创建区域视图
        /// </summary>
        public ConsoleViewObject CreateArea(int templateId, float x, float z, float radius)
        {
            if (_disposed)
            {
                Platform.Log.Error("[ViewFactory] Cannot create area: disposed");
                return null;
            }

            var id = _nextAreaId++;
            var obj = new ConsoleViewObject
            {
                Id = id,
                Type = ConsoleViewObjectType.AreaEffect,
                Name = $"Area_{templateId}",
                X = x,
                Z = z,
                Scale = radius,
                Rotation = 0f,
                IsActive = true,
                CreatedTime = (float)_logicTimeSeconds
            };

            _areas[id] = obj;
            Platform.Log.View($"[ViewFactory] Created Area #{id} at ({x:F1}, {z:F1}) radius {radius:F1}");

            return obj;
        }

        /// <summary>
        /// 创建 VFX 视图
        /// </summary>
        public ConsoleViewObject CreateVfx(int templateId, float x, float y, float z)
        {
            if (_disposed)
            {
                Platform.Log.Error("[ViewFactory] Cannot create VFX: disposed");
                return null;
            }

            var id = _nextVfxId++;
            var obj = new ConsoleViewObject
            {
                Id = id,
                Type = ConsoleViewObjectType.Vfx,
                Name = $"Vfx_{templateId}",
                X = x,
                Y = y,
                Z = z,
                Rotation = 0f,
                Scale = 1f,
                IsActive = true,
                CreatedTime = (float)_logicTimeSeconds
            };

            _vfx[id] = obj;
            Platform.Log.View($"[ViewFactory] Created VFX #{id} at ({x:F1}, {y:F1}, {z:F1})");

            return obj;
        }

        /// <summary>
        /// 更新角色位置
        /// </summary>
        public bool UpdateCharacterPosition(int id, float x, float y, float z, float rotation)
        {
            if (_characters.TryGetValue(id, out var obj))
            {
                obj.X = x;
                obj.Y = y;
                obj.Z = z;
                obj.Rotation = rotation;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新投射物位置
        /// </summary>
        public bool UpdateProjectilePosition(int id, float x, float y, float z)
        {
            if (_projectiles.TryGetValue(id, out var obj))
            {
                obj.X = x;
                obj.Y = y;
                obj.Z = z;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 销毁角色
        /// </summary>
        public bool DestroyCharacter(int id)
        {
            if (_characters.Remove(id))
            {
                Platform.Log.View($"[ViewFactory] Destroyed Character #{id}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 销毁投射物
        /// </summary>
        public bool DestroyProjectile(int id)
        {
            if (_projectiles.Remove(id))
            {
                Platform.Log.View($"[ViewFactory] Destroyed Projectile #{id}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 销毁区域
        /// </summary>
        public bool DestroyArea(int id)
        {
            if (_areas.Remove(id))
            {
                Platform.Log.View($"[ViewFactory] Destroyed Area #{id}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 销毁 VFX
        /// </summary>
        public bool DestroyVfx(int id)
        {
            if (_vfx.Remove(id))
            {
                Platform.Log.View($"[ViewFactory] Destroyed VFX #{id}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取角色
        /// </summary>
        public bool TryGetCharacter(int id, out ConsoleViewObject obj)
            => _characters.TryGetValue(id, out obj);

        /// <summary>
        /// 获取投射物
        /// </summary>
        public bool TryGetProjectile(int id, out ConsoleViewObject obj)
            => _projectiles.TryGetValue(id, out obj);

        /// <summary>
        /// 获取区域
        /// </summary>
        public bool TryGetArea(int id, out ConsoleViewObject obj)
            => _areas.TryGetValue(id, out obj);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public IEnumerable<ConsoleViewObject> GetAllCharacters() => _characters.Values;

        /// <summary>
        /// 获取所有投射物
        /// </summary>
        public IEnumerable<ConsoleViewObject> GetAllProjectiles() => _projectiles.Values;

        /// <summary>
        /// 获取所有区域
        /// </summary>
        public IEnumerable<ConsoleViewObject> GetAllAreas() => _areas.Values;

        /// <summary>
        /// 获取所有 VFX
        /// </summary>
        public IEnumerable<ConsoleViewObject> GetAllVfx() => _vfx.Values;

        /// <summary>
        /// 角色数量
        /// </summary>
        public int CharacterCount => _characters.Count;

        /// <summary>
        /// 投射物数量
        /// </summary>
        public int ProjectileCount => _projectiles.Count;

        /// <summary>
        /// 区域数量
        /// </summary>
        public int AreaCount => _areas.Count;

        /// <summary>
        /// VFX 数量
        /// </summary>
        public int VfxCount => _vfx.Count;

        /// <summary>
        /// 清除所有视图对象
        /// </summary>
        public void Clear()
        {
            var charCount = _characters.Count;
            var projCount = _projectiles.Count;
            var areaCount = _areas.Count;
            var vfxCount = _vfx.Count;

            _characters.Clear();
            _projectiles.Clear();
            _areas.Clear();
            _vfx.Clear();

            Platform.Log.View($"[ViewFactory] Cleared: {charCount} chars, {projCount} projs, {areaCount} areas, {vfxCount} vfx");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Clear();
            Platform.Log.View("[ViewFactory] Disposed");
        }
    }
}
