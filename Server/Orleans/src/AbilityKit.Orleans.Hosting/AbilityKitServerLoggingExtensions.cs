using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitServerLoggingExtensions
{
    public static ILoggingBuilder AddAbilityKitServerLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string applicationName)
    {
        var options = configuration.GetAbilityKitLoggingOptions();

        logging.ClearProviders();
        logging.AddConfiguration(configuration.GetSection(AbilityKitServerConfigurationSections.Logging));
        logging.AddSimpleConsole(consoleOptions =>
        {
            consoleOptions.SingleLine = options.SingleLine;
            consoleOptions.TimestampFormat = options.TimestampFormat;
            consoleOptions.IncludeScopes = options.IncludeScopes;
        });
        logging.AddDebug();
        logging.SetMinimumLevel(ParseLogLevel(options.MinimumLevel));
        logging.AddFilter("Microsoft", ParseLogLevel(options.MicrosoftLevel));
        logging.AddFilter("Microsoft.Hosting.Lifetime", ParseLogLevel(options.HostingLifetimeLevel));
        logging.AddFilter("Orleans", ParseLogLevel(options.OrleansLevel));
        logging.AddFilter(applicationName, ParseLogLevel(options.ApplicationLevel));
        return logging;
    }

    private static LogLevel ParseLogLevel(string value)
    {
        return Enum.Parse<LogLevel>(value, ignoreCase: true);
    }
}
