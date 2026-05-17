using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 实体显示信息
    /// </summary>
    public sealed class ConsoleEntityInfo
    {
        public int ActorId;
        public string Name;
        public string Type;
        public float X;
        public float Y;
        public float Z;
        public float Hp;
        public float MaxHp;

        public bool IsDead => Hp <= 0;
        public float HpPercent => MaxHp > 0 ? Hp / MaxHp : 0f;
    }

    /// <summary>
    /// Console 实体显示服务
    /// </summary>
    public sealed class ConsoleEntityDisplayService
    {
        private readonly Dictionary<int, ConsoleEntityInfo> _entities = new();

        public void Add(int actorId, string name, string type, float hp, float maxHp, float x, float y, float z)
        {
            if (!_entities.TryGetValue(actorId, out var info))
            {
                info = new ConsoleEntityInfo();
                _entities[actorId] = info;
            }

            info.ActorId = actorId;
            info.Name = name;
            info.Type = type;
            info.X = x;
            info.Y = y;
            info.Z = z;
            info.Hp = hp;
            info.MaxHp = maxHp;
        }

        public void UpdatePosition(int actorId, float x, float y, float z)
        {
            if (_entities.TryGetValue(actorId, out var info))
            {
                info.X = x;
                info.Y = y;
                info.Z = z;
            }
        }

        public void UpdateHp(int actorId, float hp, float maxHp)
        {
            if (_entities.TryGetValue(actorId, out var info))
            {
                info.Hp = hp;
                info.MaxHp = maxHp;
            }
        }

        public void Remove(int actorId) => _entities.Remove(actorId);
        public bool TryGet(int actorId, out ConsoleEntityInfo info) => _entities.TryGetValue(actorId, out info);
        public IEnumerable<ConsoleEntityInfo> GetAll() => _entities.Values;
        public int Count => _entities.Count;
        public void Clear() => _entities.Clear();

        public void TickInterpolation(float deltaTime)
        {
        }
    }
}
