using System.Collections.Generic;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Ability.World.Abstractions;
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
        public List<EC.IEntityId> DirtyEntities;
    }
}
