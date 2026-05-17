using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// 弹道信息
    /// </summary>
    public sealed class ProjectileInfo
    {
        public int ProjectileId;
        public int TemplateId;
        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>
    /// Console 弹道显示服务
    /// </summary>
    public sealed class ConsoleProjectileDisplayService
    {
        private readonly Dictionary<int, ProjectileInfo> _projectiles = new();

        public void Spawn(int projectileId, int templateId, float x, float y, float z)
        {
            if (_projectiles.ContainsKey(projectileId))
            {
                _projectiles.Remove(projectileId);
            }

            _projectiles[projectileId] = new ProjectileInfo
            {
                ProjectileId = projectileId,
                TemplateId = templateId,
                X = x,
                Y = y,
                Z = z
            };
        }

        public void Remove(int projectileId) => _projectiles.Remove(projectileId);
        public IReadOnlyDictionary<int, ProjectileInfo> GetAll() => _projectiles;
        public void Clear() => _projectiles.Clear();
    }
}
