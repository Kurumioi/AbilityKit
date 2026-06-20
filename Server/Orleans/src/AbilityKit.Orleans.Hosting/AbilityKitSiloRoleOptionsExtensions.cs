namespace AbilityKit.Orleans.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static partial class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitSiloRoleOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitSiloRoleOptions>()
            .Bind(configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentRole))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Role), "Deployment:Role is required")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitSiloRoleOptions GetAbilityKitSiloRoleOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentRole).Get<AbilityKitSiloRoleOptions>() ?? new AbilityKitSiloRoleOptions();
    }
}
