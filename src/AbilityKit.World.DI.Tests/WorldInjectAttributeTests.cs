using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using Xunit;

namespace AbilityKit.World.DI.Tests;

public sealed class WorldInjectAttributeTests
{
    [Fact]
    public void Resolve_InjectsFieldsAndProperties()
    {
        var builder = new WorldContainerBuilder();
        builder.RegisterType<FieldDependency, FieldDependency>(WorldLifetime.Scoped);
        builder.RegisterType<PropertyDependency, PropertyDependency>(WorldLifetime.Scoped);
        builder.RegisterType<TargetWithMembers, TargetWithMembers>(WorldLifetime.Scoped);

        using var container = builder.Build();
        using var scope = container.CreateScope();

        var target = scope.Resolve<TargetWithMembers>();

        Assert.NotNull(target.FieldDependency);
        Assert.NotNull(target.PropertyDependency);
    }

    [Fact]
    public void Resolve_IgnoresMissingOptionalDependency()
    {
        var builder = new WorldContainerBuilder();
        builder.RegisterType<TargetWithOptionalMember, TargetWithOptionalMember>(WorldLifetime.Scoped);

        using var container = builder.Build();
        using var scope = container.CreateScope();

        var target = scope.Resolve<TargetWithOptionalMember>();

        Assert.Null(target.OptionalDependency);
    }

    [Fact]
    public void Resolve_ThrowsWhenRequiredDependencyIsMissing()
    {
        var builder = new WorldContainerBuilder();
        builder.RegisterType<TargetWithRequiredMember, TargetWithRequiredMember>(WorldLifetime.Scoped);

        using var container = builder.Build();
        using var scope = container.CreateScope();

        var ex = Assert.Throws<InvalidOperationException>(() => scope.Resolve<TargetWithRequiredMember>());

        Assert.Contains("Required world service injection failed", ex.Message);
    }

    [Fact]
    public void Resolve_UsesExplicitServiceType()
    {
        var builder = new WorldContainerBuilder();
        builder.RegisterType<IDependencyContract, ContractDependency>(WorldLifetime.Scoped);
        builder.RegisterType<TargetWithExplicitServiceType, TargetWithExplicitServiceType>(WorldLifetime.Scoped);

        using var container = builder.Build();
        using var scope = container.CreateScope();

        var target = scope.Resolve<TargetWithExplicitServiceType>();

        Assert.IsType<ContractDependency>(target.Dependency);
    }

    private sealed class FieldDependency
    {
    }

    private sealed class PropertyDependency
    {
    }

    private sealed class MissingDependency
    {
    }

    private interface IDependencyContract
    {
    }

    private sealed class ContractDependency : IDependencyContract
    {
    }

    private sealed class TargetWithMembers
    {
        [WorldInject] private FieldDependency _fieldDependency = null!;

        [WorldInject]
        public PropertyDependency PropertyDependency { get; private set; } = null!;

        public FieldDependency FieldDependency => _fieldDependency;
    }

    private sealed class TargetWithOptionalMember
    {
        [WorldInject(required: false)]
        public MissingDependency? OptionalDependency { get; private set; }
    }

    private sealed class TargetWithRequiredMember
    {
        [WorldInject] private MissingDependency _requiredDependency = null!;

        public MissingDependency RequiredDependency => _requiredDependency;
    }

    private sealed class TargetWithExplicitServiceType
    {
        [WorldInject(typeof(IDependencyContract))]
        public IDependencyContract Dependency { get; private set; } = null!;
    }
}
