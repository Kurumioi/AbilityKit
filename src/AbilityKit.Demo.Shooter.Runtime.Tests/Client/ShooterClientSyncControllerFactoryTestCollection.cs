using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ShooterClientSyncControllerFactoryTestCollection
{
    public const string Name = "ShooterClientSyncControllerFactory";
}
