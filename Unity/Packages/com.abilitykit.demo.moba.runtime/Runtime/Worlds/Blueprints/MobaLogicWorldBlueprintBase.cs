using System;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Demo.Moba.Systems;

namespace AbilityKit.Demo.Moba.Worlds.Blueprints
{
    public abstract class MobaLogicWorldBlueprintBase : IWorldBlueprint
    {
        public abstract string WorldType { get; }

        public void Configure(WorldCreateOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.ServiceBuilder ??= WorldServiceContainerFactory.CreateDefaultOnly();
            options.SetEntitasContextsFactory(new MobaEntitasContextsFactory());
            EnsureModule(options, () => new MobaWorldBootstrapModule());
        }

        protected virtual void EnsureModule<TModule>(WorldCreateOptions options, Func<TModule> factory) where TModule : class, IWorldModule
        {
            for (int i = 0; i < options.Modules.Count; i++)
            {
                if (options.Modules[i] != null && options.Modules[i].GetType() == typeof(TModule))
                {
                    return;
                }
            }

            options.Modules.Add(factory());
        }
    }
}
