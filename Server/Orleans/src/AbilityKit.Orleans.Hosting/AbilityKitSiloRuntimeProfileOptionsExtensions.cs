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
            .Validate(options => options.MaxRooms >= 0, "Deployment:Affinity:MaxRooms must be greater than or equal to zero")
            .Validate(options => options.MaxBattles >= 0, "Deployment:Affinity:MaxBattles must be greater than or equal to zero")
            .Validate(options => options.MaxSessions >= 0, "Deployment:Affinity:MaxSessions must be greater than or equal to zero")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitSiloRuntimeProfileOptions GetAbilityKitSiloRuntimeProfileOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(AbilityKitDeploymentConfigurationSections.DeploymentAffinity).Get<AbilityKitSiloRuntimeProfileOptions>() ?? new AbilityKitSiloRuntimeProfileOptions();
    }
}
