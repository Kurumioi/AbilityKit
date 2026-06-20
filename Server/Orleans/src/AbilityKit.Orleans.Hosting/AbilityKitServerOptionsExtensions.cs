using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitServerOptionsExtensions
{
    public static IServiceCollection AddAbilityKitServerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AbilityKitOrleansClusterOptions>()
            .Bind(GetRequiredSection(configuration, AbilityKitServerConfigurationSections.Orleans, AbilityKitServerConfigurationSections.LegacyOrleans))
            .Validate(ValidateOrleansOptions, "AbilityKit:Orleans configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<AbilityKitLoggingOptions>()
            .Bind(GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Logging))
            .Validate(ValidateLoggingOptions, "AbilityKit:Logging configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<AbilityKitStorageOptions>()
            .Bind(GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Storage))
            .Validate(ValidateStorageOptions, "AbilityKit:Storage configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<AbilityKitGatewayOptions>()
            .Bind(GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Gateway))
            .Validate(ValidateGatewayOptions, "AbilityKit:Gateway configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<AbilityKitServerRuntimeOptions>()
            .Bind(GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Runtime))
            .Validate(ValidateRuntimeOptions, "AbilityKit:Runtime configuration is invalid.")
            .ValidateOnStart();

        return services;
    }

    public static AbilityKitOrleansClusterOptions GetAbilityKitOrleansOptions(this IConfiguration configuration)
    {
        var options = new AbilityKitOrleansClusterOptions();
        GetRequiredSection(configuration, AbilityKitServerConfigurationSections.Orleans, AbilityKitServerConfigurationSections.LegacyOrleans).Bind(options);
        ValidateOrleansOptions(options, throwOnInvalid: true);
        return options;
    }

    public static AbilityKitLoggingOptions GetAbilityKitLoggingOptions(this IConfiguration configuration)
    {
        var options = new AbilityKitLoggingOptions();
        GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Logging).Bind(options);
        ValidateLoggingOptions(options, throwOnInvalid: true);
        return options;
    }

    public static AbilityKitStorageOptions GetAbilityKitStorageOptions(this IConfiguration configuration)
    {
        var options = new AbilityKitStorageOptions();
        GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Storage).Bind(options);
        ValidateStorageOptions(options, throwOnInvalid: true);
        return options;
    }

    public static AbilityKitGatewayOptions GetAbilityKitGatewayOptions(this IConfiguration configuration)
    {
        var options = new AbilityKitGatewayOptions();
        GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Gateway).Bind(options);
        ApplyLegacyTcpGatewayHttpFallback(configuration, options);
        ValidateGatewayOptions(options, throwOnInvalid: true);
        return options;
    }

    public static AbilityKitServerRuntimeOptions GetAbilityKitRuntimeOptions(this IConfiguration configuration)
    {
        var options = new AbilityKitServerRuntimeOptions();
        GetOptionalSection(configuration, AbilityKitServerConfigurationSections.Runtime).Bind(options);
        ValidateRuntimeOptions(options, throwOnInvalid: true);
        return options;
    }

    private static IConfigurationSection GetRequiredSection(IConfiguration configuration, string sectionName, string? fallbackSectionName = null)
    {
        var section = configuration.GetSection(sectionName);
        if (section.Exists())
        {
            return section;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSectionName))
        {
            var fallbackSection = configuration.GetSection(fallbackSectionName);
            if (fallbackSection.Exists())
            {
                return fallbackSection;
            }
        }

        throw new OptionsValidationException(sectionName, typeof(object), [$"Missing required configuration section '{sectionName}'."]);
    }

    private static IConfigurationSection GetOptionalSection(IConfiguration configuration, string sectionName)
    {
        return configuration.GetSection(sectionName);
    }

    private static bool ValidateOrleansOptions(AbilityKitOrleansClusterOptions options)
    {
        return ValidateOrleansOptions(options, throwOnInvalid: false);
    }

    private static bool ValidateOrleansOptions(AbilityKitOrleansClusterOptions options, bool throwOnInvalid)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ClusterId))
        {
            failures.Add("ClusterId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ServiceId))
        {
            failures.Add("ServiceId is required.");
        }

        ValidatePort(options.SiloPort, nameof(options.SiloPort), failures);
        ValidatePort(options.GatewayPort, nameof(options.GatewayPort), failures);

        return ValidateOrThrow(nameof(AbilityKitOrleansClusterOptions), typeof(AbilityKitOrleansClusterOptions), failures, throwOnInvalid);
    }

    private static bool ValidateLoggingOptions(AbilityKitLoggingOptions options)
    {
        return ValidateLoggingOptions(options, throwOnInvalid: false);
    }

    private static bool ValidateLoggingOptions(AbilityKitLoggingOptions options, bool throwOnInvalid)
    {
        var failures = new List<string>();
        ValidateLogLevel(options.MinimumLevel, nameof(options.MinimumLevel), failures);
        ValidateLogLevel(options.MicrosoftLevel, nameof(options.MicrosoftLevel), failures);
        ValidateLogLevel(options.HostingLifetimeLevel, nameof(options.HostingLifetimeLevel), failures);
        ValidateLogLevel(options.OrleansLevel, nameof(options.OrleansLevel), failures);
        ValidateLogLevel(options.ApplicationLevel, nameof(options.ApplicationLevel), failures);

        if (string.IsNullOrWhiteSpace(options.TimestampFormat))
        {
            failures.Add("TimestampFormat is required.");
        }

        return ValidateOrThrow(nameof(AbilityKitLoggingOptions), typeof(AbilityKitLoggingOptions), failures, throwOnInvalid);
    }

    private static bool ValidateStorageOptions(AbilityKitStorageOptions options)
    {
        return ValidateStorageOptions(options, throwOnInvalid: false);
    }

    private static bool ValidateStorageOptions(AbilityKitStorageOptions options, bool throwOnInvalid)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("Provider is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SessionStateProvider))
        {
            failures.Add("SessionStateProvider is required.");
        }

        if (string.IsNullOrWhiteSpace(options.RoomStateProvider))
        {
            failures.Add("RoomStateProvider is required.");
        }

        if (options.Required && string.IsNullOrWhiteSpace(options.ConnectionStringName) && string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add("ConnectionStringName or ConnectionString is required when storage is required.");
        }

        return ValidateOrThrow(nameof(AbilityKitStorageOptions), typeof(AbilityKitStorageOptions), failures, throwOnInvalid);
    }

    private static bool ValidateGatewayOptions(AbilityKitGatewayOptions options)
    {
        return ValidateGatewayOptions(options, throwOnInvalid: false);
    }

    private static bool ValidateGatewayOptions(AbilityKitGatewayOptions options, bool throwOnInvalid)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Http.Scheme))
        {
            failures.Add("Http.Scheme is required.");
        }
        else if (!string.Equals(options.Http.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(options.Http.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Http.Scheme must be http or https.");
        }

        if (string.IsNullOrWhiteSpace(options.Http.Host))
        {
            failures.Add("Http.Host is required.");
        }

        ValidatePort(options.Http.Port, "Http.Port", failures);

        return ValidateOrThrow(nameof(AbilityKitGatewayOptions), typeof(AbilityKitGatewayOptions), failures, throwOnInvalid);
    }

    private static bool ValidateRuntimeOptions(AbilityKitServerRuntimeOptions options)
    {
        return ValidateRuntimeOptions(options, throwOnInvalid: false);
    }

    private static bool ValidateRuntimeOptions(AbilityKitServerRuntimeOptions options, bool throwOnInvalid)
    {
        var failures = new List<string>();
        if (options.PreserveWorkingDirectory && string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            failures.Add("WorkingDirectory is required when PreserveWorkingDirectory is enabled.");
        }

        if (options.RestartGracePeriodSeconds < 0)
        {
            failures.Add("RestartGracePeriodSeconds must be greater than or equal to zero.");
        }

        return ValidateOrThrow(nameof(AbilityKitServerRuntimeOptions), typeof(AbilityKitServerRuntimeOptions), failures, throwOnInvalid);
    }

    private static void ApplyLegacyTcpGatewayHttpFallback(IConfiguration configuration, AbilityKitGatewayOptions options)
    {
        var gatewaySection = configuration.GetSection(AbilityKitServerConfigurationSections.Gateway);
        if (gatewaySection.Exists())
        {
            return;
        }

        var legacySection = configuration.GetSection(AbilityKitServerConfigurationSections.LegacyTcpGateway);
        if (!legacySection.Exists())
        {
            return;
        }
    }

    private static void ValidatePort(int? port, string name, ICollection<string> failures)
    {
        if (port is <= 0 or > 65535)
        {
            failures.Add($"{name} must be between 1 and 65535.");
        }
    }

    private static void ValidateLogLevel(string value, string name, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(value, ignoreCase: true, out _))
        {
            failures.Add($"{name} must be a valid Microsoft.Extensions.Logging.LogLevel value.");
        }
    }

    private static bool ValidateOrThrow(string optionsName, Type optionsType, IReadOnlyCollection<string> failures, bool throwOnInvalid)
    {
        if (failures.Count == 0)
        {
            return true;
        }

        if (throwOnInvalid)
        {
            throw new OptionsValidationException(optionsName, optionsType, failures);
        }

        return false;
    }
}
