using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Continuous;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA-scoped continuous behavior manager for buffs, skill pipelines, movement, and other runtime processes.
    /// </summary>
    [WorldService(typeof(IContinuousManager), WorldLifetime.Scoped)]
    public sealed class MobaContinuousManager : DefaultContinuousManager, IWorldInitializable
    {
        public void OnInit(IWorldResolver services)
        {
            if (services == null) return;

            services.TryResolve(out IGameplayTagService tags);
            var modifierProjectors = CreateDefaultModifierProjectors(services);
            AddLifecycleBinder(new MobaContinuousLifecycleBinder(tags, modifierProjectors));
        }

        private static MobaContinuousModifierProjectorRegistry CreateDefaultModifierProjectors(IWorldResolver services)
        {
            var registry = new MobaContinuousModifierProjectorRegistry();
            registry.Register(new MobaAttributeContinuousModifierProjector());
            registry.OnInit(services);
            return registry;
        }

        /// <summary>
        /// Releases all registered continuous behaviors when the MOBA world scope is disposed.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
