using AbilityKit.Orleans.Grains.Persistence;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Persistence;

public sealed class AbilityKitStateStorageProviderPlanTests : GrainTestBase
{
    [Theory]
    [InlineData("InMemory")]
    [InlineData("None")]
    public void Create_WhenProviderIsLocal_ReturnsInMemoryPlan(string provider)
    {
        var plan = AbilityKitStateStorageProviderPlan.Create(provider, "Session");

        AssertEqualSnapshot(AbilityKitStateStorageProviderKind.InMemory, plan.Kind);
        Assert.False(plan.RequiresConnectionString);
        Assert.True(plan.IsInMemory);
    }

    [Theory]
    [InlineData("External")]
    [InlineData("Redis")]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void Create_WhenProviderIsExternal_ReturnsExternalPlan(string provider)
    {
        var plan = AbilityKitStateStorageProviderPlan.Create(provider, "Room");

        AssertEqualSnapshot(AbilityKitStateStorageProviderKind.External, plan.Kind);
        Assert.True(plan.RequiresConnectionString);
        Assert.True(plan.IsExternal);
    }

    [Fact]
    public void Create_WhenProviderUnsupported_ThrowsNotSupported()
    {
        var exception = Assert.Throws<NotSupportedException>(() => AbilityKitStateStorageProviderPlan.Create("FileSystem", "Room"));

        Assert.Contains("FileSystem", exception.Message);
    }
}
