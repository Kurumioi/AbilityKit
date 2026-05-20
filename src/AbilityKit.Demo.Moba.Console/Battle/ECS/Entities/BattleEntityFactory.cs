using System;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Entities
{
    /// <summary>
    /// 战斗实体工厂
    /// 用于创建战斗相关实体
    /// </summary>
    public sealed class BattleEntityFactory
    {
        private readonly EC.IECWorld _world;
        private readonly BattleEntityLookup _lookup;
        private readonly EC.IEntity _parent;

        public BattleEntityFactory(EC.IECWorld world, BattleEntityLookup lookup, EC.IEntity parent = default)
        {
            _world = world;
            _lookup = lookup;
            _parent = parent;
        }

        public BattleNetId CreateCharacter(int actorId, int entityCode = 0)
        {
            var netId = new BattleNetId(actorId);

            EC.IEntity entity;
            if (_parent.IsValid)
            {
                entity = _world.CreateChild(_parent);
                entity.SetName($"Actor_{netId.Value}");
            }
            else
            {
                entity = _world.Create($"Actor_{netId.Value}");
            }

            entity.WithRef(new Components.BattleNetIdComponent { NetId = netId });
            entity.WithRef(new Components.BattleEntityMetaComponent { Kind = Components.BattleEntityKind.Character, EntityCode = entityCode });
            entity.WithRef(new Components.BattleTransformComponent());
            entity.WithRef(new Components.BattleCharacterComponent());

            _lookup?.Bind(netId, entity.Id);
            return netId;
        }

        public BattleNetId CreateProjectile(int actorId, BattleNetId ownerNetId, int entityCode = 0)
        {
            var netId = new BattleNetId(actorId);

            EC.IEntity entity;
            if (_parent.IsValid)
            {
                entity = _world.CreateChild(_parent);
                entity.SetName($"Projectile_{netId.Value}");
            }
            else
            {
                entity = _world.Create($"Projectile_{netId.Value}");
            }

            entity.WithRef(new Components.BattleNetIdComponent { NetId = netId });
            entity.WithRef(new Components.BattleEntityMetaComponent { Kind = Components.BattleEntityKind.Projectile, EntityCode = entityCode });
            entity.WithRef(new Components.BattleTransformComponent());
            entity.WithRef(new Components.BattleProjectileComponent { OwnerNetId = ownerNetId });

            _lookup?.Bind(netId, entity.Id);
            return netId;
        }
    }
}
