namespace AbilityKit.Orleans.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static partial class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitSiloPlacementOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitSiloPlacementOptions>()
            .Bind(configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentAffinity))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Role), "Deployment:Affinity:Role is required")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitSiloPlacementOptions GetAbilityKitSiloPlacementOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentAffinity).Get<AbilityKitSiloPlacementOptions>() ?? new AbilityKitSiloPlacementOptions();
    }
}
