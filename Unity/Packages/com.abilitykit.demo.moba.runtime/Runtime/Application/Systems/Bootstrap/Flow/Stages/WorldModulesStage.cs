using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Triggering.Registry;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// Registers MOBA world modules and triggering runtime services.
    /// </summary>
    [MobaBootstrapStage]
    public sealed class WorldModulesStage : MobaBootstrapStageBase
    {
        public override string Name => "WorldModules";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            builder.TryRegisterType<AbilityKit.Triggering.Eventing.IEventBus, AbilityKit.Triggering.Eventing.EventBus>(WorldLifetime.Singleton);
            builder.Register<FunctionRegistry>(WorldLifetime.Singleton, _ => new FunctionRegistry());
            builder.Register<ActionRegistry>(WorldLifetime.Singleton, _ => new ActionRegistry());
            builder.Register<AbilityKit.Demo.Moba.Services.SkillConditionRegistry>(WorldLifetime.Singleton, _ => new AbilityKit.Demo.Moba.Services.SkillConditionRegistry());
            builder.Register<AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>>(WorldLifetime.Singleton, r =>
                new AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>(
                    r.Resolve<AbilityKit.Triggering.Eventing.IEventBus>(),
                    r.Resolve<FunctionRegistry>(),
                    r.Resolve<ActionRegistry>()));

            builder.AddModule(new EntitasEcsWorldModule());
            builder.AddModule(new ProjectileWorldModule());
            builder.AddModule(new MobaServicesAutoModule());
        }

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
        }
    }
}
