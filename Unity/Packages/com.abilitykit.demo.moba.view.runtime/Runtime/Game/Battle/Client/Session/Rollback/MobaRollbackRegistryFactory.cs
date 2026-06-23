using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Rollback;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Game.Battle
{
    public sealed class MobaRollbackRegistryFactory : IBattleRollbackRegistryFactory
    {
        public RollbackRegistry Create(IWorld world)
        {
            var reg = new RollbackRegistry();
            if (world?.Services == null) return reg;

            if (world.Services.TryResolve<MobaActorRegistry>(out var actorReg) && actorReg != null)
            {
                reg.Register(new MobaActorTransformRollbackProvider(actorReg));
            }

            if (world.Services.TryResolve<RollbackWorldRandom>(out var rng) && rng != null)
            {
                reg.Register(rng);
            }

            return reg;
        }
    }
}
