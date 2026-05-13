using System;
using AbilityKit.World.ECS;
using EC = AbilityKit.World.ECS;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Components;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Entities
{
    /// <summary>
    /// ???? - ????????????
    /// </summary>
    public sealed class BattleEntityFactory
    {
        private readonly EC.IECWorld _world;
        private readonly BattleEntityLookup _lookup;
        private readonly EC.IEntity _parent;

        public BattleEntityFactory(EC.IECWorld world, BattleEntityLookup lookup = null, EC.IEntity parent = default)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _lookup = lookup;
            _parent = parent;
        }

        /// <summary>
        /// ??????
        /// </summary>
        public EC.IEntity CreateCharacter(BattleNetId netId, int entityCode = 0)
        {
            EC.IEntity e;
            if (_parent.IsValid)
            {
                e = _world.CreateChild(_parent);
                e.SetName($"Actor_{netId.Value}");
            }
            else
            {
                e = _world.Create($"Actor_{netId.Value}");
            }

            e.WithRef(new BattleNetIdComponent { NetId = netId });
            e.WithRef(new BattleEntityMetaComponent { Kind = BattleEntityKind.Character, EntityCode = entityCode });
            e.WithRef(new BattleTransformComponent());
            e.WithRef(new BattleCharacterComponent());

            _lookup?.Bind(netId, e);
            return e;
        }

        /// <summary>
        /// ????????
        /// </summary>
        public EC.IEntity CreateProjectile(BattleNetId netId, BattleNetId ownerNetId, int entityCode = 0)
        {
            EC.IEntity e;
            if (_parent.IsValid)
            {
                e = _world.CreateChild(_parent);
                e.SetName($"Projectile_{netId.Value}");
            }
            else
            {
                e = _world.Create($"Projectile_{netId.Value}");
            }

            e.WithRef(new BattleNetIdComponent { NetId = netId });
            e.WithRef(new BattleEntityMetaComponent { Kind = BattleEntityKind.Projectile, EntityCode = entityCode });
            e.WithRef(new BattleTransformComponent());
            e.WithRef(new BattleProjectileComponent { OwnerNetId = ownerNetId });

            _lookup?.Bind(netId, e);
            return e;
        }

        /// <summary>
        /// ?? VFX ??
        /// </summary>
        public EC.IEntity CreateVfx(int vfxId, EC.IEntity parent)
        {
            var e = _world.CreateChild(parent);
            e.SetName($"Vfx_{vfxId}");

            e.WithRef(new BattleEntityMetaComponent { Kind = BattleEntityKind.Vfx, EntityCode = vfxId });
            e.WithRef(new BattleTransformComponent());

            return e;
        }

        /// <summary>
        /// ??????
        /// </summary>
        public EC.IEntity Parent => _parent;

        /// <summary>
        /// ????
        /// </summary>
        public EC.IECWorld World => _world;
    }
}
