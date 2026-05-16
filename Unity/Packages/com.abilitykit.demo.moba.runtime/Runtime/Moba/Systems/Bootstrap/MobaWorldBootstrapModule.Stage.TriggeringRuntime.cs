using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        private static void RegisterTriggeringRuntime(WorldContainerBuilder builder)
        {
            builder.TryRegister<AbilityKit.Triggering.Eventing.IEventBus>(WorldLifetime.Scoped, _ => new AbilityKit.Triggering.Eventing.EventBus());

            builder.TryRegister<FunctionRegistry>(WorldLifetime.Singleton, _ => new FunctionRegistry());
            builder.TryRegister<ActionRegistry>(WorldLifetime.Singleton, _ => new ActionRegistry());
            builder.TryRegister<AbilityKit.Triggering.Runtime.ITriggerContextSource<IWorldResolver>>(WorldLifetime.Scoped, r => new WorldResolverContextSource(r));
            builder.TryRegister<AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>>(WorldLifetime.Scoped, r =>
            {
                var planBus = r.Resolve<AbilityKit.Triggering.Eventing.IEventBus>();
                var funcs = r.Resolve<FunctionRegistry>();
                var acts = r.Resolve<ActionRegistry>();
                var ctxSource = r.Resolve<AbilityKit.Triggering.Runtime.ITriggerContextSource<IWorldResolver>>();
                return new AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>(planBus, funcs, acts, contextSource: ctxSource);
            });

            builder.RegisterService<MobaTriggerPlanSubscriptionService, MobaTriggerPlanSubscriptionService>();
        }
    }
}
