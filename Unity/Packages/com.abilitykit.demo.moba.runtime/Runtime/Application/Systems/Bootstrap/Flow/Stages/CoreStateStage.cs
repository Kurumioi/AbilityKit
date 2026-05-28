using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Rollback;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// CoreState Stage
    /// 注册核心状态服务和基础服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class CoreStateStage : MobaBootstrapStageBase
    {
        public override string Name => "CoreState";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            builder.AddModule(new DefaultWorldServicesModule());
            AttributeWorldServicesModule.ClearCache();

            // Deterministic + rollbackable RNG (override default world random)
            builder.Register<IWorldRandom>(WorldLifetime.Scoped, _ => new RollbackWorldRandom());
        }
    }
}
