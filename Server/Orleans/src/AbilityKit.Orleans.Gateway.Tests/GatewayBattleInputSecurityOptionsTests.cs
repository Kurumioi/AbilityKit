using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Handlers;
using AbilityKit.Orleans.Gateway.HttpApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class GatewayBattleInputSecurityOptionsTests
{
    [Fact]
    public void GatewayModule_BindsOptionsAndCreatesSingletonGuard()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [$"{BattleInputSecurityOptions.ConfigurationSection}:ReplayWindowSize"] = "4",
            [$"{BattleInputSecurityOptions.ConfigurationSection}:MaxGatewayTrackedKeys"] = "8",
            [$"{BattleInputSecurityOptions.ConfigurationSection}:GatewayIdleStateTtlSeconds"] = "30"
        });

        var options = provider.GetRequiredService<IOptions<BattleInputSecurityOptions>>().Value;
        var first = provider.GetRequiredService<GatewayBattleInputGuard>();
        var second = provider.GetRequiredService<GatewayBattleInputGuard>();

        Assert.Equal(4, options.ReplayWindowSize);
        Assert.Equal(8, options.MaxGatewayTrackedKeys);
        Assert.Equal(30, options.GatewayIdleStateTtlSeconds);
        Assert.Same(first, second);
        Assert.Equal(4, first.Options.ReplayWindowSize);
    }

    [Fact]
    public void GatewayModule_InvalidOptionsThrowBeforeGuardIsCreated()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            [$"{BattleInputSecurityOptions.ConfigurationSection}:MaxGatewayTrackedKeys"] = "0"
        });

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<GatewayBattleInputGuard>());

        Assert.Contains(nameof(BattleInputSecurityOptions.MaxGatewayTrackedKeys), exception.Message);
    }

    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddAbilityKitGatewayModule(configuration);
        return services.BuildServiceProvider();
    }
}
