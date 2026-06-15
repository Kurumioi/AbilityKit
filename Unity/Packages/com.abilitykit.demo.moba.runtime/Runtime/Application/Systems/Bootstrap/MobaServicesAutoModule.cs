using System;
using System.Reflection;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Logging;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// Aggregates the runtime service registration modules used by the MOBA logic world.
    /// Keep this as the stable entry point for hosts while the internal groups stay replaceable.
    /// </summary>
    public sealed class MobaServicesAutoModule : IWorldModule
    {
        private readonly Assembly _targetAssembly;

        public static readonly string[] TargetNamespacePrefixes = MobaApplicationServicesModule.NamespacePrefixes;

        public MobaServicesAutoModule() : this(null)
        {
        }

        public MobaServicesAutoModule(Assembly targetAssembly)
        {
            _targetAssembly = targetAssembly ?? typeof(MobaServicesAutoModule).Assembly;
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            Log.Info($"[MobaServicesAutoModule] Configure services for assembly: {_targetAssembly.GetName().Name}");

            builder.AddModule(new MobaApplicationServicesModule(_targetAssembly));
            builder.AddModule(new MobaApplicationSystemsServicesModule(_targetAssembly));
            builder.AddModule(new MobaInfrastructureServicesModule(_targetAssembly));

            Log.Info("[MobaServicesAutoModule] Configure done");
        }
    }

    public sealed class MobaApplicationServicesModule : IWorldModule
    {
        private readonly Assembly _targetAssembly;

        public static readonly string[] NamespacePrefixes = new[]
        {
            "AbilityKit.Demo.Moba.Services",
            "AbilityKit.Demo.Moba.Gameplay",
        };

        public MobaApplicationServicesModule(Assembly targetAssembly)
        {
            _targetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));
        }

        public void Configure(WorldContainerBuilder builder)
        {
            MobaServiceModuleUtil.AddAttributeModule(builder, _targetAssembly, NamespacePrefixes);
        }
    }

    public sealed class MobaApplicationSystemsServicesModule : IWorldModule
    {
        private readonly Assembly _targetAssembly;

        public static readonly string[] NamespacePrefixes = new[]
        {
            "AbilityKit.Demo.Moba.Systems",
        };

        public MobaApplicationSystemsServicesModule(Assembly targetAssembly)
        {
            _targetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));
        }

        public void Configure(WorldContainerBuilder builder)
        {
            MobaServiceModuleUtil.AddAttributeModule(builder, _targetAssembly, NamespacePrefixes);
        }
    }

    public sealed class MobaInfrastructureServicesModule : IWorldModule
    {
        private readonly Assembly _targetAssembly;

        public static readonly string[] NamespacePrefixes = new[]
        {
            "AbilityKit.Demo.Moba.Util",
            "AbilityKit.Demo.Moba.Util.Generator",
        };

        public MobaInfrastructureServicesModule(Assembly targetAssembly)
        {
            _targetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));
        }

        public void Configure(WorldContainerBuilder builder)
        {
            MobaServiceModuleUtil.AddAttributeModule(builder, _targetAssembly, NamespacePrefixes);
        }
    }

    internal static class MobaServiceModuleUtil
    {
        public static void AddAttributeModule(WorldContainerBuilder builder, Assembly assembly, string[] namespacePrefixes)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            builder.AddModule(new AttributeWorldServicesModule(
                WorldServiceProfile.All,
                assemblies: new[] { assembly },
                namespacePrefixes: namespacePrefixes));
        }
    }
}
