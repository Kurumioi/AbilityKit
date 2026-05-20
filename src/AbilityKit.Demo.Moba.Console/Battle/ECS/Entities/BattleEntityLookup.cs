using System;
using System.Collections.Generic;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Entities
{
    /// <summary>
    /// 战斗实体查找器
    /// 根据 NetId 查找实体
    /// </summary>
    public sealed class BattleEntityLookup
    {
        private readonly Dictionary<int, EC.IEntityId> _netIdToEntityId = new();

        public int Count => _netIdToEntityId.Count;

        public void Bind(BattleNetId netId, EC.IEntityId entityId)
        {
            _netIdToEntityId[netId.Value] = entityId;
        }

        public bool TryResolve(EC.IECWorld world, BattleNetId netId, out EC.IEntity entity)
        {
            entity = default;
            if (world == null) return false;
            if (!_netIdToEntityId.TryGetValue(netId.Value, out var id)) return false;
            if (!world.IsAlive(id)) return false;
            entity = world.Wrap(id);
            return true;
        }

        public bool Unbind(BattleNetId netId)
        {
            return _netIdToEntityId.Remove(netId.Value);
        }

        public bool UnbindByEntityId(EC.IEntityId id)
        {
            foreach (var kv in _netIdToEntityId)
            {
                if (kv.Value.Equals(id))
                {
                    return _netIdToEntityId.Remove(kv.Key);
                }
            }
            return false;
        }

        public void Clear()
        {
            _netIdToEntityId.Clear();
        }
    }
}
