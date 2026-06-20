namespace AbilityKit.Orleans.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static partial class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitDeploymentOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitDeploymentOptions>()
            .Bind(configuration.GetSection(AbilityKitDeploymentConfigurationSections.Deployment))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Role), "Deployment:Role is required")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitDeploymentOptions GetAbilityKitDeploymentOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.Deployment).Get<AbilityKitDeploymentOptions>() ?? new AbilityKitDeploymentOptions();
    }
}
