using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// AOE 区域视图信息
    /// </summary>
    public sealed class AreaViewInfo
    {
        public int AreaId;
        public int TemplateId;
        public float CenterX;
        public float CenterZ;
        public float Radius;
    }

    /// <summary>
    /// Console 区域视图系统
    /// </summary>
    public sealed class ConsoleAreaViewSystem
    {
        private readonly Dictionary<int, AreaViewInfo> _areas = new();

        public void Spawn(int areaId, int templateId, float centerX, float centerZ, float radius)
        {
            if (_areas.ContainsKey(areaId))
            {
                _areas.Remove(areaId);
            }

            _areas[areaId] = new AreaViewInfo
            {
                AreaId = areaId,
                TemplateId = templateId,
                CenterX = centerX,
                CenterZ = centerZ,
                Radius = radius
            };
        }

        public void Remove(int areaId) => _areas.Remove(areaId);
        public IReadOnlyDictionary<int, AreaViewInfo> GetAll() => _areas;
        public void Clear() => _areas.Clear();
    }
}
