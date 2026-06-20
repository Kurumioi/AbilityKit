namespace AbilityKit.Orleans.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static partial class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitSiloRuntimeProfileOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitSiloRuntimeProfileOptions>()
            .Bind(configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentAffinity))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Role), "Deployment:Affinity:Role is required")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitSiloRuntimeProfileOptions GetAbilityKitSiloRuntimeProfileOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentAffinity).Get<AbilityKitSiloRuntimeProfileOptions>() ?? new AbilityKitSiloRuntimeProfileOptions();
    }
}
