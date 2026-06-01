using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using Xunit;

namespace AbilityKit.World.DI.Tests;

public sealed class WorldDeinitializableTests
{
    [Fact]
    public void ScopeDispose_CallsDeinitBeforeDispose_InReverseCreationOrder()
    {
        var events = new List<string>();
        var builder = new WorldContainerBuilder();
        builder.RegisterInstance(events);
        builder.RegisterType<ScopedDependency, ScopedDependency>(WorldLifetime.Scoped);
        builder.RegisterType<ScopedTarget, ScopedTarget>(WorldLifetime.Scoped);

        using var container = builder.Build();
        var scope = container.CreateScope();

        scope.Resolve<ScopedDependency>();
        scope.Resolve<ScopedTarget>();

        scope.Dispose();

        Assert.Equal(
            new[]
            {
                "target:deinit:resolver=True",
                "target:dispose",
                "dependency:deinit",
                "dependency:dispose"
            },
            events);
    }

    [Fact]
    public void ContainerDispose_CallsSingletonDeinitBeforeDispose_InReverseCreationOrder()
    {
        var events = new List<string>();
        var builder = new WorldContainerBuilder();
        builder.RegisterInstance(events);
        builder.RegisterType<SingletonDependency, SingletonDependency>(WorldLifetime.Singleton);
        builder.RegisterType<SingletonTarget, SingletonTarget>(WorldLifetime.Singleton);

        var container = builder.Build();

        container.Resolve<SingletonDependency>();
        container.Resolve<SingletonTarget>();

        container.Dispose();

        Assert.Equal(
            new[]
            {
                "target:deinit:resolver=True",
                "target:dispose",
                "dependency:deinit",
                "dependency:dispose"
            },
            events);
    }

    private sealed class ScopedDependency : IWorldDeinitializable
    {
        private readonly List<string> _events;

        public ScopedDependency(List<string> events)
        {
            _events = events;
        }

        public void OnDeinit(IWorldResolver services)
        {
            _events.Add("dependency:deinit");
        }

        public void Dispose()
        {
            _events.Add("dependency:dispose");
        }
    }

    private sealed class ScopedTarget : IWorldDeinitializable
    {
        private readonly List<string> _events;

        public ScopedTarget(List<string> events)
        {
            _events = events;
        }

        public void OnDeinit(IWorldResolver services)
        {
            _events.Add($"target:deinit:resolver={services.TryResolve<ScopedDependency>(out _)}");
        }

        public void Dispose()
        {
            _events.Add("target:dispose");
        }
    }

    private sealed class SingletonDependency : IWorldDeinitializable
    {
        private readonly List<string> _events;

        public SingletonDependency(List<string> events)
        {
            _events = events;
        }

        public void OnDeinit(IWorldResolver services)
        {
            _events.Add("dependency:deinit");
        }

        public void Dispose()
        {
            _events.Add("dependency:dispose");
        }
    }

    private sealed class SingletonTarget : IWorldDeinitializable
    {
        private readonly List<string> _events;

        public SingletonTarget(List<string> events)
        {
            _events = events;
        }

        public void OnDeinit(IWorldResolver services)
        {
            _events.Add($"target:deinit:resolver={services.TryResolve<SingletonDependency>(out _)}");
        }

        public void Dispose()
        {
            _events.Add("target:dispose");
        }
    }
}
