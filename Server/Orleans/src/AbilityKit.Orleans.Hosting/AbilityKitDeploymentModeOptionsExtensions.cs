namespace AbilityKit.Orleans.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static partial class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitDeploymentModeOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitDeploymentModeOptions>()
            .Bind(configuration.GetSection(AbilityKitDeploymentConfigurationSections.Deployment + ":Mode"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Mode), "Deployment:Mode is required")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitDeploymentModeOptions GetAbilityKitDeploymentModeOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.Deployment + ":Mode").Get<AbilityKitDeploymentModeOptions>() ?? new AbilityKitDeploymentModeOptions();
    }
}
