using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Variables.Numeric;

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
            builder.Register<AbilityKit.Demo.Moba.Services.MobaBattleRouteRegistry>(WorldLifetime.Singleton, _ => AbilityKit.Demo.Moba.Services.MobaBattleRouteRegistry.CreateDefault());
            builder.Register<AbilityKit.Demo.Moba.Services.MobaInputCommandContractRegistry>(WorldLifetime.Singleton, _ => AbilityKit.Demo.Moba.Services.MobaInputCommandContractRegistry.CreateDefault());
            builder.TryRegister<MobaGameplayConfigSettings>(WorldLifetime.Scoped, _ => new MobaGameplayConfigSettings());
            builder.Register<AbilityKit.Demo.Moba.Services.MobaTriggerPayloadResolverRegistry>(WorldLifetime.Singleton, _ => new AbilityKit.Demo.Moba.Services.MobaTriggerPayloadResolverRegistry());
            builder.Register<AbilityKit.Demo.Moba.Services.MobaTriggerConditionRegistry>(WorldLifetime.Singleton, _ => new AbilityKit.Demo.Moba.Services.MobaTriggerConditionRegistry());
            builder.Register<IPayloadAccessorRegistry>(WorldLifetime.Singleton, _ =>
            {
                var payloads = new PayloadAccessorRegistry();
                var gameplayAccessor = new GameplayLifecyclePayloadAccessor();
                payloads.RegisterIntAccessor(gameplayAccessor);
                payloads.RegisterDoubleAccessor(gameplayAccessor);

                var battleAccessor = new MobaBattlePayloadAccessor();
                payloads.RegisterIntAccessor<AttackInfo>(battleAccessor);
                payloads.RegisterIntAccessor<DamageResult>(battleAccessor);
                payloads.RegisterDoubleAccessor<DamageResult>(battleAccessor);
                payloads.RegisterIntAccessor<Events.Unit.UnitDieEventPayload>(battleAccessor);
                payloads.RegisterDoubleAccessor<Events.Unit.UnitDieEventPayload>(battleAccessor);

                var skillAccessor = new SkillPipelineContextPayloadAccessor(_);
                payloads.RegisterIntAccessor<SkillPipelineContext>(skillAccessor);
                payloads.RegisterDoubleAccessor<SkillPipelineContext>(skillAccessor);

                var skillObjectAccessor = new SkillPipelineContextObjectPayloadAccessor(skillAccessor);
                payloads.RegisterIntAccessor<object>(skillObjectAccessor);
                payloads.RegisterDoubleAccessor<object>(skillObjectAccessor);
                return payloads;
            });
            builder.Register<INumericVarDomainRegistry>(WorldLifetime.Singleton, _ =>
            {
                var registry = new NumericVarDomainRegistry();
                registry.Register(new MobaGameplayNumericVarDomain());
                return registry;
            });
            builder.Register<AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>>(WorldLifetime.Singleton, r =>
                new AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>(
                    r.Resolve<AbilityKit.Triggering.Eventing.IEventBus>(),
                    r.Resolve<FunctionRegistry>(),
                    r.Resolve<ActionRegistry>(),
                    payloads: r.Resolve<IPayloadAccessorRegistry>(),
                    numericDomains: r.Resolve<INumericVarDomainRegistry>()));

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
