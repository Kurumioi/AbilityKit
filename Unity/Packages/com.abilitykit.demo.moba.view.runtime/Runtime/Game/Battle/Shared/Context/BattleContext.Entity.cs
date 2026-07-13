using System.Collections.Generic;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Vfx;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleContext
    {
        public WorldId RuntimeWorldId;
        public bool HasRuntimeWorldId;
        public EC.IEntity EntityNode;
        public EC.IECWorld EntityWorld;
        public BattleEntityLookup EntityLookup;
        public BattleEntityFactory EntityFactory;
        public IBattleEntityQuery EntityQuery;
        public BattleVfxManager ViewVfxManager;
        public EC.IEntity ViewVfxNode;
        public List<EC.IEntityId> DirtyEntities;
    }
}
