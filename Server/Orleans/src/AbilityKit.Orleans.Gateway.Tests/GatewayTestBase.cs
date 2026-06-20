using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public abstract class GatewayTestBase
{
    protected static void AssertContainsText(string expected, string actual)
    {
        Assert.Contains(expected, actual);
    }
}
