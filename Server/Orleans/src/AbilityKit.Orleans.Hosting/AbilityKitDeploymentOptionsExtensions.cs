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
            .Validate(options => options.TargetSiloCount > 0, "Deployment:TargetSiloCount must be greater than zero")
            .Validate(options => options.MaxRoomsPerSilo >= 0, "Deployment:MaxRoomsPerSilo must be greater than or equal to zero")
            .Validate(options => options.MaxBattlesPerSilo >= 0, "Deployment:MaxBattlesPerSilo must be greater than or equal to zero")
            .Validate(options => options.MaxSessionsPerGateway >= 0, "Deployment:MaxSessionsPerGateway must be greater than or equal to zero")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitDeploymentOptions GetAbilityKitDeploymentOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.Deployment).Get<AbilityKitDeploymentOptions>() ?? new AbilityKitDeploymentOptions();
    }
}
