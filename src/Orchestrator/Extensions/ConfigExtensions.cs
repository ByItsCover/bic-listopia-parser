using Microsoft.Extensions.Configuration;

namespace Orchestrator.Extensions;

public static class ConfigExtensions
{
    public static IConfigurationManager AddAppSettings(this IConfigurationManager config, string? environment)
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(true, true)
            .AddEnvironmentVariables();

        if (environment != null)
        {
            config.AddJsonFile($"appsettings.{environment}.json", true, true);
        }

        return config;
    }
}