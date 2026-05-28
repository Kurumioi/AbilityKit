using System;
using System.Reflection;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// Auto service discovery module for Moba runtime services.
    /// Scans all loaded assemblies for classes marked with [WorldService] attribute
    /// and automatically registers them to the WorldContainerBuilder.
    ///
    /// Usage:
    /// 1. Mark service classes with [WorldService(typeof(TService))]
    /// 2. Add this module to the builder:
    ///    builder.AddModule(new MobaServicesAutoModule());
    ///
    /// Service classes must:
    /// - Implement IService interface
    /// - Have a public constructor (dependencies auto-injected via WorldActivator)
    ///
    /// Example:
    /// [WorldService(typeof(MobaEntityManager))]
    /// public sealed class MobaEntityManager : IService
    /// {
    ///     public MobaEntityManager(IEventBus eventBus) { ... }
    /// }
    /// </summary>
    public sealed class MobaServicesAutoModule : IWorldModule
    {
        private readonly Assembly? _targetAssembly;

        /// <summary>
        /// Namespace prefixes to scan for service classes.
        /// Only classes in these namespaces will be auto-registered.
        /// </summary>
        public static readonly string[] TargetNamespacePrefixes = new[]
        {
            "AbilityKit.Demo.Moba",
            "AbilityKit.Demo.Moba.Services",
            "AbilityKit.Demo.Moba.Services.Search",
            "AbilityKit.Demo.Moba.Services.Snapshot",
            "AbilityKit.Demo.Moba.Services.Area",
            "AbilityKit.Demo.Moba.Services.Actor",
            "AbilityKit.Demo.Moba.Services.Buffs",
            "AbilityKit.Demo.Moba.Services.Combat",
            "AbilityKit.Demo.Moba.Services.ComponentTemplates",
            "AbilityKit.Demo.Moba.Services.Core",
            "AbilityKit.Demo.Moba.Services.Effect",
            "AbilityKit.Demo.Moba.Services.Element",
            "AbilityKit.Demo.Moba.Services.EnterGame",
            "AbilityKit.Demo.Moba.Services.EntityManager",
            "AbilityKit.Demo.Moba.Services.FrameSync",
            "AbilityKit.Demo.Moba.Services.Input",
            "AbilityKit.Demo.Moba.Services.Movement",
            "AbilityKit.Demo.Moba.Services.OngoingEffects",
            "AbilityKit.Demo.Moba.Services.Projectile",
            "AbilityKit.Demo.Moba.Services.Rollback",
            "AbilityKit.Demo.Moba.Services.Skill",
            "AbilityKit.Demo.Moba.Services.Spawn",
            "AbilityKit.Demo.Moba.Services.Summon",
            "AbilityKit.Demo.Moba.Services.Triggering",
            "AbilityKit.Demo.Moba.Services.Templates",
            "AbilityKit.Demo.Moba.Snapshot",
            "AbilityKit.Demo.Moba.Combat",
            "AbilityKit.Demo.Moba.Skill",
            "AbilityKit.Demo.Moba.Buff",
            "AbilityKit.Demo.Moba.Movement",
            "AbilityKit.Demo.Moba.Triggering",
            "AbilityKit.Demo.Moba.Effect",
            "AbilityKit.Demo.Moba.Summon",
            "AbilityKit.Demo.Moba.Projectile",
            "AbilityKit.Demo.Moba.Config",
            "AbilityKit.Demo.Moba.Actor",
            "AbilityKit.Demo.Moba.Core",
            "AbilityKit.Demo.Moba.FrameSync",
            "AbilityKit.Demo.Moba.Rollback",
            "AbilityKit.Demo.Moba.Systems",
            "AbilityKit.Demo.Moba.Trace",
            "AbilityKit.Demo.Moba.Projectile",
            "AbilityKit.Demo.Moba.Util",
            "AbilityKit.Demo.Moba.Util.Generator",
        };

    /// <summary>
    /// Creates a new MobaServicesAutoModule that scans the specified assembly.
    /// This is the preferred constructor for Console/ET environments where
    /// explicit assembly control is needed.
    /// </summary>
        public MobaServicesAutoModule() : this(null)
        {
        }

        /// <summary>
        /// Creates a new MobaServicesAutoModule that scans the specified assembly.
        /// </summary>
        /// <param name="targetAssembly">The assembly to scan. If null, uses the declaring assembly.</param>
        public MobaServicesAutoModule(Assembly? targetAssembly)
        {
            _targetAssembly = targetAssembly ?? typeof(MobaServicesAutoModule).Assembly;
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            Log.Info($"[MobaServicesAutoModule] Configure called - scanning assembly: {_targetAssembly?.GetName().Name}");

            // Use explicit assembly instead of scanning all loaded assemblies
            var assemblies = _targetAssembly != null ? new[] { _targetAssembly } : null;

            builder.AddModule(new AttributeWorldServicesModule(
                WorldServiceProfile.All,
                assemblies: assemblies,
                namespacePrefixes: TargetNamespacePrefixes
            ));

            Log.Info($"[MobaServicesAutoModule] Configure done - scanning {TargetNamespacePrefixes.Length} namespace prefixes");
        }
    }
}
