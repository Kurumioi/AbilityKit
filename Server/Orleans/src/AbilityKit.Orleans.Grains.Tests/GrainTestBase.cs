using Xunit;

namespace AbilityKit.Orleans.Grains.Tests;

public abstract class GrainTestBase
{
    protected static void AssertEqualSnapshot<T>(T expected, T actual)
        where T : notnull
    {
        Assert.Equal(expected, actual);
    }
}
