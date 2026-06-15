using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

/// <summary>
/// Serializes the acceptance test classes. The Shooter rollback machinery shares a process-wide
/// static pool registry, so concurrent test classes that each spin up a predict-rollback controller
/// must not run in parallel. This collection has no shared fixture state; it only enforces ordering.
/// </summary>
[CollectionDefinition("ShooterAcceptance")]
public sealed class ShooterAcceptanceTestCollection
{
}
